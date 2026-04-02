using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.Areas;
using Advance_Control.Services.Operaciones;
using Advance_Control.Services.Equipos;
using Advance_Control.Services.Ubicaciones;
using Advance_Control.Services.Logging;
using Advance_Control.Services.Quotes;
using Advance_Control.Services.Entidades;
using Advance_Control.Services.Clientes;
using Advance_Control.Services.Activity;
using Advance_Control.Services.CheckOperacion;
using Advance_Control.Services.Session;
using Advance_Control.Services.RelacionUsuarioArea;

namespace Advance_Control.ViewModels
{
    /// <summary>
    /// ViewModel para la vista de Operaciones.
    /// Gestiona la lógica de presentación para operaciones del sistema.
    /// </summary>
    public class OperacionesViewModel : ViewModelBase
    {
        /// <summary>
        /// IVA rate (16%) for Mexican tax calculation
        /// </summary>
        private const double IVA_RATE = 0.16;
        
        private readonly IOperacionService _operacionService;
        private readonly IEquipoService _equipoService;
        private readonly IUbicacionService _ubicacionService;
        private readonly ILoggingService _logger;
        private readonly IQuoteService _quoteService;
        private readonly IEntidadService _entidadService;
        private readonly IClienteService _clienteService;
        private readonly IActivityService _activityService;
        private readonly ICheckOperacionService _checkService;
        private readonly IUserSessionService _userSession;
        private readonly IRelacionUsuarioAreaService _relacionAreaService;
        private readonly IAreasService _areasService;
        private ObservableCollection<OperacionDto> _operaciones;
        private ObservableCollection<AreaDto> _areas;
        private ObservableCollection<string> _clienteSugerencias;
        private ObservableCollection<string> _equipoSugerencias;
        private ObservableCollection<AreaDto> _areaSugerencias;
        private List<(int Id, string Texto)> _todosLosClientes = new();
        private List<(int Id, string Identificador)> _todosLosEquipos = new();
        private bool _isLoading;
        private string? _errorMessage;
        private int _idTipoFilter;
        private int _idClienteFilter;
        private int _idEquipoFilter;
        private int _idAtiendeFilter;
        private string? _notaFilter;
        private string? _selectedClienteText;
        private string? _selectedEquipoText;
        private DateTimeOffset? _fechaInicialFilter;
        private DateTimeOffset? _fechaFinalFilter;
        private AreaDto? _selectedAreaFilter;

        public OperacionesViewModel(IOperacionService operacionService, IEquipoService equipoService, IUbicacionService ubicacionService, ILoggingService logger, IQuoteService quoteService, IEntidadService entidadService, IClienteService clienteService, IActivityService activityService, ICheckOperacionService checkService, IUserSessionService userSession, IRelacionUsuarioAreaService relacionAreaService, IAreasService areasService)
        {
            _operacionService  = operacionService  ?? throw new ArgumentNullException(nameof(operacionService));
            _clienteService    = clienteService    ?? throw new ArgumentNullException(nameof(clienteService));
            _equipoService     = equipoService     ?? throw new ArgumentNullException(nameof(equipoService));
            _ubicacionService  = ubicacionService  ?? throw new ArgumentNullException(nameof(ubicacionService));
            _logger            = logger            ?? throw new ArgumentNullException(nameof(logger));
            _quoteService      = quoteService      ?? throw new ArgumentNullException(nameof(quoteService));
            _entidadService    = entidadService    ?? throw new ArgumentNullException(nameof(entidadService));
            _activityService   = activityService   ?? throw new ArgumentNullException(nameof(activityService));
            _checkService      = checkService      ?? throw new ArgumentNullException(nameof(checkService));
            _userSession       = userSession       ?? throw new ArgumentNullException(nameof(userSession));
            _relacionAreaService = relacionAreaService ?? throw new ArgumentNullException(nameof(relacionAreaService));
            _areasService      = areasService      ?? throw new ArgumentNullException(nameof(areasService));
            _operaciones         = new ObservableCollection<OperacionDto>();
            _areas               = new ObservableCollection<AreaDto>();
            _clienteSugerencias  = new ObservableCollection<string>();
            _equipoSugerencias   = new ObservableCollection<string>();
            _areaSugerencias     = new ObservableCollection<AreaDto>();
        }

