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
        private string? _errorMessage;
        private string? _successMessage;
        private string? _movimientoBusquedaTexto;
        private string? _facturaFolioBusquedaTexto;

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

        public string? MovimientoBusquedaTexto
        {
            get => _movimientoBusquedaTexto;
            set
            {
                if (SetProperty(ref _movimientoBusquedaTexto, value))
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

        public string ResumenMovimientos => ConstruirResumenColeccion(
            MovimientosPendientes.Count,
            _movimientosPendientesBase.Count,
            "movimiento no conciliado",
            "movimientos no conciliados");

        public string ResumenFacturas => ConstruirResumenColeccion(
            FacturasPendientes.Count,
            _facturasPendientesBase.Count,
            "factura no finiquitada",
            "facturas no finiquitadas");
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
                    OnPropertyChanged(nameof(OpacidadIndicadorConciliacion));
                }
            }
        }

        public bool ConciliacionPanelHabilitado => !IsConciliacionAutomaticaEnProceso;
        public double OpacidadIndicadorConciliacion => IsConciliacionAutomaticaEnProceso ? 1d : 0d;
        public bool CanAbonarMovimiento => !IsLoading
            && FacturaCargada != null
            && MovimientoCargado != null
            && MovimientoCargado.Abono > 0
            && FacturaCargada.SaldoPendiente > 0
            && MovimientoCargado.Abono <= FacturaCargada.SaldoPendiente;
        public bool CanEjecutarConciliacionAutomatica => !IsLoading
            && !IsConciliacionAutomaticaEnProceso
            && _conciliacionMatchingEngine.CanRunUnoAUno(_facturasPendientesBase, _movimientosPendientesBase);
        public bool CanEjecutarConciliacionAutomaticaConvinacional => !IsLoading
            && !IsConciliacionAutomaticaEnProceso
            && _conciliacionMatchingEngine.CanRunCombinacional(_facturasPendientesBase, _movimientosPendientesBase);
        public bool CanEjecutarConciliacionAutomaticaAbonos => !IsLoading
            && !IsConciliacionAutomaticaEnProceso
            && _conciliacionMatchingEngine.CanRunAbonos(_facturasPendientesBase, _movimientosPendientesBase);

        public async Task CargarDatosAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = null;
                SuccessMessage = null;

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
                        .Where(grupo => !grupo.Conciliado && grupo.Abono > 0)
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
                            RelacionadosCount = grupo.MovimientosRelacionados.Count,
                            PeriodoTexto = detalle.EstadoCuenta.PeriodoTexto,
                            MetadatosTexto = grupo.MetadatosTexto
                        }))
                    .OrderBy(movimiento => movimiento.Fecha)
                    .ThenBy(movimiento => movimiento.IdMovimiento)
                    .ToList();

                var facturasPendientes = facturas
                    .Where(factura => factura.Finiquito != true)
                    .OrderBy(factura => factura.Fecha)
                    .ThenBy(factura => factura.IdFactura)
                    .ToList();

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

        public async Task CargarDetalleFacturaAsync(int idFactura)
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

            if (MovimientoCargado.Abono <= 0)
            {
                await MostrarErrorConciliacionAsync("El movimiento seleccionado no tiene un abono valido.");
                return;
            }

            if (MovimientoCargado.Abono > FacturaCargada.SaldoPendiente)
            {
                await MostrarErrorConciliacionAsync("El abono del movimiento excede el saldo pendiente de la factura cargada.");
                return;
            }

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
                    MontoAbono = MovimientoCargado.Abono,
                    Referencia = MovimientoCargado.Referencia,
                    Observaciones = $"Abono generado desde conciliacion con movimiento {MovimientoCargado.GrupoId}."
                });

                if (!result.Success)
                {
                    await MostrarErrorConciliacionAsync(string.IsNullOrWhiteSpace(result.Message)
                        ? "No se pudo registrar el abono."
                        : result.Message);
                    return;
                }

                await CargarDatosAsync();
                await CargarDetalleFacturaAsync(idFactura);
                await MostrarExitoConciliacionAsync(string.IsNullOrWhiteSpace(result.Message)
                    ? "Abono registrado correctamente."
                    : result.Message);
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
                .Where(_conciliacionMatchingEngine.EsFacturaElegibleParaConciliacionUnoAUno)
                .OrderBy(factura => factura.Fecha)
                .ThenBy(factura => factura.IdFactura)
                .ToList();

            if (facturasObjetivo.Count == 0)
            {
                await MostrarErrorConciliacionAsync("No hay facturas compatibles para conciliacion automatica 1 a 1.");
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

                var conciliacionesUnoAUno = 0;
                FacturaResumenDto? ultimaFacturaConciliada = null;
                ConciliacionMovimientoResumenDto? ultimoMovimientoConciliado = null;

                foreach (var facturaObjetivo in facturasObjetivo)
                {
                    var totalFactura = _conciliacionMatchingEngine.ObtenerTotalFactura(facturaObjetivo);
                    var movimientoObjetivo = _conciliacionMatchingEngine.BuscarMovimientoCoincidente(
                        movimientosDisponibles,
                        totalFactura,
                        new[] { facturaObjetivo },
                        facturaObjetivo.Fecha);
                    if (movimientoObjetivo == null)
                    {
                        continue;
                    }

                    await ConciliarMovimientoConFacturasAsync(
                        movimientoObjetivo,
                        new List<FacturaResumenDto> { facturaObjetivo },
                        $"Conciliacion automatica 1 a 1 con movimiento {movimientoObjetivo.GrupoId}.");

                    FacturaCargada = facturaObjetivo;
                    MovimientoCargado = movimientoObjetivo;
                    movimientosDisponibles.Remove(movimientoObjetivo);
                    conciliacionesUnoAUno++;
                    ultimaFacturaConciliada = facturaObjetivo;
                    ultimoMovimientoConciliado = movimientoObjetivo;
                }

                await CargarDatosAsync();
                await ActualizarDetallePostConciliacionAsync(ultimaFacturaConciliada, ultimoMovimientoConciliado);

                if (conciliacionesUnoAUno == 0)
                {
                    await MostrarErrorConciliacionAsync("No se encontro ningun movimiento compatible para las facturas pendientes.");
                    return;
                }

                var facturasSaltadas = facturasObjetivo.Count - conciliacionesUnoAUno;
                var segmentosResumen = new List<string>
                {
                    $"{conciliacionesUnoAUno} factura(s) por relacion 1 a 1"
                };
                if (facturasSaltadas > 0)
                {
                    segmentosResumen.Add($"{facturasSaltadas} factura(s) sin conciliar");
                }

                await MostrarExitoConciliacionAsync($"Conciliacion automatica completada. {string.Join("; ", segmentosResumen)}.");
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
                var resultadoCombinacional = await EjecutarConciliacionCombinacionalAsync(facturasRemanentes, movimientosDisponibles);

                await CargarDatosAsync();
                await ActualizarDetallePostConciliacionAsync(resultadoCombinacional.UltimaFacturaConciliada, resultadoCombinacional.UltimoMovimientoConciliado);

                if (resultadoCombinacional.FacturasConciliadas == 0)
                {
                    await MostrarErrorConciliacionAsync("No se encontro ninguna combinacion exacta para las facturas pendientes.");
                    return;
                }

                var facturasSaltadas = facturasObjetivo.Count - resultadoCombinacional.FacturasConciliadas;
                var segmentosResumen = new List<string>
                {
                    $"{resultadoCombinacional.FacturasConciliadas} factura(s) en {resultadoCombinacional.GruposConciliados} grupo(s) combinacional(es)"
                };

                if (facturasSaltadas > 0)
                {
                    segmentosResumen.Add($"{facturasSaltadas} factura(s) sin conciliar");
                }

                await MostrarExitoConciliacionAsync($"Conciliacion automatica convinacional completada. {string.Join("; ", segmentosResumen)}.");
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

                var facturasConciliadas = 0;
                var movimientosAplicados = 0;
                FacturaResumenDto? ultimaFacturaConciliada = null;
                ConciliacionMovimientoResumenDto? ultimoMovimientoConciliado = null;

                foreach (var facturaObjetivo in facturasObjetivo)
                {
                    var saldoFactura = _conciliacionMatchingEngine.ObtenerMontoPendienteFactura(facturaObjetivo);
                    var candidatos = _conciliacionMatchingEngine.ObtenerMovimientosCandidatosParaFactura(movimientosDisponibles, facturaObjetivo, saldoFactura);
                    if (candidatos.Count < 2)
                    {
                        continue;
                    }

                    var combinacion = _conciliacionMatchingEngine.BuscarCombinacionMovimientosParaFactura(candidatos, saldoFactura, facturaObjetivo.Fecha);
                    if (combinacion == null)
                    {
                        continue;
                    }

                    await AplicarMovimientosSobreFacturaAsync(facturaObjetivo, combinacion);

                    foreach (var movimiento in combinacion)
                    {
                        movimientosDisponibles.RemoveAll(item => item.IdMovimiento == movimiento.IdMovimiento);
                    }

                    facturasConciliadas++;
                    movimientosAplicados += combinacion.Count;
                    ultimaFacturaConciliada = facturaObjetivo;
                    ultimoMovimientoConciliado = combinacion
                        .OrderBy(movimiento => Math.Abs((movimiento.Fecha - facturaObjetivo.Fecha).Ticks))
                        .ThenBy(movimiento => movimiento.Fecha)
                        .ThenBy(movimiento => movimiento.IdMovimiento)
                        .FirstOrDefault();
                }

                await CargarDatosAsync();
                await ActualizarDetallePostConciliacionAsync(ultimaFacturaConciliada, ultimoMovimientoConciliado);

                if (facturasConciliadas == 0)
                {
                    await MostrarErrorConciliacionAsync("No se encontro ninguna combinacion exacta de abonos para las facturas pendientes.");
                    return;
                }

                var facturasSaltadas = facturasObjetivo.Count - facturasConciliadas;
                var segmentosResumen = new List<string>
                {
                    $"{facturasConciliadas} factura(s) conciliada(s) con {movimientosAplicados} movimiento(s)"
                };

                if (facturasSaltadas > 0)
                {
                    segmentosResumen.Add($"{facturasSaltadas} factura(s) sin conciliar");
                }

                await MostrarExitoConciliacionAsync($"Conciliacion automatica de abonos completada. {string.Join("; ", segmentosResumen)}.");
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

        private void PrecargarMovimientoCoincidente(FacturaResumenDto factura)
        {
            MovimientoCargado = _conciliacionMatchingEngine.BuscarMovimientoCoincidente(
                _movimientosPendientesBase,
                _conciliacionMatchingEngine.ObtenerMontoPendienteFactura(factura),
                new[] { factura },
                factura.Fecha);
        }

        private async Task<(int FacturasConciliadas, int GruposConciliados, FacturaResumenDto? UltimaFacturaConciliada, ConciliacionMovimientoResumenDto? UltimoMovimientoConciliado)> EjecutarConciliacionCombinacionalAsync(
            List<FacturaResumenDto> facturasRemanentes,
            List<ConciliacionMovimientoResumenDto> movimientosDisponibles)
        {
            var facturasConciliadas = 0;
            var gruposConciliados = 0;
            FacturaResumenDto? ultimaFacturaConciliada = null;
            ConciliacionMovimientoResumenDto? ultimoMovimientoConciliado = null;

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

                    var combinacion = _conciliacionMatchingEngine.BuscarCombinacionFacturasCompatible(facturasRfc, movimientosDisponibles, maximoAbonoDisponible, out var movimientoObjetivo);
                    if (combinacion == null || movimientoObjetivo == null)
                    {
                        break;
                    }

                    await ConciliarMovimientoConFacturasAsync(
                        movimientoObjetivo,
                        combinacion,
                        $"Conciliacion automatica combinacional con movimiento {movimientoObjetivo.GrupoId} para RFC {grupoRfc.Key}.");

                    var idsConciliados = combinacion.Select(factura => factura.IdFactura).ToHashSet();
                    facturasRemanentes.RemoveAll(factura => idsConciliados.Contains(factura.IdFactura));
                    facturasRfc.RemoveAll(factura => idsConciliados.Contains(factura.IdFactura));
                    movimientosDisponibles.RemoveAll(movimiento => movimiento.IdMovimiento == movimientoObjetivo.IdMovimiento);

                    facturasConciliadas += combinacion.Count;
                    gruposConciliados++;
                    ultimaFacturaConciliada = combinacion
                        .OrderBy(factura => factura.Fecha)
                        .ThenBy(factura => factura.IdFactura)
                        .Last();
                    ultimoMovimientoConciliado = movimientoObjetivo;
                }

                if (facturasRfc.Count >= 2)
                {
                    await MostrarErrorConciliacionAsync($"No se encontro combinacion exacta para RFC {grupoRfc.Key} con {facturasRfc.Count} factura(s) pendiente(s).");
                }
            }

            return (facturasConciliadas, gruposConciliados, ultimaFacturaConciliada, ultimoMovimientoConciliado);
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
                Observaciones = observaciones
            };

            var response = await _estadoCuentaXmlService.ConciliarAutomaticamenteAsync(request);
            if (!response.Success)
            {
                throw new InvalidOperationException(string.IsNullOrWhiteSpace(response.Message)
                    ? "No fue posible completar la conciliacion automatica."
                    : response.Message);
            }
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
                    Observaciones = $"Abono generado desde conciliacion automatica de abonos con movimiento {movimiento.GrupoId}."
                });

                if (!resultado.Success)
                {
                    throw new InvalidOperationException(string.IsNullOrWhiteSpace(resultado.Message)
                        ? "No fue posible registrar uno de los abonos combinados."
                        : resultado.Message);
                }
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

        private async Task RefrescarFacturasPendientesAsync()
        {
            var facturas = await _facturaService.ObtenerFacturasAsync();
            var facturasPendientes = facturas
                .Where(factura => factura.Finiquito != true)
                .OrderBy(factura => factura.Fecha)
                .ThenBy(factura => factura.IdFactura)
                .ToList();

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
                .OrderBy(movimiento => movimiento.Fecha)
                .ThenBy(movimiento => movimiento.IdMovimiento)
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
            var termino = MovimientoBusquedaTexto?.Trim();
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
                || ContieneTexto(movimiento.ReferenciaTexto, termino)
                || ContieneTexto(movimiento.AbonoTexto, termino)
                || ContieneTexto(movimiento.CargoTexto, termino)
                || ContieneTexto(movimiento.SaldoTexto, termino))
            {
                return true;
            }

            return IntentarParsearMontoBusqueda(termino, out var montoBuscado)
                && (CoincideMonto(movimiento.Abono, montoBuscado)
                    || CoincideMonto(movimiento.Cargo, montoBuscado)
                    || CoincideMonto(movimiento.Saldo, montoBuscado));
        }

        private bool CoincideFacturaBusqueda(FacturaResumenDto factura)
        {
            var termino = FacturaFolioBusquedaTexto?.Trim();
            if (string.IsNullOrWhiteSpace(termino))
            {
                return true;
            }

            return ContieneTexto(factura.Folio, termino)
                || ContieneTexto(factura.FolioTitulo, termino);
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
    }
}
