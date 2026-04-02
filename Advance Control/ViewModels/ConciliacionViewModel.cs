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
                    OnPropertyChanged(nameof(CanEjecutarConciliacionAutomatica));
                    OnPropertyChanged(nameof(CanEjecutarConciliacionAutomaticaConvinacional));
                    OnPropertyChanged(nameof(CanEjecutarConciliacionAutomaticaAbonos));
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
            set
            {
                if (SetProperty(ref _aplicarReglaPueMismoMes, value))
                {
                    OnPropertyChanged(nameof(CanEjecutarConciliacionAutomatica));
                    OnPropertyChanged(nameof(CanEjecutarConciliacionAutomaticaConvinacional));
                    OnPropertyChanged(nameof(CanEjecutarConciliacionAutomaticaAbonos));
                }
            }
        }

        public bool UsarRfcComoRegla
        {
            get => _usarRfcComoRegla;
            set
            {
                if (SetProperty(ref _usarRfcComoRegla, value))
                {
                    OnPropertyChanged(nameof(CanEjecutarConciliacionAutomatica));
                    OnPropertyChanged(nameof(CanEjecutarConciliacionAutomaticaConvinacional));
                    OnPropertyChanged(nameof(CanEjecutarConciliacionAutomaticaAbonos));
                }
            }
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
                    OnPropertyChanged(nameof(CanEjecutarConciliacionAutomatica));
                    OnPropertyChanged(nameof(CanEjecutarConciliacionAutomaticaConvinacional));
                    OnPropertyChanged(nameof(CanEjecutarConciliacionAutomaticaAbonos));
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
        public bool CanEjecutarConciliacionAutomatica => !IsLoading
            && !IsConciliacionAutomaticaEnProceso
            && _conciliacionMatchingEngine.CanRunUnoAUno(_facturasPendientesBase, _movimientosPendientesBase);
        public bool CanEjecutarConciliacionAutomaticaConvinacional => !IsLoading
            && !IsConciliacionAutomaticaEnProceso
            && _conciliacionMatchingEngine.CanRunCombinacional(_facturasPendientesBase, _movimientosPendientesBase);
        public bool CanEjecutarConciliacionAutomaticaAbonos => !IsLoading
            && !IsConciliacionAutomaticaEnProceso
            && _conciliacionMatchingEngine.CanRunAbonos(_facturasPendientesBase, _movimientosPendientesBase);
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

                OnPropertyChanged(nameof(CanEjecutarConciliacionAutomatica));
                OnPropertyChanged(nameof(CanEjecutarConciliacionAutomaticaConvinacional));
                OnPropertyChanged(nameof(CanEjecutarConciliacionAutomaticaAbonos));
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
                PrecargarMovimientoCoincidente(detalle.Factura);
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

        public async Task EjecutarConciliacionAutomaticaAsync()
        {
            var facturasObjetivo = _facturasPendientesBase
                .Where(factura => _conciliacionMatchingEngine.ObtenerMontoPendienteFactura(factura) > 0)
                .OrderBy(factura => factura.Fecha)
                .ThenBy(factura => factura.IdFactura)
                .ToList();
            var facturasUnoAUno = facturasObjetivo
                .Where(_conciliacionMatchingEngine.EsFacturaElegibleParaConciliacionUnoAUno)
                .OrderBy(factura => factura.Fecha)
                .ThenBy(factura => factura.IdFactura)
                .ToList();

            if (facturasObjetivo.Count == 0)
            {
                await MostrarErrorConciliacionAsync("No hay facturas con saldo pendiente para conciliacion automatica.");
                return;
            }

            try
            {
                IsConciliacionAutomaticaEnProceso = true;
                ErrorMessage = null;
                SuccessMessage = null;

                var movimientosDisponibles = _movimientosPendientesBase
                    .OrderBy(movimiento => movimiento.Fecha)
                    .ThenBy(movimiento => movimiento.IdMovimiento)
                    .ToList();
                var facturasRemanentes = new List<FacturaResumenDto>(facturasObjetivo);

                // Recolectar propuestas 1 a 1 (sin aplicar)
                var propuestasUnoAUno = RecolectarPropuestasUnoAUno(facturasUnoAUno, movimientosDisponibles, facturasRemanentes);

                // Recolectar propuestas combinacionales del pool restante (sin aplicar)
                var propuestasCombinacional = RecolectarPropuestasCombinacional(facturasRemanentes, movimientosDisponibles);

                var todasLasPropuestas = propuestasUnoAUno.Concat(propuestasCombinacional).ToList();

                if (todasLasPropuestas.Count == 0)
                {
                    await MostrarErrorConciliacionAsync("No se encontro ningun movimiento compatible para las facturas pendientes.");
                    return;
                }

                // Mostrar diálogo de confirmación y aplicar solo las aprobadas
                var (aprobadas, cancelado) = await MostrarDialogoYAplicarPropuestasAsync(todasLasPropuestas);
                if (cancelado)
                {
                    return;
                }

                await CargarDatosAsync();

                var ultimaFactura = aprobadas.LastOrDefault()?.Facturas.LastOrDefault();
                var ultimoMovimiento = aprobadas.LastOrDefault()?.Movimiento;
                await ActualizarDetallePostConciliacionAsync(ultimaFactura, ultimoMovimiento);

                var conciliacionesUnoAUno = aprobadas.Count(p => p.Tipo == "1 a 1");
                var conciliacionesCombinacionales = aprobadas.Where(p => p.Tipo == "Combinacional").Sum(p => p.Facturas.Count);
                var gruposCombinacionales = aprobadas.Count(p => p.Tipo == "Combinacional");
                var totalConciliadas = conciliacionesUnoAUno + conciliacionesCombinacionales;
                var facturasFallidas = facturasObjetivo.Count - totalConciliadas;

                var segmentosResumen = new List<string>();
                if (conciliacionesUnoAUno > 0)
                {
                    segmentosResumen.Add($"{conciliacionesUnoAUno} factura(s) por relacion 1 a 1");
                }

                if (conciliacionesCombinacionales > 0)
                {
                    segmentosResumen.Add($"{conciliacionesCombinacionales} factura(s) en {gruposCombinacionales} grupo(s) combinacional(es)");
                }

                if (facturasFallidas > 0)
                {
                    segmentosResumen.Add($"{facturasFallidas} factura(s) fallida(s)");
                }

                await MostrarResultadoFinalConciliacionAsync("Conciliacion automatica", segmentosResumen, facturasFallidas);
            }
            catch (Exception ex)
            {
                await MostrarErrorConciliacionAsync($"Error al ejecutar la conciliacion automatica: {ex.Message}");
            }
            finally
            {
                IsConciliacionAutomaticaEnProceso = false;
            }
        }

        public async Task EjecutarConciliacionAutomaticaConvinacionalAsync()
        {
            var facturasObjetivo = _facturasPendientesBase
                .Where(factura => _conciliacionMatchingEngine.ObtenerMontoPendienteFactura(factura) > 0)
                .OrderBy(factura => factura.Fecha)
                .ThenBy(factura => factura.IdFactura)
                .ToList();

            if (!_conciliacionMatchingEngine.CanRunCombinacional(facturasObjetivo, _movimientosPendientesBase))
            {
                await MostrarErrorConciliacionAsync("No hay grupos de facturas pendientes para conciliacion automatica convinacional.");
                return;
            }

            try
            {
                IsConciliacionAutomaticaEnProceso = true;
                ErrorMessage = null;
                SuccessMessage = null;

                var movimientosDisponibles = _movimientosPendientesBase
                    .OrderBy(movimiento => movimiento.Fecha)
                    .ThenBy(movimiento => movimiento.IdMovimiento)
                    .ToList();

                var facturasRemanentes = new List<FacturaResumenDto>(facturasObjetivo);
                var propuestas = RecolectarPropuestasCombinacional(facturasRemanentes, movimientosDisponibles);

                if (propuestas.Count == 0)
                {
                    await MostrarErrorConciliacionAsync("No se encontraron combinaciones compatibles para las facturas pendientes.");
                    return;
                }

                var (aprobadas, cancelado) = await MostrarDialogoYAplicarPropuestasAsync(propuestas);
                if (cancelado)
                {
                    return;
                }

                await CargarDatosAsync();

                var ultimaFactura = aprobadas.LastOrDefault()?.Facturas.LastOrDefault();
                var ultimoMovimiento = aprobadas.LastOrDefault()?.Movimiento;
                await ActualizarDetallePostConciliacionAsync(ultimaFactura, ultimoMovimiento);

                var facturasConciliadas = aprobadas.Sum(p => p.Facturas.Count);
                var gruposConciliados = aprobadas.Count;
                var facturasFallidas = facturasObjetivo.Count - facturasConciliadas;

                var segmentosResumen = new List<string>
                {
                    $"{facturasConciliadas} factura(s) en {gruposConciliados} grupo(s) combinacional(es)"
                };

                if (facturasFallidas > 0)
                {
                    segmentosResumen.Add($"{facturasFallidas} factura(s) fallida(s)");
                }

                await MostrarResultadoFinalConciliacionAsync("Conciliacion automatica convinacional", segmentosResumen, facturasFallidas);
            }
            catch (Exception ex)
            {
                await MostrarErrorConciliacionAsync($"Error al ejecutar la conciliacion automatica convinacional: {ex.Message}");
            }
            finally
            {
                IsConciliacionAutomaticaEnProceso = false;
            }
        }

        public async Task EjecutarConciliacionAutomaticaAbonosAsync()
        {
            var facturasObjetivo = _facturasPendientesBase
                .Where(factura => _conciliacionMatchingEngine.ObtenerMontoPendienteFactura(factura) > 0)
                .OrderBy(factura => factura.Fecha)
                .ThenBy(factura => factura.IdFactura)
                .ToList();

            if (facturasObjetivo.Count == 0)
            {
                await MostrarErrorConciliacionAsync("No hay facturas con saldo pendiente para conciliacion automatica de abonos.");
                return;
            }

            try
            {
                IsConciliacionAutomaticaEnProceso = true;
                ErrorMessage = null;
                SuccessMessage = null;

                var movimientosDisponibles = _movimientosPendientesBase
                    .Where(movimiento => decimal.Round(movimiento.Abono, 2) > 0)
                    .OrderBy(movimiento => movimiento.Fecha)
                    .ThenBy(movimiento => movimiento.IdMovimiento)
                    .ToList();

                // Recolectar propuestas sin aplicar
                var propuestas = RecolectarPropuestasAbonos(facturasObjetivo, movimientosDisponibles);

                if (propuestas.Count == 0)
                {
                    await MostrarErrorConciliacionAsync("No se encontraron combinaciones de movimientos para las facturas pendientes.");
                    return;
                }

                var (aprobadas, cancelado) = await MostrarDialogoYAplicarPropuestasAsync(propuestas);
                if (cancelado)
                {
                    return;
                }

                await CargarDatosAsync();

                var ultimaFactura = aprobadas.LastOrDefault()?.Facturas.FirstOrDefault();
                var ultimoMovimiento = aprobadas.LastOrDefault()?.Movimiento;
                await ActualizarDetallePostConciliacionAsync(ultimaFactura, ultimoMovimiento);

                var facturasConciliadas = aprobadas.Count;
                var movimientosAplicados = aprobadas.Sum(p => p.TodosLosMovimientos.Count);
                var facturasFallidas = facturasObjetivo.Count - facturasConciliadas;

                var segmentosResumen = new List<string>
                {
                    $"{facturasConciliadas} factura(s) conciliada(s) con {movimientosAplicados} movimiento(s)"
                };

                if (facturasFallidas > 0)
                {
                    segmentosResumen.Add($"{facturasFallidas} factura(s) fallida(s)");
                }

                await MostrarResultadoFinalConciliacionAsync("Conciliacion automatica de abonos", segmentosResumen, facturasFallidas);
            }
            catch (Exception ex)
            {
                await MostrarErrorConciliacionAsync($"Error al ejecutar la conciliacion automatica de abonos: {ex.Message}");
            }
            finally
            {
                IsConciliacionAutomaticaEnProceso = false;
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

        private void PrecargarMovimientoCoincidente(FacturaResumenDto factura)
        {
            MovimientoCargado = _conciliacionMatchingEngine.BuscarMovimientoCoincidente(
                _movimientosPendientesBase,
                _conciliacionMatchingEngine.ObtenerMontoPendienteFactura(factura),
                new[] { factura },
                factura.Fecha);
        }

        private List<ConciliacionMatchPropuestaDto> RecolectarPropuestasAbonos(
            List<FacturaResumenDto> facturasObjetivo,
            List<ConciliacionMovimientoResumenDto> movimientosDisponibles)
        {
            var propuestas = new List<ConciliacionMatchPropuestaDto>();

            foreach (var facturaObjetivo in facturasObjetivo)
            {
                var saldoFactura = _conciliacionMatchingEngine.ObtenerMontoPendienteFactura(facturaObjetivo);
                var candidatos = _conciliacionMatchingEngine.ObtenerMovimientosCandidatosParaFactura(
                    movimientosDisponibles,
                    facturaObjetivo,
                    saldoFactura,
                    aplicarReglaPueMismoMes: AplicarReglaPueMismoMes,
                    limitarCandidatos: false);

                if (candidatos.Count < 2)
                {
                    continue;
                }

                var combinacion = _conciliacionMatchingEngine.BuscarCombinacionMovimientosParaFactura(
                    candidatos, saldoFactura, facturaObjetivo.Fecha);

                if (combinacion == null || combinacion.Count == 0)
                {
                    continue;
                }

                var movimientoPrincipal = combinacion
                    .OrderBy(m => Math.Abs((m.Fecha - facturaObjetivo.Fecha).Ticks))
                    .ThenBy(m => m.Fecha)
                    .ThenBy(m => m.IdMovimiento)
                    .First();
                var adicionales = combinacion.Where(m => m.IdMovimiento != movimientoPrincipal.IdMovimiento).ToList();

                propuestas.Add(new ConciliacionMatchPropuestaDto
                {
                    Tipo = "Abonos",
                    Facturas = new List<FacturaResumenDto> { facturaObjetivo },
                    Movimiento = movimientoPrincipal,
                    MovimientosAdicionales = adicionales,
                    Observaciones = $"Conciliacion automatica de abonos para factura {facturaObjetivo.Folio}."
                });

                // Retirar del pool
                foreach (var movimiento in combinacion)
                {
                    movimientosDisponibles.RemoveAll(item => item.IdMovimiento == movimiento.IdMovimiento);
                }
            }

            return propuestas;
        }


        private List<ConciliacionMatchPropuestaDto> RecolectarPropuestasUnoAUno(
            List<FacturaResumenDto> facturasUnoAUno,
            List<ConciliacionMovimientoResumenDto> movimientosDisponibles,
            List<FacturaResumenDto> facturasRemanentes)
        {
            var propuestas = new List<ConciliacionMatchPropuestaDto>();

            foreach (var facturaObjetivo in facturasUnoAUno)
            {
                var totalFactura = _conciliacionMatchingEngine.ObtenerTotalFactura(facturaObjetivo);
                var movimientoObjetivo = _conciliacionMatchingEngine.BuscarMovimientoCoincidente(
                    movimientosDisponibles,
                    totalFactura,
                    new[] { facturaObjetivo },
                    facturaObjetivo.Fecha,
                    AplicarReglaPueMismoMes);

                if (movimientoObjetivo == null)
                {
                    continue;
                }

                propuestas.Add(new ConciliacionMatchPropuestaDto
                {
                    Tipo = "1 a 1",
                    Facturas = new List<FacturaResumenDto> { facturaObjetivo },
                    Movimiento = movimientoObjetivo,
                    Observaciones = $"Conciliacion automatica 1 a 1 con movimiento {movimientoObjetivo.GrupoId}."
                });

                // Retirar del pool para que no se usen en combinacionales
                movimientosDisponibles.Remove(movimientoObjetivo);
                facturasRemanentes.RemoveAll(f => f.IdFactura == facturaObjetivo.IdFactura);
            }

            return propuestas;
        }

        private List<ConciliacionMatchPropuestaDto> RecolectarPropuestasCombinacional(
            List<FacturaResumenDto> facturasRemanentes,
            List<ConciliacionMovimientoResumenDto> movimientosDisponibles)
        {
            var propuestas = new List<ConciliacionMatchPropuestaDto>();

            var gruposPorRfc = facturasRemanentes
                .Where(factura => !string.IsNullOrWhiteSpace(factura.ReceptorRfc))
                .GroupBy(factura => factura.ReceptorRfc!.Trim(), StringComparer.OrdinalIgnoreCase)
                .OrderBy(grupo => grupo.Min(factura => factura.Fecha))
                .ThenBy(grupo => grupo.Key, StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var grupoRfc in gruposPorRfc)
            {
                var facturasRfc = facturasRemanentes
                    .Where(factura => string.Equals(factura.ReceptorRfc?.Trim(), grupoRfc.Key, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(factura => factura.Fecha)
                    .ThenBy(factura => factura.IdFactura)
                    .ToList();

                if (facturasRfc.Count < 2)
                {
                    continue;
                }

                while (facturasRfc.Count >= 2 && movimientosDisponibles.Count > 0)
                {
                    var maximoAbonoDisponible = movimientosDisponibles.Max(movimiento => movimiento.Abono);
                    if (maximoAbonoDisponible <= 0)
                    {
                        break;
                    }

                    var combinacion = _conciliacionMatchingEngine.BuscarCombinacionFacturasCompatible(
                        facturasRfc,
                        movimientosDisponibles,
                        maximoAbonoDisponible,
                        out var movimientoObjetivo,
                        AplicarReglaPueMismoMes);

                    if (combinacion == null || movimientoObjetivo == null)
                    {
                        break;
                    }

                    propuestas.Add(new ConciliacionMatchPropuestaDto
                    {
                        Tipo = "Combinacional",
                        Facturas = combinacion,
                        Movimiento = movimientoObjetivo,
                        Observaciones = $"Conciliacion automatica combinacional con movimiento {movimientoObjetivo.GrupoId} para RFC {grupoRfc.Key}."
                    });

                    // Retirar del pool para buscar siguientes combinaciones
                    var idsConciliados = combinacion.Select(factura => factura.IdFactura).ToHashSet();
                    facturasRemanentes.RemoveAll(factura => idsConciliados.Contains(factura.IdFactura));
                    facturasRfc.RemoveAll(factura => idsConciliados.Contains(factura.IdFactura));
                    movimientosDisponibles.RemoveAll(movimiento => movimiento.IdMovimiento == movimientoObjetivo.IdMovimiento);
                }
            }

            return propuestas;
        }

        private async Task<(IReadOnlyList<ConciliacionMatchPropuestaDto> Aprobadas, bool Cancelado)> MostrarDialogoYAplicarPropuestasAsync(
            IReadOnlyList<ConciliacionMatchPropuestaDto> propuestas)
        {
            var ventana = new ConfirmacionConciliacionWindow(propuestas);
            ventana.Activate();

            var aprobadas = await ventana.ResultTask;

            if (aprobadas == null)
            {
                return (Array.Empty<ConciliacionMatchPropuestaDto>(), true);
            }

            foreach (var propuesta in aprobadas)
            {
                if (propuesta.MovimientosAdicionales.Count > 0)
                {
                    // Tipo "Abonos": N movimientos → 1 factura
                    await AplicarMovimientosSobreFacturaAsync(
                        propuesta.Facturas[0],
                        propuesta.TodosLosMovimientos);
                }
                else
                {
                    // Tipo "1 a 1" o "Combinacional": 1 movimiento → N facturas
                    await ConciliarMovimientoConFacturasAsync(propuesta.Movimiento, propuesta.Facturas, propuesta.Observaciones);
                }
            }

            return (aprobadas, false);
        }

        private async Task ConciliarMovimientoConFacturasAsync(
            ConciliacionMovimientoResumenDto movimiento,
            IReadOnlyList<FacturaResumenDto> facturas,
            string observaciones)
        {
            var montoAplicado = decimal.Round(facturas.Sum(_conciliacionMatchingEngine.ObtenerMontoPendienteFactura), 2);
            var request = new ConciliacionAutomaticaRequestDto
            {
                IdFactura = facturas.Count == 1 ? facturas[0].IdFactura : 0,
                Facturas = facturas
                    .Select(factura => new ConciliacionAutomaticaFacturaDto
                    {
                        IdFactura = factura.IdFactura,
                        MontoAbono = _conciliacionMatchingEngine.ObtenerMontoPendienteFactura(factura)
                    })
                    .ToList(),
                IdMovimiento = movimiento.IdMovimiento,
                FechaAbono = movimiento.Fecha,
                MontoAbono = montoAplicado,
                Referencia = movimiento.Referencia,
                Observaciones = observaciones,
                RegistrarEnBitacoraConciliacion = true,
                TipoOperacionBitacoraConciliacion = "automatica"
            };

            var response = await _estadoCuentaXmlService.ConciliarAutomaticamenteAsync(request);
            if (!response.Success)
            {
                throw new InvalidOperationException(string.IsNullOrWhiteSpace(response.Message)
                    ? "No fue posible completar la conciliacion automatica."
                    : response.Message);
            }

            OperacionesConciliacionPendientes = response.OperacionesConciliacionPendientes;
        }

        private async Task AplicarMovimientosSobreFacturaAsync(
            FacturaResumenDto factura,
            IReadOnlyList<ConciliacionMovimientoResumenDto> movimientos)
        {
            foreach (var movimiento in movimientos
                .OrderBy(item => item.Fecha)
                .ThenBy(item => item.IdMovimiento))
            {
                var resultado = await _facturaService.RegistrarAbonoAsync(new RegistrarAbonoFacturaRequestDto
                {
                    IdFactura = factura.IdFactura,
                    IdMovimiento = movimiento.IdMovimiento,
                    FechaAbono = movimiento.Fecha,
                    MontoAbono = movimiento.Abono,
                    Referencia = movimiento.Referencia,
                    Observaciones = $"Abono generado desde conciliacion automatica de abonos con movimiento {movimiento.GrupoId}.",
                    RegistrarEnBitacoraConciliacion = true,
                    TipoOperacionBitacoraConciliacion = "automatica_abonos"
                });

                if (!resultado.Success)
                {
                    throw new InvalidOperationException(string.IsNullOrWhiteSpace(resultado.Message)
                        ? "No fue posible registrar uno de los abonos combinados."
                        : resultado.Message);
                }

                OperacionesConciliacionPendientes = resultado.OperacionesConciliacionPendientes;
            }
        }

        private async Task ActualizarDetallePostConciliacionAsync(
            FacturaResumenDto? ultimaFacturaConciliada,
            ConciliacionMovimientoResumenDto? ultimoMovimientoConciliado)
        {
            if (ultimaFacturaConciliada == null)
            {
                return;
            }

            await CargarDetalleFacturaAsync(ultimaFacturaConciliada.IdFactura);
            MovimientoCargado = _movimientosPendientesBase.FirstOrDefault(movimiento => movimiento.IdMovimiento == ultimoMovimientoConciliado?.IdMovimiento);
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
            OnPropertyChanged(nameof(CanEjecutarConciliacionAutomatica));
            OnPropertyChanged(nameof(CanEjecutarConciliacionAutomaticaConvinacional));
            OnPropertyChanged(nameof(CanEjecutarConciliacionAutomaticaAbonos));
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
            OnPropertyChanged(nameof(CanEjecutarConciliacionAutomatica));
            OnPropertyChanged(nameof(CanEjecutarConciliacionAutomaticaConvinacional));
            OnPropertyChanged(nameof(CanEjecutarConciliacionAutomaticaAbonos));
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
            OnPropertyChanged(nameof(CanEjecutarConciliacionAutomatica));
            OnPropertyChanged(nameof(CanEjecutarConciliacionAutomaticaConvinacional));
            OnPropertyChanged(nameof(CanEjecutarConciliacionAutomaticaAbonos));
        }

        private async Task MostrarErrorConciliacionAsync(string mensaje)
        {
            ErrorMessage = mensaje;
            SuccessMessage = null;
            await _notificacionService.MostrarAsync("Error de conciliacion", mensaje);
        }

        private async Task MostrarExitoConciliacionAsync(string mensaje)
        {
            SuccessMessage = mensaje;
            ErrorMessage = null;
            await _notificacionService.MostrarAsync("Conciliacion", mensaje);
        }

        private async Task MostrarResultadoFinalConciliacionAsync(
            string proceso,
            IReadOnlyCollection<string> segmentosResumen,
            int facturasFallidas)
        {
            var mensaje = $"{proceso} completada. {string.Join("; ", segmentosResumen)}.";
            if (facturasFallidas > 0)
            {
                await MostrarErrorConciliacionAsync(mensaje);
                return;
            }

            await MostrarExitoConciliacionAsync(mensaje);
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
                .OrderBy(factura => factura.Fecha)
                .ThenBy(factura => factura.IdFactura)
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
