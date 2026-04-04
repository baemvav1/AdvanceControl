using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.Areas;
using Advance_Control.Services.Equipos;
using Advance_Control.Services.OrdenServicio;
using Advance_Control.Services.Logging;
using Advance_Control.Services.Session;
using Advance_Control.Services.Activity;
using Advance_Control.Services.RelacionUsuarioArea;

namespace Advance_Control.ViewModels
{
    /// <summary>
    /// ViewModel para la vista de Órdenes de Servicio.
    /// Gestiona la lógica de presentación para operaciones de órdenes de servicio.
    /// </summary>
    public class OrdenServicioViewModel : ViewModelBase
    {
        private readonly IOrdenServicioService _ordenServicioService;
        private readonly IEquipoService _equipoService;
        private readonly ILoggingService _logger;
        private readonly IUserSessionService _userSession;
        private readonly IActivityService _activityService;
        private readonly IRelacionUsuarioAreaService _relacionAreaService;
        private readonly IAreasService _areasService;
        private ObservableCollection<OrdenServicioDto> _ordenesServicio;
        private ObservableCollection<AreaDto> _areas;
        private ObservableCollection<string> _equipoSugerencias;
        private ObservableCollection<AreaDto> _areaSugerencias;
        private List<string> _todosLosIdentificadores = new();
        private bool _isLoading;
        private string? _errorMessage;
        private string? _identificadorFilter;
        private int _idClienteFilter;
        private AreaDto? _selectedAreaFilter;

        public OrdenServicioViewModel(IOrdenServicioService ordenServicioService, IEquipoService equipoService, ILoggingService logger, IUserSessionService userSession, IActivityService activityService, IRelacionUsuarioAreaService relacionAreaService, IAreasService areasService)
        {
            _ordenServicioService = ordenServicioService ?? throw new ArgumentNullException(nameof(ordenServicioService));
            _equipoService        = equipoService        ?? throw new ArgumentNullException(nameof(equipoService));
            _logger               = logger               ?? throw new ArgumentNullException(nameof(logger));
            _userSession          = userSession          ?? throw new ArgumentNullException(nameof(userSession));
            _activityService      = activityService      ?? throw new ArgumentNullException(nameof(activityService));
            _relacionAreaService  = relacionAreaService   ?? throw new ArgumentNullException(nameof(relacionAreaService));
            _areasService         = areasService         ?? throw new ArgumentNullException(nameof(areasService));
            _ordenesServicio     = new ObservableCollection<OrdenServicioDto>();
            _areas               = new ObservableCollection<AreaDto>();
            _equipoSugerencias   = new ObservableCollection<string>();
            _areaSugerencias     = new ObservableCollection<AreaDto>();
        }

        public ObservableCollection<OrdenServicioDto> OrdenesServicio
        {
            get => _ordenesServicio;
            set
            {
                if (SetProperty(ref _ordenesServicio, value))
                    OnPropertyChanged(nameof(IsEmpty));
            }
        }

        public ObservableCollection<AreaDto> Areas
        {
            get => _areas;
            set => SetProperty(ref _areas, value);
        }

        public ObservableCollection<string> EquipoSugerencias
        {
            get => _equipoSugerencias;
            set => SetProperty(ref _equipoSugerencias, value);
        }

        public ObservableCollection<AreaDto> AreaSugerencias
        {
            get => _areaSugerencias;
            set => SetProperty(ref _areaSugerencias, value);
        }

        public AreaDto? SelectedAreaFilter
        {
            get => _selectedAreaFilter;
            set => SetProperty(ref _selectedAreaFilter, value);
        }

