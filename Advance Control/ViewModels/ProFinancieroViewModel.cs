using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.Facturas;

namespace Advance_Control.ViewModels
{
    /// <summary>
    /// ViewModel del prototipo "Visor de factura" (grupo Financiero). El mecanismo de carga es
    /// genérico (recibe serie + folio), pero de momento este prototipo siempre apunta a la
    /// factura 962 (sin serie) mientras se valida el diseño. Candidato a sustituir el visor
    /// actual de Facturas (FacturasPage/DetailFacturaWindow) una vez aprobado.
    /// </summary>
    public class ProFinancieroViewModel : INotifyPropertyChanged
    {
        /// <summary>Serie objetivo mientras el visor está en prototipo (de momento hardcodeada a la factura 962, sin serie).</summary>
        public const string SeriePrototipo = "";

        /// <summary>Folio objetivo mientras el visor está en prototipo (de momento hardcodeada a la factura 962).</summary>
        public const string FolioPrototipo = "962";

        private readonly IFacturaService _facturaService;
        private readonly IFacturaPdfService _facturaPdfService;
        private readonly IComplementoPagoPdfService _complementoPagoPdfService;

        private bool _isLoading;
        private string? _errorMessage;
        private string? _successMessage;
        private FacturaResumenDto? _factura;
        private string? _pdfPath;

        public ProFinancieroViewModel(IFacturaService facturaService, IFacturaPdfService facturaPdfService, IComplementoPagoPdfService complementoPagoPdfService)
        {
            _facturaService = facturaService ?? throw new ArgumentNullException(nameof(facturaService));
            _facturaPdfService = facturaPdfService ?? throw new ArgumentNullException(nameof(facturaPdfService));
            _complementoPagoPdfService = complementoPagoPdfService ?? throw new ArgumentNullException(nameof(complementoPagoPdfService));
        }

        public bool IsLoading
        {
            get => _isLoading;
            private set => SetProperty(ref _isLoading, value);
        }

        public string? ErrorMessage
        {
            get => _errorMessage;
            private set
            {
                if (SetProperty(ref _errorMessage, value))
                {
                    OnPropertyChanged(nameof(HasError));
                }
            }
        }

        public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

        public string? SuccessMessage
        {
            get => _successMessage;
            private set
            {
                if (SetProperty(ref _successMessage, value))
                {
                    OnPropertyChanged(nameof(HasSuccess));
                }
            }
        }

        public bool HasSuccess => !string.IsNullOrWhiteSpace(SuccessMessage);

        public FacturaResumenDto? Factura
        {
            get => _factura;
            private set
            {
                if (SetProperty(ref _factura, value))
                {
                    OnPropertyChanged(nameof(ClienteTexto));
                    OnPropertyChanged(nameof(FolioTexto));
                    OnPropertyChanged(nameof(EstadoTexto));
                }
            }
        }

        /// <summary>Ruta del PDF ya generado en disco (misma mecánica que <see cref="IFacturaPdfService"/> usa hoy: QuestPDF escribe a Documentos\Advance Control\Facturas).</summary>
        public string? PdfPath
        {
            get => _pdfPath;
            private set
            {
                if (SetProperty(ref _pdfPath, value))
                {
                    OnPropertyChanged(nameof(HasPdf));
                }
            }
        }

        public bool HasPdf => !string.IsNullOrWhiteSpace(PdfPath);

        /// <summary>Complementos de pago (CFDI) ya timbrados que documentan abonos de esta factura.</summary>
        public ObservableCollection<ComplementoPagoRelacionadoDto> ComplementosRelacionados { get; } = new();

        public string ClienteTexto => Factura?.ReceptorNombre ?? "Sin cliente";

        public string FolioTexto => Factura?.FolioTitulo ?? string.Empty;

