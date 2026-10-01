using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.EstadoCuenta;
using Advance_Control.Services.Facturas;
using Advance_Control.Services.Notificacion;
using Advance_Control.Utilities;

namespace Advance_Control.ViewModels
{
    /// <summary>
    /// ViewModel de Conciliación (ConciliacionPage): facturas y movimientos pendientes, reglas de
    /// los pasos automáticos, "Abonar" con la selección y deshacer desde la bitácora.
    /// </summary>
    public class ConciliacionViewModel : ViewModelBase
    {
        private static readonly CultureInfo CulturaMx = new("es-MX");

        private readonly IFacturaService _facturaService;
        private readonly IEstadoCuentaXmlService _estadoCuentaXmlService;
        private readonly INotificacionService _notificacionService;
        private ObservableCollection<ConciliacionFacturaFila> _facturasPendientes = new();
        private ObservableCollection<ConciliacionMovimientoFila> _movimientosPendientes = new();
        private IReadOnlyList<ConciliacionFacturaFila> _facturasSeleccionadas = Array.Empty<ConciliacionFacturaFila>();
        private IReadOnlyList<ConciliacionMovimientoFila> _movimientosSeleccionados = Array.Empty<ConciliacionMovimientoFila>();
        private bool _isLoading;
        private bool _isConciliacionEnProceso;
        private string? _errorMessage;
        private bool _aplicarReglaPueMismoMes = true;
        private bool _aplicarReglaPpdSiguienteMes;
        private bool _usarRfcComoRegla;
        private int _operacionesConciliacionPendientes;

        public ConciliacionViewModel(
            IFacturaService facturaService,
            IEstadoCuentaXmlService estadoCuentaXmlService,
            INotificacionService notificacionService)
        {
            _facturaService = facturaService ?? throw new ArgumentNullException(nameof(facturaService));
            _estadoCuentaXmlService = estadoCuentaXmlService ?? throw new ArgumentNullException(nameof(estadoCuentaXmlService));
            _notificacionService = notificacionService ?? throw new ArgumentNullException(nameof(notificacionService));
        }

        // ---------------- Datos ----------------

        public ObservableCollection<ConciliacionFacturaFila> FacturasPendientes
        {
            get => _facturasPendientes;
            set => SetProperty(ref _facturasPendientes, value);
        }

        public ObservableCollection<ConciliacionMovimientoFila> MovimientosPendientes
        {
            get => _movimientosPendientes;
            set => SetProperty(ref _movimientosPendientes, value);
        }

