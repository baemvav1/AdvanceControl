using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.Reportes;

namespace Advance_Control.ViewModels
{
    public class RPTFinancieroFacturacionViewModel : ViewModelBase
    {
        private readonly IReporteFinancieroFacturacionService _reporteService;
        private readonly IReporteFinancieroFacturacionExportService _exportService;
        private readonly IHistorialCobranzaService _historialService;
        private readonly List<ReporteFinancieroFacturacionCabeceraDto> _cabecerasBase = new();
        private readonly List<ReporteFinancieroFacturacionDetalleDto> _detallesBase = new();
        private ObservableCollection<ReporteFinancieroFacturacionListadoItemDto> _listadoItems = new();
        private bool _isLoading;
        private string? _errorMessage;
        private string? _successMessage;
        private string? _receptorRfcFiltro;
        private string? _referenciaFiltro;
        private DateTimeOffset? _fechaInicioFiltro;
        private DateTimeOffset? _fechaFinFiltro;
        private bool _soloFiniquitadas;
        private bool _noFiniquitadas;
        private int _movimientosNcCount;
        private decimal _movimientosNcTotal;
        private bool _mostrarMovimientosNoConciliados = true;
        private ObservableCollection<ReporteFinancieroFacturacionCabeceraDto> _clientesHistorial = new();
        private ReporteFinancieroFacturacionCabeceraDto? _clienteHistorialSeleccionado;
        private DateTimeOffset? _fechaInicioHistorial;
        private DateTimeOffset? _fechaFinHistorial;
        private bool _isGenerandoHistorial;
        private string? _historialProgresoTexto;
        private HistorialCobranzaResultadoDto? _historialResultado;

        public RPTFinancieroFacturacionViewModel(
            IReporteFinancieroFacturacionService reporteService,
            IReporteFinancieroFacturacionExportService exportService,
            IHistorialCobranzaService historialService)
        {
            _reporteService = reporteService ?? throw new ArgumentNullException(nameof(reporteService));
            _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
            _historialService = historialService ?? throw new ArgumentNullException(nameof(historialService));
        }

