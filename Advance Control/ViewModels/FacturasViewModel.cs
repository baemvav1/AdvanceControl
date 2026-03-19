using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Advance_Control.Models;
using Advance_Control.Services.Facturas;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace Advance_Control.ViewModels
{
    public class FacturasViewModel : ViewModelBase
    {
        private readonly IFacturaService _facturaService;
        private readonly List<FacturaResumenDto> _facturasExistentesBase = new();
        private GuardarFacturaRequestDto? _facturaActualRequest;
        private FacturaResumenDto? _facturaActual;
        private ObservableCollection<FacturaConceptoDto> _conceptosFactura;
        private ObservableCollection<FacturaTrasladoDto> _trasladosGlobales;
        private ObservableCollection<FacturaResumenDto> _facturasExistentes;
        private bool _isLoading;
        private string? _errorMessage;
        private string? _successMessage;
        private DateTimeOffset? _fechaFiltroDesde;
        private DateTimeOffset? _fechaFiltroHasta;
        private string? _folioFiltro;
        private string? _receptorFiltro;
        private string? _metodoPagoFiltro;
        private string? _totalFiltro;

        public FacturasViewModel(IFacturaService facturaService)
        {
            _facturaService = facturaService ?? throw new ArgumentNullException(nameof(facturaService));
            _conceptosFactura = new ObservableCollection<FacturaConceptoDto>();
            _trasladosGlobales = new ObservableCollection<FacturaTrasladoDto>();
            _facturasExistentes = new ObservableCollection<FacturaResumenDto>();
        }

        public FacturaResumenDto? FacturaActual
        {
            get => _facturaActual;
            set
            {
                if (SetProperty(ref _facturaActual, value))
                {
                    OnPropertyChanged(nameof(CanSave));
                }
            }
        }

        public ObservableCollection<FacturaConceptoDto> ConceptosFactura
        {
            get => _conceptosFactura;
            set => SetProperty(ref _conceptosFactura, value);
        }

        public ObservableCollection<FacturaTrasladoDto> TrasladosGlobales
        {
            get => _trasladosGlobales;
            set => SetProperty(ref _trasladosGlobales, value);
        }

        public ObservableCollection<FacturaResumenDto> FacturasExistentes
        {
            get => _facturasExistentes;
            set => SetProperty(ref _facturasExistentes, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (SetProperty(ref _isLoading, value))
                {
                    OnPropertyChanged(nameof(CanSave));
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

        public DateTimeOffset? FechaFiltroDesde
        {
            get => _fechaFiltroDesde;
            set
            {
                if (SetProperty(ref _fechaFiltroDesde, value))
                {
                    AplicarFiltrosFacturasExistentes();
                }
            }
        }

        public DateTimeOffset? FechaFiltroHasta
        {
            get => _fechaFiltroHasta;
            set
            {
                if (SetProperty(ref _fechaFiltroHasta, value))
                {
                    AplicarFiltrosFacturasExistentes();
                }
            }
        }

        public string? FolioFiltro
        {
            get => _folioFiltro;
            set
            {
                if (SetProperty(ref _folioFiltro, value))
                {
                    AplicarFiltrosFacturasExistentes();
                }
            }
        }

        public string? ReceptorFiltro
        {
            get => _receptorFiltro;
            set
            {
                if (SetProperty(ref _receptorFiltro, value))
                {
                    AplicarFiltrosFacturasExistentes();
                }
            }
        }

        public string? MetodoPagoFiltro
        {
            get => _metodoPagoFiltro;
            set
            {
                if (SetProperty(ref _metodoPagoFiltro, value))
                {
                    AplicarFiltrosFacturasExistentes();
                }
            }
        }

        public string? TotalFiltro
        {
            get => _totalFiltro;
            set
            {
                if (SetProperty(ref _totalFiltro, value))
                {
                    AplicarFiltrosFacturasExistentes();
                }
            }
        }

        public bool CanSave => _facturaActualRequest != null && ConceptosFactura.Count > 0 && !IsLoading;
        public string ResumenFacturasExistentes => $"{FacturasExistentes.Count} facturas mostradas";

        public async Task CargarArchivoXmlAsync(nint windowHandle)
        {
            try
            {
                IsLoading = true;
                ErrorMessage = null;
                SuccessMessage = null;

                var picker = new FileOpenPicker
                {
                    ViewMode = PickerViewMode.List,
                    SuggestedStartLocation = PickerLocationId.DocumentsLibrary
                };
                picker.FileTypeFilter.Add(".xml");

                WinRT.Interop.InitializeWithWindow.Initialize(picker, windowHandle);
                var file = await picker.PickSingleFileAsync();
                if (file != null)
                {
                    var xmlContent = await FileIO.ReadTextAsync(file);
                    var request = ConstruirFacturaDesdeXml(xmlContent);
                    var result = await ValidarYGuardarFacturaAsync(request);
                    AplicarFacturaActual(request);

                    if (!result.Success && !string.Equals(result.Accion, "existente", StringComparison.OrdinalIgnoreCase))
                    {
                        ErrorMessage = string.IsNullOrWhiteSpace(result.Message)
                            ? $"No se pudo guardar la factura {file.Name}."
                            : $"{file.Name}: {result.Message}";
                        SuccessMessage = null;
                        return;
                    }

                    await ActualizarFacturasExistentesAsync();
                    SuccessMessage = string.IsNullOrWhiteSpace(result.Message)
                        ? $"Archivo {file.Name} cargado y guardado exitosamente."
                        : $"{file.Name}: {result.Message}";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al cargar la factura XML: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task CargarYGuardarMultiplesFacturasAsync(nint windowHandle)
        {
            try
            {
                IsLoading = true;
                ErrorMessage = null;
                SuccessMessage = null;

                var picker = new FileOpenPicker
                {
                    ViewMode = PickerViewMode.List,
                    SuggestedStartLocation = PickerLocationId.DocumentsLibrary
                };
                picker.FileTypeFilter.Add(".xml");

                WinRT.Interop.InitializeWithWindow.Initialize(picker, windowHandle);
                var files = await picker.PickMultipleFilesAsync();
                if (files == null || files.Count == 0)
                {
                    return;
                }

                var cargadas = 0;
                var duplicadas = 0;
                var fallidas = 0;
                var errores = new List<string>();
                GuardarFacturaRequestDto? ultimaFacturaValida = null;

                foreach (var file in files)
                {
                    try
                    {
                        var xmlContent = await FileIO.ReadTextAsync(file);
                        var request = ConstruirFacturaDesdeXml(xmlContent);

                        if (string.IsNullOrWhiteSpace(request.Folio))
                        {
                            fallidas++;
                            errores.Add($"{file.Name}: no contiene folio.");
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(request.EmisorRfc))
                        {
                            fallidas++;
                            errores.Add($"{file.Name}: no contiene RFC del emisor.");
                            continue;
                        }

                        if (request.Conceptos.Count == 0)
                        {
                            fallidas++;
                            errores.Add($"{file.Name}: no contiene conceptos.");
                            continue;
                        }

                        var result = await ValidarYGuardarFacturaAsync(request);
                        ultimaFacturaValida = request;

                        if (result.Success)
                        {
                            cargadas++;
                            continue;
                        }

                        if (string.Equals(result.Accion, "existente", StringComparison.OrdinalIgnoreCase))
                        {
                            duplicadas++;
                            continue;
                        }

                        fallidas++;
                        errores.Add($"{file.Name}: {result.Message}");
                    }
                    catch (Exception ex)
                    {
                        fallidas++;
                        errores.Add($"{file.Name}: {ex.Message}");
                    }
                }

                if (ultimaFacturaValida != null)
                {
                    AplicarFacturaActual(ultimaFacturaValida);
                }

                await ActualizarFacturasExistentesAsync();

                SuccessMessage = $"Carga masiva finalizada. Nuevas: {cargadas}. Duplicadas: {duplicadas}. Fallidas: {fallidas}.";
                ErrorMessage = errores.Count > 0
                    ? string.Join(" | ", errores.Take(3)) + (errores.Count > 3 ? $" | Y {errores.Count - 3} error(es) adicional(es)." : string.Empty)
                    : null;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al cargar varias facturas XML: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task GuardarFacturaAsync()
        {
            if (_facturaActualRequest == null)
            {
                ErrorMessage = "Primero debes cargar una factura XML.";
                return;
            }

            if (string.IsNullOrWhiteSpace(_facturaActualRequest.Folio))
            {
                ErrorMessage = "La factura debe incluir un folio para poder guardarse.";
                return;
            }

            if (string.IsNullOrWhiteSpace(_facturaActualRequest.EmisorRfc))
            {
                ErrorMessage = "La factura debe incluir el RFC del emisor.";
                return;
            }

            try
            {
                IsLoading = true;
                ErrorMessage = null;
                SuccessMessage = null;

                var result = await _facturaService.GuardarFacturaAsync(_facturaActualRequest);
                if (!result.Success && !string.Equals(result.Accion, "existente", StringComparison.OrdinalIgnoreCase))
                {
                    ErrorMessage = string.IsNullOrWhiteSpace(result.Message)
                        ? "No se pudo guardar la factura."
                        : result.Message;
                    return;
                }

                if (FacturaActual != null)
                {
                    FacturaActual.IdFactura = result.IdFactura;
                }

                await ActualizarFacturasExistentesAsync();
                SuccessMessage = string.IsNullOrWhiteSpace(result.Message)
                    ? "Factura guardada correctamente."
                    : $"{result.Message} Conceptos procesados: {result.ConceptosProcesados}.";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al guardar la factura: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task CargarFacturasExistentesAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = null;

                await ActualizarFacturasExistentesAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al consultar las facturas existentes: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        public void LimpiarFiltrosFacturas()
        {
            FechaFiltroDesde = null;
            FechaFiltroHasta = null;
            FolioFiltro = null;
            ReceptorFiltro = null;
            MetodoPagoFiltro = null;
            TotalFiltro = null;
            AplicarFiltrosFacturasExistentes();
        }

        private GuardarFacturaRequestDto ConstruirFacturaDesdeXml(string xmlContent)
        {
            var doc = XDocument.Parse(xmlContent);
            var comprobante = doc.Root ?? throw new InvalidOperationException("El XML no contiene el nodo Comprobante.");
            var emisor = ElementByLocalName(comprobante, "Emisor");
            var receptor = ElementByLocalName(comprobante, "Receptor");
            var impuestos = ElementByLocalName(comprobante, "Impuestos");
            var timbre = ElementByLocalName(ElementByLocalName(comprobante, "Complemento"), "TimbreFiscalDigital");

            var conceptos = comprobante
                .Elements()
                .FirstOrDefault(element => string.Equals(element.Name.LocalName, "Conceptos", StringComparison.OrdinalIgnoreCase))?
                .Elements()
                .Where(element => string.Equals(element.Name.LocalName, "Concepto", StringComparison.OrdinalIgnoreCase))
                .Select((concepto, index) => CrearConcepto(concepto, index + 1))
                .ToList() ?? new List<FacturaConceptoDto>();

            var trasladosGlobales = impuestos?
                .Elements()
                .FirstOrDefault(element => string.Equals(element.Name.LocalName, "Traslados", StringComparison.OrdinalIgnoreCase))?
                .Elements()
                .Where(element => string.Equals(element.Name.LocalName, "Traslado", StringComparison.OrdinalIgnoreCase))
                .Select((traslado, index) => CrearTraslado(traslado, index + 1))
                .ToList() ?? new List<FacturaTrasladoDto>();

            var request = new GuardarFacturaRequestDto
            {
                VersionXml = GetStringAttr(comprobante, "Version") ?? "4.0",
                Folio = GetStringAttr(comprobante, "Folio"),
                Fecha = GetDateTimeAttr(comprobante, "Fecha") ?? DateTime.Now,
                FormaPago = GetStringAttr(comprobante, "FormaPago"),
                NoCertificado = GetStringAttr(comprobante, "NoCertificado"),
                Certificado = GetStringAttr(comprobante, "Certificado"),
                Sello = GetStringAttr(comprobante, "Sello"),
                CondicionesDePago = GetStringAttr(comprobante, "CondicionesDePago"),
                SubTotal = GetDecimalAttr(comprobante, "SubTotal"),
                Moneda = GetStringAttr(comprobante, "Moneda") ?? "MXN",
                Total = GetDecimalAttr(comprobante, "Total"),
                TipoDeComprobante = GetStringAttr(comprobante, "TipoDeComprobante"),
                Exportacion = GetStringAttr(comprobante, "Exportacion"),
                MetodoPago = GetStringAttr(comprobante, "MetodoPago"),
                LugarExpedicion = GetStringAttr(comprobante, "LugarExpedicion"),
                TotalImpuestosTrasladados = GetDecimalAttr(impuestos, "TotalImpuestosTrasladados"),
                EmisorRfc = GetStringAttr(emisor, "Rfc"),
                EmisorNombre = GetStringAttr(emisor, "Nombre"),
                EmisorRegimenFiscal = GetStringAttr(emisor, "RegimenFiscal"),
                ReceptorRfc = GetStringAttr(receptor, "Rfc"),
                ReceptorNombre = GetStringAttr(receptor, "Nombre"),
                ReceptorDomicilioFiscal = GetStringAttr(receptor, "DomicilioFiscalReceptor"),
                ReceptorRegimenFiscal = GetStringAttr(receptor, "RegimenFiscalReceptor"),
                ReceptorUsoCfdi = GetStringAttr(receptor, "UsoCFDI"),
                Uuid = GetStringAttr(timbre, "UUID"),
                FechaTimbrado = GetDateTimeAttr(timbre, "FechaTimbrado"),
                RfcProvCertif = GetStringAttr(timbre, "RfcProvCertif"),
                NoCertificadoSat = GetStringAttr(timbre, "NoCertificadoSAT"),
                SelloCfd = GetStringAttr(timbre, "SelloCFD"),
                SelloSat = GetStringAttr(timbre, "SelloSAT"),
                XmlContenido = xmlContent,
                Conceptos = conceptos,
                TrasladosGlobales = trasladosGlobales
            };

            return request;
        }

        private void AplicarFacturaActual(GuardarFacturaRequestDto request)
        {
            _facturaActualRequest = request;
            FacturaActual = CrearResumen(request);
            ReemplazarColeccion(ConceptosFactura, request.Conceptos);
            ReemplazarColeccion(TrasladosGlobales, request.TrasladosGlobales);
            OnPropertyChanged(nameof(CanSave));
        }

        private async Task ActualizarFacturasExistentesAsync()
        {
            var facturas = await _facturaService.ObtenerFacturasAsync();
            _facturasExistentesBase.Clear();
            _facturasExistentesBase.AddRange(facturas.OrderByDescending(f => f.Fecha).ThenByDescending(f => f.IdFactura));
            AplicarFiltrosFacturasExistentes();
        }

        private static FacturaConceptoDto CrearConcepto(XElement concepto, int orden)
        {
            var traslados = concepto
                .Elements()
                .FirstOrDefault(element => string.Equals(element.Name.LocalName, "Impuestos", StringComparison.OrdinalIgnoreCase))?
                .Elements()
                .FirstOrDefault(element => string.Equals(element.Name.LocalName, "Traslados", StringComparison.OrdinalIgnoreCase))?
                .Elements()
                .Where(element => string.Equals(element.Name.LocalName, "Traslado", StringComparison.OrdinalIgnoreCase))
                .Select((traslado, index) => CrearTraslado(traslado, index + 1))
                .ToList() ?? new List<FacturaTrasladoDto>();

            return new FacturaConceptoDto
            {
                Orden = orden,
                ClaveProdServ = GetStringAttr(concepto, "ClaveProdServ"),
                Cantidad = GetDecimalAttr(concepto, "Cantidad"),
                ClaveUnidad = GetStringAttr(concepto, "ClaveUnidad"),
                Unidad = GetStringAttr(concepto, "Unidad"),
                Descripcion = GetStringAttr(concepto, "Descripcion") ?? string.Empty,
                ValorUnitario = GetDecimalAttr(concepto, "ValorUnitario"),
                Importe = GetDecimalAttr(concepto, "Importe"),
                ObjetoImp = GetStringAttr(concepto, "ObjetoImp"),
                Traslados = traslados
            };
        }

        private static FacturaTrasladoDto CrearTraslado(XElement traslado, int orden)
        {
            return new FacturaTrasladoDto
            {
                Orden = orden,
                Base = GetDecimalAttr(traslado, "Base"),
                Impuesto = GetStringAttr(traslado, "Impuesto"),
                TipoFactor = GetStringAttr(traslado, "TipoFactor"),
                TasaOCuota = GetDecimalAttr(traslado, "TasaOCuota"),
                Importe = GetDecimalAttr(traslado, "Importe")
            };
        }

        private static FacturaResumenDto CrearResumen(GuardarFacturaRequestDto request)
        {
            return new FacturaResumenDto
            {
                VersionXml = request.VersionXml,
                Folio = request.Folio,
                Fecha = request.Fecha,
                FormaPago = request.FormaPago,
                NoCertificado = request.NoCertificado,
                CondicionesDePago = request.CondicionesDePago,
                SubTotal = request.SubTotal,
                Moneda = request.Moneda,
                Total = request.Total,
                TipoDeComprobante = request.TipoDeComprobante,
                Exportacion = request.Exportacion,
                MetodoPago = request.MetodoPago,
                LugarExpedicion = request.LugarExpedicion,
                TotalImpuestosTrasladados = request.TotalImpuestosTrasladados,
                EmisorRfc = request.EmisorRfc,
                EmisorNombre = request.EmisorNombre,
                EmisorRegimenFiscal = request.EmisorRegimenFiscal,
                ReceptorRfc = request.ReceptorRfc,
                ReceptorNombre = request.ReceptorNombre,
                ReceptorDomicilioFiscal = request.ReceptorDomicilioFiscal,
                ReceptorRegimenFiscal = request.ReceptorRegimenFiscal,
                ReceptorUsoCfdi = request.ReceptorUsoCfdi,
                Uuid = request.Uuid,
                FechaTimbrado = request.FechaTimbrado,
                RfcProvCertif = request.RfcProvCertif,
                NoCertificadoSat = request.NoCertificadoSat
            };
        }

        private void AplicarFiltrosFacturasExistentes()
        {
            var fechaDesde = FechaFiltroDesde?.Date;
            var fechaHasta = FechaFiltroHasta?.Date;
            var folioFiltro = FolioFiltro?.Trim();
            var receptorFiltro = ReceptorFiltro?.Trim();
            var metodoPagoFiltro = MetodoPagoFiltro?.Trim();
            var totalFiltro = TotalFiltro?.Trim();
            var totalBuscado = decimal.TryParse(totalFiltro, NumberStyles.Any, new CultureInfo("es-MX"), out var totalValor)
                ? totalValor
                : decimal.TryParse(totalFiltro, NumberStyles.Any, CultureInfo.InvariantCulture, out totalValor)
                    ? totalValor
                    : (decimal?)null;

            if (fechaDesde.HasValue && fechaHasta.HasValue && fechaDesde.Value > fechaHasta.Value)
            {
                (fechaDesde, fechaHasta) = (fechaHasta, fechaDesde);
            }

            var filtradas = _facturasExistentesBase
                .Where(f => !fechaDesde.HasValue || f.Fecha.Date >= fechaDesde.Value.Date)
                .Where(f => !fechaHasta.HasValue || f.Fecha.Date <= fechaHasta.Value.Date)
                .Where(f => string.IsNullOrWhiteSpace(folioFiltro)
                    || string.Equals(f.Folio?.Trim(), folioFiltro, StringComparison.OrdinalIgnoreCase))
                .Where(f => string.IsNullOrWhiteSpace(receptorFiltro)
                    || (!string.IsNullOrWhiteSpace(f.ReceptorNombre) && f.ReceptorNombre.Contains(receptorFiltro, StringComparison.OrdinalIgnoreCase))
                    || (!string.IsNullOrWhiteSpace(f.ReceptorRfc) && f.ReceptorRfc.Contains(receptorFiltro, StringComparison.OrdinalIgnoreCase)))
                .Where(f => string.IsNullOrWhiteSpace(metodoPagoFiltro)
                    || (!string.IsNullOrWhiteSpace(f.MetodoPago) && f.MetodoPago.Contains(metodoPagoFiltro, StringComparison.OrdinalIgnoreCase))
                    || (!string.IsNullOrWhiteSpace(f.FormaPago) && f.FormaPago.Contains(metodoPagoFiltro, StringComparison.OrdinalIgnoreCase)))
                .Where(f => string.IsNullOrWhiteSpace(totalFiltro)
                    || (totalBuscado.HasValue && f.Total == totalBuscado.Value)
                    || f.TotalTexto.Contains(totalFiltro, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(f => f.Fecha)
                .ThenByDescending(f => f.IdFactura)
                .ToList();

            ReemplazarColeccion(FacturasExistentes, filtradas);
            OnPropertyChanged(nameof(ResumenFacturasExistentes));
        }

        private static void ValidarFacturaParaGuardado(GuardarFacturaRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Folio))
            {
                throw new InvalidOperationException("La factura debe incluir un folio para poder guardarse.");
            }

            if (string.IsNullOrWhiteSpace(request.EmisorRfc))
            {
                throw new InvalidOperationException("La factura debe incluir el RFC del emisor.");
            }

            if (request.Conceptos.Count == 0)
            {
                throw new InvalidOperationException("La factura no contiene conceptos.");
            }
        }

        private async Task<GuardarFacturaResponseDto> ValidarYGuardarFacturaAsync(GuardarFacturaRequestDto request)
        {
            ValidarFacturaParaGuardado(request);
            return await _facturaService.GuardarFacturaAsync(request);
        }

        private static void ReemplazarColeccion<T>(ObservableCollection<T> destino, IReadOnlyCollection<T>? origen)
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

        private static XElement? ElementByLocalName(XElement? parent, string localName)
            => parent?
                .Elements()
                .FirstOrDefault(element => string.Equals(element.Name.LocalName, localName, StringComparison.OrdinalIgnoreCase));

        private static string? GetStringAttr(XElement? element, string attributeName)
            => element?.Attribute(attributeName)?.Value;

        private static decimal GetDecimalAttr(XElement? element, string attributeName)
        {
            var value = GetStringAttr(element, attributeName);
            return decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result)
                ? result
                : 0m;
        }

        private static DateTime? GetDateTimeAttr(XElement? element, string attributeName)
        {
            var value = GetStringAttr(element, attributeName);
            return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result)
                ? result
                : null;
        }
    }
}