        public ObservableCollection<OperacionDto> Operaciones
        {
            get => _operaciones;
            set
            {
                if (SetProperty(ref _operaciones, value))
                    OnPropertyChanged(nameof(IsEmpty));
            }
        }

        public ObservableCollection<AreaDto> Areas
        {
            get => _areas;
            set => SetProperty(ref _areas, value);
        }

        public ObservableCollection<string> ClienteSugerencias
        {
            get => _clienteSugerencias;
            set => SetProperty(ref _clienteSugerencias, value);
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
        public bool IsEmpty => !_isLoading && _operaciones.Count == 0;

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

        public int IdTipoFilter
        {
            get => _idTipoFilter;
            set => SetProperty(ref _idTipoFilter, value);
        }

        public int IdClienteFilter
        {
            get => _idClienteFilter;
            set => SetProperty(ref _idClienteFilter, value);
        }

        public int IdEquipoFilter
        {
            get => _idEquipoFilter;
            set => SetProperty(ref _idEquipoFilter, value);
        }

        public int IdAtiendeFilter
        {
            get => _idAtiendeFilter;
            set => SetProperty(ref _idAtiendeFilter, value);
        }

        public string? NotaFilter
        {
            get => _notaFilter;
            set => SetProperty(ref _notaFilter, value);
        }

        /// <summary>
        /// Texto que muestra el cliente seleccionado
        /// </summary>
        public string? SelectedClienteText
        {
            get => _selectedClienteText;
            set => SetProperty(ref _selectedClienteText, value);
        }

        /// <summary>
        /// Texto que muestra el equipo seleccionado
        /// </summary>
        public string? SelectedEquipoText
        {
            get => _selectedEquipoText;
            set => SetProperty(ref _selectedEquipoText, value);
        }

        /// <summary>
        /// Fecha inicial del rango de filtro (fechaInicio >= FechaInicialFilter)
        /// </summary>
        public DateTimeOffset? FechaInicialFilter
        {
            get => _fechaInicialFilter;
            set => SetProperty(ref _fechaInicialFilter, value);
        }

        /// <summary>
        /// Fecha final del rango de filtro (fechaFinal <= FechaFinalFilter)
        /// </summary>
        public DateTimeOffset? FechaFinalFilter
        {
            get => _fechaFinalFilter;
            set => SetProperty(ref _fechaFinalFilter, value);
        }

        /// <summary>
        /// Inicializa los datos de la vista
        /// </summary>
        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await _logger.LogInformationAsync("Vista de Operaciones inicializada", "OperacionesViewModel", "InitializeAsync");
                var areas = await _areasService.GetAreasAsync(activo: true, cancellationToken: cancellationToken);
                Areas.Clear();
                foreach (var a in areas)
                    Areas.Add(a);

                var clientes = await _clienteService.GetClientesAsync(null, cancellationToken);
                _todosLosClientes = clientes
                    .Select(c => (c.IdCliente, Texto: c.RazonSocial ?? c.NombreComercial ?? ""))
                    .Where(c => !string.IsNullOrWhiteSpace(c.Texto))
                    .ToList();

                var equipos = await _equipoService.GetEquiposAsync(null, cancellationToken);
                _todosLosEquipos = equipos
                    .Where(e => !string.IsNullOrWhiteSpace(e.Identificador))
                    .Select(e => (Id: e.IdEquipo, Identificador: e.Identificador!))
                    .DistinctBy(e => e.Identificador, StringComparer.OrdinalIgnoreCase)
                    .OrderBy(e => e.Identificador)
                    .ToList();
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error al inicializar la vista de Operaciones.";
                await _logger.LogErrorAsync("Error al inicializar OperacionesViewModel", ex, "OperacionesViewModel", "InitializeAsync");
            }
        }

