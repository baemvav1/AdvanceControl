using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.Conciliacion;
using Advance_Control.Services.EstadoCuenta;
using Advance_Control.Services.Facturas;
using Advance_Control.Services.Notificacion;
using Advance_Control.Utilities;
using Advance_Control.Views.Windows;

namespace Advance_Control.ViewModels
{
    public class ConciliacionViewModel : ViewModelBase
    {
        private readonly IEstadoCuentaXmlService _estadoCuentaXmlService;
        private readonly IFacturaService _facturaService;
        private readonly INotificacionService _notificacionService;
        private readonly ConciliacionMatchingEngine _conciliacionMatchingEngine;
        private readonly List<ConciliacionMovimientoResumenDto> _movimientosPendientesBase;
        private readonly List<FacturaResumenDto> _facturasPendientesBase;
        private ObservableCollection<ConciliacionMovimientoResumenDto> _movimientosPendientes;
        private ObservableCollection<FacturaResumenDto> _facturasPendientes;
        private ConciliacionMovimientoResumenDto? _movimientoCargado;
        private FacturaResumenDto? _facturaCargada;
        private bool _isLoading;
        private bool _isConciliacionAutomaticaEnProceso;
        private bool _bitacoraConciliacionInicializada;
        private int _operacionesConciliacionPendientes;
        private string? _errorMessage;
        private string? _successMessage;
        private bool _aplicarReglaPueMismoMes = true;
        private bool _usarRfcComoRegla = false;
        private string? _movimientoMetadatoBusquedaTexto;
        private string? _movimientoAbonoBusquedaTexto;
        private DateTimeOffset? _movimientoFechaInicio;
        private DateTimeOffset? _movimientoFechaFin;
        private string? _facturaFolioBusquedaTexto;
        private string? _facturaTotalBusqueda;
        private string? _facturaNombreBusquedaTexto;
        private string? _facturaRfcBusquedaTexto;

        public ConciliacionViewModel(
            IEstadoCuentaXmlService estadoCuentaXmlService,
            IFacturaService facturaService,
            INotificacionService notificacionService,
            ConciliacionMatchingEngine conciliacionMatchingEngine)
        {
            _estadoCuentaXmlService = estadoCuentaXmlService ?? throw new ArgumentNullException(nameof(estadoCuentaXmlService));
            _facturaService = facturaService ?? throw new ArgumentNullException(nameof(facturaService));
            _notificacionService = notificacionService ?? throw new ArgumentNullException(nameof(notificacionService));
            _conciliacionMatchingEngine = conciliacionMatchingEngine ?? throw new ArgumentNullException(nameof(conciliacionMatchingEngine));
            _movimientosPendientesBase = new List<ConciliacionMovimientoResumenDto>();
            _facturasPendientesBase = new List<FacturaResumenDto>();
            _movimientosPendientes = new ObservableCollection<ConciliacionMovimientoResumenDto>();
            _facturasPendientes = new ObservableCollection<FacturaResumenDto>();
            FacturaCargadaConceptos = new ObservableCollection<FacturaConceptoDto>();
            FacturaCargadaAbonos = new ObservableCollection<AbonoFacturaDto>();
        }

        public ObservableCollection<ConciliacionMovimientoResumenDto> MovimientosPendientes
        {
            get => _movimientosPendientes;
            set => SetProperty(ref _movimientosPendientes, value);
        }

        public ObservableCollection<FacturaResumenDto> FacturasPendientes
        {
            get => _facturasPendientes;
            set => SetProperty(ref _facturasPendientes, value);
        }

        public FacturaResumenDto? FacturaCargada
        {
            get => _facturaCargada;
            private set
            {
                if (SetProperty(ref _facturaCargada, value))
                {
                    NotificarCambioFacturaCargada();
                }
            }
        }

        public ConciliacionMovimientoResumenDto? MovimientoCargado
        {
            get => _movimientoCargado;
            private set
            {
                if (SetProperty(ref _movimientoCargado, value))
                {
                    NotificarCambioMovimientoCargado();
                }
            }
        }

