using System;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.Entidades;
using Advance_Control.Services.Logging;

namespace Advance_Control.ViewModels
{
    public class EntidadesViewModel : ViewModelBase
    {
        private readonly IEntidadService _entidadService;
        private readonly ILoggingService _logger;
        private ObservableCollection<EntidadDto> _entidades;
        private bool _isLoading;
        private string? _errorMessage;
        private string? _nombreComercialFilter;
        private string? _razonSocialFilter;
        private string? _rfcFilter;
        private string? _estadoFilter;
        private string? _ciudadFilter;

        public EntidadesViewModel(IEntidadService entidadService, ILoggingService logger)
        {
            _entidadService = entidadService ?? throw new ArgumentNullException(nameof(entidadService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _entidades = new ObservableCollection<EntidadDto>();
        }

        public ObservableCollection<EntidadDto> Entidades
        {
            get => _entidades;
            set => SetProperty(ref _entidades, value);
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

        public string? NombreComercialFilter
        {
            get => _nombreComercialFilter;
            set => SetProperty(ref _nombreComercialFilter, value);
        }

        public string? RazonSocialFilter
        {
            get => _razonSocialFilter;
            set => SetProperty(ref _razonSocialFilter, value);
        }

        public string? RfcFilter
        {
            get => _rfcFilter;
            set => SetProperty(ref _rfcFilter, value);
        }

        public string? EstadoFilter
        {
            get => _estadoFilter;
            set => SetProperty(ref _estadoFilter, value);
        }

        public string? CiudadFilter
        {
            get => _ciudadFilter;
            set => SetProperty(ref _ciudadFilter, value);
        }

        /// <summary>
        /// Carga las entidades desde el servicio con los filtros aplicados
        /// </summary>
        public async Task LoadEntidadesAsync(CancellationToken cancellationToken = default)
        {
            if (IsLoading)
                return;

            try
            {
                IsLoading = true;
                ErrorMessage = null;
                await _logger.LogInformationAsync("Cargando entidades...", "EntidadesViewModel", "LoadEntidadesAsync");

                var query = new EntidadQueryDto
                {
                    NombreComercial = NombreComercialFilter,
                    RazonSocial = RazonSocialFilter,
                    RFC = RfcFilter,
                    Estado = EstadoFilter,
                    Ciudad = CiudadFilter
                };

                var entidades = await _entidadService.GetEntidadesAsync(query, cancellationToken);

                if (entidades == null)
                {
                    ErrorMessage = "Error: El servicio no devolvió datos válidos.";
                    await _logger.LogWarningAsync("GetEntidadesAsync devolvió null", "EntidadesViewModel", "LoadEntidadesAsync");
                    return;
                }

                Entidades.Clear();
                foreach (var entidad in entidades)
                {
                    Entidades.Add(entidad);
                }

                await _logger.LogInformationAsync($"Se cargaron {entidades.Count} entidades exitosamente", "EntidadesViewModel", "LoadEntidadesAsync");
            }
            catch (OperationCanceledException)
            {
                ErrorMessage = "La operación fue cancelada.";
                await _logger.LogInformationAsync("Operación de carga de entidades cancelada por el usuario", "EntidadesViewModel", "LoadEntidadesAsync");
            }
            catch (HttpRequestException ex)
            {
                ErrorMessage = "Error de conexión: No se pudo conectar con el servidor. Verifique su conexión a internet.";
                await _logger.LogErrorAsync("Error de conexión al cargar entidades", ex, "EntidadesViewModel", "LoadEntidadesAsync");
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error inesperado al cargar entidades: {ex.Message}";
                await _logger.LogErrorAsync("Error inesperado al cargar entidades", ex, "EntidadesViewModel", "LoadEntidadesAsync");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Limpia los filtros y recarga todas las entidades
        /// </summary>
        public async Task ClearFiltersAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                NombreComercialFilter = null;
                RazonSocialFilter = null;
                RfcFilter = null;
                EstadoFilter = null;
                CiudadFilter = null;
                ErrorMessage = null;
                await LoadEntidadesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error al limpiar filtros y recargar entidades.";
                await _logger.LogErrorAsync("Error al limpiar filtros", ex, "EntidadesViewModel", "ClearFiltersAsync");
            }
        }

        /// <summary>
        /// Crea una nueva entidad
        /// </summary>
        public async Task<bool> CreateEntidadAsync(
            string nombreComercial,
            string razonSocial,
            string? rfc = null,
            string? cp = null,
            string? estado = null,
            string? ciudad = null,
            string? pais = null,
            string? calle = null,
            string? numExt = null,
            string? numInt = null,
            string? colonia = null,
            string? apoderado = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await _logger.LogInformationAsync($"Creando entidad: {nombreComercial}", "EntidadesViewModel", "CreateEntidadAsync");

                var response = await _entidadService.CreateEntidadAsync(
                    nombreComercial,
                    razonSocial,
                    rfc,
                    cp,
                    estado,
                    ciudad,
                    pais,
                    calle,
                    numExt,
                    numInt,
                    colonia,
                    apoderado,
                    cancellationToken);

                if (response.Success)
                {
                    await _logger.LogInformationAsync($"Entidad creada exitosamente: {nombreComercial}", "EntidadesViewModel", "CreateEntidadAsync");
                    
                    // Recargar la lista de entidades
                    await LoadEntidadesAsync(cancellationToken);
                    return true;
                }
                else
                {
                    await _logger.LogWarningAsync($"No se pudo crear la entidad: {response.Message}", "EntidadesViewModel", "CreateEntidadAsync");
                    return false;
                }
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error al crear entidad", ex, "EntidadesViewModel", "CreateEntidadAsync");
                return false;
            }
        }
    }
}
