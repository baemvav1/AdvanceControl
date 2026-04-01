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
using Advance_Control.Services.Mantenimiento;
using Advance_Control.Services.Logging;
using Advance_Control.Services.Session;
using Advance_Control.Services.Activity;
using Advance_Control.Services.RelacionUsuarioArea;

namespace Advance_Control.ViewModels
{
    /// <summary>
    /// ViewModel para la vista de Mantenimiento (Mtto).
    /// Gestiona la lógica de presentación para operaciones de mantenimiento.
    /// </summary>
    public class MttoViewModel : ViewModelBase
    {
        private readonly IMantenimientoService _mantenimientoService;
        private readonly IEquipoService _equipoService;
        private readonly ILoggingService _logger;
        private readonly IUserSessionService _userSession;
        private readonly IActivityService _activityService;
        private readonly IRelacionUsuarioAreaService _relacionAreaService;
        private readonly IAreasService _areasService;
        private ObservableCollection<MantenimientoDto> _mantenimientos;
        private ObservableCollection<AreaDto> _areas;
        private ObservableCollection<string> _equipoSugerencias;
        private ObservableCollection<AreaDto> _areaSugerencias;
        private List<string> _todosLosIdentificadores = new();
        private bool _isLoading;
        private string? _errorMessage;
        private string? _identificadorFilter;
        private int _idClienteFilter;
        private AreaDto? _selectedAreaFilter;

        public MttoViewModel(IMantenimientoService mantenimientoService, IEquipoService equipoService, ILoggingService logger, IUserSessionService userSession, IActivityService activityService, IRelacionUsuarioAreaService relacionAreaService, IAreasService areasService)
        {
            _mantenimientoService = mantenimientoService ?? throw new ArgumentNullException(nameof(mantenimientoService));
            _equipoService        = equipoService        ?? throw new ArgumentNullException(nameof(equipoService));
            _logger               = logger               ?? throw new ArgumentNullException(nameof(logger));
            _userSession          = userSession          ?? throw new ArgumentNullException(nameof(userSession));
            _activityService      = activityService      ?? throw new ArgumentNullException(nameof(activityService));
            _relacionAreaService  = relacionAreaService   ?? throw new ArgumentNullException(nameof(relacionAreaService));
            _areasService         = areasService         ?? throw new ArgumentNullException(nameof(areasService));
            _mantenimientos      = new ObservableCollection<MantenimientoDto>();
            _areas               = new ObservableCollection<AreaDto>();
            _equipoSugerencias   = new ObservableCollection<string>();
            _areaSugerencias     = new ObservableCollection<AreaDto>();
        }

