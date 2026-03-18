using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Advance_Control.Models;
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
        private ObservableCollection<ConciliacionMovimientoResumenDto> _movimientosPendientes;
        private ObservableCollection<FacturaResumenDto> _facturasPendientes;
        private ConciliacionMovimientoResumenDto? _movimientoCargado;
        private FacturaResumenDto? _facturaCargada;
        private bool _isLoading;
        private bool _isConciliacionAutomaticaEnProceso;
        private string? _errorMessage;
        private string? _successMessage;

        public ConciliacionViewModel(
            IEstadoCuentaXmlService estadoCuentaXmlService,
            IFacturaService facturaService,
            INotificacionService notificacionService)
        {
            _estadoCuentaXmlService = estadoCuentaXmlService ?? throw new ArgumentNullException(nameof(estadoCuentaXmlService));
            _facturaService = facturaService ?? throw new ArgumentNullException(nameof(facturaService));
            _notificacionService = notificacionService ?? throw new ArgumentNullException(nameof(notificacionService));
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

        public string ResumenMovimientos => $"{MovimientosPendientes.Count} movimientos no conciliados";
        public string ResumenFacturas => $"{FacturasPendientes.Count} facturas no finiquitadas";
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
            && FacturasPendientes.Any(EsFacturaElegibleParaConciliacionUnoAUno)
            && MovimientosPendientes.Any(movimiento => decimal.Round(movimiento.Abono, 2) > 0);
        public bool CanEjecutarConciliacionAutomaticaConvinacional => !IsLoading
            && !IsConciliacionAutomaticaEnProceso
            && MovimientosPendientes.Count > 0
            && FacturasPendientes
                .Where(factura => ObtenerMontoPendienteFactura(factura) > 0 && !string.IsNullOrWhiteSpace(factura.ReceptorRfc))
                .GroupBy(factura => factura.ReceptorRfc!.Trim(), StringComparer.OrdinalIgnoreCase)
                .Any(grupo => grupo.Count() >= 2);

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

                var estados = estadosTask.Result;
                var facturas = facturasTask.Result;

                var detalleTasks = estados
                    .OrderBy(estado => estado.FechaCorte)
                    .ThenBy(estado => estado.IdEstadoCuenta)
                    .Select(estado => _estadoCuentaXmlService.ObtenerDetalleEstadoCuentaAsync(estado.IdEstadoCuenta))
                    .ToList();

                var detalles = detalleTasks.Count == 0
                    ? Array.Empty<EstadoCuentaDetalleDto?>()
                    : await Task.WhenAll(detalleTasks);

                var movimientosPendientes = detalles
                    .Where(detalle => detalle?.EstadoCuenta != null)
                    .SelectMany(detalle => detalle!.Grupos
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

                MovimientosPendientes = new ObservableCollection<ConciliacionMovimientoResumenDto>(movimientosPendientes);
                FacturasPendientes = new ObservableCollection<FacturaResumenDto>(facturasPendientes);
                OnPropertyChanged(nameof(ResumenMovimientos));
                OnPropertyChanged(nameof(ResumenFacturas));
                OnPropertyChanged(nameof(CanEjecutarConciliacionAutomatica));
                OnPropertyChanged(nameof(CanEjecutarConciliacionAutomaticaConvinacional));
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
                PrecargarMovimientoCoincidente(ObtenerMontoPendienteFactura(detalle.Factura), detalle.Factura.Fecha);
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

                await RefrescarFacturasPendientesAsync();
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
            var facturasObjetivo = FacturasPendientes
                .Where(EsFacturaElegibleParaConciliacionUnoAUno)
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

                var movimientosDisponibles = MovimientosPendientes
                    .OrderBy(movimiento => movimiento.Fecha)
                    .ThenBy(movimiento => movimiento.IdMovimiento)
                    .ToList();

                var conciliacionesUnoAUno = 0;
                FacturaResumenDto? ultimaFacturaConciliada = null;
                ConciliacionMovimientoResumenDto? ultimoMovimientoConciliado = null;

                foreach (var facturaObjetivo in facturasObjetivo)
                {
                    var totalFactura = ObtenerTotalFactura(facturaObjetivo);
                    var movimientoObjetivo = BuscarMovimientoCoincidente(
                        movimientosDisponibles,
                        totalFactura,
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
            var facturasObjetivo = FacturasPendientes
                .Where(factura => ObtenerMontoPendienteFactura(factura) > 0)
                .OrderBy(factura => factura.Fecha)
                .ThenBy(factura => factura.IdFactura)
                .ToList();

            if (!facturasObjetivo
                .Where(factura => !string.IsNullOrWhiteSpace(factura.ReceptorRfc))
                .GroupBy(factura => factura.ReceptorRfc!.Trim(), StringComparer.OrdinalIgnoreCase)
                .Any(grupo => grupo.Count() >= 2))
            {
                await MostrarErrorConciliacionAsync("No hay grupos de facturas pendientes para conciliacion automatica convinacional.");
                return;
            }

            try
            {
                IsConciliacionAutomaticaEnProceso = true;
                ErrorMessage = null;
                SuccessMessage = null;

                var movimientosDisponibles = MovimientosPendientes
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

        private void PrecargarMovimientoCoincidente(decimal totalFactura, DateTime fechaFactura)
        {
            MovimientoCargado = BuscarMovimientoCoincidente(
                MovimientosPendientes,
                totalFactura,
                fechaFactura);
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

                    var combinacion = BuscarCombinacionFacturasCompatible(facturasRfc, movimientosDisponibles, maximoAbonoDisponible, out var movimientoObjetivo);
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
            var montoAplicado = decimal.Round(facturas.Sum(ObtenerMontoPendienteFactura), 2);
            var request = new ConciliacionAutomaticaRequestDto
            {
                IdFactura = facturas.Count == 1 ? facturas[0].IdFactura : 0,
                Facturas = facturas
                    .Select(factura => new ConciliacionAutomaticaFacturaDto
                    {
                        IdFactura = factura.IdFactura,
                        MontoAbono = ObtenerMontoPendienteFactura(factura)
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

        private static List<FacturaResumenDto>? BuscarCombinacionFacturasCompatible(
            IReadOnlyList<FacturaResumenDto> facturas,
            IReadOnlyList<ConciliacionMovimientoResumenDto> movimientosDisponibles,
            decimal maximoAbonoDisponible,
            out ConciliacionMovimientoResumenDto? movimientoObjetivo)
        {
            movimientoObjetivo = null;
            var buffer = new List<FacturaResumenDto>();

            for (var tamano = 2; tamano <= facturas.Count; tamano++)
            {
                var combinacion = BuscarCombinacionFacturasCompatibleRecursiva(
                    facturas,
                    movimientosDisponibles,
                    maximoAbonoDisponible,
                    tamano,
                    0,
                    0m,
                    buffer,
                    out movimientoObjetivo);

                if (combinacion != null)
                {
                    return combinacion;
                }
            }

            return null;
        }

        private static List<FacturaResumenDto>? BuscarCombinacionFacturasCompatibleRecursiva(
            IReadOnlyList<FacturaResumenDto> facturas,
            IReadOnlyList<ConciliacionMovimientoResumenDto> movimientosDisponibles,
            decimal maximoAbonoDisponible,
            int tamanoObjetivo,
            int indiceInicio,
            decimal sumaActual,
            List<FacturaResumenDto> combinacionActual,
            out ConciliacionMovimientoResumenDto? movimientoObjetivo)
        {
            movimientoObjetivo = null;

            if (combinacionActual.Count == tamanoObjetivo)
            {
                var montoObjetivo = decimal.Round(sumaActual, 2);
                if (montoObjetivo <= 0 || montoObjetivo > maximoAbonoDisponible)
                {
                    return null;
                }

                var fechaMasNueva = combinacionActual.Max(factura => factura.Fecha);
                movimientoObjetivo = BuscarMovimientoCoincidente(
                    movimientosDisponibles,
                    montoObjetivo,
                    fechaMasNueva);
                return movimientoObjetivo == null ? null : new List<FacturaResumenDto>(combinacionActual);
            }

            var restantesNecesarios = tamanoObjetivo - combinacionActual.Count;
            for (var indice = indiceInicio; indice <= facturas.Count - restantesNecesarios; indice++)
            {
                var factura = facturas[indice];
                var nuevaSuma = decimal.Round(sumaActual + ObtenerMontoPendienteFactura(factura), 2);
                if (nuevaSuma > maximoAbonoDisponible)
                {
                    continue;
                }

                combinacionActual.Add(factura);

                var combinacionEncontrada = BuscarCombinacionFacturasCompatibleRecursiva(
                    facturas,
                    movimientosDisponibles,
                    maximoAbonoDisponible,
                    tamanoObjetivo,
                    indice + 1,
                    nuevaSuma,
                    combinacionActual,
                    out movimientoObjetivo);

                if (combinacionEncontrada != null)
                {
                    return combinacionEncontrada;
                }

                combinacionActual.RemoveAt(combinacionActual.Count - 1);
            }

            return null;
        }

        private static ConciliacionMovimientoResumenDto? BuscarMovimientoCoincidente(
            System.Collections.Generic.IEnumerable<ConciliacionMovimientoResumenDto> movimientos,
            decimal totalFactura,
            DateTime fechaFactura)
        {
            if (totalFactura <= 0)
            {
                return null;
            }

            return movimientos
                .Where(movimiento =>
                    decimal.Round(movimiento.Abono, 2) > 0
                    &&
                    decimal.Round(movimiento.Abono, 2) == decimal.Round(totalFactura, 2)
                    && EsMismoMes(fechaFactura, movimiento.Fecha))
                .OrderBy(movimiento => Math.Abs((movimiento.Fecha - fechaFactura).Ticks))
                .ThenByDescending(movimiento => movimiento.Fecha)
                .FirstOrDefault();
        }

        private static bool EsMismoMes(DateTime fechaFactura, DateTime fechaMovimiento)
        {
            return fechaFactura.Year == fechaMovimiento.Year
                && fechaFactura.Month == fechaMovimiento.Month;
        }

        private static bool EsFacturaElegibleParaConciliacionUnoAUno(FacturaResumenDto factura)
        {
            var totalFactura = ObtenerTotalFactura(factura);
            if (totalFactura <= 0)
            {
                return false;
            }

            var saldoPendiente = decimal.Round(factura.SaldoPendiente, 2);
            var totalAbonado = decimal.Round(factura.TotalAbonado, 2);

            return saldoPendiente == totalFactura
                && totalAbonado == 0
                && factura.NumeroAbonos == 0
                && factura.Finiquito != true;
        }

        private static decimal ObtenerTotalFactura(FacturaResumenDto factura)
        {
            return decimal.Round(factura.Total, 2);
        }

        private static decimal ObtenerMontoPendienteFactura(FacturaResumenDto factura)
        {
            var montoPendiente = factura.SaldoPendiente > 0 ? factura.SaldoPendiente : factura.Total;
            return decimal.Round(montoPendiente, 2);
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
            MovimientoCargado = MovimientosPendientes.FirstOrDefault(movimiento => movimiento.IdMovimiento == ultimoMovimientoConciliado?.IdMovimiento);
        }

        private async Task RefrescarFacturasPendientesAsync()
        {
            var facturas = await _facturaService.ObtenerFacturasAsync();
            var facturasPendientes = facturas
                .Where(factura => factura.Finiquito != true)
                .OrderBy(factura => factura.Fecha)
                .ThenBy(factura => factura.IdFactura)
                .ToList();

            FacturasPendientes = new ObservableCollection<FacturaResumenDto>(facturasPendientes);
            OnPropertyChanged(nameof(ResumenFacturas));
            OnPropertyChanged(nameof(CanEjecutarConciliacionAutomatica));
            OnPropertyChanged(nameof(CanEjecutarConciliacionAutomaticaConvinacional));
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

        private static void ReemplazarColeccion<T>(ObservableCollection<T> destino, System.Collections.Generic.IReadOnlyCollection<T> origen)
        {
            destino.Clear();
            foreach (var item in origen)
            {
                destino.Add(item);
            }
        }
    }
}
