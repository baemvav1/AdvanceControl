using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.Activity;
using Advance_Control.Services.CheckOperacion;
using Advance_Control.Services.Logging;
using Advance_Control.Services.Operaciones;
using Advance_Control.Services.Session;

namespace Advance_Control.ViewModels
{
    /// <summary>
    /// ViewModel para la página de inicio (Dashboard).
    /// Muestra bienvenida personalizada y actividad reciente del usuario.
    /// </summary>
    public class DashboardViewModel : ViewModelBase
    {
        private readonly IUserSessionService _userSessionService;
        private readonly ILoggingService _logger;
        private readonly IActivityService _activityService;
        private readonly IOperacionService _operacionService;
        private readonly ICheckOperacionService _checkService;

        private string _saludo = "Bienvenido";
        private string _nombreUsuario = string.Empty;
        private string _tipoUsuario = string.Empty;
        private string _iniciales = string.Empty;
        private string _fechaHoy = string.Empty;
        private bool _isLoading;
        private bool _isActividadLoading;
        private bool _isActividadEmpty = true;
        private bool _isTareasLoading;
        private bool _isTareasEmpty = true;

        public DashboardViewModel(
            IUserSessionService userSessionService,
            ILoggingService logger,
            IActivityService activityService,
            IOperacionService operacionService,
            ICheckOperacionService checkService)
        {
            _userSessionService = userSessionService ?? throw new ArgumentNullException(nameof(userSessionService));
            _logger             = logger             ?? throw new ArgumentNullException(nameof(logger));
            _activityService    = activityService    ?? throw new ArgumentNullException(nameof(activityService));
            _operacionService   = operacionService   ?? throw new ArgumentNullException(nameof(operacionService));
            _checkService       = checkService       ?? throw new ArgumentNullException(nameof(checkService));
            ActividadReciente = new ObservableCollection<ActivityItem>();
            OperacionesPendientes = new ObservableCollection<OperacionTodoItem>();
        }

        public string Saludo
        {
            get => _saludo;
            set => SetProperty(ref _saludo, value);
        }

        public string NombreUsuario
        {
            get => _nombreUsuario;
            set => SetProperty(ref _nombreUsuario, value);
        }

        public string TipoUsuario
        {
            get => _tipoUsuario;
            set => SetProperty(ref _tipoUsuario, value);
        }

        public string Iniciales
        {
            get => _iniciales;
            set => SetProperty(ref _iniciales, value);
        }

        public string FechaHoy
        {
            get => _fechaHoy;
            set => SetProperty(ref _fechaHoy, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public bool IsActividadLoading
        {
            get => _isActividadLoading;
            set
            {
                SetProperty(ref _isActividadLoading, value);
                OnPropertyChanged(nameof(IsActividadEmpty));
            }
        }

        /// <summary>True cuando no hay actividad y no está cargando</summary>
        public bool IsActividadEmpty
        {
            get => !_isActividadLoading && _isActividadEmpty;
        }

        public bool IsTareasLoading
        {
            get => _isTareasLoading;
            set
            {
                SetProperty(ref _isTareasLoading, value);
                OnPropertyChanged(nameof(IsTareasEmpty));
            }
        }

        /// <summary>True cuando no hay tareas pendientes y no está cargando</summary>
        public bool IsTareasEmpty => !_isTareasLoading && _isTareasEmpty;

        public ObservableCollection<ActivityItem> ActividadReciente { get; }
        public ObservableCollection<OperacionTodoItem> OperacionesPendientes { get; }

        public async Task LoadAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                IsLoading = true;

                if (!_userSessionService.IsLoaded)
                    await _userSessionService.LoadAsync(cancellationToken);

                var nombre = _userSessionService.NombreCompleto ?? string.Empty;
                NombreUsuario = nombre;
                TipoUsuario = _userSessionService.TipoUsuario ?? string.Empty;
                Iniciales = ObtenerIniciales(nombre);
                Saludo = ObtenerSaludo();
                FechaHoy = DateTime.Now.ToString("dddd, dd 'de' MMMM 'de' yyyy",
                    new System.Globalization.CultureInfo("es-MX"));
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error al cargar sesión en dashboard", ex,
                    "DashboardViewModel", "LoadAsync", "Sistema", "DashboardPage");
            }
            finally
            {
                IsLoading = false;
            }