        // ---------------- Estado ----------------

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (SetProperty(ref _isLoading, value))
                {
                    NotificarEstadoAcciones();
                }
            }
        }

        /// <summary>Hay una ventana de conciliación automática abierta.</summary>
        public bool IsConciliacionEnProceso
        {
            get => _isConciliacionEnProceso;
            set
            {
                if (SetProperty(ref _isConciliacionEnProceso, value))
                {
                    NotificarEstadoAcciones();
                }
            }
        }

        public string? ErrorMessage
        {
            get => _errorMessage;
            set
            {
                if (SetProperty(ref _errorMessage, value))
                {
                    OnPropertyChanged(nameof(HayError));
                }
            }
        }

        public bool HayError
        {
            get => !string.IsNullOrWhiteSpace(ErrorMessage);
            set
            {
                // El InfoBar lo pone en false al cerrarlo.
                if (!value)
                {
                    ErrorMessage = null;
                }
            }
        }

        // ---------------- Reglas (toggles de la cinta) ----------------

        public bool AplicarReglaPueMismoMes
        {
            get => _aplicarReglaPueMismoMes;
            set => SetProperty(ref _aplicarReglaPueMismoMes, value);
        }

        public bool AplicarReglaPpdSiguienteMes
        {
            get => _aplicarReglaPpdSiguienteMes;
            set => SetProperty(ref _aplicarReglaPpdSiguienteMes, value);
        }

        public bool UsarRfcComoRegla
        {
            get => _usarRfcComoRegla;
            set => SetProperty(ref _usarRfcComoRegla, value);
        }

        // ---------------- Bitácora (Revertir) ----------------

        /// <summary>Operaciones registradas en bitacora_conciliacion que se pueden deshacer.</summary>
        public int OperacionesConciliacionPendientes
        {
            get => _operacionesConciliacionPendientes;
            private set
            {
                if (SetProperty(ref _operacionesConciliacionPendientes, value))
                {
                    NotificarEstadoAcciones();
                }
            }
        }

        public bool PuedeOperar => !IsLoading && !IsConciliacionEnProceso;
        public bool CanDeshacer => PuedeOperar && OperacionesConciliacionPendientes > 0;

        public string DeshacerTooltip => OperacionesConciliacionPendientes > 0
            ? $"{OperacionesConciliacionPendientes} operación(es) de conciliación registradas en la bitácora."
            : "No hay operaciones de conciliación por deshacer.";

        // ---------------- Selección (Acciones) ----------------

        public IReadOnlyList<ConciliacionFacturaFila> FacturasSeleccionadas => _facturasSeleccionadas;
        public IReadOnlyList<ConciliacionMovimientoFila> MovimientosSeleccionados => _movimientosSeleccionados;

        public bool HaySeleccion => _facturasSeleccionadas.Count > 0 || _movimientosSeleccionados.Count > 0;
        public bool CanAbonar => PuedeOperar && _facturasSeleccionadas.Count > 0 && _movimientosSeleccionados.Count > 0;
        public bool CanLimpiar => HaySeleccion;

        /// <summary>Resumen de lo seleccionado para el tooltip de "Abonar".</summary>
        public string ResumenSeleccion
        {
            get
            {
                if (!HaySeleccion)
                {
                    return "Selecciona al menos una factura y un movimiento.";
                }

                var saldoFacturas = _facturasSeleccionadas.Sum(fila => fila.SaldoPendiente);
                var disponibleMovimientos = _movimientosSeleccionados.Sum(fila => fila.MontoRestante);
                var diferencia = disponibleMovimientos - saldoFacturas;

                return $"{_facturasSeleccionadas.Count} factura(s): {saldoFacturas.ToString("C2", CulturaMx)} por cobrar"
                    + Environment.NewLine
                    + $"{_movimientosSeleccionados.Count} movimiento(s): {disponibleMovimientos.ToString("C2", CulturaMx)} disponible"
                    + Environment.NewLine
                    + $"Diferencia: {diferencia.ToString("C2", CulturaMx)}";
            }
        }

        /// <summary>La página avisa qué renglones están seleccionados (fijados) en cada tabla.</summary>
        public void ActualizarSeleccion(
            IEnumerable<ConciliacionFacturaFila> facturas,
            IEnumerable<ConciliacionMovimientoFila> movimientos)
        {
            _facturasSeleccionadas = facturas.ToList();
            _movimientosSeleccionados = movimientos.ToList();
            OnPropertyChanged(nameof(FacturasSeleccionadas));
            OnPropertyChanged(nameof(MovimientosSeleccionados));
            OnPropertyChanged(nameof(HaySeleccion));
            OnPropertyChanged(nameof(CanAbonar));
            OnPropertyChanged(nameof(CanLimpiar));
            OnPropertyChanged(nameof(ResumenSeleccion));
        }

        private void NotificarEstadoAcciones()
        {
            OnPropertyChanged(nameof(PuedeOperar));
            OnPropertyChanged(nameof(CanDeshacer));
            OnPropertyChanged(nameof(DeshacerTooltip));
            OnPropertyChanged(nameof(CanAbonar));
        }

        // ---------------- Carga ----------------

        /// <summary>Carga facturas, movimientos y el contador de la bitácora en paralelo.</summary>
        public async Task CargarDatosAsync()
        {
            IsLoading = true;
            ErrorMessage = null;
            try
            {
                await Task.WhenAll(
                    CargarFacturasPendientesAsync(),
                    CargarMovimientosPendientesAsync(),
                    ActualizarOperacionesPendientesAsync());
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ActualizarOperacionesPendientesAsync()
        {
            try
            {
                var resultado = await _facturaService.InicializarBitacoraConciliacionAsync();
                if (resultado.Success)
                {
                    OperacionesConciliacionPendientes = resultado.OperacionesPendientes;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"No fue posible consultar la bitácora de conciliación: {ex.Message}";
            }
        }

        /// <summary>
        /// Carga las facturas pendientes con la misma regla que Conciliación
        /// (no finiquitadas y con total mayor a cero). La razón social es la del receptor
        /// del CFDI.
        /// </summary>
        private async Task CargarFacturasPendientesAsync()
        {
            try
            {
                var filas = (await _facturaService.ObtenerFacturasAsync())
                    .Where(factura => factura.Finiquito != true)
                    .Where(factura => decimal.Round(factura.Total, 2) > 0)
                    .OrderBy(factura => factura.Fecha)
                    .ThenBy(factura => factura.IdFactura)
                    .Select((factura, indice) => new ConciliacionFacturaFila
                    {
                        IdFactura = factura.IdFactura,
                        Orden = indice,
                        Folio = factura.FolioTitulo,
                        Fecha = factura.Fecha,
                        Total = factura.Total,
                        RazonSocial = factura.ReceptorNombre?.Trim() ?? string.Empty,
                        Rfc = factura.ReceptorRfc?.Trim() ?? string.Empty,
                        Factura = factura
                    });

                FacturasPendientes = new ObservableCollection<ConciliacionFacturaFila>(filas);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"No fue posible cargar las facturas pendientes: {ex.Message}";
            }
        }

        /// <summary>
        /// Carga los abonos no conciliados de todos los estados de cuenta, con la misma regla que
        /// Conciliación (grupo no conciliado con monto restante mayor a cero: un abono aplicado en
        /// parte sigue disponible por lo que le queda). Un estado de cuenta cuyo detalle falle se
        /// omite y se sigue con el resto. En cheques (DEPOSITO_SBC, sin RFC) la columna RFC muestra
        /// el folio del cheque del metadato FOLIO_CHEQUE; el campo referencia de esos movimientos
        /// es la sucursal, no el cheque.
        /// </summary>
        private async Task CargarMovimientosPendientesAsync()
        {
            try
            {
                var estados = await _estadoCuentaXmlService.ObtenerEstadosCuentaAsync();
                var detalles = await Task.WhenAll(estados.Select(estado => CargarDetalleSeguroAsync(estado.IdEstadoCuenta)));

                var filas = detalles
                    .Where(detalle => detalle?.EstadoCuenta != null)
                    .SelectMany(detalle => detalle!.Grupos
                        .Where(grupo => !grupo.Conciliado && grupo.MontoRestante > 0)
                        .Select(grupo => new ConciliacionMovimientoFila
                        {
                            IdMovimiento = grupo.IdMovimiento,
                            Abono = grupo.Abono,
                            MontoRestante = grupo.MontoRestante,
                            Metadatos = grupo.MetadatosResumen,
                            MetadatosTooltip = FormatearMetadatosPorLinea(grupo),
                            RfcReferencia = ObtenerRfcOReferenciaCheque(grupo),
                            Banco = ObtenerBancoEmisor(grupo),
                            Fecha = grupo.Fecha,
                            Grupo = grupo,
                            EstadoCuenta = detalle.EstadoCuenta!
                        }))
                    .OrderBy(fila => fila.Fecha)
                    .ThenBy(fila => fila.IdMovimiento)
                    .Select((fila, indice) =>
                    {
                        fila.Orden = indice;
                        return fila;
                    });

                MovimientosPendientes = new ObservableCollection<ConciliacionMovimientoFila>(filas);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"No fue posible cargar los movimientos pendientes: {ex.Message}";
            }
        }

        // ---------------- Abonar ----------------

        /// <summary>
        /// Arma los abonos a registrar con la selección actual. Casos válidos:
        /// 1 movimiento → 1 o varias facturas (se reparte el disponible del movimiento entre las
        /// facturas, de la más antigua a la más nueva) y varios movimientos → 1 factura (se aplican
        /// los movimientos del más antiguo al más nuevo hasta liquidarla). Cada abono es por lo
        /// que alcance: el menor entre lo disponible del movimiento y el saldo de la factura, igual
        /// que en Conciliación. Devuelve null y el motivo si la selección no es válida.
        /// </summary>
        public IReadOnlyList<ConciliacionAbonoPlaneado>? PlanearAbonos(out string? motivo)
        {
            motivo = null;
            var facturas = _facturasSeleccionadas.OrderBy(fila => fila.Fecha).ThenBy(fila => fila.IdFactura).ToList();
            var movimientos = _movimientosSeleccionados.OrderBy(fila => fila.Fecha).ThenBy(fila => fila.IdMovimiento).ToList();

            if (facturas.Count == 0 || movimientos.Count == 0)
            {
                motivo = "Selecciona al menos una factura y un movimiento.";
                return null;
            }

            if (facturas.Count > 1 && movimientos.Count > 1)
            {
                motivo = "Selecciona un movimiento con una o varias facturas, o una factura con varios movimientos.";
                return null;
            }

            var saldoPorFactura = facturas.ToDictionary(fila => fila, fila => decimal.Round(fila.SaldoPendiente, 2));
            var disponiblePorMovimiento = movimientos.ToDictionary(fila => fila, fila => decimal.Round(fila.MontoRestante, 2));
            var plan = new List<ConciliacionAbonoPlaneado>();

            foreach (var movimiento in movimientos)
            {
                foreach (var factura in facturas)
                {
                    var monto = Math.Min(disponiblePorMovimiento[movimiento], saldoPorFactura[factura]);
                    if (monto <= 0)
                    {
                        continue;
                    }

                    plan.Add(new ConciliacionAbonoPlaneado(factura, movimiento, monto));
                    disponiblePorMovimiento[movimiento] -= monto;
                    saldoPorFactura[factura] -= monto;
                }
            }

            if (plan.Count == 0)
            {
                motivo = "Las facturas seleccionadas no tienen saldo o los movimientos no tienen monto disponible.";
                return null;
            }

            return plan;
        }

        /// <summary>Texto de confirmación: cada abono y lo que queda sin aplicar o sin cubrir.</summary>
        public string DescribirPlan(IReadOnlyList<ConciliacionAbonoPlaneado> plan)
        {
            var lineas = plan
                .Select(abono => $"• Factura {abono.Factura.Folio} ← movimiento del {abono.Movimiento.FechaTexto} ({abono.Movimiento.AbonoTexto}): {abono.MontoTexto}")
                .ToList();

            var aplicadoPorMovimiento = plan.GroupBy(abono => abono.Movimiento).ToDictionary(g => g.Key, g => g.Sum(abono => abono.Monto));
            var aplicadoPorFactura = plan.GroupBy(abono => abono.Factura).ToDictionary(g => g.Key, g => g.Sum(abono => abono.Monto));

            foreach (var movimiento in _movimientosSeleccionados)
            {
                var sobrante = movimiento.MontoRestante - aplicadoPorMovimiento.GetValueOrDefault(movimiento);
                if (sobrante > 0)
                {
                    lineas.Add($"El movimiento del {movimiento.FechaTexto} queda con {sobrante.ToString("C2", CulturaMx)} disponible (no se concilia).");
                }
            }

            foreach (var factura in _facturasSeleccionadas)
            {
                var pendiente = factura.SaldoPendiente - aplicadoPorFactura.GetValueOrDefault(factura);
                if (pendiente > 0)
                {
                    lineas.Add($"La factura {factura.Folio} queda con {pendiente.ToString("C2", CulturaMx)} por cobrar.");
                }
            }

            return string.Join(Environment.NewLine, lineas);
        }

        /// <summary>
        /// Registra los abonos uno por uno (mismo endpoint y bitácora "manual" que Conciliación).
        /// Si uno falla se detiene y se informa cuántos sí se registraron. Al final recarga.
        /// </summary>
        public async Task AbonarAsync(IReadOnlyList<ConciliacionAbonoPlaneado> plan)
        {
            var registrados = 0;
            IsLoading = true;
            ErrorMessage = null;
            try
            {
                foreach (var abono in plan)
                {
                    var movimiento = abono.Movimiento.Grupo;
                    var resultado = await _facturaService.RegistrarAbonoAsync(new RegistrarAbonoFacturaRequestDto
                    {
                        IdFactura = abono.Factura.IdFactura,
                        IdMovimiento = abono.Movimiento.IdMovimiento,
                        FechaAbono = abono.Movimiento.Fecha,
                        MontoAbono = abono.Monto,
                        Referencia = movimiento.Referencia,
                        Observaciones = $"Abono generado desde conciliacion con movimiento {movimiento.GrupoId}.",
                        RegistrarEnBitacoraConciliacion = true,
                        TipoOperacionBitacoraConciliacion = "manual"
                    });

                    if (!resultado.Success)
                    {
                        var detalle = string.IsNullOrWhiteSpace(resultado.Message) ? "No se pudo registrar el abono." : resultado.Message;
                        ErrorMessage = registrados == 0
                            ? detalle
                            : $"Se registraron {registrados} de {plan.Count} abonos. Falló la factura {abono.Factura.Folio}: {detalle}";
                        break;
                    }

                    registrados++;
                    OperacionesConciliacionPendientes = resultado.OperacionesConciliacionPendientes;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Se registraron {registrados} de {plan.Count} abonos. Error: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }

            if (registrados > 0)
            {
                await CargarDatosAsync();
                if (registrados == plan.Count)
                {
                    await _notificacionService.MostrarAsync("Conciliación", $"{registrados} abono(s) registrado(s).");
                }
            }
        }

        // ---------------- Revertir ----------------

        public Task DeshacerUltimoAsync() => DeshacerAsync(
            () => _facturaService.DeshacerUltimaOperacionConciliacionAsync(),
            "Se deshizo la última operación de conciliación.");

        public Task DeshacerTodoAsync() => DeshacerAsync(
            () => _facturaService.DeshacerTodasOperacionesConciliacionAsync(),
            "Se deshicieron todas las operaciones de conciliación.");

        private async Task DeshacerAsync(Func<Task<BitacoraConciliacionResponseDto>> accion, string mensajeExito)
        {
            BitacoraConciliacionResponseDto resultado;
            IsLoading = true;
            ErrorMessage = null;
            try
            {
                resultado = await accion();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"No fue posible deshacer: {ex.Message}";
                return;
            }
            finally
            {
                IsLoading = false;
            }

            if (!resultado.Success)
            {
                ErrorMessage = string.IsNullOrWhiteSpace(resultado.Message) ? "No fue posible deshacer." : resultado.Message;
                return;
            }

            OperacionesConciliacionPendientes = resultado.OperacionesPendientes;
            await CargarDatosAsync();
            await _notificacionService.MostrarAsync("Conciliación", string.IsNullOrWhiteSpace(resultado.Message) ? mensajeExito : resultado.Message);
        }

        // ---------------- Auxiliares de carga ----------------

        /// <summary>Metadatos "CLAVE: valor", uno por línea; vacío si el movimiento no trae.</summary>
        public static string FormatearMetadatosPorLinea(EstadoCuentaGrupoDetalleDto grupo) =>
            string.Join(Environment.NewLine, grupo.Metadatos
                .Where(par => !string.IsNullOrWhiteSpace(par.Value))
                .Select(par => $"{par.Key.Replace("_", " ")}: {par.Value!.Trim()}"));

        private async Task<EstadoCuentaDetalleDto?> CargarDetalleSeguroAsync(int idEstadoCuenta)
        {
            try
            {
                return await _estadoCuentaXmlService.ObtenerDetalleEstadoCuentaAsync(idEstadoCuenta);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Banco del que salió el pago (no el del estado de cuenta, que es el nuestro). Solo los
        /// SPEI recibidos lo traen: se deduce de la CLABE del emisor (metadato CUENTA_EMISOR, o
        /// CUENTA en estados viejos). Cheques, órdenes de pago y compensaciones no traen dato de
        /// banco emisor y quedan en blanco.
        /// </summary>
        private static string ObtenerBancoEmisor(EstadoCuentaGrupoDetalleDto grupo)
        {
            var clabe = grupo.Metadatos.TryGetValue("CUENTA_EMISOR", out var cuentaEmisor) && !string.IsNullOrWhiteSpace(cuentaEmisor)
                ? cuentaEmisor
                : grupo.Metadatos.TryGetValue("CUENTA", out var cuenta) ? cuenta : null;

            return BancosMexico.ObtenerNombrePorClabe(clabe);
        }

        private static string ObtenerRfcOReferenciaCheque(EstadoCuentaGrupoDetalleDto grupo)
        {
            if (string.Equals(grupo.TipoOperacion, "DEPOSITO_SBC", StringComparison.OrdinalIgnoreCase))
            {
                return grupo.Metadatos.TryGetValue("FOLIO_CHEQUE", out var folioCheque) && !string.IsNullOrWhiteSpace(folioCheque)
                    ? folioCheque.Trim()
                    : grupo.Referencia?.Trim() ?? string.Empty;
            }

            return (grupo.RfcEmisor
                ?? grupo.MovimientosRelacionados
                    .Select(relacionado => relacionado.Rfc)
                    .FirstOrDefault(rfc => !string.IsNullOrWhiteSpace(rfc)))?.Trim()
                ?? string.Empty;
        }
    }
}
