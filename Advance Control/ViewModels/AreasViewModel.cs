using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.GoogleMaps;
using Advance_Control.Services.Areas;
using Advance_Control.Services.Logging;
using System.Collections.Generic;
using System.Linq;

namespace Advance_Control.ViewModels
{
    /// <summary>
    /// ViewModel para la gestión de áreas geográficas con Google Maps
    /// </summary>
    public class AreasViewModel : ViewModelBase
    {
        private readonly IGoogleMapsConfigService _googleMapsConfigService;
        private readonly IAreasService _areasService;
        private readonly ILoggingService _logger;

        private GoogleMapsConfigDto? _mapsConfig;
        private ObservableCollection<AreaDto> _areas;
        private AreaDto? _selectedArea;
        private bool _isLoading;
        private string? _errorMessage;
        private bool _isMapInitialized;

        public AreasViewModel(
            IGoogleMapsConfigService googleMapsConfigService,
            IAreasService areasService,
            ILoggingService logger)
        {
            _googleMapsConfigService = googleMapsConfigService ?? throw new ArgumentNullException(nameof(googleMapsConfigService));
            _areasService = areasService ?? throw new ArgumentNullException(nameof(areasService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _areas = new ObservableCollection<AreaDto>();
        }

        /// <summary>
        /// Configuración de Google Maps
        /// </summary>
        public GoogleMapsConfigDto? MapsConfig
        {
            get => _mapsConfig;
            set => SetProperty(ref _mapsConfig, value);
        }

        /// <summary>
        /// Colección de áreas
        /// </summary>
        public ObservableCollection<AreaDto> Areas
        {
            get => _areas;
            set => SetProperty(ref _areas, value);
        }

        /// <summary>
        /// Área seleccionada actualmente
        /// </summary>
        public AreaDto? SelectedArea
        {
            get => _selectedArea;
            set => SetProperty(ref _selectedArea, value);
        }

        /// <summary>
        /// Indica si se está cargando información
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        /// <summary>
        /// Mensaje de error para mostrar al usuario
        /// </summary>
        public string? ErrorMessage
        {
            get => _errorMessage;
            set
            {
                if (SetProperty(ref _errorMessage, value))
                {
                    OnPropertyChanged(nameof(HasError));
                }
            }
        }

        /// <summary>
        /// Indica si hay un mensaje de error activo
        /// </summary>
        public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

        /// <summary>
        /// Indica si el mapa ha sido inicializado
        /// </summary>
        public bool IsMapInitialized
        {
            get => _isMapInitialized;
            set => SetProperty(ref _isMapInitialized, value);
        }

        /// <summary>
        /// Inicializa el mapa y carga la configuración y áreas
        /// </summary>
        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            if (IsLoading)
                return;

            try
            {
                IsLoading = true;
                ErrorMessage = null;

                await _logger.LogInformationAsync("Inicializando mapa de áreas", "AreasViewModel", "InitializeAsync");

                // Cargar configuración de Google Maps
                await LoadConfigurationAsync(cancellationToken);

                // Cargar áreas
                await LoadAreasAsync(cancellationToken);

                IsMapInitialized = true;

                await _logger.LogInformationAsync("Mapa de áreas inicializado correctamente", "AreasViewModel", "InitializeAsync");
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error al inicializar el mapa de áreas", ex, "AreasViewModel", "InitializeAsync");
                ErrorMessage = "Error al inicializar el mapa. Por favor, intente nuevamente.";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Carga la configuración de Google Maps desde el API
        /// </summary>
        public async Task LoadConfigurationAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await _logger.LogInformationAsync("Cargando configuración de Google Maps", "AreasViewModel", "LoadConfigurationAsync");

                var config = await _googleMapsConfigService.GetConfigAsync(cancellationToken);

                if (config != null)
                {
                    MapsConfig = config;
                    await _logger.LogInformationAsync($"Configuración cargada: Centro={config.DefaultCenter}, Zoom={config.DefaultZoom}", "AreasViewModel", "LoadConfigurationAsync");
                }
                else
                {
                    await _logger.LogWarningAsync("No se pudo obtener la configuración de Google Maps", "AreasViewModel", "LoadConfigurationAsync");
                    ErrorMessage = "No se pudo cargar la configuración del mapa.";
                }
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error al cargar la configuración de Google Maps", ex, "AreasViewModel", "LoadConfigurationAsync");
                throw;
            }
        }

        /// <summary>
        /// Carga las áreas desde el API
        /// </summary>
        public async Task LoadAreasAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await _logger.LogInformationAsync("Cargando áreas", "AreasViewModel", "LoadAreasAsync");

                var areas = await _areasService.GetAreasAsync(cancellationToken: cancellationToken);

                Areas.Clear();
                foreach (var area in areas)
                {
                    Areas.Add(area);
                }

                await _logger.LogInformationAsync($"Se cargaron {areas.Count} áreas", "AreasViewModel", "LoadAreasAsync");
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error al cargar áreas", ex, "AreasViewModel", "LoadAreasAsync");
                throw;
            }
        }

