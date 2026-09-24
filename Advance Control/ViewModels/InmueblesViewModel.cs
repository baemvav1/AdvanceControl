using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.Inmuebles;
using Advance_Control.Services.Logging;
using Advance_Control.Services.Ubicaciones;
using Advance_Control.ViewModels.Common;

namespace Advance_Control.ViewModels
{
    public class InmueblesViewModel : ViewModelBase
    {
        public PaginadorViewModel<InmuebleDto> Paginacion { get; } = new();

        private List<InmuebleDto> _catalogoSugerencias = new();
        private bool HayFiltrosActivos =>
            !string.IsNullOrWhiteSpace(DescripcionFilter) || !string.IsNullOrWhiteSpace(IdentificadorFilter) ||
            SelectedUbicacionFilter != null;

        public IEnumerable<string?> ValoresIdentificador => _catalogoSugerencias.Select(i => i.Identificador);
        public IEnumerable<string?> ValoresDescripcion => _catalogoSugerencias.Select(i => i.Descripcion);

        private readonly IInmuebleService _inmuebleService;
        private readonly IUbicacionService _ubicacionService;
        private readonly ILoggingService _logger;
        private ObservableCollection<InmuebleDto> _inmuebles;
        private ObservableCollection<UbicacionDto> _ubicaciones;
        private UbicacionDto? _selectedUbicacionFilter;
        private bool _isLoading;
        private string? _errorMessage;
        private string? _descripcionFilter;
        private string? _identificadorFilter;

        public InmueblesViewModel(IInmuebleService inmuebleService, IUbicacionService ubicacionService, ILoggingService logger)
        {
            _inmuebleService  = inmuebleService  ?? throw new ArgumentNullException(nameof(inmuebleService));
            _ubicacionService = ubicacionService ?? throw new ArgumentNullException(nameof(ubicacionService));
            _logger           = logger           ?? throw new ArgumentNullException(nameof(logger));
            _inmuebles   = new ObservableCollection<InmuebleDto>();
            _ubicaciones = new ObservableCollection<UbicacionDto>();
        }

        public ObservableCollection<InmuebleDto> Inmuebles
        {
            get => _inmuebles;
            set => SetProperty(ref _inmuebles, value);
        }

        public ObservableCollection<UbicacionDto> Ubicaciones
        {
            get => _ubicaciones;
            set => SetProperty(ref _ubicaciones, value);
        }

        public UbicacionDto? SelectedUbicacionFilter
        {
            get => _selectedUbicacionFilter;
            set => SetProperty(ref _selectedUbicacionFilter, value);
        }

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

        public string? DescripcionFilter
        {
            get => _descripcionFilter;
            set => SetProperty(ref _descripcionFilter, value);
        }

        public string? IdentificadorFilter
        {
            get => _identificadorFilter;
            set => SetProperty(ref _identificadorFilter, value);
        }