        public ObservableCollection<MantenimientoDto> Mantenimientos
        {
            get => _mantenimientos;
            set
            {
                if (SetProperty(ref _mantenimientos, value))
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
        public bool IsEmpty => !_isLoading && _mantenimientos.Count == 0;

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
                await _logger.LogErrorAsync("Error al inicializar filtros de mantenimiento", ex, "MttoViewModel", "InitializeAsync");
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
            await LoadMantenimientosAsync();
        }

        /// <summary>Aplica el filtro de área y recarga la lista</summary>
        public async Task AplicarFiltroArea(AreaDto? area)
        {
            SelectedAreaFilter = area;
            await LoadMantenimientosAsync();
        }

        /// <summary>
        /// Carga los mantenimientos desde el servicio con los filtros aplicados
        /// </summary>
        public async Task LoadMantenimientosAsync(CancellationToken cancellationToken = default)
        {
            if (IsLoading)
                return;

            try
            {
                IsLoading = true;
                ErrorMessage = null;
                await _logger.LogInformationAsync("Cargando mantenimientos...", "MttoViewModel", "LoadMantenimientosAsync");

                var query = new MantenimientoQueryDto
                {
                    Identificador = IdentificadorFilter,
                    IdCliente = IdClienteFilter
                };

                var mantenimientos = await _mantenimientoService.GetMantenimientosAsync(query, cancellationToken);

                // Filtrado por área según tipo de usuario
                var filtrados = await FiltrarPorAreaAsync(mantenimientos, cancellationToken);

                // Filtro manual de área seleccionado por el usuario
                if (SelectedAreaFilter != null)
                {
                    var ids = await _areasService.GetIdentificadoresEnAreaAsync(SelectedAreaFilter.IdArea, cancellationToken);
                    var set = new HashSet<string>(ids, StringComparer.OrdinalIgnoreCase);
                    filtrados = filtrados.Where(m => !string.IsNullOrEmpty(m.Identificador) && set.Contains(m.Identificador)).ToList();
                }

                Mantenimientos.Clear();
                foreach (var mantenimiento in filtrados)
                {
                    Mantenimientos.Add(mantenimiento);
                }
                OnPropertyChanged(nameof(IsEmpty));

                await _logger.LogInformationAsync($"Se cargaron {filtrados.Count} mantenimientos exitosamente (de {mantenimientos.Count} totales)", "MttoViewModel", "LoadMantenimientosAsync");
            }
            catch (OperationCanceledException)
            {
                ErrorMessage = "La operación fue cancelada.";
                await _logger.LogInformationAsync("Operación de carga de mantenimientos cancelada por el usuario", "MttoViewModel", "LoadMantenimientosAsync");
            }
            catch (HttpRequestException ex)
            {
                ErrorMessage = "Error de conexión: No se pudo conectar con el servidor. Verifique su conexión a internet.";
                await _logger.LogErrorAsync("Error de conexión al cargar mantenimientos", ex, "MttoViewModel", "LoadMantenimientosAsync");
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error inesperado al cargar mantenimientos: {ex.Message}";
                await _logger.LogErrorAsync("Error inesperado al cargar mantenimientos", ex, "MttoViewModel", "LoadMantenimientosAsync");
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
        private async Task<List<MantenimientoDto>> FiltrarPorAreaAsync(List<MantenimientoDto> items, CancellationToken ct)
        {
            if (!_userSession.IsLoaded) return items;

            var tipo = _userSession.TipoUsuario;
            if (tipo is "Devs" or "Director" or "Admin" or "Cont" or "AuxAdm" or "AuxCont")
                return items;

            if (tipo is "TecSup" or "Tecnico")
            {
                var permitidos = await _relacionAreaService.GetEquiposEnAreasAsync(_userSession.CredencialId, ct);
                var set = new HashSet<string>(permitidos, StringComparer.OrdinalIgnoreCase);
                return items.Where(m => !string.IsNullOrEmpty(m.Identificador) && set.Contains(m.Identificador)).ToList();
            }

            // Niveles inferiores: no ven mantenimientos
            return new List<MantenimientoDto>();
        }

        /// <summary>
        /// Limpia los filtros y recarga todos los mantenimientos
        /// </summary>
        public async Task ClearFiltersAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                IdentificadorFilter = null;
                IdClienteFilter = 0;
                SelectedAreaFilter = null;
                ErrorMessage = null;
                await LoadMantenimientosAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error al limpiar filtros y recargar mantenimientos.";
                await _logger.LogErrorAsync("Error al limpiar filtros", ex, "MttoViewModel", "ClearFiltersAsync");
            }
        }

        /// <summary>
        /// Elimina un mantenimiento por su ID
        /// </summary>
        public async Task<bool> DeleteMantenimientoAsync(int idMantenimiento, CancellationToken cancellationToken = default)
        {
            try
            {
                await _logger.LogInformationAsync($"Eliminando mantenimiento {idMantenimiento}...", "MttoViewModel", "DeleteMantenimientoAsync");

                var result = await _mantenimientoService.DeleteMantenimientoAsync(idMantenimiento, cancellationToken);

                if (result)
                {
                    await _activityService.CrearActividadAsync("Mantenimiento", $"Mantenimiento eliminado (ID: {idMantenimiento})");
                    await _logger.LogInformationAsync($"Mantenimiento {idMantenimiento} eliminado exitosamente", "MttoViewModel", "DeleteMantenimientoAsync");
                    // Recargar la lista de mantenimientos
                    await LoadMantenimientosAsync(cancellationToken);
                }
                else
                {
                    ErrorMessage = "No se pudo eliminar el mantenimiento.";
                    await _logger.LogWarningAsync($"No se pudo eliminar el mantenimiento {idMantenimiento}", "MttoViewModel", "DeleteMantenimientoAsync");
                }

                return result;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al eliminar mantenimiento: {ex.Message}";
                await _logger.LogErrorAsync($"Error al eliminar mantenimiento {idMantenimiento}", ex, "MttoViewModel", "DeleteMantenimientoAsync");
                return false;
            }
        }

        /// <summary>
        /// Crea un nuevo mantenimiento
        /// </summary>
        public async Task<bool> CreateMantenimientoAsync(int idTipoMantenimiento, int idCliente, int idEquipo, string? nota = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var credencialId = _userSession.IsLoaded ? _userSession.CredencialId : 0;
                await _logger.LogInformationAsync($"Creando nuevo mantenimiento...", "MttoViewModel", "CreateMantenimientoAsync");

                var result = await _mantenimientoService.CreateMantenimientoAsync(idTipoMantenimiento, idCliente, idEquipo, nota, credencialId, cancellationToken);

                if (result)
                {
                    await _activityService.CrearActividadAsync("Mantenimiento", "Mantenimiento registrado");
                    await _logger.LogInformationAsync($"Mantenimiento creado exitosamente", "MttoViewModel", "CreateMantenimientoAsync");
                    // Recargar la lista de mantenimientos
                    await LoadMantenimientosAsync(cancellationToken);
                }
                else
                {
                    ErrorMessage = "No se pudo crear el mantenimiento.";
                    await _logger.LogWarningAsync($"No se pudo crear el mantenimiento", "MttoViewModel", "CreateMantenimientoAsync");
                }

                return result;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al crear mantenimiento: {ex.Message}";
                await _logger.LogErrorAsync($"Error al crear mantenimiento", ex, "MttoViewModel", "CreateMantenimientoAsync");
                return false;
            }
        }

        /// <summary>
        /// Actualiza el estado de atendido de un mantenimiento
        /// </summary>
        public async Task<bool> UpdateAtendidoAsync(int idMantenimiento, int idAtendio, CancellationToken cancellationToken = default)
        {
            try
            {
                await _logger.LogInformationAsync($"Actualizando estado atendido del mantenimiento {idMantenimiento}...", "MttoViewModel", "UpdateAtendidoAsync");

                var result = await _mantenimientoService.UpdateAtendidoAsync(idMantenimiento, idAtendio, cancellationToken);

                if (result)
                {
                    await _logger.LogInformationAsync($"Estado atendido del mantenimiento {idMantenimiento} actualizado exitosamente", "MttoViewModel", "UpdateAtendidoAsync");
                    // Recargar la lista de mantenimientos
                    await LoadMantenimientosAsync(cancellationToken);
                }
                else
                {
                    ErrorMessage = "No se pudo actualizar el estado atendido del mantenimiento.";
                    await _logger.LogWarningAsync($"No se pudo actualizar el estado atendido del mantenimiento {idMantenimiento}", "MttoViewModel", "UpdateAtendidoAsync");
                }

                return result;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al actualizar estado atendido: {ex.Message}";
                await _logger.LogErrorAsync($"Error al actualizar estado atendido del mantenimiento {idMantenimiento}", ex, "MttoViewModel", "UpdateAtendidoAsync");
                return false;
            }
        }
    }
}