            // Cargar secciones de forma independiente
            await Task.WhenAll(
                LoadActividadAsync(cancellationToken),
                LoadOperacionesPendientesAsync(cancellationToken));
        }

        public async Task LoadActividadAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                IsActividadLoading = true;
                ActividadReciente.Clear();
                _isActividadEmpty = true;

                if (!_userSessionService.IsLoaded)
                    await _userSessionService.LoadAsync(cancellationToken);

                if (_userSessionService.CredencialId <= 0)
                {
                    _isActividadEmpty = true;
                    return;
                }

                var items = await _activityService.GetActividadAsync(
                    _userSessionService.CredencialId,
                    cancellationToken);

                foreach (var item in items)
                    ActividadReciente.Add(item);

                _isActividadEmpty = ActividadReciente.Count == 0;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error al cargar actividad reciente", ex,
                    "DashboardViewModel", "LoadActividadAsync", "Sistema", "DashboardPage");
            }
            finally
            {
                IsActividadLoading = false;
                OnPropertyChanged(nameof(IsActividadEmpty));
            }
        }

        public async Task LoadOperacionesPendientesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                IsTareasLoading = true;
                OperacionesPendientes.Clear();
                _isTareasEmpty = true;

                if (!_userSessionService.IsLoaded)
                    await _userSessionService.LoadAsync(cancellationToken);

                if (_userSessionService.CredencialId <= 0) return;

                var query = new OperacionQueryDto { IdAtiende = _userSessionService.CredencialId };
                var operaciones = await _operacionService.GetOperacionesAsync(query, cancellationToken);

                foreach (var op in operaciones)
                {
                    var idOp = op.IdOperacion ?? 0;
                    if (idOp == 0) continue;

                    var check = await _checkService.GetAsync(idOp);
                    if (check == null || check.Completo) continue;

                    var item = new OperacionTodoItem
                    {
                        IdOperacion = idOp,
                        Nota        = op.Nota ?? $"#{idOp}",
                        RazonSocial = op.RazonSocial ?? string.Empty,
                        Pasos = new List<CheckPasoItem>
                        {
                            new() { Nombre = "Cotización generada",    Completado = check.CotizacionGenerada },
                            new() { Nombre = "Cotización enviada",     Completado = check.CotizacionEnviada  },
                            new() { Nombre = "Reporte generado",       Completado = check.ReporteGenerado    },
                            new() { Nombre = "Reporte enviado",        Completado = check.ReporteEnviado     },
                            new() { Nombre = "Prefactura cargada",     Completado = check.PrefacturaCargada  },
                            new() { Nombre = "Hoja de servicio",       Completado = check.HojaServicioCargada},
                            new() { Nombre = "Orden de compra",        Completado = check.OrdenCompraCargada },
                            new() { Nombre = "Factura cargada",        Completado = check.FacturaCargada     },
                        }
                    };

                    OperacionesPendientes.Add(item);
                }

                _isTareasEmpty = OperacionesPendientes.Count == 0;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error al cargar tareas pendientes de operaciones", ex,
                    "DashboardViewModel", "LoadOperacionesPendientesAsync", "Sistema", "DashboardPage");
            }
            finally
            {
                IsTareasLoading = false;
                OnPropertyChanged(nameof(IsTareasEmpty));
            }
        }

        private static string ObtenerIniciales(string nombreCompleto)
        {
            if (string.IsNullOrWhiteSpace(nombreCompleto)) return "?";
            var partes = nombreCompleto.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return partes.Length switch
            {
                0 => "?",
                1 => partes[0][0].ToString().ToUpperInvariant(),
                _ => $"{partes[0][0]}{partes[^1][0]}".ToUpperInvariant()
            };
        }

        private static string ObtenerSaludo()
        {
            var hora = DateTime.Now.Hour;
            return hora switch
            {
                >= 6 and < 12 => "Buenos días",
                >= 12 and < 19 => "Buenas tardes",
                _ => "Buenas noches"
            };
        }
    }
}
