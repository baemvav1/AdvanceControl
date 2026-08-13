using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.Email;
using Advance_Control.Services.Reportes;

namespace Advance_Control.ViewModels
{
    public class RPTFinancieroFacturacionViewModel : ViewModelBase
    {
        private readonly IReporteFinancieroFacturacionService _reporteService;
        private readonly IReporteFinancieroFacturacionExportService _exportService;
        private readonly IHistorialCobranzaService _historialService;
        private readonly IEmailService _emailService;
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
        private bool _isGenerandoHistorial;
        private string? _historialProgresoTexto;
        private HistorialCobranzaResultadoDto? _historialResultado;

        public RPTFinancieroFacturacionViewModel(
            IReporteFinancieroFacturacionService reporteService,
            IReporteFinancieroFacturacionExportService exportService,
            IHistorialCobranzaService historialService,
            IEmailService emailService)
        {
            _reporteService = reporteService ?? throw new ArgumentNullException(nameof(reporteService));
            _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
            _historialService = historialService ?? throw new ArgumentNullException(nameof(historialService));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
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
            set
            {
                if (SetProperty(ref _receptorRfcFiltro, value))
                {
                    OnPropertyChanged(nameof(CanGenerarHistorial));
                }
            }
        }

        public string? ReferenciaFiltro
        {
            get => _referenciaFiltro;
            set => SetProperty(ref _referenciaFiltro, value);
        }

        public DateTimeOffset? FechaInicioFiltro
        {
            get => _fechaInicioFiltro;
            set
            {
                if (SetProperty(ref _fechaInicioFiltro, value))
                {
                    OnPropertyChanged(nameof(CanGenerarHistorial));
                }
            }
        }

        public DateTimeOffset? FechaFinFiltro
        {
            get => _fechaFinFiltro;
            set
            {
                if (SetProperty(ref _fechaFinFiltro, value))
                {
                    OnPropertyChanged(nameof(CanGenerarHistorial));
                }
            }
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

        public async Task<string> EnviarAlertaCobranzaAsync(
            ReporteFinancieroFacturacionCabeceraDto cliente,
            IReadOnlyList<string> destinatarios)
        {
            if (cliente == null || string.IsNullOrWhiteSpace(cliente.ReceptorRfc))
            {
                throw new InvalidOperationException("El cliente seleccionado no tiene RFC.");
            }

            if (destinatarios == null || destinatarios.Count == 0)
            {
                throw new InvalidOperationException("Selecciona al menos un contacto con correo para enviar la alerta.");
            }

            try
            {
                IsLoading = true;
                ErrorMessage = null;
                SuccessMessage = null;

                var items = await _reporteService.ObtenerReporteCobranzaAsync(cliente.ReceptorRfc, null, null, null, null);
                if (items.Count == 0)
                {
                    throw new InvalidOperationException("El cliente no tiene facturas pendientes para incluir en la alerta de cobranza.");
                }

                var rutaPdf = await _exportService.GenerarReporteCobranzaPdfAsync(items, cliente.ReceptorRfc, null, null, null, null);
                var pdfBytes = await File.ReadAllBytesAsync(rutaPdf);

                var mensaje = new EmailMessage
                {
                    Para = destinatarios.ToList(),
                    Asunto = $"Alerta de cobranza - {cliente.ReceptorNombreTexto}",
                    CuerpoTexto =
                        $"Estimado(a),{Environment.NewLine}{Environment.NewLine}" +
                        $"Le compartimos el estado de cuenta de facturación pendiente de {cliente.ReceptorNombreTexto} (RFC {cliente.ReceptorRfcTexto}).{Environment.NewLine}{Environment.NewLine}" +
                        $"Saldo pendiente: {(cliente.TotalFacturado - cliente.TotalAbonadoMovimientos).ToString("C2", new CultureInfo("es-MX"))}{Environment.NewLine}" +
                        $"Facturas pendientes: {cliente.NumeroFacturasNoFiniquitadas}{Environment.NewLine}{Environment.NewLine}" +
                        "Se adjunta el detalle. Quedamos atentos.",
                    Adjuntos = { (Path.GetFileName(rutaPdf), pdfBytes) },
                    CarpetaCliente = cliente.ReceptorNombreTexto
                };

                await _emailService.SendEmailAsync(mensaje);

                SuccessMessage = $"Alerta de cobranza enviada a {destinatarios.Count} contacto(s) de {cliente.ReceptorNombreTexto}.";
                return rutaPdf;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al enviar la alerta de cobranza: {ex.Message}";
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
                    OnPropertyChanged(nameof(HistorialErrores));
                    OnPropertyChanged(nameof(HistorialTieneErrores));
                }
            }
        }

        public string HistorialResumenTexto => HistorialResultado?.ResumenTexto ?? string.Empty;

        public ObservableCollection<string> HistorialErrores => HistorialResultado?.Errores.Count > 0
            ? new ObservableCollection<string>(HistorialResultado.Errores)
            : new ObservableCollection<string>();

        public bool HistorialTieneErrores => HistorialResultado?.Errores.Count > 0;

        public bool CanGenerarHistorial =>
            !IsGenerandoHistorial &&
            !string.IsNullOrWhiteSpace(ReceptorRfcFiltro) &&
            FechaInicioFiltro.HasValue &&
            FechaFinFiltro.HasValue &&
            FechaFinFiltro.Value.Date >= FechaInicioFiltro.Value.Date;

        /// <summary>
        /// Genera el historial de cobranza del cliente identificado por ReceptorRfcFiltro,
        /// usando el mismo rango de fechas (FechaInicioFiltro/FechaFinFiltro) que el reporte
        /// principal. idCliente y nombreCliente se resuelven en la página a partir del RFC.
        /// </summary>
        public async Task GenerarHistorialAsync(int idCliente, string nombreCliente, string? dirigidoA)
        {
            if (string.IsNullOrWhiteSpace(ReceptorRfcFiltro))
            {
                throw new InvalidOperationException("Selecciona un cliente (RFC o nombre) para generar el historial.");
            }

            if (!FechaInicioFiltro.HasValue || !FechaFinFiltro.HasValue)
            {
                throw new InvalidOperationException("Selecciona un rango de fechas para generar el historial.");
            }

            if (idCliente <= 0)
            {
                throw new InvalidOperationException("El cliente seleccionado no es válido.");
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
                    ReceptorRfcFiltro.Trim().ToUpperInvariant(),
                    nombreCliente,
                    idCliente,
                    FechaInicioFiltro.Value,
                    FechaFinFiltro.Value,
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