        public ObservableCollection<FacturaConceptoDto> FacturaCargadaConceptos { get; }
        public ObservableCollection<AbonoFacturaDto> FacturaCargadaAbonos { get; }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (SetProperty(ref _isLoading, value))
                {
                    OnPropertyChanged(nameof(CanAbonarMovimiento));
                    OnPropertyChanged(nameof(CanIniciarConciliacionAutomatica));
                    OnPropertyChanged(nameof(CanDeshacerUltimaOperacionConciliacion));
                    OnPropertyChanged(nameof(CanDeshacerTodasOperacionesConciliacion));
                }
            }
        }

        public string? ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public string? SuccessMessage
        {
            get => _successMessage;
            set => SetProperty(ref _successMessage, value);
        }

        public bool AplicarReglaPueMismoMes
        {
            get => _aplicarReglaPueMismoMes;
            set => SetProperty(ref _aplicarReglaPueMismoMes, value);
        }

        public bool UsarRfcComoRegla
        {
            get => _usarRfcComoRegla;
            set => SetProperty(ref _usarRfcComoRegla, value);
        }

        public string? MovimientoMetadatoBusquedaTexto
        {
            get => _movimientoMetadatoBusquedaTexto;
            set
            {
                if (SetProperty(ref _movimientoMetadatoBusquedaTexto, value))
                {
                    AplicarFiltrosVisibles();
                }
            }
        }

        public string? MovimientoAbonoBusquedaTexto
        {
            get => _movimientoAbonoBusquedaTexto;
            set
            {
                if (SetProperty(ref _movimientoAbonoBusquedaTexto, value))
                {
                    AplicarFiltrosVisibles();
                }
            }
        }

        public string? FacturaFolioBusquedaTexto
        {
            get => _facturaFolioBusquedaTexto;
            set
            {
                if (SetProperty(ref _facturaFolioBusquedaTexto, value))
                {
                    AplicarFiltrosVisibles();
                }
            }
        }

        public DateTimeOffset? MovimientoFechaInicio
        {
            get => _movimientoFechaInicio;
            set
            {
                if (SetProperty(ref _movimientoFechaInicio, value))
                {
                    AplicarFiltrosVisibles();
                }
            }
        }

        public DateTimeOffset? MovimientoFechaFin
        {
            get => _movimientoFechaFin;
            set
            {
                if (SetProperty(ref _movimientoFechaFin, value))
                {
                    AplicarFiltrosVisibles();
                }
            }
        }

        public string? FacturaTotalBusqueda
        {
            get => _facturaTotalBusqueda;
            set
            {
                if (SetProperty(ref _facturaTotalBusqueda, value))
                {
                    AplicarFiltrosVisibles();
                }
            }
        }

        public string? FacturaNombreBusquedaTexto
        {
            get => _facturaNombreBusquedaTexto;
            set
            {
                if (SetProperty(ref _facturaNombreBusquedaTexto, value))
                {
                    AplicarFiltrosVisibles();
                }
            }
        }

        public string? FacturaRfcBusquedaTexto
        {
            get => _facturaRfcBusquedaTexto;
            set
            {
                if (SetProperty(ref _facturaRfcBusquedaTexto, value))
                {
                    AplicarFiltrosVisibles();
                }
            }
        }

        public string ResumenMovimientos => ConstruirResumenColeccion(
            MovimientosPendientes.Count,
            _movimientosPendientesBase.Count,
            "",
            "");

        public string ResumenFacturas => ConstruirResumenColeccion(
            FacturasPendientes.Count,
            _facturasPendientesBase.Count,
            "",
            "");
        public string MensajeFacturaCargada => FacturaCargada == null
            ? "Selecciona una factura del panel derecho para ver su detalle."
            : $"Factura cargada: {FacturaCargada.FolioTitulo}";
        public string MensajeMovimientoCargado => MovimientoCargado == null
            ? "Selecciona un movimiento del panel izquierdo para ver su detalle."
            : $"Movimiento cargado: {MovimientoCargado.TipoTitulo}";
        public string FacturaCargadaUuidTexto => FacturaCargada?.UuidTexto ?? "Sin factura seleccionada";
        public string FacturaCargadaFechaTexto => FacturaCargada?.FechaTexto ?? string.Empty;
        public string FacturaCargadaEmisorTexto => FacturaCargada?.EmisorNombre ?? string.Empty;
        public string FacturaCargadaReceptorTexto => FacturaCargada?.ReceptorNombre ?? string.Empty;
        public string FacturaCargadaRfcTexto => FacturaCargada?.RfcTexto ?? string.Empty;
        public string FacturaCargadaMetodoFormaTexto => FacturaCargada?.MetodoFormaPagoTexto ?? string.Empty;
        public string FacturaCargadaTotalesTexto => FacturaCargada?.TotalesTexto ?? string.Empty;
        public string FacturaCargadaEstadoPagoTexto => FacturaCargada?.EstadoPagoTexto ?? "Sin estado";
        public string FacturaCargadaTotalAbonadoTexto => FacturaCargada?.TotalAbonadoTexto ?? "$0.00";
        public string FacturaCargadaSaldoPendienteTexto => FacturaCargada?.SaldoPendienteTexto ?? "$0.00";
        public string ResumenFacturaCargadaConceptos => $"Conceptos ({FacturaCargadaConceptos.Count})";
        public string ResumenFacturaCargadaAbonos => $"Abonos ({FacturaCargadaAbonos.Count})";
        public string MovimientoCargadoCuentaTexto => MovimientoCargado?.CuentaTitulo ?? string.Empty;
        public string MovimientoCargadoBancoTexto => MovimientoCargado?.BancoTitularTexto ?? string.Empty;
        public string MovimientoCargadoPeriodoTexto => MovimientoCargado?.PeriodoTexto ?? string.Empty;
        public string MovimientoCargadoFechaTexto => MovimientoCargado?.FechaTexto ?? string.Empty;
        public string MovimientoCargadoReferenciaTexto => MovimientoCargado?.ReferenciaTexto ?? string.Empty;
        public string MovimientoCargadoCargoTexto => MovimientoCargado?.CargoTexto ?? "-";
        public string MovimientoCargadoAbonoTexto => MovimientoCargado?.AbonoTexto ?? "-";
        public string MovimientoCargadoMontoRestanteTexto => MovimientoCargado?.MontoRestanteTexto ?? "-";
        public string MovimientoCargadoSaldoTexto => MovimientoCargado?.SaldoTexto ?? "$0.00";
        public string MovimientoCargadoRelacionadosTexto => MovimientoCargado?.RelacionadosTexto ?? "Sin relacionados";
        public string MovimientoCargadoMetadatosTexto => MovimientoCargado?.MetadatosTexto ?? "Sin metadatos adicionales.";
        public bool IsConciliacionAutomaticaEnProceso
        {
            get => _isConciliacionAutomaticaEnProceso;
            set
            {
                if (SetProperty(ref _isConciliacionAutomaticaEnProceso, value))
                {
                    OnPropertyChanged(nameof(ConciliacionPanelHabilitado));
                    OnPropertyChanged(nameof(CanIniciarConciliacionAutomatica));
                    OnPropertyChanged(nameof(CanDeshacerUltimaOperacionConciliacion));
                    OnPropertyChanged(nameof(CanDeshacerTodasOperacionesConciliacion));
                    OnPropertyChanged(nameof(OpacidadIndicadorConciliacion));
                }
            }
        }

        public int OperacionesConciliacionPendientes
        {
            get => _operacionesConciliacionPendientes;
            private set
            {
                if (SetProperty(ref _operacionesConciliacionPendientes, value))
                {
                    OnPropertyChanged(nameof(CanDeshacerUltimaOperacionConciliacion));
                    OnPropertyChanged(nameof(CanDeshacerTodasOperacionesConciliacion));
                }
            }
        }

        public bool ConciliacionPanelHabilitado => !IsConciliacionAutomaticaEnProceso;
        public double OpacidadIndicadorConciliacion => IsConciliacionAutomaticaEnProceso ? 1d : 0d;
        public bool CanAbonarMovimiento => !IsLoading
            && FacturaCargada != null
            && MovimientoCargado != null
            && MovimientoCargado.MontoRestante > 0
            && FacturaCargada.SaldoPendiente > 0;
        public bool CanIniciarConciliacionAutomatica => !IsLoading
            && !IsConciliacionAutomaticaEnProceso
            && (_conciliacionMatchingEngine.CanRunUnoAUno(_facturasPendientesBase, _movimientosPendientesBase)
                || _conciliacionMatchingEngine.CanRunCombinacional(_facturasPendientesBase, _movimientosPendientesBase)
                || _conciliacionMatchingEngine.CanRunAbonos(_facturasPendientesBase, _movimientosPendientesBase));
        public bool CanDeshacerUltimaOperacionConciliacion => !IsLoading
            && !IsConciliacionAutomaticaEnProceso
            && OperacionesConciliacionPendientes > 0;
        public bool CanDeshacerTodasOperacionesConciliacion => CanDeshacerUltimaOperacionConciliacion;

        public async Task CargarDatosAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = null;
                SuccessMessage = null;

                await InicializarBitacoraConciliacionSiEsNecesarioAsync();

                var estadosTask = _estadoCuentaXmlService.ObtenerEstadosCuentaAsync();
                var facturasTask = _facturaService.ObtenerFacturasAsync();

                await Task.WhenAll(estadosTask, facturasTask);

                var estados = await estadosTask;
                var facturas = await facturasTask;

                var detalleTasks = estados
                    .OrderBy(estado => estado.FechaCorte)
                    .ThenBy(estado => estado.IdEstadoCuenta)
                    .Select(CargarDetalleEstadoSeguroAsync)
                    .ToList();

                var resultadosDetalle = detalleTasks.Count == 0
                    ? Array.Empty<(EstadoCuentaDetalleDto? Detalle, string? Error)>()
                    : await Task.WhenAll(detalleTasks);

                var detallesValidos = resultadosDetalle
                    .Where(resultado => resultado.Detalle?.EstadoCuenta != null)
                    .Select(resultado => resultado.Detalle!)
                    .ToList();

                var erroresDetalle = resultadosDetalle
                    .Where(resultado => !string.IsNullOrWhiteSpace(resultado.Error))
                    .Select(resultado => resultado.Error!)
                    .ToList();

                var movimientosPendientes = detallesValidos
                    .SelectMany(detalle => detalle.Grupos
                        .Where(grupo => !grupo.Conciliado && grupo.MontoRestante > 0)
                        .Select(grupo => new ConciliacionMovimientoResumenDto
                        {
                            IdEstadoCuenta = detalle.EstadoCuenta!.IdEstadoCuenta,
                            IdMovimiento = grupo.IdMovimiento,
                            NumeroCuenta = detalle.EstadoCuenta.NumeroCuenta,
                            TipoCuenta = detalle.EstadoCuenta.TipoCuenta,
                            Banco = detalle.EstadoCuenta.NombreBanco,
                            Titular = detalle.EstadoCuenta.Titular,
                            GrupoId = grupo.GrupoId,
                            Fecha = grupo.Fecha,
                            TipoOperacion = grupo.TipoOperacion,
                            SubtipoOperacion = grupo.SubtipoOperacion,
                            Descripcion = grupo.Descripcion,
                            Referencia = grupo.Referencia,
                            Cargo = grupo.Cargo,
                            Abono = grupo.Abono,
                            Saldo = grupo.Saldo,
                            MontoRestante = grupo.MontoRestante,
                            RelacionadosCount = grupo.MovimientosRelacionados.Count,
                            RfcEmisor = grupo.RfcEmisor
                                ?? grupo.MovimientosRelacionados
                                .Select(r => r.Rfc)
                                .FirstOrDefault(rfc => !string.IsNullOrWhiteSpace(rfc)),
                            PeriodoTexto = detalle.EstadoCuenta.PeriodoTexto,
                            MetadatosTexto = grupo.MetadatosTexto
                        }))
                    .OrderBy(movimiento => movimiento.Fecha)
                    .ThenBy(movimiento => movimiento.IdMovimiento)
                    .ToList();

                var facturasPendientes = FiltrarFacturasConciliables(facturas);

                _movimientosPendientesBase.Clear();
                _movimientosPendientesBase.AddRange(movimientosPendientes);
                _facturasPendientesBase.Clear();
                _facturasPendientesBase.AddRange(facturasPendientes);
                AplicarFiltrosVisibles();

                if (erroresDetalle.Count > 0)
                {
                    var resumenErrores = erroresDetalle.Count == 1
                        ? erroresDetalle[0]
                        : $"Se omitieron {erroresDetalle.Count} estados de cuenta con error al cargar su detalle.";

                    ErrorMessage = resumenErrores;
                    SuccessMessage = null;
                }

                OnPropertyChanged(nameof(CanIniciarConciliacionAutomatica));
            }
            catch (Exception ex)
            {
                await MostrarErrorConciliacionAsync($"Error al cargar los datos de conciliacion: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task CargarDetalleFacturaAsync(int idFactura, bool autocompletarFiltroAbono = true)
        {
            try
            {
                IsLoading = true;
                ErrorMessage = null;
                SuccessMessage = null;

                var detalle = await _facturaService.ObtenerDetalleFacturaAsync(idFactura);
                if (detalle?.Factura == null)
                {
                    await MostrarErrorConciliacionAsync("No se encontro el detalle de la factura seleccionada.");
                    LimpiarFacturaCargada();
                    return;
                }

                FacturaCargada = detalle.Factura;
                ReemplazarColeccion(FacturaCargadaConceptos, detalle.Conceptos);
                ReemplazarColeccion(FacturaCargadaAbonos, detalle.Abonos);
                MovimientoAbonoBusquedaTexto = autocompletarFiltroAbono
                    ? _conciliacionMatchingEngine
                        .ObtenerMontoPendienteFactura(detalle.Factura)
                        .ToString("0.##", CultureInfo.InvariantCulture)
                    : string.Empty;
                OnPropertyChanged(nameof(ResumenFacturaCargadaConceptos));
                OnPropertyChanged(nameof(ResumenFacturaCargadaAbonos));
            }
            catch (Exception ex)
            {
                await MostrarErrorConciliacionAsync($"Error al cargar el detalle de la factura seleccionada: {ex.Message}");
                LimpiarFacturaCargada();
            }
            finally
            {
                IsLoading = false;
            }
        }

        public void CargarDetalleMovimiento(ConciliacionMovimientoResumenDto movimiento)
        {
            MovimientoCargado = movimiento;
        }

        public void LimpiarMovimientoCargado()
        {
            MovimientoCargado = null;
        }

        public void LimpiarFiltrosMovimientos()
        {
            MovimientoMetadatoBusquedaTexto = null;
            MovimientoAbonoBusquedaTexto = null;
            MovimientoFechaInicio = null;
            MovimientoFechaFin = null;
        }

        public void LimpiarFiltrosFacturas()
        {
            FacturaFolioBusquedaTexto = null;
            FacturaTotalBusqueda = null;
            FacturaNombreBusquedaTexto = null;
            FacturaRfcBusquedaTexto = null;
        }

        public async Task AbonarMovimientoCargadoAsync()
        {
            if (FacturaCargada == null)
            {
                await MostrarErrorConciliacionAsync("Primero selecciona una factura.");
                return;
            }

            if (MovimientoCargado == null)
            {
                await MostrarErrorConciliacionAsync("Primero selecciona un movimiento.");
                return;
            }

            if (MovimientoCargado.MontoRestante <= 0)
            {
                await MostrarErrorConciliacionAsync("El movimiento seleccionado no tiene un abono valido.");
                return;
            }

            var abonoExcedeFactura = MovimientoCargado.MontoRestante > FacturaCargada.SaldoPendiente;
            var montoAbono = abonoExcedeFactura ? FacturaCargada.SaldoPendiente : MovimientoCargado.MontoRestante;

            try
            {
                IsLoading = true;
                ErrorMessage = null;
                SuccessMessage = null;

                var idFactura = FacturaCargada.IdFactura;
                var result = await _facturaService.RegistrarAbonoAsync(new RegistrarAbonoFacturaRequestDto
                {
                    IdFactura = idFactura,
                    IdMovimiento = MovimientoCargado.IdMovimiento,
                    FechaAbono = MovimientoCargado.Fecha,
                    MontoAbono = montoAbono,
                    Referencia = MovimientoCargado.Referencia,
                    Observaciones = $"Abono generado desde conciliacion con movimiento {MovimientoCargado.GrupoId}.",
                    RegistrarEnBitacoraConciliacion = true,
                    TipoOperacionBitacoraConciliacion = "manual"
                });

                if (!result.Success)
                {
                    await MostrarErrorConciliacionAsync(string.IsNullOrWhiteSpace(result.Message)
                        ? "No se pudo registrar el abono."
                        : result.Message);
                    return;
                }

                OperacionesConciliacionPendientes = result.OperacionesConciliacionPendientes;
                var idMovimientoActual = MovimientoCargado?.IdMovimiento;
                await CargarDatosAsync();
                await CargarDetalleFacturaAsync(idFactura, autocompletarFiltroAbono: false);

                // Si fue overpayment el movimiento no queda conciliado: refrescar desde la base actualizada
                // para que MontoRestante refleje el nuevo saldo disponible.
                if (abonoExcedeFactura && idMovimientoActual.HasValue)
                {
                    MovimientoCargado = _movimientosPendientesBase
                        .FirstOrDefault(m => m.IdMovimiento == idMovimientoActual.Value);
                }

                var mensaje = abonoExcedeFactura
                    ? $"Factura pagada correctamente. El movimiento no fue conciliado porque tiene saldo restante disponible para otras facturas."
                    : (string.IsNullOrWhiteSpace(result.Message) ? "Abono registrado correctamente." : result.Message);
                await MostrarExitoConciliacionAsync(mensaje);
            }
            catch (Exception ex)
            {
                await MostrarErrorConciliacionAsync($"Error al registrar el abono desde conciliacion: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task DeshacerUltimaOperacionConciliacionAsync()
        {
            if (!CanDeshacerUltimaOperacionConciliacion)
            {
                await MostrarErrorConciliacionAsync("No hay operaciones de conciliacion pendientes por deshacer.");
                return;
            }

            try
            {
                IsLoading = true;
                ErrorMessage = null;
                SuccessMessage = null;

                var resultado = await _facturaService.DeshacerUltimaOperacionConciliacionAsync();
                if (!resultado.Success)
                {
                    await MostrarErrorConciliacionAsync(string.IsNullOrWhiteSpace(resultado.Message)
                        ? "No fue posible deshacer la ultima operacion de conciliacion."
                        : resultado.Message);
                    return;
                }

                OperacionesConciliacionPendientes = resultado.OperacionesPendientes;
                await CargarDatosAsync();
                await RecargarDetalleTrasDeshacerAsync(resultado);
                await MostrarExitoConciliacionAsync(string.IsNullOrWhiteSpace(resultado.Message)
                    ? "Se deshizo la ultima operacion de conciliacion."
                    : resultado.Message);
            }
            catch (Exception ex)
            {
                await MostrarErrorConciliacionAsync($"Error al deshacer la ultima operacion de conciliacion: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task DeshacerTodasOperacionesConciliacionAsync()
        {
            if (!CanDeshacerTodasOperacionesConciliacion)
            {
                await MostrarErrorConciliacionAsync("No hay operaciones de conciliacion pendientes por deshacer.");
                return;
            }

            try
            {
                IsLoading = true;
                ErrorMessage = null;
                SuccessMessage = null;

                var resultado = await _facturaService.DeshacerTodasOperacionesConciliacionAsync();
                if (!resultado.Success)
                {
                    await MostrarErrorConciliacionAsync(string.IsNullOrWhiteSpace(resultado.Message)
                        ? "No fue posible deshacer todas las operaciones de conciliacion."
                        : resultado.Message);
                    return;
                }

                OperacionesConciliacionPendientes = resultado.OperacionesPendientes;
                await CargarDatosAsync();
                await RecargarDetalleTrasDeshacerAsync(resultado);
                await MostrarExitoConciliacionAsync(string.IsNullOrWhiteSpace(resultado.Message)
                    ? "Se deshicieron todas las operaciones de conciliacion."
                    : resultado.Message);
            }
            catch (Exception ex)
            {
                await MostrarErrorConciliacionAsync($"Error al deshacer todas las operaciones de conciliacion: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task InicializarBitacoraConciliacionSiEsNecesarioAsync()
        {
            if (_bitacoraConciliacionInicializada)
            {
                return;
            }

            var resultado = await _facturaService.InicializarBitacoraConciliacionAsync();
            if (!resultado.Success)
            {
                throw new InvalidOperationException(string.IsNullOrWhiteSpace(resultado.Message)
                    ? "No fue posible inicializar la bitacora de conciliacion."
                    : resultado.Message);
            }

            _bitacoraConciliacionInicializada = true;
            OperacionesConciliacionPendientes = resultado.OperacionesPendientes;
        }

        private async Task RecargarDetalleTrasDeshacerAsync(BitacoraConciliacionResponseDto resultado)
        {
            var idFacturaObjetivo = resultado.IdFactura ?? FacturaCargada?.IdFactura;
            if (idFacturaObjetivo.HasValue && idFacturaObjetivo.Value > 0)
            {
                await CargarDetalleFacturaAsync(idFacturaObjetivo.Value);
            }
            else
            {
                LimpiarFacturaCargada();
            }

            var idMovimientoObjetivo = resultado.IdMovimiento ?? MovimientoCargado?.IdMovimiento;
            MovimientoCargado = idMovimientoObjetivo.HasValue
                ? _movimientosPendientesBase.FirstOrDefault(movimiento => movimiento.IdMovimiento == idMovimientoObjetivo.Value)
                : null;
        }

        private async Task RefrescarFacturasPendientesAsync()
        {
            var facturas = await _facturaService.ObtenerFacturasAsync();
            var facturasPendientes = FiltrarFacturasConciliables(facturas);

            _facturasPendientesBase.Clear();
            _facturasPendientesBase.AddRange(facturasPendientes);
            AplicarFiltrosVisibles();
            OnPropertyChanged(nameof(CanIniciarConciliacionAutomatica));
        }

        private void LimpiarFacturaCargada()
        {
            FacturaCargada = null;
            FacturaCargadaConceptos.Clear();
            FacturaCargadaAbonos.Clear();
            OnPropertyChanged(nameof(ResumenFacturaCargadaConceptos));
            OnPropertyChanged(nameof(ResumenFacturaCargadaAbonos));
        }

        private void NotificarCambioFacturaCargada()
        {
            OnPropertyChanged(nameof(MensajeFacturaCargada));
            OnPropertyChanged(nameof(FacturaCargadaUuidTexto));
            OnPropertyChanged(nameof(FacturaCargadaFechaTexto));
            OnPropertyChanged(nameof(FacturaCargadaEmisorTexto));
            OnPropertyChanged(nameof(FacturaCargadaReceptorTexto));
            OnPropertyChanged(nameof(FacturaCargadaRfcTexto));
            OnPropertyChanged(nameof(FacturaCargadaMetodoFormaTexto));
            OnPropertyChanged(nameof(FacturaCargadaTotalesTexto));
            OnPropertyChanged(nameof(FacturaCargadaEstadoPagoTexto));
            OnPropertyChanged(nameof(FacturaCargadaTotalAbonadoTexto));
            OnPropertyChanged(nameof(FacturaCargadaSaldoPendienteTexto));
            OnPropertyChanged(nameof(CanAbonarMovimiento));
            OnPropertyChanged(nameof(CanIniciarConciliacionAutomatica));
        }

        private void NotificarCambioMovimientoCargado()
        {
            OnPropertyChanged(nameof(MensajeMovimientoCargado));
            OnPropertyChanged(nameof(MovimientoCargadoCuentaTexto));
            OnPropertyChanged(nameof(MovimientoCargadoBancoTexto));
            OnPropertyChanged(nameof(MovimientoCargadoPeriodoTexto));
            OnPropertyChanged(nameof(MovimientoCargadoFechaTexto));
            OnPropertyChanged(nameof(MovimientoCargadoReferenciaTexto));
            OnPropertyChanged(nameof(MovimientoCargadoCargoTexto));
            OnPropertyChanged(nameof(MovimientoCargadoAbonoTexto));
            OnPropertyChanged(nameof(MovimientoCargadoMontoRestanteTexto));
            OnPropertyChanged(nameof(MovimientoCargadoSaldoTexto));
            OnPropertyChanged(nameof(MovimientoCargadoRelacionadosTexto));
            OnPropertyChanged(nameof(MovimientoCargadoMetadatosTexto));
            OnPropertyChanged(nameof(CanAbonarMovimiento));
            OnPropertyChanged(nameof(CanIniciarConciliacionAutomatica));
        }

        private Task MostrarErrorConciliacionAsync(string mensaje)
        {
            ErrorMessage = mensaje;
            SuccessMessage = null;
            return Task.CompletedTask;
        }

        private async Task MostrarExitoConciliacionAsync(string mensaje)
        {
            SuccessMessage = mensaje;
            ErrorMessage = null;
            await _notificacionService.MostrarAsync("Conciliacion", mensaje);
        }

        private async Task<(EstadoCuentaDetalleDto? Detalle, string? Error)> CargarDetalleEstadoSeguroAsync(EstadoCuentaResumenDto estado)
        {
            try
            {
                var detalle = await _estadoCuentaXmlService.ObtenerDetalleEstadoCuentaAsync(estado.IdEstadoCuenta);
                if (detalle?.EstadoCuenta == null)
                {
                    return (null, $"No se pudo cargar el detalle del estado {estado.CuentaTitulo}.");
                }

                return (detalle, null);
            }
            catch (Exception ex)
            {
                return (null, $"Se omitio el estado {estado.CuentaTitulo}: {ex.Message}");
            }
        }

        private static void ReemplazarColeccion<T>(ObservableCollection<T> destino, System.Collections.Generic.IReadOnlyCollection<T>? origen)
        {
            destino.Clear();
            if (origen == null)
            {
                return;
            }

            foreach (var item in origen)
            {
                destino.Add(item);
            }
        }

        private void AplicarFiltrosVisibles()
        {
            var movimientosFiltrados = _movimientosPendientesBase
                .Where(CoincideMovimientoBusqueda)
                .OrderByDescending(movimiento => movimiento.Abono)
                .ThenByDescending(movimiento => movimiento.Fecha)
                .ThenByDescending(movimiento => movimiento.IdMovimiento)
                .ToList();

            var facturasFiltradas = _facturasPendientesBase
                .Where(CoincideFacturaBusqueda)
                .OrderByDescending(factura => factura.Folio)
                .ThenByDescending(factura => factura.IdFactura)
                .ToList();

            MovimientosPendientes = new ObservableCollection<ConciliacionMovimientoResumenDto>(movimientosFiltrados);
            FacturasPendientes = new ObservableCollection<FacturaResumenDto>(facturasFiltradas);
            OnPropertyChanged(nameof(ResumenMovimientos));
            OnPropertyChanged(nameof(ResumenFacturas));
        }

        private bool CoincideMovimientoBusqueda(ConciliacionMovimientoResumenDto movimiento)
        {
            return CoincideMovimientoMetadatoBusqueda(movimiento)
                && CoincideMovimientoAbonoBusqueda(movimiento)
                && CoincideMovimientoFecha(movimiento);
        }

        private bool CoincideMovimientoMetadatoBusqueda(ConciliacionMovimientoResumenDto movimiento)
        {
            var termino = MovimientoMetadatoBusquedaTexto?.Trim();
            if (string.IsNullOrWhiteSpace(termino))
            {
                return true;
            }

            if (ContieneTexto(movimiento.GrupoId, termino)
                || ContieneTexto(movimiento.TipoOperacion, termino)
                || ContieneTexto(movimiento.SubtipoOperacion, termino)
                || ContieneTexto(movimiento.Descripcion, termino)
                || ContieneTexto(movimiento.Referencia, termino)
                || ContieneTexto(movimiento.MetadatosTexto, termino)
                || ContieneTexto(movimiento.Banco, termino)
                || ContieneTexto(movimiento.Titular, termino)
                || ContieneTexto(movimiento.NumeroCuenta, termino)
                || ContieneTexto(movimiento.ReferenciaTexto, termino))
            {
                return true;
            }

            return false;
        }

        private bool CoincideMovimientoAbonoBusqueda(ConciliacionMovimientoResumenDto movimiento)
        {
            var termino = MovimientoAbonoBusquedaTexto?.Trim();
            if (string.IsNullOrWhiteSpace(termino))
            {
                return true;
            }

            return IntentarParsearMontoBusqueda(termino, out var montoBuscado)
                && decimal.Round(movimiento.Abono, 2) <= decimal.Round(montoBuscado, 2);
        }

        private bool CoincideFacturaBusqueda(FacturaResumenDto factura)
        {
            return CoincideFacturaFolioBusqueda(factura)
                && CoincideFacturaTotalBusqueda(factura)
                && CoincideFacturaNombreBusqueda(factura)
                && CoincideFacturaRfcBusqueda(factura);
        }

        private bool CoincideFacturaFolioBusqueda(FacturaResumenDto factura)
        {
            var termino = FacturaFolioBusquedaTexto?.Trim();
            if (string.IsNullOrWhiteSpace(termino))
            {
                return true;
            }

            return ContieneTexto(factura.Folio, termino)
                || ContieneTexto(factura.FolioTitulo, termino);
        }

        private bool CoincideMovimientoFecha(ConciliacionMovimientoResumenDto movimiento)
        {
            var inicio = _movimientoFechaInicio?.Date;
            var fin = _movimientoFechaFin?.Date;
            var fecha = movimiento.Fecha.Date;

            if (inicio.HasValue && fin.HasValue)
            {
                return fecha >= inicio.Value && fecha <= fin.Value;
            }

            if (inicio.HasValue)
            {
                return fecha >= inicio.Value;
            }

            if (fin.HasValue)
            {
                return fecha <= fin.Value;
            }

            return true;
        }

        private bool CoincideFacturaTotalBusqueda(FacturaResumenDto factura)
        {
            var termino = FacturaTotalBusqueda?.Trim();
            if (string.IsNullOrWhiteSpace(termino))
            {
                return true;
            }

            return IntentarParsearMontoBusqueda(termino, out var montoBuscado)
                && CoincideMonto(factura.Total, montoBuscado);
        }

        private bool CoincideFacturaNombreBusqueda(FacturaResumenDto factura)
        {
            var termino = FacturaNombreBusquedaTexto?.Trim();
            if (string.IsNullOrWhiteSpace(termino))
            {
                return true;
            }

            return ContieneTexto(factura.ReceptorNombre, termino);
        }

        private bool CoincideFacturaRfcBusqueda(FacturaResumenDto factura)
        {
            var termino = FacturaRfcBusquedaTexto?.Trim();
            if (string.IsNullOrWhiteSpace(termino))
            {
                return true;
            }

            return ContieneTexto(factura.ReceptorRfc, termino);
        }

        private static bool ContieneTexto(string? valor, string termino)
        {
            return !string.IsNullOrWhiteSpace(valor)
                && valor.Contains(termino, StringComparison.OrdinalIgnoreCase);
        }

        private static bool CoincideMonto(decimal valor, decimal montoBuscado)
        {
            return decimal.Round(valor, 2) == decimal.Round(montoBuscado, 2);
        }

        private static bool IntentarParsearMontoBusqueda(string termino, out decimal monto)
        {
            var normalizado = termino
                .Replace("$", string.Empty, StringComparison.Ordinal)
                .Replace(",", string.Empty, StringComparison.Ordinal)
                .Trim();

            return decimal.TryParse(normalizado, NumberStyles.Number | NumberStyles.AllowCurrencySymbol, CultureInfo.InvariantCulture, out monto)
                || decimal.TryParse(normalizado, NumberStyles.Number | NumberStyles.AllowCurrencySymbol, new CultureInfo("es-MX"), out monto);
        }

        private static string ConstruirResumenColeccion(int visibles, int total, string etiquetaSingular, string etiquetaPlural)
        {
            if (visibles == total)
            {
                return visibles == 1
                    ? $"1 {etiquetaSingular}"
                    : $"{visibles} {etiquetaPlural}";
            }

            return $"{visibles} de {total} visibles";
        }

        private List<FacturaResumenDto> FiltrarFacturasConciliables(IEnumerable<FacturaResumenDto> facturas)
        {
            return facturas
                .Where(factura => factura.Finiquito != true)
                .Where(factura => _conciliacionMatchingEngine.ObtenerTotalFactura(factura) > 0)
                .OrderBy(factura => factura.Fecha)
                .ThenBy(factura => factura.IdFactura)
                .ToList();
        }
    }
}