        /// <summary>
        /// Indica si hay una operación en curso
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (SetProperty(ref _isLoading, value))
                    OnPropertyChanged(nameof(IsEmpty));
            }
        }

        /// <summary>
        /// Indica si la lista está vacía y no está cargando
        /// </summary>
        public bool IsEmpty => !_isLoading && _ordenesServicio.Count == 0;

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

        public string? IdentificadorFilter
        {
            get => _identificadorFilter;
            set => SetProperty(ref _identificadorFilter, value);
        }

        public int IdClienteFilter
        {
            get => _idClienteFilter;
            set => SetProperty(ref _idClienteFilter, value);
        }

        /// <summary>
        /// Carga las áreas e identificadores de equipos para los filtros AutoSuggestBox
        /// </summary>
        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var areas = await _areasService.GetAreasAsync(activo: true, cancellationToken: cancellationToken);
                Areas.Clear();
                foreach (var a in areas)
                    Areas.Add(a);

                var equipos = await _equipoService.GetEquiposAsync(null, cancellationToken);
                _todosLosIdentificadores = equipos
                    .Select(e => e.Identificador)
                    .Where(id => !string.IsNullOrWhiteSpace(id))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(id => id)
                    .ToList()!;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error al inicializar filtros de órdenes de servicio", ex, "OrdenServicioViewModel", "InitializeAsync");
            }
        }

        /// <summary>Filtra las sugerencias del ASB de equipo según el texto ingresado</summary>
        public void ActualizarSugerenciasEquipo(string texto)
        {
            _equipoSugerencias.Clear();
            if (string.IsNullOrWhiteSpace(texto)) return;
            foreach (var id in _todosLosIdentificadores
                .Where(id => id.Contains(texto, StringComparison.OrdinalIgnoreCase))
                .Take(10))
                _equipoSugerencias.Add(id);
        }

        /// <summary>Filtra las sugerencias del ASB de área según el texto ingresado</summary>
        public void ActualizarSugerenciasArea(string texto)
        {
            _areaSugerencias.Clear();
            foreach (var a in Areas
                .Where(a => string.IsNullOrWhiteSpace(texto) || a.Nombre.Contains(texto, StringComparison.OrdinalIgnoreCase))
                .Take(10))
                _areaSugerencias.Add(a);
        }

        /// <summary>Aplica el filtro de equipo y recarga la lista</summary>
        public async Task AplicarFiltroEquipo(string? identificador)
        {
            IdentificadorFilter = string.IsNullOrWhiteSpace(identificador) ? null : identificador;
            await LoadOrdenesServicioAsync();
        }

        /// <summary>Aplica el filtro de área y recarga la lista</summary>
        public async Task AplicarFiltroArea(AreaDto? area)
        {
            SelectedAreaFilter = area;
            await LoadOrdenesServicioAsync();
        }

        /// <summary>
        /// Carga las órdenes de servicio desde el servicio con los filtros aplicados
        /// </summary>
        public async Task LoadOrdenesServicioAsync(CancellationToken cancellationToken = default)
        {
            if (IsLoading)
                return;

            try
            {
                IsLoading = true;
                ErrorMessage = null;
                await _logger.LogInformationAsync("Cargando órdenes de servicio...", "OrdenServicioViewModel", "LoadOrdenesServicioAsync");

                var query = new OrdenServicioQueryDto
                {
                    Identificador = IdentificadorFilter,
                    IdCliente = IdClienteFilter
                };

                var ordenes = await _ordenServicioService.GetOrdenesServicioAsync(query, cancellationToken);

                // Filtrado por área según tipo de usuario
                var filtrados = await FiltrarPorAreaAsync(ordenes, cancellationToken);

                // Filtro manual de área seleccionado por el usuario
                if (SelectedAreaFilter != null)
                {
                    var ids = await _areasService.GetIdentificadoresEnAreaAsync(SelectedAreaFilter.IdArea, cancellationToken);
                    var set = new HashSet<string>(ids, StringComparer.OrdinalIgnoreCase);
                    filtrados = filtrados.Where(o => !string.IsNullOrEmpty(o.Identificador) && set.Contains(o.Identificador)).ToList();
                }

                OrdenesServicio.Clear();
                foreach (var orden in filtrados)
                {
                    OrdenesServicio.Add(orden);
                }
                OnPropertyChanged(nameof(IsEmpty));

                await _logger.LogInformationAsync($"Se cargaron {filtrados.Count} órdenes de servicio exitosamente (de {ordenes.Count} totales)", "OrdenServicioViewModel", "LoadOrdenesServicioAsync");
            }
            catch (OperationCanceledException)
            {
                ErrorMessage = "La operación fue cancelada.";
                await _logger.LogInformationAsync("Operación de carga de órdenes de servicio cancelada por el usuario", "OrdenServicioViewModel", "LoadOrdenesServicioAsync");
            }
            catch (HttpRequestException ex)
            {
                ErrorMessage = "Error de conexión: No se pudo conectar con el servidor. Verifique su conexión a internet.";
                await _logger.LogErrorAsync("Error de conexión al cargar órdenes de servicio", ex, "OrdenServicioViewModel", "LoadOrdenesServicioAsync");
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error inesperado al cargar órdenes de servicio: {ex.Message}";
                await _logger.LogErrorAsync("Error inesperado al cargar órdenes de servicio", ex, "OrdenServicioViewModel", "LoadOrdenesServicioAsync");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Filtra registros según las áreas asignadas al usuario.
        /// Niveles superiores (Devs, Director, Admin, etc.) ven todo.
        /// TecSup/Tecnico solo ven equipos en sus áreas asignadas.
        /// Niveles inferiores no ven nada.
        /// </summary>
        private async Task<List<OrdenServicioDto>> FiltrarPorAreaAsync(List<OrdenServicioDto> items, CancellationToken ct)
        {
            if (!_userSession.IsLoaded) return items;

            var tipo = _userSession.TipoUsuario;
            if (tipo is "Devs" or "Director" or "Admin" or "Cont" or "AuxAdm" or "AuxCont")
                return items;

            if (tipo is "TecSup" or "Tecnico")
            {
                var permitidos = await _relacionAreaService.GetEquiposEnAreasAsync(_userSession.CredencialId, ct);
                var set = new HashSet<string>(permitidos, StringComparer.OrdinalIgnoreCase);
                return items.Where(o => !string.IsNullOrEmpty(o.Identificador) && set.Contains(o.Identificador)).ToList();
            }

            // Niveles inferiores: no ven órdenes de servicio
            return new List<OrdenServicioDto>();
        }

        /// <summary>
        /// Limpia los filtros y recarga todas las órdenes de servicio
        /// </summary>
        public async Task ClearFiltersAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                IdentificadorFilter = null;
                IdClienteFilter = 0;
                SelectedAreaFilter = null;
                ErrorMessage = null;
                await LoadOrdenesServicioAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error al limpiar filtros y recargar órdenes de servicio.";
                await _logger.LogErrorAsync("Error al limpiar filtros", ex, "OrdenServicioViewModel", "ClearFiltersAsync");
            }
        }

        /// <summary>
        /// Elimina una orden de servicio por su ID
        /// </summary>
        public async Task<bool> DeleteOrdenServicioAsync(int idOrdenServicio, CancellationToken cancellationToken = default)
        {
            try
            {
                await _logger.LogInformationAsync($"Eliminando orden de servicio {idOrdenServicio}...", "OrdenServicioViewModel", "DeleteOrdenServicioAsync");

                var result = await _ordenServicioService.DeleteOrdenServicioAsync(idOrdenServicio, cancellationToken);

                if (result)
                {
                    await _activityService.CrearActividadAsync("OrdenServicio", $"Orden de servicio eliminada (ID: {idOrdenServicio})");
                    await _logger.LogInformationAsync($"Orden de servicio {idOrdenServicio} eliminada exitosamente", "OrdenServicioViewModel", "DeleteOrdenServicioAsync");
                    await LoadOrdenesServicioAsync(cancellationToken);
                }
                else
                {
                    ErrorMessage = "No se pudo eliminar la orden de servicio.";
                    await _logger.LogWarningAsync($"No se pudo eliminar la orden de servicio {idOrdenServicio}", "OrdenServicioViewModel", "DeleteOrdenServicioAsync");
                }

                return result;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al eliminar orden de servicio: {ex.Message}";
                await _logger.LogErrorAsync($"Error al eliminar orden de servicio {idOrdenServicio}", ex, "OrdenServicioViewModel", "DeleteOrdenServicioAsync");
                return false;
            }
        }

        /// <summary>
        /// Crea una nueva orden de servicio
        /// </summary>
        public async Task<bool> CreateOrdenServicioAsync(int idTipoMantenimiento, int idCliente, int idEquipo, string? nota = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var credencialId = _userSession.IsLoaded ? _userSession.CredencialId : 0;
                await _logger.LogInformationAsync($"Creando nueva orden de servicio...", "OrdenServicioViewModel", "CreateOrdenServicioAsync");

                var result = await _ordenServicioService.CreateOrdenServicioAsync(idTipoMantenimiento, idCliente, idEquipo, nota, credencialId, cancellationToken);

                if (result)
                {
                    await _activityService.CrearActividadAsync("OrdenServicio", "Orden de servicio registrada");
                    await _logger.LogInformationAsync($"Orden de servicio creada exitosamente", "OrdenServicioViewModel", "CreateOrdenServicioAsync");
                    await LoadOrdenesServicioAsync(cancellationToken);
                }
                else
                {
                    ErrorMessage = "No se pudo crear la orden de servicio.";
                    await _logger.LogWarningAsync($"No se pudo crear la orden de servicio", "OrdenServicioViewModel", "CreateOrdenServicioAsync");
                }

                return result;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al crear orden de servicio: {ex.Message}";
                await _logger.LogErrorAsync($"Error al crear orden de servicio", ex, "OrdenServicioViewModel", "CreateOrdenServicioAsync");
                return false;
            }
        }

        /// <summary>
        /// Actualiza el estado de atendido de una orden de servicio
        /// </summary>
        public async Task<bool> UpdateAtendidoAsync(int idOrdenServicio, int idAtendio, CancellationToken cancellationToken = default)
        {
            try
            {
                await _logger.LogInformationAsync($"Actualizando estado atendido de orden de servicio {idOrdenServicio}...", "OrdenServicioViewModel", "UpdateAtendidoAsync");

                var result = await _ordenServicioService.UpdateAtendidoAsync(idOrdenServicio, idAtendio, cancellationToken);

                if (result)
                {
                    await _logger.LogInformationAsync($"Estado atendido de orden de servicio {idOrdenServicio} actualizado exitosamente", "OrdenServicioViewModel", "UpdateAtendidoAsync");
                    await LoadOrdenesServicioAsync(cancellationToken);
                }
                else
                {
                    ErrorMessage = "No se pudo actualizar el estado atendido de la orden de servicio.";
                    await _logger.LogWarningAsync($"No se pudo actualizar el estado atendido de la orden de servicio {idOrdenServicio}", "OrdenServicioViewModel", "UpdateAtendidoAsync");
                }

                return result;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al actualizar estado atendido: {ex.Message}";
                await _logger.LogErrorAsync($"Error al actualizar estado atendido de orden de servicio {idOrdenServicio}", ex, "OrdenServicioViewModel", "UpdateAtendidoAsync");
                return false;
            }
        }
    }
}