        /// <summary>Filtra las sugerencias del ASB de cliente según el texto ingresado</summary>
        public void ActualizarSugerenciasCliente(string texto)
        {
            _clienteSugerencias.Clear();
            if (string.IsNullOrWhiteSpace(texto)) return;
            foreach (var c in _todosLosClientes
                .Where(c => c.Texto.Contains(texto, StringComparison.OrdinalIgnoreCase))
                .Take(10))
                _clienteSugerencias.Add(c.Texto);
        }

        /// <summary>Filtra las sugerencias del ASB de equipo según el texto ingresado</summary>
        public void ActualizarSugerenciasEquipo(string texto)
        {
            _equipoSugerencias.Clear();
            if (string.IsNullOrWhiteSpace(texto)) return;
            foreach (var e in _todosLosEquipos
                .Where(e => e.Identificador.Contains(texto, StringComparison.OrdinalIgnoreCase))
                .Take(10))
                _equipoSugerencias.Add(e.Identificador);
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

        /// <summary>Resuelve el texto a un IdCliente y aplica el filtro</summary>
        public void AplicarFiltroCliente(string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) { IdClienteFilter = 0; SelectedClienteText = null; return; }
            var match = _todosLosClientes.FirstOrDefault(c => c.Texto.Equals(texto, StringComparison.OrdinalIgnoreCase));
            IdClienteFilter = match.Id;
            SelectedClienteText = match.Id > 0 ? match.Texto : null;
        }

        /// <summary>Resuelve el identificador a un IdEquipo y aplica el filtro</summary>
        public void AplicarFiltroEquipo(string? identificador)
        {
            if (string.IsNullOrWhiteSpace(identificador)) { IdEquipoFilter = 0; SelectedEquipoText = null; return; }
            var match = _todosLosEquipos.FirstOrDefault(e => e.Identificador.Equals(identificador, StringComparison.OrdinalIgnoreCase));
            IdEquipoFilter = match.Id;
            SelectedEquipoText = match.Id > 0 ? match.Identificador : null;
        }

        /// <summary>Aplica el filtro de área</summary>
        public void AplicarFiltroArea(AreaDto? area) => SelectedAreaFilter = area;