        public ObservableCollection<ReporteFinancieroFacturacionListadoItemDto> ListadoItems
        {
            get => _listadoItems;
            private set
            {
                if (SetProperty(ref _listadoItems, value))
                {
                    NotificarResumenes();
                }
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (SetProperty(ref _isLoading, value))
                {
                    OnPropertyChanged(nameof(CanGenerarReporte));
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

        public string? ReceptorRfcFiltro
        {
            get => _receptorRfcFiltro;
            set => SetProperty(ref _receptorRfcFiltro, value);
        }

        public string? ReferenciaFiltro
        {
            get => _referenciaFiltro;
            set => SetProperty(ref _referenciaFiltro, value);
        }

        public DateTimeOffset? FechaInicioFiltro
        {
            get => _fechaInicioFiltro;
            set => SetProperty(ref _fechaInicioFiltro, value);
        }

        public DateTimeOffset? FechaFinFiltro
        {
            get => _fechaFinFiltro;
            set => SetProperty(ref _fechaFinFiltro, value);
        }

        public bool SoloFiniquitadas
        {
            get => _soloFiniquitadas;
            set
            {
                if (SetProperty(ref _soloFiniquitadas, value) && value && NoFiniquitadas)
                {
                    NoFiniquitadas = false;
                }
            }
        }

        public bool NoFiniquitadas
        {
            get => _noFiniquitadas;
            set
            {
                if (SetProperty(ref _noFiniquitadas, value) && value && SoloFiniquitadas)
                {
                    SoloFiniquitadas = false;
                }
            }
        }

        public string TotalFacturadoTexto => _cabecerasBase.Sum(item => item.TotalFacturado).ToString("C2", new CultureInfo("es-MX"));
        public string TotalAbonadoTexto => _cabecerasBase.Sum(item => item.TotalAbonadoMovimientos).ToString("C2", new CultureInfo("es-MX"));
        public string ResumenCabecerasTexto => $"{_cabecerasBase.Sum(item => item.NumeroFacturas)} factura(s) en {_cabecerasBase.Count} cliente(s)";
        public string ResumenDetalleTexto => $"{_detallesBase.Count} detalle(s) visibles en el listado";
        public string TotalMovimientosNoConciliadosTexto => _movimientosNcTotal.ToString("C2", new CultureInfo("es-MX"));
        public string NumMovimientosNoConciliadosTexto => $"{_movimientosNcCount} movimiento(s) sin conciliar";
        public string TotalRestanteTexto
        {
            get
            {
                var totalFacturado = _cabecerasBase.Sum(item => item.TotalFacturado);
                var totalFiniquitado = _cabecerasBase.Sum(item => item.TotalAbonadoMovimientos);
                var restante = totalFacturado - totalFiniquitado - _movimientosNcTotal;
                return restante.ToString("C2", new CultureInfo("es-MX"));
            }
        }
        public bool CanGenerarReporte => !IsLoading && _detallesBase.Count > 0;

        public bool MostrarMovimientosNoConciliados
        {
            get => _mostrarMovimientosNoConciliados;
            set => SetProperty(ref _mostrarMovimientosNoConciliados, value);
        }

        public async Task CargarReporteAsync()
        {
            if (FechaInicioFiltro.HasValue && FechaFinFiltro.HasValue && FechaFinFiltro.Value.Date < FechaInicioFiltro.Value.Date)
            {
                ErrorMessage = "La fecha fin no puede ser menor que la fecha inicio.";
                SuccessMessage = null;
                return;
            }

            try
            {
                IsLoading = true;
                ErrorMessage = null;
                SuccessMessage = null;

                var resultado = await _reporteService.ObtenerReporteAsync(
                    string.IsNullOrWhiteSpace(ReceptorRfcFiltro) ? null : ReceptorRfcFiltro.Trim().ToUpperInvariant(),
                    ObtenerFiniquitoFiltro(),
                    string.IsNullOrWhiteSpace(ReferenciaFiltro) ? null : ReferenciaFiltro.Trim(),
                    FechaInicioFiltro,
                    FechaFinFiltro);

                _cabecerasBase.Clear();
                _cabecerasBase.AddRange(resultado.Cabeceras.OrderBy(item => item.ReceptorNombreTexto).ThenBy(item => item.ReceptorRfcTexto));
                _detallesBase.Clear();
                _detallesBase.AddRange(resultado.Detalles.OrderBy(item => item.ReceptorNombreTexto).ThenByDescending(item => item.FechaTimbrado).ThenBy(item => item.FolioTexto));

                _movimientosNcCount = resultado.MovimientosNoConciliadosCount;
                _movimientosNcTotal = resultado.MovimientosNoConciliadosTotal;

                ListadoItems = new ObservableCollection<ReporteFinancieroFacturacionListadoItemDto>(ConstruirListadoVertical());

                SuccessMessage = _detallesBase.Count == 0
                    ? "No se encontraron registros con los filtros actuales."
                    : $"Reporte cargado correctamente. Se obtuvieron {_detallesBase.Count} factura(s).";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al cargar el reporte financiero de facturación: {ex.Message}";
                ListadoItems = new ObservableCollection<ReporteFinancieroFacturacionListadoItemDto>();
            }
            finally
            {
                IsLoading = false;
            }
        }

        public void LimpiarFiltros()
        {
            ReceptorRfcFiltro = null;
            ReferenciaFiltro = null;
            FechaInicioFiltro = null;
            FechaFinFiltro = null;
            SoloFiniquitadas = false;
            NoFiniquitadas = false;
        }

        public async Task<string> GenerarReporteAsync()
        {
            if (_detallesBase.Count == 0)
            {
                throw new InvalidOperationException("No hay registros visibles para generar el reporte.");
            }

            try
            {
                IsLoading = true;
                ErrorMessage = null;
                SuccessMessage = null;

                var rutaArchivo = await _exportService.GenerarReportePdfAsync(
                    _cabecerasBase,
                    _detallesBase,
                    ReceptorRfcFiltro,
                    ReferenciaFiltro,
                    FechaInicioFiltro,
                    FechaFinFiltro,
                    ObtenerFiniquitoFiltro(),
                    _movimientosNcCount,
                    _movimientosNcTotal,
                    _mostrarMovimientosNoConciliados);

                SuccessMessage = $"Reporte generado correctamente: {rutaArchivo}";
                return rutaArchivo;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al generar el reporte financiero de facturación: {ex.Message}";
                SuccessMessage = null;
                throw;
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task<string> GenerarReporteSimplificadoAsync()
        {
            if (_detallesBase.Count == 0)
            {
                throw new InvalidOperationException("No hay registros visibles para generar el reporte simplificado.");
            }

            try
            {
                IsLoading = true;
                ErrorMessage = null;
                SuccessMessage = null;

                var rutaArchivo = await _exportService.GenerarReporteSimplificadoPdfAsync(
                    _cabecerasBase,
                    _detallesBase,
                    ReceptorRfcFiltro,
                    ReferenciaFiltro,
                    FechaInicioFiltro,
                    FechaFinFiltro,
                    ObtenerFiniquitoFiltro(),
                    _movimientosNcCount,
                    _movimientosNcTotal,
                    _mostrarMovimientosNoConciliados);

                SuccessMessage = $"Reporte simplificado generado: {rutaArchivo}";
                return rutaArchivo;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al generar el reporte simplificado: {ex.Message}";
                SuccessMessage = null;
                throw;
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task<string> GenerarReporteCobranzaAsync()
        {
            if (FechaInicioFiltro.HasValue && FechaFinFiltro.HasValue && FechaFinFiltro.Value.Date < FechaInicioFiltro.Value.Date)
            {
                throw new InvalidOperationException("La fecha fin no puede ser menor que la fecha inicio.");
            }

            try
            {
                IsLoading = true;
                ErrorMessage = null;
                SuccessMessage = null;

                var items = await _reporteService.ObtenerReporteCobranzaAsync(
                    string.IsNullOrWhiteSpace(ReceptorRfcFiltro) ? null : ReceptorRfcFiltro.Trim().ToUpperInvariant(),
                    ObtenerFiniquitoFiltro(),
                    string.IsNullOrWhiteSpace(ReferenciaFiltro) ? null : ReferenciaFiltro.Trim(),
                    FechaInicioFiltro,
                    FechaFinFiltro);

                if (items.Count == 0)
                {
                    throw new InvalidOperationException("No hay facturas con los filtros actuales para generar el reporte de cobranza.");
                }

                var rutaArchivo = await _exportService.GenerarReporteCobranzaPdfAsync(
                    items,
                    ReceptorRfcFiltro,
                    ReferenciaFiltro,
                    FechaInicioFiltro,
                    FechaFinFiltro,
                    ObtenerFiniquitoFiltro());

                SuccessMessage = $"Reporte de cobranza generado: {rutaArchivo}";
                return rutaArchivo;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al generar el reporte de cobranza: {ex.Message}";
                SuccessMessage = null;
                throw;
            }
            finally
            {
                IsLoading = false;
            }
        }

        private IEnumerable<ReporteFinancieroFacturacionListadoItemDto> ConstruirListadoVertical()
        {
            foreach (var cabecera in _cabecerasBase)
            {
                yield return new ReporteFinancieroFacturacionListadoItemDto
                {
                    Cabecera = cabecera
                };

                foreach (var detalle in _detallesBase.Where(item => string.Equals(item.ReceptorRfc, cabecera.ReceptorRfc, StringComparison.OrdinalIgnoreCase)))
                {
                    yield return new ReporteFinancieroFacturacionListadoItemDto
                    {
                        Detalle = detalle
                    };
                }
            }
        }

        private void NotificarResumenes()
        {
            OnPropertyChanged(nameof(TotalFacturadoTexto));
            OnPropertyChanged(nameof(TotalAbonadoTexto));
            OnPropertyChanged(nameof(TotalMovimientosNoConciliadosTexto));
            OnPropertyChanged(nameof(NumMovimientosNoConciliadosTexto));
            OnPropertyChanged(nameof(TotalRestanteTexto));
            OnPropertyChanged(nameof(ResumenCabecerasTexto));
            OnPropertyChanged(nameof(ResumenDetalleTexto));
            OnPropertyChanged(nameof(CanGenerarReporte));
        }

        private bool? ObtenerFiniquitoFiltro()
        {
            if (SoloFiniquitadas)
            {
                return true;
            }

            if (NoFiniquitadas)
            {
                return false;
            }

            return null;
        }

        public ObservableCollection<ReporteFinancieroFacturacionCabeceraDto> ClientesHistorial
        {
            get => _clientesHistorial;
            private set => SetProperty(ref _clientesHistorial, value);
        }

        public ReporteFinancieroFacturacionCabeceraDto? ClienteHistorialSeleccionado
        {
            get => _clienteHistorialSeleccionado;
            set
            {
                if (SetProperty(ref _clienteHistorialSeleccionado, value))
                {
                    OnPropertyChanged(nameof(CanGenerarHistorial));
                }
            }
        }

        public DateTimeOffset? FechaInicioHistorial
        {
            get => _fechaInicioHistorial;
            set
            {
                if (SetProperty(ref _fechaInicioHistorial, value))
                {
                    OnPropertyChanged(nameof(CanGenerarHistorial));
                }
            }
        }

        public DateTimeOffset? FechaFinHistorial
        {
            get => _fechaFinHistorial;
            set
            {
                if (SetProperty(ref _fechaFinHistorial, value))
                {
                    OnPropertyChanged(nameof(CanGenerarHistorial));
                }
            }
        }

        public bool IsGenerandoHistorial
        {
            get => _isGenerandoHistorial;
            set
            {
                if (SetProperty(ref _isGenerandoHistorial, value))
                {
                    OnPropertyChanged(nameof(CanGenerarHistorial));
                }
            }
        }

        public string? HistorialProgresoTexto
        {
            get => _historialProgresoTexto;
            set => SetProperty(ref _historialProgresoTexto, value);
        }

        public HistorialCobranzaResultadoDto? HistorialResultado
        {
            get => _historialResultado;
            private set
            {
                if (SetProperty(ref _historialResultado, value))
                {
                    OnPropertyChanged(nameof(HistorialResumenTexto));
                }
            }
        }

        public string HistorialResumenTexto => HistorialResultado?.ResumenTexto ?? string.Empty;

        public bool CanGenerarHistorial =>
            !IsGenerandoHistorial &&
            ClienteHistorialSeleccionado != null &&
            FechaInicioHistorial.HasValue &&
            FechaFinHistorial.HasValue &&
            FechaFinHistorial.Value.Date >= FechaInicioHistorial.Value.Date;

        /// <summary>
        /// Carga la lista de clientes con facturas cargadas (sin filtros) para alimentar el
        /// selector de RFC del historial de cobranza, independiente de los filtros del reporte principal.
        /// </summary>
        public async Task CargarClientesHistorialAsync()
        {
            try
            {
                var resultado = await _reporteService.ObtenerReporteAsync(null, null, null, null, null);
                ClientesHistorial = new ObservableCollection<ReporteFinancieroFacturacionCabeceraDto>(
                    resultado.Cabeceras.OrderBy(item => item.ReceptorNombreTexto).ThenBy(item => item.ReceptorRfcTexto));
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al cargar el listado de clientes para historial: {ex.Message}";
            }
        }

        public async Task GenerarHistorialAsync(int idCliente, string? dirigidoA)
        {
            if (ClienteHistorialSeleccionado == null || !FechaInicioHistorial.HasValue || !FechaFinHistorial.HasValue)
            {
                throw new InvalidOperationException("Selecciona un cliente y un rango de fechas para generar el historial.");
            }

            if (string.IsNullOrWhiteSpace(ClienteHistorialSeleccionado.ReceptorRfc))
            {
                throw new InvalidOperationException("El cliente seleccionado no tiene RFC.");
            }

            try
            {
                IsGenerandoHistorial = true;
                ErrorMessage = null;
                SuccessMessage = null;
                HistorialResultado = null;
                HistorialProgresoTexto = "Iniciando...";

                var progreso = new Progress<string>(texto => HistorialProgresoTexto = texto);

                var resultado = await _historialService.GenerarHistorialAsync(
                    ClienteHistorialSeleccionado.ReceptorRfc,
                    ClienteHistorialSeleccionado.ReceptorNombreTexto,
                    idCliente,
                    FechaInicioHistorial.Value,
                    FechaFinHistorial.Value,
                    dirigidoA,
                    progreso);

                HistorialResultado = resultado;
                SuccessMessage = $"Historial generado en: {resultado.CarpetaHistorial} ({resultado.ResumenTexto})";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al generar el historial de cobranza: {ex.Message}";
                SuccessMessage = null;
                throw;
            }
            finally
            {
                IsGenerandoHistorial = false;
                HistorialProgresoTexto = null;
            }
        }
    }
}