        /// <summary>
        /// Saldo insoluto real según el último Complemento de Pago timbrado (el que documenta la
        /// parcialidad más reciente) -- null si la factura no tiene ningún complemento ligado, en
        /// cuyo caso se cae de vuelta a los campos internos (abonos_factura / SaldoPendiente). Es
        /// necesario porque una factura pagada solo vía Complementos de Pago reales (sin usar nunca
        /// "Registrar abono") tiene TotalAbonado/SaldoPendiente en 0/Total: esos campos solo
        /// reflejan control interno, no lo que ya documentan los CFDI de pago timbrados.
        /// </summary>
        private decimal? RestanteSegunComplementos =>
            ComplementosRelacionados.Count == 0
                ? null
                : ComplementosRelacionados.OrderByDescending(c => c.NumParcialidad).First().ImpSaldoInsoluto;

        public string PagadoTexto
        {
            get
            {
                if (Factura == null)
                {
                    return string.Empty;
                }

                var restante = RestanteSegunComplementos;
                return restante.HasValue
                    ? (Factura.Total - restante.Value).ToString("C2", new CultureInfo("es-MX"))
                    : Factura.TotalAbonadoTexto;
            }
        }

        public string RestanteTexto
        {
            get
            {
                if (Factura == null)
                {
                    return string.Empty;
                }

                var restante = RestanteSegunComplementos;
                return restante.HasValue
                    ? restante.Value.ToString("C2", new CultureInfo("es-MX"))
                    : Factura.SaldoPendienteTexto;
            }
        }

        /// <summary>
        /// Variante de estatus de pago específica para este visor: distingue "Abierta" (sin
        /// ningún abono todavía) de "Parcialmente pagada" (con abono pero saldo pendiente > 0),
        /// a diferencia de FacturaResumenDto.EstadoPagoTexto que solo tiene 3 valores genéricos.
        /// Considera tanto abonos internos como Complementos de Pago reales.
        /// </summary>
        public string EstadoTexto
        {
            get
            {
                if (Factura == null)
                {
                    return string.Empty;
                }

                var restanteComplementos = RestanteSegunComplementos;
                var restante = restanteComplementos ?? Factura.SaldoPendiente;

                if (Factura.Finiquito == true || restante <= 0)
                {
                    return "Pagada";
                }

                var tieneAlgunPago = restanteComplementos.HasValue || Factura.NumeroAbonos > 0;
                return tieneAlgunPago ? "Parcialmente pagada" : "Abierta";
            }
        }