        /// <summary>
        /// Crea una nueva área
        /// </summary>
        public async Task<ApiResponse> CreateAreaAsync(AreaDto area, CancellationToken cancellationToken = default)
        {
            try
            {
                if (area == null)
                {
                    await _logger.LogWarningAsync("Se intentó crear un área nula", "AreasViewModel", "CreateAreaAsync");
                    return new ApiResponse { Success = false, Message = "El área no puede ser nula" };
                }

                await _logger.LogInformationAsync($"Creando área: {area.Nombre}", "AreasViewModel", "CreateAreaAsync");

                var result = await _areasService.CreateAreaAsync(area!, cancellationToken);

                if (result.Success)
                {
                    // Recargar áreas
                    await LoadAreasAsync(cancellationToken);
                }

                return result;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error al crear área", ex, "AreasViewModel", "CreateAreaAsync");
                return new ApiResponse { Success = false, Message = "Error al crear área" };
            }
        }

        /// <summary>
        /// Actualiza un área existente
        /// </summary>
        public async Task<ApiResponse> UpdateAreaAsync(AreaDto area, CancellationToken cancellationToken = default)
        {
            try
            {
                if (area.IdArea <= 0)
                {
                    return new ApiResponse { Success = false, Message = "ID de área inválido" };
                }

                await _logger.LogInformationAsync($"Actualizando área ID: {area.IdArea}", "AreasViewModel", "UpdateAreaAsync");

                var result = await _areasService.UpdateAreaAsync(area.IdArea, area, cancellationToken);

                if (result.Success)
                {
                    // Recargar áreas
                    await LoadAreasAsync(cancellationToken);
                }

                return result;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error al actualizar área", ex, "AreasViewModel", "UpdateAreaAsync");
                return new ApiResponse { Success = false, Message = "Error al actualizar área" };
            }
        }

        /// <summary>
        /// Elimina un área
        /// </summary>
        public async Task<ApiResponse> DeleteAreaAsync(int idArea, CancellationToken cancellationToken = default)
        {
            try
            {
                await _logger.LogInformationAsync($"Eliminando área ID: {idArea}", "AreasViewModel", "DeleteAreaAsync");

                var result = await _areasService.DeleteAreaAsync(idArea, cancellationToken);

                if (result.Success)
                {
                    // Recargar áreas
                    await LoadAreasAsync(cancellationToken);
                }

                return result;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error al eliminar área", ex, "AreasViewModel", "DeleteAreaAsync");
                return new ApiResponse { Success = false, Message = "Error al eliminar área" };
            }
        }

        /// <summary>
        /// Recarga las áreas del mapa
        /// </summary>
        public async Task RefreshAreasAsync(CancellationToken cancellationToken = default)
        {
            if (IsLoading)
                return;

            try
            {
                IsLoading = true;
                ErrorMessage = null;

                await LoadAreasAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error al recargar áreas", ex, "AreasViewModel", "RefreshAreasAsync");
                ErrorMessage = "Error al recargar las áreas. Por favor, intente nuevamente.";
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
