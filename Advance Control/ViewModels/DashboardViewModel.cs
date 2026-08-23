using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Services.Facturas;
using Advance_Control.Services.Logging;
using Advance_Control.Services.Operaciones;
using Advance_Control.Services.RelacionUsuarioArea;
using Advance_Control.Services.Session;
using Advance_Control.Utilities;

namespace Advance_Control.ViewModels
{
    /// <summary>
    /// ViewModel para la página de inicio (Dashboard).
    /// Muestra bienvenida personalizada y el grid "Ops" con las operaciones/facturas
    /// pendientes de resolver, filtradas según el alcance del usuario logueado.
    /// </summary>
    public class DashboardViewModel : ViewModelBase
    {
        private readonly IUserSessionService _userSessionService;
        private readonly ILoggingService _logger;
        private readonly IOperacionService _operacionService;
        private readonly IFacturaService _facturaService;
        private readonly IRelacionUsuarioAreaService _relacionUsuarioAreaService;

        private string _saludo = "Bienvenido";
        private string _nombreUsuario = string.Empty;
        private string _fechaHoy = string.Empty;
        private bool _isLoading;
        private int _opsAbiertasPendientesCount;
        private int _opsTFinalizadoPendientesCount;
        private int _opsFinalizadasPendientesCount;
        private bool _isOpsLoading;
        private bool _showOps;

        public DashboardViewModel(
            IUserSessionService userSessionService,
            ILoggingService logger,
            IOperacionService operacionService,
            IFacturaService facturaService,
            IRelacionUsuarioAreaService relacionUsuarioAreaService)
        {
            _userSessionService = userSessionService ?? throw new ArgumentNullException(nameof(userSessionService));
            _logger             = logger             ?? throw new ArgumentNullException(nameof(logger));
            _operacionService   = operacionService   ?? throw new ArgumentNullException(nameof(operacionService));
            _facturaService     = facturaService     ?? throw new ArgumentNullException(nameof(facturaService));
            _relacionUsuarioAreaService = relacionUsuarioAreaService ?? throw new ArgumentNullException(nameof(relacionUsuarioAreaService));
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

        // ════════════════════════ OPS (pendientes por resolver) ════════════════════════

        /// <summary>Operaciones que no se han marcado como t-Finalizado.</summary>
        public int OpsAbiertasPendientesCount
        {
            get => _opsAbiertasPendientesCount;
            set => SetProperty(ref _opsAbiertasPendientesCount, value);
        }

        /// <summary>Operaciones t-Finalizado que todavía no se facturan.</summary>
        public int OpsTFinalizadoPendientesCount
        {
            get => _opsTFinalizadoPendientesCount;
            set => SetProperty(ref _opsTFinalizadoPendientesCount, value);
        }

        /// <summary>Operaciones facturadas cuya factura todavía no se cobra por completo.</summary>
        public int OpsFinalizadasPendientesCount
        {
            get => _opsFinalizadasPendientesCount;
            set => SetProperty(ref _opsFinalizadasPendientesCount, value);
        }

        public bool IsOpsLoading
        {
            get => _isOpsLoading;
            set => SetProperty(ref _isOpsLoading, value);
        }

        /// <summary>Controla la visibilidad del grid "Ops". Nivel sin acceso (Senior Cliente) no lo ve.</summary>
        public bool ShowOps
        {
            get => _showOps;
            set => SetProperty(ref _showOps, value);
        }

        public async Task LoadAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                IsLoading = true;

                if (!_userSessionService.IsLoaded)
                    await _userSessionService.LoadAsync(cancellationToken);

                NombreUsuario = _userSessionService.NombreCompleto ?? string.Empty;
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

            await LoadOpsAsync(cancellationToken);
        }

        /// <summary>
        /// Carga las 3 cards del grid "Ops": abiertas (no t-Finalizado), t-Finalizado sin
        /// facturar, y facturadas sin cobrar -- cada una filtrada según el alcance del usuario
        /// (ver AlcanceOperacionesResolver: todo / solo su área / solo lo atendido por él).
        /// </summary>
        public async Task LoadOpsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                IsOpsLoading = true;

                if (!_userSessionService.IsLoaded)
                    await _userSessionService.LoadAsync(cancellationToken);

                var alcance = AlcanceOperacionesResolver.Resolver(_userSessionService.Nivel);
                ShowOps = alcance != AlcanceOperaciones.SinAcceso;
                if (!ShowOps)
                {
                    OpsAbiertasPendientesCount = 0;
                    OpsTFinalizadoPendientesCount = 0;
                    OpsFinalizadasPendientesCount = 0;
                    return;
                }

                HashSet<string>? equiposEnArea = null;
                if (alcance == AlcanceOperaciones.PorArea)
                {
                    var identificadores = await _relacionUsuarioAreaService.GetEquiposEnAreasAsync(
                        _userSessionService.CredencialId, cancellationToken);
                    equiposEnArea = new HashSet<string>(identificadores, StringComparer.OrdinalIgnoreCase);
                }

                // --- Cards 1 y 2: abiertas + t-Finalizado sin facturar (GetOperacionesAsync sin IncluirFinalizadas
                //     ya excluye lo cerrado/facturado, pero t-Finalizado sin facturar sigue teniendo FechaFinal nula) ---
                var query = new Advance_Control.Models.OperacionQueryDto
                {
                    IdAtiende = alcance == AlcanceOperaciones.PorTecnico ? _userSessionService.CredencialId : 0
                };
                var operaciones = await _operacionService.GetOperacionesAsync(query, cancellationToken);

                if (equiposEnArea != null)
                {
                    operaciones = operaciones
                        .Where(o => !string.IsNullOrEmpty(o.Identificador) && equiposEnArea.Contains(o.Identificador))
                        .ToList();
                }

                OpsAbiertasPendientesCount = operaciones.Count(o => !o.TFinalizado);
                OpsTFinalizadoPendientesCount = operaciones.Count(o => o.TFinalizado);

                // --- Card 3: facturadas con saldo pendiente de cobro ---
                var facturadas = await _facturaService.ObtenerOperacionesFacturadasAsync(cancellationToken);
                var pendientesCobro = facturadas.Where(f => f.Total > f.TotalAbonado);

                pendientesCobro = alcance switch
                {
                    AlcanceOperaciones.PorTecnico => pendientesCobro.Where(f => f.IdAtiende == _userSessionService.CredencialId),
                    AlcanceOperaciones.PorArea => pendientesCobro.Where(f => !string.IsNullOrEmpty(f.Identificador) && equiposEnArea!.Contains(f.Identificador)),
                    _ => pendientesCobro
                };

                OpsFinalizadasPendientesCount = pendientesCobro.Count();
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error al cargar Ops del dashboard", ex,
                    "DashboardViewModel", "LoadOpsAsync", "Sistema", "DashboardPage");
            }
            finally
            {
                IsOpsLoading = false;
            }
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