        /// <summary>
        /// Carga las operaciones desde el servicio con los filtros aplicados
        /// </summary>
        public async Task LoadOperacionesAsync(Func<List<OperacionDto>, Task>? onBeforeCommit = null, CancellationToken cancellationToken = default)
        {
            if (IsLoading)
                return;

            try
            {
                IsLoading = true;
                ErrorMessage = null;
                await _logger.LogInformationAsync("Cargando operaciones...", "OperacionesViewModel", "LoadOperacionesAsync");

                var query = new OperacionQueryDto
                {
                    IdTipo = IdTipoFilter,
                    IdCliente = IdClienteFilter,
                    IdEquipo = IdEquipoFilter,
                    IdAtiende = IdAtiendeFilter,
                    Nota = NotaFilter,
                    FechaInicial = FechaInicialFilter,
                    FechaFinalFiltro = FechaFinalFilter
                };

                var operaciones = await _operacionService.GetOperacionesAsync(query, cancellationToken);

                // Filtrado por área según tipo de usuario
                var filtrados = await FiltrarPorAreaAsync(operaciones, cancellationToken);

                // Filtro manual de área seleccionado por el usuario
                if (SelectedAreaFilter != null)
                {
                    var ids = await _areasService.GetIdentificadoresEnAreaAsync(SelectedAreaFilter.IdArea, cancellationToken);
                    var set = new HashSet<string>(ids, StringComparer.OrdinalIgnoreCase);
                    filtrados = filtrados.Where(o => !string.IsNullOrEmpty(o.Identificador) && set.Contains(o.Identificador)).ToList();
                }

                foreach (var operacion in filtrados)
                    operacion.BuildCheckFromInlineFields();

                if (onBeforeCommit != null)
                    await onBeforeCommit(filtrados);

                // Asignar de una sola vez para evitar renders intermedios con TotalMonto = 0
                Operaciones = new ObservableCollection<OperacionDto>(filtrados);

                await _logger.LogInformationAsync($"Se cargaron {filtrados.Count} operaciones exitosamente (de {operaciones.Count} totales)", "OperacionesViewModel", "LoadOperacionesAsync");
            }
            catch (OperationCanceledException)
            {
                ErrorMessage = "La operación fue cancelada.";
                await _logger.LogInformationAsync("Operación de carga de operaciones cancelada por el usuario", "OperacionesViewModel", "LoadOperacionesAsync");
            }
            catch (HttpRequestException ex)
            {
                ErrorMessage = "Error de conexión: No se pudo conectar con el servidor. Verifique su conexión a internet.";
                await _logger.LogErrorAsync("Error de conexión al cargar operaciones", ex, "OperacionesViewModel", "LoadOperacionesAsync");
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error inesperado al cargar operaciones: {ex.Message}";
                await _logger.LogErrorAsync("Error inesperado al cargar operaciones", ex, "OperacionesViewModel", "LoadOperacionesAsync");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Filtra registros según las áreas asignadas al usuario.
        /// Niveles superiores ven todo. TecSup/Tecnico solo ven equipos en sus áreas.
        /// Niveles inferiores no ven nada.
        /// </summary>
        private async Task<List<OperacionDto>> FiltrarPorAreaAsync(List<OperacionDto> items, CancellationToken ct)
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

            return new List<OperacionDto>();
        }

        /// <summary>
        /// Limpia los filtros y recarga todas las operaciones
        /// </summary>
        public async Task ClearFiltersAsync(Func<List<OperacionDto>, Task>? onBeforeCommit = null, CancellationToken cancellationToken = default)
        {
            try
            {
                IdTipoFilter = 0;
                IdClienteFilter = 0;
                IdEquipoFilter = 0;
                IdAtiendeFilter = 0;
                NotaFilter = null;
                SelectedClienteText = null;
                SelectedEquipoText = null;
                FechaInicialFilter = null;
                FechaFinalFilter = null;
                SelectedAreaFilter = null;
                _clienteSugerencias.Clear();
                _equipoSugerencias.Clear();
                _areaSugerencias.Clear();
                ErrorMessage = null;
                await LoadOperacionesAsync(onBeforeCommit, cancellationToken);
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error al limpiar filtros y recargar operaciones.";
                await _logger.LogErrorAsync("Error al limpiar filtros", ex, "OperacionesViewModel", "ClearFiltersAsync");
            }
        }

        /// <summary>
        /// Elimina una operación por su ID
        /// </summary>
        public async Task<bool> DeleteOperacionAsync(int idOperacion, CancellationToken cancellationToken = default)
        {
            try
            {
                await _logger.LogInformationAsync($"Eliminando operación {idOperacion}...", "OperacionesViewModel", "DeleteOperacionAsync");

                var result = await _operacionService.DeleteOperacionAsync(idOperacion, cancellationToken);

                if (result)
                {
                    await _activityService.CrearActividadAsync("Operaciones", $"Operación eliminada (ID: {idOperacion})");
                    await _logger.LogInformationAsync($"Operación {idOperacion} eliminada exitosamente", "OperacionesViewModel", "DeleteOperacionAsync");
                    await LoadOperacionesAsync(cancellationToken);
                }
                else
                {
                    ErrorMessage = "No se pudo eliminar la operación.";
                    await _logger.LogWarningAsync($"No se pudo eliminar la operación {idOperacion}", "OperacionesViewModel", "DeleteOperacionAsync");
                }

                return result;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al eliminar operación: {ex.Message}";
                await _logger.LogErrorAsync($"Error al eliminar operación {idOperacion}", ex, "OperacionesViewModel", "DeleteOperacionAsync");
                return false;
            }
        }

        /// <summary>
        /// Actualiza el monto u otros campos de una operación
        /// </summary>
        public async Task<bool> UpdateOperacionAsync(int idOperacion, int idTipo = 0, int idCliente = 0, int idEquipo = 0, int idAtiende = 0, decimal monto = 0, string? nota = null, DateTime? fechaFinal = null, CancellationToken cancellationToken = default)
        {
            try
            {
                await _logger.LogInformationAsync($"Actualizando operación {idOperacion}...", "OperacionesViewModel", "UpdateOperacionAsync");
                var resultado = await _operacionService.UpdateOperacionAsync(idOperacion, idTipo, idCliente, idEquipo, idAtiende, monto, nota, fechaFinal, cancellationToken);
                if (resultado)
                    await _activityService.CrearActividadAsync("Operaciones", $"Operación actualizada (ID: {idOperacion})");
                return resultado;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al actualizar operación: {ex.Message}";
                await _logger.LogErrorAsync($"Error al actualizar operación {idOperacion}", ex, "OperacionesViewModel", "UpdateOperacionAsync");
                return false;
            }
        }

        /// <summary>
        /// Reabre una operación limpiando su fechaFinal
        /// </summary>
        public async Task<bool> ReopenOperacionAsync(int idOperacion, CancellationToken cancellationToken = default)
        {
            try
            {
                await _logger.LogInformationAsync($"Reabriendo operación {idOperacion}...", "OperacionesViewModel", "ReopenOperacionAsync");
                return await _operacionService.ReopenOperacionAsync(idOperacion, cancellationToken);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al reabrir operación: {ex.Message}";
                await _logger.LogErrorAsync($"Error al reabrir operación {idOperacion}", ex, "OperacionesViewModel", "ReopenOperacionAsync");
                return false;
            }
        }

        /// <summary>
        /// Genera una cotización PDF para la operación especificada con sus cargos
        /// </summary>
        public async Task<string?> GenerateQuoteAsync(OperacionDto operacion, string? dirigidoA = null, CancellationToken cancellationToken = default)
        {
            try
            {
                if (operacion == null)
                {
                    ErrorMessage = "No se puede generar cotización: operación no válida.";
                    return null;
                }

                if (operacion.Cargos == null || operacion.Cargos.Count == 0)
                {
                    ErrorMessage = "No se puede generar cotización: no hay cargos asociados a esta operación.";
                    return null;
                }

                await _logger.LogInformationAsync($"Generando cotización para operación {operacion.IdOperacion}...", "OperacionesViewModel", "GenerateQuoteAsync");

                // Get active entity to use company name
                string? nombreEmpresa = null;
                string? apoderadoNombre = null;
                try
                {
                    var entidadActiva = await _entidadService.GetActiveEntidadAsync(cancellationToken);
                    nombreEmpresa = entidadActiva?.NombreComercial;
                    apoderadoNombre = entidadActiva?.Apoderado;
                    if (!string.IsNullOrWhiteSpace(nombreEmpresa))
                    {
                        await _logger.LogInformationAsync($"Usando nombre comercial de entidad activa: {nombreEmpresa}", "OperacionesViewModel", "GenerateQuoteAsync");
                    }
                    if (!string.IsNullOrWhiteSpace(apoderadoNombre))
                    {
                        await _logger.LogInformationAsync($"Usando apoderado de entidad activa: {apoderadoNombre}", "OperacionesViewModel", "GenerateQuoteAsync");
                    }
                }
                catch (Exception ex)
                {
                    // Log but don't fail if we can't get the entity
                    await _logger.LogWarningAsync($"No se pudo obtener la entidad activa: {ex.Message}", "OperacionesViewModel", "GenerateQuoteAsync");
                }

                // Get equipment location if available
                string? ubicacionNombre = null;
                try
                {
                    if (!string.IsNullOrWhiteSpace(operacion.Identificador))
                    {
                        // Search for equipment by identifier
                        var equipos = await _equipoService.GetEquiposAsync(new EquipoQueryDto { Identificador = operacion.Identificador }, cancellationToken);
                        var equipo = equipos?.FirstOrDefault();
                        
                        if (equipo?.IdUbicacion.HasValue == true && equipo.IdUbicacion.Value > 0)
                        {
                            var ubicacion = await _ubicacionService.GetUbicacionByIdAsync(equipo.IdUbicacion.Value, cancellationToken);
                            ubicacionNombre = ubicacion?.Nombre;
                            await _logger.LogInformationAsync($"Ubicación encontrada para equipo {operacion.Identificador}: {ubicacionNombre}", "OperacionesViewModel", "GenerateQuoteAsync");
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log but don't fail if we can't get the location
                    await _logger.LogWarningAsync($"No se pudo obtener la ubicación del equipo: {ex.Message}", "OperacionesViewModel", "GenerateQuoteAsync");
                }
                // Get client credit limit if available
                decimal? limiteCredito = null;
                try
                {
                    if (operacion.IdCliente.HasValue && operacion.IdCliente.Value > 0)
                    {
                        var clientes = await _clienteService.GetClienteByIdAsync(operacion.IdCliente.Value, cancellationToken);
                        limiteCredito = clientes?.FirstOrDefault()?.LimiteCredito;
                    }
                }
                catch (Exception ex)
                {
                    await _logger.LogWarningAsync($"No se pudo obtener el límite de crédito del cliente: {ex.Message}", "OperacionesViewModel", "GenerateQuoteAsync");
                }

                var filePath = await _quoteService.GenerateQuotePdfAsync(operacion, operacion.Cargos, ubicacionNombre, nombreEmpresa, apoderadoNombre, limiteCredito, dirigidoA);

                // Calculate total with IVA and update local model
                if (operacion.IdOperacion.HasValue)
                {
                    var subtotal = operacion.Cargos.Sum(c => c.Monto ?? 0);
                    var iva = subtotal * IVA_RATE;
                    operacion.Monto = (decimal)(subtotal + iva);
                    await _logger.LogInformationAsync($"Monto local de operación {operacion.IdOperacion} calculado: {operacion.Monto:N2}", "OperacionesViewModel", "GenerateQuoteAsync");
                }

                await _logger.LogInformationAsync($"Cotización generada exitosamente: {filePath}", "OperacionesViewModel", "GenerateQuoteAsync");

                return filePath;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al generar cotización: {ex.Message}";
                await _logger.LogErrorAsync($"Error al generar cotización para operación {operacion?.IdOperacion}", ex, "OperacionesViewModel", "GenerateQuoteAsync");
                return null;
            }
        }

        /// <summary>
        /// Genera un reporte PDF para la operación especificada con sus cargos y fotografías
        /// </summary>
        public async Task<string?> GenerateReporteAsync(OperacionDto operacion, string? dirigidoA = null, CancellationToken cancellationToken = default)
        {
            try
            {
                if (operacion == null)
                {
                    ErrorMessage = "No se puede generar reporte: operación no válida.";
                    return null;
                }

                if (operacion.Cargos == null || operacion.Cargos.Count == 0)
                {
                    ErrorMessage = "No se puede generar reporte: no hay cargos asociados a esta operación.";
                    return null;
                }

                await _logger.LogInformationAsync($"Generando reporte para operación {operacion.IdOperacion}...", "OperacionesViewModel", "GenerateReporteAsync");

                // Get active entity to use company name
                string? nombreEmpresa = null;
                try
                {
                    var entidadActiva = await _entidadService.GetActiveEntidadAsync(cancellationToken);
                    nombreEmpresa = entidadActiva?.NombreComercial;
                    if (!string.IsNullOrWhiteSpace(nombreEmpresa))
                    {
                        await _logger.LogInformationAsync($"Usando nombre comercial de entidad activa: {nombreEmpresa}", "OperacionesViewModel", "GenerateReporteAsync");
                    }
                }
                catch (Exception ex)
                {
                    // Log but don't fail if we can't get the entity
                    await _logger.LogWarningAsync($"No se pudo obtener la entidad activa: {ex.Message}", "OperacionesViewModel", "GenerateReporteAsync");
                }

                // Get equipment location if available
                string? ubicacionNombre = null;
                try
                {
                    if (!string.IsNullOrWhiteSpace(operacion.Identificador))
                    {
                        // Search for equipment by identifier
                        var equipos = await _equipoService.GetEquiposAsync(new EquipoQueryDto { Identificador = operacion.Identificador }, cancellationToken);
                        var equipo = equipos?.FirstOrDefault();
                        
                        if (equipo?.IdUbicacion.HasValue == true && equipo.IdUbicacion.Value > 0)
                        {
                            var ubicacion = await _ubicacionService.GetUbicacionByIdAsync(equipo.IdUbicacion.Value, cancellationToken);
                            ubicacionNombre = ubicacion?.Nombre;
                            await _logger.LogInformationAsync($"Ubicación encontrada para equipo {operacion.Identificador}: {ubicacionNombre}", "OperacionesViewModel", "GenerateReporteAsync");
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log but don't fail if we can't get the location
                    await _logger.LogWarningAsync($"No se pudo obtener la ubicación del equipo: {ex.Message}", "OperacionesViewModel", "GenerateReporteAsync");
                }

                var filePath = await _quoteService.GenerateReportePdfAsync(operacion, operacion.Cargos, ubicacionNombre, nombreEmpresa, dirigidoA);

                await _logger.LogInformationAsync($"Reporte generado exitosamente: {filePath}", "OperacionesViewModel", "GenerateReporteAsync");

                return filePath;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al generar reporte: {ex.Message}";
                await _logger.LogErrorAsync($"Error al generar reporte para operación {operacion?.IdOperacion}", ex, "OperacionesViewModel", "GenerateReporteAsync");
                return null;
            }
        }

        /// <summary>
        /// Busca un PDF existente de cotización o reporte para una operación.
        /// </summary>
        public string? FindExistingPdf(int idOperacion, string tipo)
            => _quoteService.FindExistingPdf(idOperacion, tipo);

        /// <summary>
        /// Elimina todos los PDFs de un tipo para una operación. tipo: "Cotizacion", "Reporte" o "*" para ambos.
        /// </summary>
        public void DeleteOperacionPdfs(int idOperacion, string tipo)
            => _quoteService.DeleteOperacionPdfs(idOperacion, tipo);

        /// <summary>
        /// Actualiza las propiedades CotizacionPdfPath y ReportePdfPath del DTO consultando el sistema de archivos.
        /// </summary>
        public void RefreshPdfPaths(Models.OperacionDto operacion)
        {
            if (!operacion.IdOperacion.HasValue) return;
            operacion.CotizacionPdfPath = _quoteService.FindExistingPdf(operacion.IdOperacion.Value, "Cotizacion");
            operacion.ReportePdfPath = _quoteService.FindExistingPdf(operacion.IdOperacion.Value, "Reporte");
        }

        /// <summary>
        /// Carga el checkOperacion para una operación y lo asigna a operacion.CheckOperacion.
        /// </summary>
        public async Task LoadCheckAsync(OperacionDto operacion)
        {
            if (!operacion.IdOperacion.HasValue) return;
            try
            {
                var check = await _checkService.GetAsync(operacion.IdOperacion.Value);
                operacion.CheckOperacion = check;
            }
            catch (Exception ex)
            {
                await _logger.LogWarningAsync($"No se pudo cargar checkOperacion: {ex.Message}", "OperacionesViewModel", "LoadCheckAsync");
            }
        }

        /// <summary>
        /// Actualiza un campo del checkOperacion a true y refresca operacion.CheckOperacion.
        /// </summary>
        public async Task UpdateCheckAsync(OperacionDto operacion, string campo)
        {
            if (!operacion.IdOperacion.HasValue) return;
            try
            {
                await _checkService.UpdateCampoAsync(operacion.IdOperacion.Value, campo, true);
                var check = await _checkService.GetAsync(operacion.IdOperacion.Value);
                operacion.CheckOperacion = check;
            }
            catch (Exception ex)
            {
                await _logger.LogWarningAsync($"No se pudo actualizar checkOperacion ({campo}): {ex.Message}", "OperacionesViewModel", "UpdateCheckAsync");
            }
        }
    }
}
