using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.GoogleMaps;
using Advance_Control.Services.Areas;
using Advance_Control.Services.Logging;
using System.Collections.Generic;

namespace Advance_Control.ViewModels
{
    /// <summary>
    /// ViewModel para la página de Ubicaciones con Google Maps
    /// </summary>
    public class UbicacionesViewModel : ViewModelBase
    {
        private readonly IGoogleMapsConfigService _googleMapsConfigService;
        private readonly IAreasService _areasService;
        private readonly ILoggingService _logger;

        private GoogleMapsConfigDto? _mapsConfig;
        private ObservableCollection<GoogleMapsAreaDto> _areas;
        private bool _isLoading;
        private string? _errorMessage;
        private bool _isMapInitialized;

        public UbicacionesViewModel(
            IGoogleMapsConfigService googleMapsConfigService,
            IAreasService areasService,
            ILoggingService logger)
        {
            _googleMapsConfigService = googleMapsConfigService ?? throw new ArgumentNullException(nameof(googleMapsConfigService));
            _areasService = areasService ?? throw new ArgumentNullException(nameof(areasService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _areas = new ObservableCollection<GoogleMapsAreaDto>();
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
        /// Colección de áreas para mostrar en el mapa
        /// </summary>
        public ObservableCollection<GoogleMapsAreaDto> Areas
        {
            get => _areas;
            set => SetProperty(ref _areas, value);
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

                await _logger.LogInformationAsync("Inicializando mapa de ubicaciones", "UbicacionesViewModel", "InitializeAsync");

                // Cargar configuración de Google Maps
                await LoadConfigurationAsync(cancellationToken);

                // Cargar áreas activas
                await LoadAreasAsync(cancellationToken);

                IsMapInitialized = true;

                await _logger.LogInformationAsync("Mapa de ubicaciones inicializado correctamente", "UbicacionesViewModel", "InitializeAsync");
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error al inicializar el mapa", ex, "UbicacionesViewModel", "InitializeAsync");
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
                await _logger.LogInformationAsync("Cargando configuración de Google Maps", "UbicacionesViewModel", "LoadConfigurationAsync");

                var config = await _googleMapsConfigService.GetConfigAsync(cancellationToken);

                if (config != null)
                {
                    MapsConfig = config;
                    await _logger.LogInformationAsync($"Configuración cargada: Centro={config.DefaultCenter}, Zoom={config.DefaultZoom}", "UbicacionesViewModel", "LoadConfigurationAsync");
                }
                else
                {
                    await _logger.LogWarningAsync("No se pudo obtener la configuración de Google Maps", "UbicacionesViewModel", "LoadConfigurationAsync");
                    ErrorMessage = "No se pudo cargar la configuración del mapa.";
                }
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error al cargar la configuración de Google Maps", ex, "UbicacionesViewModel", "LoadConfigurationAsync");
                throw;
            }
        }

        /// <summary>
        /// Carga las áreas activas desde el API
        /// </summary>
        public async Task LoadAreasAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await _logger.LogInformationAsync("Cargando áreas para el mapa", "UbicacionesViewModel", "LoadAreasAsync");

                var areas = await _areasService.GetAreasForGoogleMapsAsync(activo: true, cancellationToken: cancellationToken);

                Areas.Clear();
                foreach (var area in areas)
                {
                    Areas.Add(area);
                }

                await _logger.LogInformationAsync($"Se cargaron {areas.Count} áreas", "UbicacionesViewModel", "LoadAreasAsync");
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error al cargar áreas", ex, "UbicacionesViewModel", "LoadAreasAsync");
                throw;
            }
        }

        /// <summary>
        /// Valida si un punto está dentro de alguna área
        /// </summary>
        public async Task<List<AreaValidationResultDto>> ValidatePointAsync(
            decimal latitude,
            decimal longitude,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await _logger.LogInformationAsync($"Validando punto: ({latitude}, {longitude})", "UbicacionesViewModel", "ValidatePointAsync");

                var results = await _areasService.ValidatePointAsync(latitude, longitude, cancellationToken: cancellationToken);

                return results;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error al validar punto", ex, "UbicacionesViewModel", "ValidatePointAsync");
                throw;
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
                await _logger.LogErrorAsync("Error al recargar áreas", ex, "UbicacionesViewModel", "RefreshAreasAsync");
                ErrorMessage = "Error al recargar las áreas. Por favor, intente nuevamente.";
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
