using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.ConfiguracionEmisor;
using Advance_Control.Services.Entidades;
using Advance_Control.Services.Logging;
using Advance_Control.ViewModels.Common;

namespace Advance_Control.ViewModels
{
    public class EntidadesViewModel : ViewModelBase
    {
        public PaginadorViewModel<EntidadDto> Paginacion { get; } = new();

        private List<EntidadDto> _catalogoSugerencias = new();
        private bool HayFiltrosActivos =>
            !string.IsNullOrWhiteSpace(NombreComercialFilter) || !string.IsNullOrWhiteSpace(RazonSocialFilter) ||
            !string.IsNullOrWhiteSpace(RfcFilter) || !string.IsNullOrWhiteSpace(EstadoFilter) ||
            !string.IsNullOrWhiteSpace(CiudadFilter);

        public IEnumerable<string?> ValoresNombreComercial => _catalogoSugerencias.Select(e => e.NombreComercial);
        public IEnumerable<string?> ValoresRazonSocial => _catalogoSugerencias.Select(e => e.RazonSocial);
        public IEnumerable<string?> ValoresRfc => _catalogoSugerencias.Select(e => e.RFC);
        public IEnumerable<string?> ValoresEstado => _catalogoSugerencias.Select(e => e.Estado);
        public IEnumerable<string?> ValoresCiudad => _catalogoSugerencias.Select(e => e.Ciudad);

        private readonly IEntidadService _entidadService;
        private readonly IConfiguracionEmisorService _configuracionEmisorService;
        private readonly ILoggingService _logger;
        private ObservableCollection<EntidadDto> _entidades;
        private bool _isLoading;
        private string? _errorMessage;
        private string? _nombreComercialFilter;
        private string? _razonSocialFilter;
        private string? _rfcFilter;
        private string? _estadoFilter;
        private string? _ciudadFilter;

        public EntidadesViewModel(IEntidadService entidadService, IConfiguracionEmisorService configuracionEmisorService, ILoggingService logger)
        {
            _entidadService = entidadService ?? throw new ArgumentNullException(nameof(entidadService));
            _configuracionEmisorService = configuracionEmisorService ?? throw new ArgumentNullException(nameof(configuracionEmisorService));
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
                Paginacion.EstablecerElementos(entidades);

                if (!HayFiltrosActivos)
                    _catalogoSugerencias = entidades.ToList();

                await _logger.LogInformationAsync($"Se cargaron {entidades.Count} entidades exitosamente", "EntidadesViewModel", "LoadEntidadesAsync");

                await AplicarEstadoCsdFielAsync(cancellationToken);
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
        /// Marca en cada <see cref="EntidadDto"/> su CSD/PFX (cada entidad tiene el suyo) y cuántas
        /// cuentas bancarias tiene registradas.
        /// </summary>
        private async Task AplicarEstadoCsdFielAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var cuentas = await _entidadService.GetCuentasBancariasAsync(null, cancellationToken);
                foreach (var entidad in Entidades.ToList())
                {
                    var estadoCsd = await _configuracionEmisorService.ObtenerEstadoCsdAsync(entidad.IdEntidad, cancellationToken);
                    var estadoFiel = await _configuracionEmisorService.ObtenerEstadoFielAsync(entidad.IdEntidad, cancellationToken);

                    entidad.CsdCargado = estadoCsd.Cargado;
                    entidad.CsdVigente = estadoCsd.Cargado && estadoCsd.Vigente;
                    entidad.PfxCargado = estadoFiel.Cargado;
                    entidad.PfxVigente = estadoFiel.Cargado && estadoFiel.Vigente;
                    entidad.CuentasBancarias = cuentas.Count(c => c.IdEntidad == entidad.IdEntidad);
                }
            }
            catch (Exception ex)
            {
                // No es crítico para mostrar el listado -- solo se pierden los indicadores visuales.
                await _logger.LogWarningAsync($"No se pudo consultar el estado de CSD/FIEL/cuentas: {ex.Message}", "EntidadesViewModel", "AplicarEstadoCsdFielAsync");
            }
        }

        public Task<List<EntidadCuentaBancariaDto>> ObtenerCuentasAsync(int idEntidad)
            => _entidadService.GetCuentasBancariasAsync(idEntidad);

        public Task<ApiResponse> CrearCuentaAsync(int idEntidad, string numeroCuenta, string? clabe, string? banco)
            => _entidadService.CrearCuentaBancariaAsync(idEntidad, numeroCuenta, clabe, banco);

        public Task<ApiResponse> EliminarCuentaAsync(int idCuenta)
            => _entidadService.EliminarCuentaBancariaAsync(idCuenta);

        /// <summary>Último mensaje de error de crear/editar (p. ej. "El RFC ya existe").</summary>
        public string? UltimoError { get; private set; }

        private static string? ExtraerMensaje(string? contenido)
        {
            if (string.IsNullOrWhiteSpace(contenido)) return null;
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(contenido);
                if (doc.RootElement.TryGetProperty("message", out var msg))
                    return msg.GetString();
            }
            catch (System.Text.Json.JsonException) { }
            return contenido;
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
            string? regimenFiscal = null,
            bool? esPredeterminada = null,
            string? serieFacturas = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                UltimoError = null;
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
                    regimenFiscal,
                    esPredeterminada,
                    serieFacturas,
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
                    UltimoError = ExtraerMensaje(response.Message);
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

        /// <summary>
        /// Actualiza una entidad existente
        /// </summary>
        public async Task<bool> UpdateEntidadAsync(
            int idEntidad,
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
            string? regimenFiscal = null,
            bool? esPredeterminada = null,
            string? serieFacturas = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                UltimoError = null;
                await _logger.LogInformationAsync($"Actualizando entidad: {nombreComercial}", "EntidadesViewModel", "UpdateEntidadAsync");

                var response = await _entidadService.UpdateEntidadAsync(
                    idEntidad,
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
                    regimenFiscal,
                    esPredeterminada,
                    serieFacturas,
                    cancellationToken);

                if (response.Success)
                {
                    await _logger.LogInformationAsync($"Entidad actualizada exitosamente: {nombreComercial}", "EntidadesViewModel", "UpdateEntidadAsync");

                    // Recargar la lista de entidades
                    await LoadEntidadesAsync(cancellationToken);
                    return true;
                }
                else
                {
                    UltimoError = ExtraerMensaje(response.Message);
                    await _logger.LogWarningAsync($"No se pudo actualizar la entidad: {response.Message}", "EntidadesViewModel", "UpdateEntidadAsync");
                    return false;
                }
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error al actualizar entidad", ex, "EntidadesViewModel", "UpdateEntidadAsync");
                return false;
            }
        }
    }
}