        /// <summary>
        /// Mecanismo genérico: resuelve la factura por serie + folio, trae su detalle completo
        /// y genera su PDF. Cualquier pantalla futura que reemplace el visor actual puede llamar
        /// esto con la serie/folio real que el usuario seleccione.
        /// </summary>
        public async Task CargarFacturaAsync(string serie, string folio)
        {
            IsLoading = true;
            ErrorMessage = null;
            PdfPath = null;
            Factura = null;
            ComplementosRelacionados.Clear();

            try
            {
                var resumen = await _facturaService.BuscarFacturaPorFolioAsync(folio, serie);
                if (resumen == null)
                {
                    ErrorMessage = $"No se encontró ninguna factura con serie \"{serie}\" y folio \"{folio}\".";
                    return;
                }

                var detalle = await _facturaService.ObtenerDetalleFacturaAsync(resumen.IdFactura);
                if (detalle?.Factura == null)
                {
                    ErrorMessage = "No se pudo consultar el detalle completo de la factura.";
                    return;
                }

                Factura = detalle.Factura;
                PdfPath = await _facturaPdfService.GenerarFacturaPdfAsync(detalle);

                foreach (var complemento in detalle.ComplementosRelacionados)
                {
                    ComplementosRelacionados.Add(complemento);
                }

                OnPropertyChanged(nameof(PagadoTexto));
                OnPropertyChanged(nameof(RestanteTexto));
                OnPropertyChanged(nameof(EstadoTexto));
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al cargar la factura: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>Recarga la misma factura del prototipo (A7) -- se usa tras cualquier acción que cambie su estado.</summary>
        private Task RecargarAsync() => CargarFacturaAsync(SeriePrototipo, FolioPrototipo);

        public Task<string?> ObtenerXmlAsync()
        {
            return Factura == null
                ? Task.FromResult<string?>(null)
                : _facturaService.ObtenerXmlFacturaAsync(Factura.IdFactura);
        }

        /// <summary>Registra un abono interno (control de cobranza, no timbra nada ante el SAT).</summary>
        public async Task<RegistrarAbonoFacturaResponseDto> RegistrarAbonoAsync(RegistrarAbonoFacturaRequestDto request)
        {
            try
            {
                IsLoading = true;
                ErrorMessage = null;
                SuccessMessage = null;

                var resultado = await _facturaService.RegistrarAbonoAsync(request);
                if (resultado.Success)
                {
                    SuccessMessage = string.IsNullOrWhiteSpace(resultado.Message) ? "Abono registrado correctamente." : resultado.Message;
                    await RecargarAsync();
                }
                else
                {
                    ErrorMessage = string.IsNullOrWhiteSpace(resultado.Message) ? "No se pudo registrar el abono." : resultado.Message;
                }

                return resultado;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al registrar el abono: {ex.Message}";
                return new RegistrarAbonoFacturaResponseDto { Success = false, Message = ex.Message };
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>Abonos PPD propias del receptor todavía no incluidos en ningún Complemento de Pago real.</summary>
        public Task<System.Collections.Generic.List<AbonoPendienteComplementoDto>> ObtenerAbonosPendientesComplementoAsync(string receptorRfc)
            => _facturaService.ObtenerAbonosPendientesComplementoAsync(receptorRfc);

        /// <summary>Arma, sella y timbra vía FEL Bilkon un Complemento de Pago real a partir de abonos ya registrados.</summary>
        public async Task<TimbrarResultadoDto> GenerarComplementoPagoAsync(GenerarComplementoPagoRequestDto request)
        {
            try
            {
                IsLoading = true;
                ErrorMessage = null;
                SuccessMessage = null;

                var resultado = await _facturaService.GenerarComplementoPagoAsync(request);
                if (resultado.Success)
                {
                    SuccessMessage = string.IsNullOrWhiteSpace(resultado.Message) ? "Complemento de pago generado y timbrado correctamente." : resultado.Message;
                    await RecargarAsync();
                }
                else
                {
                    ErrorMessage = string.IsNullOrWhiteSpace(resultado.Message) ? "No se pudo generar el complemento de pago." : resultado.Message;
                }

                return resultado;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al generar el complemento de pago: {ex.Message}";
                return new TimbrarResultadoDto { Success = false, Message = ex.Message };
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Cancela ante el SAT (vía FEL Bilkon) el CFDI de <paramref name="idFactura"/> -- sirve tanto
        /// para cancelar la factura principal como, pasando el id de un renglón de
        /// <see cref="ComplementosRelacionados"/>, para cancelar un Complemento de Pago específico
        /// (también es una factura en la base de datos). Acción irreversible.
        /// </summary>
        public async Task<CancelarCfdiResponseDto?> CancelarCfdiAsync(int idFactura, CancelarCfdiRequestDto request)
        {
            try
            {
                IsLoading = true;
                ErrorMessage = null;
                SuccessMessage = null;

                var resultado = await _facturaService.CancelarCfdiAsync(idFactura, request);
                if (resultado.Success && resultado.Cancelada)
                {
                    SuccessMessage = "CFDI cancelado ante el SAT.";
                    await RecargarAsync();
                }
                else
                {
                    ErrorMessage = resultado.MensajeResultado ?? resultado.Message ?? "Bilkon no confirmó la cancelación del CFDI.";
                }

                return resultado;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al cancelar el CFDI: {ex.Message}";
                return null;
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>Genera el PDF de un Complemento de Pago relacionado (para el botón PDF de cada renglón de la lista).</summary>
        public async Task<string?> GenerarPdfComplementoAsync(int idFacturaComplemento)
        {
            try
            {
                ErrorMessage = null;
                var detalle = await _facturaService.ObtenerComplementoPagoDetalleAsync(idFacturaComplemento);
                if (detalle?.Factura == null)
                {
                    ErrorMessage = "No se encontró el complemento de pago seleccionado.";
                    return null;
                }

                return await _complementoPagoPdfService.GenerarComplementoPagoPdfAsync(detalle);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al generar el PDF del complemento de pago: {ex.Message}";
                return null;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value))
            {
                return false;
            }

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        private void OnPropertyChanged(string? propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