        /// <summary>
        /// Carga ubicaciones para el filtro ComboBox
        /// </summary>
        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var ubicaciones = await _ubicacionService.GetUbicacionesAsync(cancellationToken);
                Ubicaciones.Clear();
                foreach (var u in ubicaciones)
                    Ubicaciones.Add(u);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error al inicializar filtros de inmuebles", ex, "InmueblesViewModel", "InitializeAsync");
            }
        }

        /// <summary>
        /// Carga los inmuebles desde el servicio con los filtros aplicados
        /// </summary>
        public async Task LoadInmueblesAsync(CancellationToken cancellationToken = default)
        {
            if (IsLoading)
                return;

            try
            {
                IsLoading = true;
                ErrorMessage = null;
                await _logger.LogInformationAsync("Cargando inmuebles...", "InmueblesViewModel", "LoadInmueblesAsync");

                var query = new InmuebleQueryDto
                {
                    Descripcion = DescripcionFilter,
                    Identificador = IdentificadorFilter,
                    IdUbicacion = SelectedUbicacionFilter?.IdUbicacion
                };

                var inmuebles = await _inmuebleService.GetInmueblesAsync(query, cancellationToken);

                Inmuebles.Clear();
                foreach (var inmueble in inmuebles)
                {
                    Inmuebles.Add(inmueble);
                }
                Paginacion.EstablecerElementos(inmuebles);

                if (_catalogoSugerencias.Count == 0 && !HayFiltrosActivos)
                    _catalogoSugerencias = inmuebles.ToList();

                await _logger.LogInformationAsync($"Se cargaron {inmuebles.Count} inmuebles exitosamente", "InmueblesViewModel", "LoadInmueblesAsync");
            }
            catch (OperationCanceledException)
            {
                ErrorMessage = "La operación fue cancelada.";
                await _logger.LogInformationAsync("Operación de carga de inmuebles cancelada por el usuario", "InmueblesViewModel", "LoadInmueblesAsync");
            }
            catch (HttpRequestException ex)
            {
                ErrorMessage = "Error de conexión: No se pudo conectar con el servidor. Verifique su conexión a internet.";
                await _logger.LogErrorAsync("Error de conexión al cargar inmuebles", ex, "InmueblesViewModel", "LoadInmueblesAsync");
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error inesperado al cargar inmuebles: {ex.Message}";
                await _logger.LogErrorAsync("Error inesperado al cargar inmuebles", ex, "InmueblesViewModel", "LoadInmueblesAsync");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Limpia los filtros y recarga todos los inmuebles
        /// </summary>
        public async Task ClearFiltersAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                DescripcionFilter = null;
                IdentificadorFilter = null;
                SelectedUbicacionFilter = null;
                ErrorMessage = null;
                await LoadInmueblesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error al limpiar filtros y recargar inmuebles.";
                await _logger.LogErrorAsync("Error al limpiar filtros", ex, "InmueblesViewModel", "ClearFiltersAsync");
            }
        }

        /// <summary>
        /// Elimina un inmueble por su ID
        /// </summary>
        public async Task<bool> DeleteInmuebleAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                await _logger.LogInformationAsync($"Eliminando inmueble {id}...", "InmueblesViewModel", "DeleteInmuebleAsync");

                var result = await _inmuebleService.DeleteInmuebleAsync(id, cancellationToken);

                if (result)
                {
                    await _logger.LogInformationAsync($"Inmueble {id} eliminado exitosamente", "InmueblesViewModel", "DeleteInmuebleAsync");
                    await LoadInmueblesAsync(cancellationToken);
                }
                else
                {
                    ErrorMessage = "No se pudo eliminar el inmueble.";
                    await _logger.LogWarningAsync($"No se pudo eliminar el inmueble {id}", "InmueblesViewModel", "DeleteInmuebleAsync");
                }

                return result;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al eliminar inmueble: {ex.Message}";
                await _logger.LogErrorAsync($"Error al eliminar inmueble {id}", ex, "InmueblesViewModel", "DeleteInmuebleAsync");
                return false;
            }
        }

        /// <summary>
        /// Actualiza un inmueble existente
        /// </summary>
        public async Task<bool> UpdateInmuebleAsync(int id, InmuebleQueryDto updateData, CancellationToken cancellationToken = default)
        {
            try
            {
                await _logger.LogInformationAsync($"Actualizando inmueble {id}...", "InmueblesViewModel", "UpdateInmuebleAsync");

                var result = await _inmuebleService.UpdateInmuebleAsync(id, updateData, cancellationToken);

                if (result)
                {
                    await _logger.LogInformationAsync($"Inmueble {id} actualizado exitosamente", "InmueblesViewModel", "UpdateInmuebleAsync");
                    await LoadInmueblesAsync(cancellationToken);
                }
                else
                {
                    ErrorMessage = "No se pudo actualizar el inmueble.";
                    await _logger.LogWarningAsync($"No se pudo actualizar el inmueble {id}", "InmueblesViewModel", "UpdateInmuebleAsync");
                }

                return result;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al actualizar inmueble: {ex.Message}";
                await _logger.LogErrorAsync($"Error al actualizar inmueble {id}", ex, "InmueblesViewModel", "UpdateInmuebleAsync");
                return false;
            }
        }

        /// <summary>
        /// Crea un nuevo inmueble
        /// </summary>
        public async Task<bool> CreateInmuebleAsync(string? descripcion = null, string identificador = "", int? idTipoInmueble = null, double? superficieM2 = null, string? codigoPostal = null, bool estatus = true, int? idUbicacion = null, CancellationToken cancellationToken = default)
        {
            try
            {
                await _logger.LogInformationAsync("Creando nuevo inmueble...", "InmueblesViewModel", "CreateInmuebleAsync");

                var result = await _inmuebleService.CreateInmuebleAsync(descripcion, identificador, idTipoInmueble, superficieM2, codigoPostal, estatus, idUbicacion, cancellationToken);

                if (result)
                {
                    await _logger.LogInformationAsync("Inmueble creado exitosamente", "InmueblesViewModel", "CreateInmuebleAsync");
                    await LoadInmueblesAsync(cancellationToken);
                }
                else
                {
                    ErrorMessage = "No se pudo crear el inmueble.";
                    await _logger.LogWarningAsync("No se pudo crear el inmueble", "InmueblesViewModel", "CreateInmuebleAsync");
                }

                return result;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al crear inmueble: {ex.Message}";
                await _logger.LogErrorAsync("Error al crear inmueble", ex, "InmueblesViewModel", "CreateInmuebleAsync");
                return false;
            }
        }
    }
}
