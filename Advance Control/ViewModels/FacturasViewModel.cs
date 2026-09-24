using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Advance_Control.Models;
using Advance_Control.Services.Facturas;
using Advance_Control.Views.Dialogs;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace Advance_Control.ViewModels
{
    public class FacturasViewModel : ViewModelBase
    {
        private const int TamanoPagina = 8;

        private readonly IFacturaService _facturaService;
        private readonly IFacturaPdfService _facturaPdfService;

        private readonly List<FacturaResumenDto> _todasLasFacturas = new();
        private List<FacturaResumenDto> _facturasFiltradas = new();

        private ObservableCollection<FacturaResumenDto> _facturasPagina;
        private ObservableCollection<FacturaResumenDto> _sugerencias;

        private bool _isLoading;
        private string? _errorMessage;
        private string? _successMessage;

        private string? _textoBusqueda;
        private bool _incluirConSerie = true;
        private bool _incluirSinSerie = true;
        private bool _incluirComplementosPago = true;
        private DateTimeOffset? _fechaDesde;
        private DateTimeOffset? _fechaHasta;

        private int _paginaActual = 1;

        public FacturasViewModel(IFacturaService facturaService, IFacturaPdfService facturaPdfService)
        {
            _facturaService = facturaService ?? throw new ArgumentNullException(nameof(facturaService));
            _facturaPdfService = facturaPdfService ?? throw new ArgumentNullException(nameof(facturaPdfService));
            _facturasPagina = new ObservableCollection<FacturaResumenDto>();
            _sugerencias = new ObservableCollection<FacturaResumenDto>();
        }

        public ObservableCollection<FacturaResumenDto> FacturasPagina
        {
            get => _facturasPagina;
            private set => SetProperty(ref _facturasPagina, value);
        }

        public ObservableCollection<FacturaResumenDto> Sugerencias
        {
            get => _sugerencias;
            private set => SetProperty(ref _sugerencias, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (SetProperty(ref _isLoading, value))
                {
                    OnPropertyChanged(nameof(PuedeIrAnterior));
                    OnPropertyChanged(nameof(PuedeIrSiguiente));
                    OnPropertyChanged(nameof(SinResultados));
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

        public string? TextoBusqueda
        {
            get => _textoBusqueda;
            set => SetProperty(ref _textoBusqueda, value);
        }

        public bool IncluirConSerie
        {
            get => _incluirConSerie;
            set => SetProperty(ref _incluirConSerie, value);
        }

        public bool IncluirSinSerie
        {
            get => _incluirSinSerie;
            set => SetProperty(ref _incluirSinSerie, value);
        }

        public bool IncluirComplementosPago
        {
            get => _incluirComplementosPago;
            set => SetProperty(ref _incluirComplementosPago, value);
        }

        public DateTimeOffset? FechaDesde
        {
            get => _fechaDesde;
            set => SetProperty(ref _fechaDesde, value);
        }

        public DateTimeOffset? FechaHasta
        {
            get => _fechaHasta;
            set => SetProperty(ref _fechaHasta, value);
        }

        public int PaginaActual
        {
            get => _paginaActual;
            private set => SetProperty(ref _paginaActual, value);
        }

        public int TotalPaginas => Math.Max(1, (int)Math.Ceiling(_facturasFiltradas.Count / (double)TamanoPagina));
        public int TotalFacturasSistema => _todasLasFacturas.Count;
        public int TotalFacturasFiltradas => _facturasFiltradas.Count;
        public bool PuedeIrAnterior => PaginaActual > 1 && !IsLoading;
        public bool PuedeIrSiguiente => PaginaActual < TotalPaginas && !IsLoading;
        public bool SinResultados => !IsLoading && FacturasPagina.Count == 0;

        public string ResumenPaginacionTexto
        {
            get
            {
                if (_facturasFiltradas.Count == 0)
                {
                    return TotalFacturasSistema == 0
                        ? "No hay facturas registradas todavía."
                        : $"0 de {TotalFacturasSistema} facturas (sin coincidencias con los filtros aplicados).";
                }

                var inicio = ((PaginaActual - 1) * TamanoPagina) + 1;
                var fin = Math.Min(PaginaActual * TamanoPagina, _facturasFiltradas.Count);
                return $"Mostrando {inicio}–{fin} de {TotalFacturasFiltradas} facturas encontradas · {TotalFacturasSistema} en total";
            }
        }

        public string PaginaTexto => $"Página {PaginaActual} de {TotalPaginas}";

        public async Task CargarFacturasAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = null;

                var facturas = await _facturaService.ObtenerFacturasAsync();
                _todasLasFacturas.Clear();
                _todasLasFacturas.AddRange(facturas.OrderByDescending(f => f.Fecha).ThenByDescending(f => f.IdFactura));

                Buscar();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al consultar las facturas: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Primer paso de "Consolidar Complementos": pide a la API que detecte complementos de
        /// pago que llegaron sin parsear (timbrados a mano en el portal de Bilkon, o traídos por
        /// una recarga de FEL) y los vincule a la factura que pagan por UUID. El segundo paso
        /// (ligar a movimiento bancario) lo maneja el code-behind abriendo el mismo asistente que
        /// usa el botón "Complementos" de Conciliación.
        /// </summary>
        public async Task<ComplementoPagoConsolidarResultDto?> ConsolidarBackfillComplementosAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = null;
                SuccessMessage = null;

                return await _facturaService.ConsolidarComplementosPagoAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al consolidar complementos de pago: {ex.Message}";
                return null;
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Carga un XML de factura ya timbrada externamente (folio suelto del portal de Bilkon,
        /// sin Serie) y la guarda. Las facturas con Serie+Folio se generan solo por timbrado
        /// directo (TimbrarOperacionDirectoAsync); esto es exclusivamente para las que se siguen
        /// emitiendo manualmente en el sistema web del proveedor.
        /// </summary>
        public async Task CargarArchivoXmlAsync(nint windowHandle, XamlRoot xamlRoot)
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
                    var request = CfdiXmlParser.ParseXmlToRequest(xmlContent);
                    var requestPreparado = await PrepararFacturaParaGuardadoAsync(request, xamlRoot);
                    if (requestPreparado == null)
                    {
                        ErrorMessage = $"{file.Name}: la factura fue descartada porque se canceló la normalización.";
                        SuccessMessage = null;
                        return;
                    }

                    var result = await ValidarYGuardarFacturaAsync(requestPreparado);

                    if (!result.Success && !string.Equals(result.Accion, "existente", StringComparison.OrdinalIgnoreCase))
                    {
                        ErrorMessage = string.IsNullOrWhiteSpace(result.Message)
                            ? $"No se pudo guardar la factura {file.Name}."
                            : $"{file.Name}: {result.Message}";
                        SuccessMessage = null;
                        return;
                    }

                    await CargarFacturasAsync();
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

        /// <summary>Igual que CargarArchivoXmlAsync pero para varios XML en lote (mismo origen: portal de Bilkon).</summary>
        public async Task CargarYGuardarMultiplesFacturasAsync(nint windowHandle, XamlRoot xamlRoot)
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
                var descartadas = 0;
                var fallidas = 0;
                var errores = new List<string>();

                foreach (var file in files)
                {
                    try
                    {
                        var xmlContent = await FileIO.ReadTextAsync(file);
                        var request = CfdiXmlParser.ParseXmlToRequest(xmlContent);

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

                        var requestPreparado = await PrepararFacturaParaGuardadoAsync(request, xamlRoot);
                        if (requestPreparado == null)
                        {
                            descartadas++;
                            continue;
                        }

                        var result = await ValidarYGuardarFacturaAsync(requestPreparado);

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

                await CargarFacturasAsync();

                SuccessMessage = $"Carga masiva finalizada. Nuevas: {cargadas}. Duplicadas: {duplicadas}. Descartadas: {descartadas}. Fallidas: {fallidas}.";
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

        private static bool EsFacturaEnUsd(GuardarFacturaRequestDto request)
            => string.Equals(request.Moneda?.Trim(), "USD", StringComparison.OrdinalIgnoreCase);

        private async Task<GuardarFacturaRequestDto?> PrepararFacturaParaGuardadoAsync(GuardarFacturaRequestDto request, XamlRoot xamlRoot)
        {
            if (!EsFacturaEnUsd(request))
            {
                return request;
            }

            var tipoCambioInicial = ObtenerTipoCambioInicial(request);
            var dialog = new NormalizarFacturaUsdDialog(
                request.Folio,
                request.Total,
                tipoCambioInicial,
                xamlRoot);

            var resultado = await dialog.ShowAsync();
            if (resultado != ContentDialogResult.Primary)
            {
                return null;
            }

            return NormalizarFacturaUsd(request, dialog.TipoCambioCapturado);
        }

        private static decimal ObtenerTipoCambioInicial(GuardarFacturaRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.XmlContenido))
            {
                return 1m;
            }

            try
            {
                var document = XDocument.Parse(request.XmlContenido, LoadOptions.PreserveWhitespace);
                var comprobante = document.Root;
                var tipoCambio = CfdiXmlParser.GetDecimalAttr(comprobante, "TipoCambio");
                return tipoCambio > 0m ? tipoCambio : 1m;
            }
            catch
            {
                return 1m;
            }
        }

        private static GuardarFacturaRequestDto NormalizarFacturaUsd(GuardarFacturaRequestDto original, decimal tipoCambio)
        {
            if (tipoCambio <= 0m)
            {
                throw new InvalidOperationException("El tipo de cambio debe ser mayor a cero para normalizar la factura.");
            }

            if (original.Conceptos.Count == 0)
            {
                throw new InvalidOperationException("La factura no contiene conceptos para normalizar.");
            }

            var tipoCambioRedondeado = decimal.Round(tipoCambio, 6, MidpointRounding.AwayFromZero);
            var totalObjetivoMx = RedondearImporte(original.Total * tipoCambioRedondeado);
            var conceptosNormalizados = new List<FacturaConceptoDto>(original.Conceptos.Count);
            decimal subtotalNormalizado = 0m;
            decimal impuestosNormalizados = 0m;
            decimal totalAcumulado = 0m;

            for (var index = 0; index < original.Conceptos.Count; index++)
            {
                var conceptoOriginal = original.Conceptos[index];
                var totalConceptoOriginal = ObtenerTotalConcepto(conceptoOriginal);
                var totalConceptoNormalizado = index == original.Conceptos.Count - 1
                    ? totalObjetivoMx - totalAcumulado
                    : RedondearImporte(totalConceptoOriginal * tipoCambioRedondeado);

                if (totalConceptoNormalizado < 0m)
                {
                    throw new InvalidOperationException($"La normalización del concepto \"{conceptoOriginal.Descripcion}\" generó un importe negativo.");
                }

                var baseNormalizada = RedondearImporte(totalConceptoNormalizado / 1.16m);
                var ivaNormalizado = totalConceptoNormalizado - baseNormalizada;
                var cantidad = conceptoOriginal.Cantidad;
                var valorUnitario = cantidad > 0m
                    ? decimal.Round(baseNormalizada / cantidad, 6, MidpointRounding.AwayFromZero)
                    : baseNormalizada;

                var trasladoNormalizado = new FacturaTrasladoDto
                {
                    Orden = 1,
                    Base = baseNormalizada,
                    Impuesto = conceptoOriginal.Traslados.FirstOrDefault()?.Impuesto ?? "002",
                    TipoFactor = conceptoOriginal.Traslados.FirstOrDefault()?.TipoFactor ?? "Tasa",
                    TasaOCuota = 0.16m,
                    Importe = ivaNormalizado
                };

                conceptosNormalizados.Add(new FacturaConceptoDto
                {
                    IdFacturaConcepto = conceptoOriginal.IdFacturaConcepto,
                    Orden = conceptoOriginal.Orden,
                    ClaveProdServ = conceptoOriginal.ClaveProdServ,
                    Cantidad = cantidad,
                    ClaveUnidad = conceptoOriginal.ClaveUnidad,
                    Unidad = conceptoOriginal.Unidad,
                    Descripcion = conceptoOriginal.Descripcion,
                    ValorUnitario = valorUnitario,
                    Importe = baseNormalizada,
                    ObjetoImp = conceptoOriginal.ObjetoImp,
                    Traslados = new List<FacturaTrasladoDto> { trasladoNormalizado }
                });

                subtotalNormalizado += baseNormalizada;
                impuestosNormalizados += ivaNormalizado;
                totalAcumulado += totalConceptoNormalizado;
            }

            subtotalNormalizado = RedondearImporte(subtotalNormalizado);
            impuestosNormalizados = RedondearImporte(impuestosNormalizados);

            var totalNormalizado = subtotalNormalizado + impuestosNormalizados;
            var diferenciaTotal = totalObjetivoMx - totalNormalizado;
            if (diferenciaTotal != 0m)
            {
                AjustarUltimoConcepto(conceptosNormalizados, diferenciaTotal);
                subtotalNormalizado = RedondearImporte(conceptosNormalizados.Sum(concepto => concepto.Importe));
                impuestosNormalizados = RedondearImporte(conceptosNormalizados.Sum(concepto => concepto.Traslados.Sum(traslado => traslado.Importe)));
                totalNormalizado = subtotalNormalizado + impuestosNormalizados;
            }

            if (totalNormalizado != totalObjetivoMx)
            {
                throw new InvalidOperationException("No fue posible cuadrar el total normalizado con el total en USD por el tipo de cambio capturado.");
            }

            var trasladosGlobales = new List<FacturaTrasladoDto>
            {
                new FacturaTrasladoDto
                {
                    Orden = 1,
                    Base = subtotalNormalizado,
                    Impuesto = original.TrasladosGlobales.FirstOrDefault()?.Impuesto
                        ?? conceptosNormalizados.First().Traslados.First().Impuesto
                        ?? "002",
                    TipoFactor = original.TrasladosGlobales.FirstOrDefault()?.TipoFactor
                        ?? conceptosNormalizados.First().Traslados.First().TipoFactor
                        ?? "Tasa",
                    TasaOCuota = 0.16m,
                    Importe = impuestosNormalizados
                }
            };

            return new GuardarFacturaRequestDto
            {
                VersionXml = original.VersionXml,
                Folio = original.Folio,
                Fecha = original.Fecha,
                FormaPago = original.FormaPago,
                NoCertificado = original.NoCertificado,
                Certificado = original.Certificado,
                Sello = original.Sello,
                CondicionesDePago = original.CondicionesDePago,
                SubTotal = subtotalNormalizado,
                Moneda = "MXN",
                Total = totalNormalizado,
                TipoDeComprobante = original.TipoDeComprobante,
                Exportacion = original.Exportacion,
                MetodoPago = original.MetodoPago,
                LugarExpedicion = original.LugarExpedicion,
                TotalImpuestosTrasladados = impuestosNormalizados,
                EmisorRfc = original.EmisorRfc,
                EmisorNombre = original.EmisorNombre,
                EmisorRegimenFiscal = original.EmisorRegimenFiscal,
                ReceptorRfc = original.ReceptorRfc,
                ReceptorNombre = original.ReceptorNombre,
                ReceptorDomicilioFiscal = original.ReceptorDomicilioFiscal,
                ReceptorRegimenFiscal = original.ReceptorRegimenFiscal,
                ReceptorUsoCfdi = original.ReceptorUsoCfdi,
                Uuid = original.Uuid,
                FechaTimbrado = original.FechaTimbrado,
                RfcProvCertif = original.RfcProvCertif,
                NoCertificadoSat = original.NoCertificadoSat,
                SelloCfd = original.SelloCfd,
                SelloSat = original.SelloSat,
                XmlContenido = NormalizarXmlContenido(original.XmlContenido, conceptosNormalizados, subtotalNormalizado, impuestosNormalizados, totalNormalizado, tipoCambioRedondeado),
                Conceptos = conceptosNormalizados,
                TrasladosGlobales = trasladosGlobales
            };
        }

        private static decimal ObtenerTotalConcepto(FacturaConceptoDto concepto)
            => RedondearImporte(concepto.Importe + concepto.Traslados.Sum(traslado => traslado.Importe));

        private static void AjustarUltimoConcepto(IList<FacturaConceptoDto> conceptos, decimal diferenciaTotal)
        {
            if (conceptos.Count == 0 || diferenciaTotal == 0m)
            {
                return;
            }

            var ultimoConcepto = conceptos[^1];
            var ultimoTraslado = ultimoConcepto.Traslados.FirstOrDefault();
            if (ultimoTraslado == null)
            {
                throw new InvalidOperationException("El último concepto no contiene traslado para aplicar el ajuste de redondeo.");
            }

            ultimoTraslado.Importe = RedondearImporte(ultimoTraslado.Importe + diferenciaTotal);
            var totalConceptoAjustado = ultimoConcepto.Importe + ultimoTraslado.Importe;
            ultimoConcepto.Importe = RedondearImporte(totalConceptoAjustado / 1.16m);
            ultimoTraslado.Base = ultimoConcepto.Importe;
            ultimoTraslado.Importe = totalConceptoAjustado - ultimoConcepto.Importe;
            ultimoConcepto.ValorUnitario = ultimoConcepto.Cantidad > 0m
                ? decimal.Round(ultimoConcepto.Importe / ultimoConcepto.Cantidad, 6, MidpointRounding.AwayFromZero)
                : ultimoConcepto.Importe;
        }

        private static string? NormalizarXmlContenido(
            string? xmlContenidoOriginal,
            IReadOnlyList<FacturaConceptoDto> conceptosNormalizados,
            decimal subtotalNormalizado,
            decimal impuestosNormalizados,
            decimal totalNormalizado,
            decimal tipoCambioOriginal)
        {
            if (string.IsNullOrWhiteSpace(xmlContenidoOriginal))
            {
                return xmlContenidoOriginal;
            }

            var document = XDocument.Parse(xmlContenidoOriginal, LoadOptions.PreserveWhitespace);
            var comprobante = document.Root
                ?? throw new InvalidOperationException("El XML de la factura no contiene el nodo Comprobante para alinear la normalización.");

            EstablecerAtributo(comprobante, "SubTotal", FormatearImporte(subtotalNormalizado));
            EstablecerAtributo(comprobante, "Moneda", "MXN");
            EstablecerAtributo(comprobante, "Total", FormatearImporte(totalNormalizado));
            EstablecerAtributo(comprobante, "TipoCambio", "1");

            var conceptosNodo = CfdiXmlParser.ElementByLocalName(comprobante, "Conceptos")
                ?? throw new InvalidOperationException("El XML de la factura no contiene el nodo Conceptos para alinear la normalización.");

            var conceptosXml = conceptosNodo
                .Elements()
                .Where(element => string.Equals(element.Name.LocalName, "Concepto", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (conceptosXml.Count != conceptosNormalizados.Count)
            {
                throw new InvalidOperationException("El XML de la factura no coincide con la cantidad de conceptos normalizados.");
            }

            for (var index = 0; index < conceptosXml.Count; index++)
            {
                var conceptoXml = conceptosXml[index];
                var conceptoNormalizado = conceptosNormalizados[index];
                var trasladoNormalizado = conceptoNormalizado.Traslados.First();

                EstablecerAtributo(conceptoXml, "ValorUnitario", FormatearImporteAmpliado(conceptoNormalizado.ValorUnitario));
                EstablecerAtributo(conceptoXml, "Importe", FormatearImporte(conceptoNormalizado.Importe));

                var impuestosNodo = CfdiXmlParser.ElementByLocalName(conceptoXml, "Impuestos") ?? CrearHijo(conceptoXml, "Impuestos");
                var trasladosNodo = CfdiXmlParser.ElementByLocalName(impuestosNodo, "Traslados") ?? CrearHijo(impuestosNodo, "Traslados");
                trasladosNodo.RemoveNodes();

                var trasladoNodo = new XElement(
                    XName.Get("Traslado", trasladosNodo.Name.NamespaceName),
                    new XAttribute("Base", FormatearImporte(trasladoNormalizado.Base)),
                    new XAttribute("Impuesto", trasladoNormalizado.Impuesto ?? "002"),
                    new XAttribute("TipoFactor", trasladoNormalizado.TipoFactor ?? "Tasa"),
                    new XAttribute("TasaOCuota", FormatearTasa(trasladoNormalizado.TasaOCuota)),
                    new XAttribute("Importe", FormatearImporte(trasladoNormalizado.Importe)));

                trasladosNodo.Add(trasladoNodo);
            }

            var impuestosGlobalesNodo = CfdiXmlParser.ElementByLocalName(comprobante, "Impuestos") ?? CrearHijo(comprobante, "Impuestos");
            EstablecerAtributo(impuestosGlobalesNodo, "TotalImpuestosTrasladados", FormatearImporte(impuestosNormalizados));

            var trasladosGlobalesNodo = CfdiXmlParser.ElementByLocalName(impuestosGlobalesNodo, "Traslados") ?? CrearHijo(impuestosGlobalesNodo, "Traslados");
            trasladosGlobalesNodo.RemoveNodes();
            trasladosGlobalesNodo.Add(
                new XElement(
                    XName.Get("Traslado", trasladosGlobalesNodo.Name.NamespaceName),
                    new XAttribute("Base", FormatearImporte(subtotalNormalizado)),
                    new XAttribute("Impuesto", "002"),
                    new XAttribute("TipoFactor", "Tasa"),
                    new XAttribute("TasaOCuota", FormatearTasa(0.16m)),
                    new XAttribute("Importe", FormatearImporte(impuestosNormalizados))));

            document.AddFirst(new XComment($" NormalizadoAdvanceControl MonedaOriginal=USD TipoCambioOriginal={tipoCambioOriginal.ToString("0.######", CultureInfo.InvariantCulture)} "));
            return document.ToString(SaveOptions.DisableFormatting);
        }

        private static XElement CrearHijo(XElement parent, string localName)
        {
            var child = new XElement(XName.Get(localName, parent.Name.NamespaceName));
            parent.Add(child);
            return child;
        }

        private static void EstablecerAtributo(XElement element, string attributeName, string value)
            => element.SetAttributeValue(attributeName, value);

        private static decimal RedondearImporte(decimal value)
            => decimal.Round(value, 2, MidpointRounding.AwayFromZero);

        private static string FormatearImporte(decimal value)
            => value.ToString("0.00", CultureInfo.InvariantCulture);

        private static string FormatearImporteAmpliado(decimal value)
            => value.ToString("0.######", CultureInfo.InvariantCulture);

        private static string FormatearTasa(decimal value)
            => value.ToString("0.000000", CultureInfo.InvariantCulture);

        public void ActualizarSugerencias(string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
            {
                Sugerencias.Clear();
                return;
            }

            var coincidencias = _todasLasFacturas
                .Where(f => Coincide(f, texto))
                .Take(8)
                .ToList();

            ReemplazarColeccion(Sugerencias, coincidencias);
        }

        public void Buscar()
        {
            var texto = TextoBusqueda?.Trim();
            var desde = FechaDesde?.Date;
            var hasta = FechaHasta?.Date;

            if (desde.HasValue && hasta.HasValue && desde.Value > hasta.Value)
            {
                (desde, hasta) = (hasta, desde);
            }

            _facturasFiltradas = _todasLasFacturas
                .Where(f => Coincide(f, texto))
                .Where(f => !desde.HasValue || f.Fecha.Date >= desde.Value)
                .Where(f => !hasta.HasValue || f.Fecha.Date <= hasta.Value)
                .Where(f => (IncluirConSerie && f.EsConSerie)
                    || (IncluirSinSerie && f.EsSinSerie)
                    || (IncluirComplementosPago && f.EsComplementoPago))
                .ToList();

            PaginaActual = 1;
            ActualizarPagina();
        }

        public void LimpiarFiltros()
        {
            TextoBusqueda = null;
            FechaDesde = null;
            FechaHasta = null;
            IncluirConSerie = true;
            IncluirSinSerie = true;
            IncluirComplementosPago = true;
            Sugerencias.Clear();
            Buscar();
        }

        public void IrAPaginaAnterior()
        {
            if (!PuedeIrAnterior)
            {
                return;
            }

            PaginaActual--;
            ActualizarPagina();
        }

        public void IrAPaginaSiguiente()
        {
            if (!PuedeIrSiguiente)
            {
                return;
            }

            PaginaActual++;
            ActualizarPagina();
        }

        private void ActualizarPagina()
        {
            var pagina = _facturasFiltradas
                .Skip((PaginaActual - 1) * TamanoPagina)
                .Take(TamanoPagina)
                .ToList();

            ReemplazarColeccion(FacturasPagina, pagina);
            NotificarCambiosPaginacion();
        }

        private void NotificarCambiosPaginacion()
        {
            OnPropertyChanged(nameof(TotalPaginas));
            OnPropertyChanged(nameof(TotalFacturasSistema));
            OnPropertyChanged(nameof(TotalFacturasFiltradas));
            OnPropertyChanged(nameof(PuedeIrAnterior));
            OnPropertyChanged(nameof(PuedeIrSiguiente));
            OnPropertyChanged(nameof(SinResultados));
            OnPropertyChanged(nameof(ResumenPaginacionTexto));
            OnPropertyChanged(nameof(PaginaTexto));
        }

        private static bool Coincide(FacturaResumenDto f, string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
            {
                return true;
            }

            return Contiene(f.ReceptorNombre, texto)
                || Contiene(f.ReceptorRfc, texto)
                || Contiene(f.Uuid, texto)
                || Contiene(f.Folio, texto)
                || Contiene(f.FolioTitulo, texto)
                || Contiene(f.Serie, texto);
        }

        private static bool Contiene(string? valor, string texto)
            => !string.IsNullOrWhiteSpace(valor) && valor.Contains(texto, StringComparison.OrdinalIgnoreCase);

        // --- Acciones por factura ---

        public async Task<string?> GenerarPdfAsync(FacturaResumenDto factura)
        {
            try
            {
                ErrorMessage = null;
                var detalle = await _facturaService.ObtenerDetalleFacturaAsync(factura.IdFactura);
                if (detalle == null)
                {
                    ErrorMessage = "No se encontró el detalle de la factura seleccionada.";
                    return null;
                }

                return await _facturaPdfService.GenerarFacturaPdfAsync(detalle);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al generar el PDF de la factura: {ex.Message}";
                return null;
            }
        }

        /// <summary>Genera el PDF del acuse de cancelación del SAT. Solo aplica a facturas con Cancelada == true.</summary>
        public async Task<string?> GenerarAcuseCancelacionPdfAsync(FacturaResumenDto factura)
        {
            try
            {
                ErrorMessage = null;

                if (!factura.Cancelada)
                {
                    ErrorMessage = "Esta factura no está cancelada; no hay acuse de cancelación que generar.";
                    return null;
                }

                var detalle = await _facturaService.ObtenerDetalleFacturaAsync(factura.IdFactura);
                if (detalle == null)
                {
                    ErrorMessage = "No se encontró el detalle de la factura seleccionada.";
                    return null;
                }

                return await _facturaPdfService.GenerarAcuseCancelacionPdfAsync(detalle);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al generar el PDF del acuse de cancelación: {ex.Message}";
                return null;
            }
        }

        public async Task<string?> ObtenerXmlAsync(FacturaResumenDto factura)
        {
            try
            {
                ErrorMessage = null;
                var xml = await _facturaService.ObtenerXmlFacturaAsync(factura.IdFactura);
                if (string.IsNullOrWhiteSpace(xml))
                {
                    ErrorMessage = "Esta factura no tiene un XML almacenado.";
                    return null;
                }

                return xml;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al obtener el XML de la factura: {ex.Message}";
                return null;
            }
        }

        public async Task<bool> CancelarAsync(FacturaResumenDto factura)
        {
            if (!factura.PermiteGestionInterna)
            {
                ErrorMessage = "Esta factura no fue generada por el software; cancélala desde el portal de Bilkon.";
                return false;
            }

            if (factura.IdOperacion is not int idOperacion)
            {
                ErrorMessage = "Esta factura no está vinculada a una operación; no se puede cancelar desde aquí.";
                return false;
            }

            try
            {
                IsLoading = true;
                ErrorMessage = null;
                SuccessMessage = null;

                var resultado = await _facturaService.CancelarFacturaOperacionAsync(idOperacion);
                SuccessMessage = string.IsNullOrWhiteSpace(resultado.Mensaje)
                    ? "Factura cancelada correctamente."
                    : resultado.Mensaje;

                await CargarFacturasAsync();
                return true;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al cancelar la factura: {ex.Message}";
                return false;
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Cancela un CFDI ya timbrado (Serie+Folio) ante el SAT vía FEL Bilkon. Solo si Bilkon
        /// confirma código 201 la operación vinculada queda desvinculada automáticamente en el
        /// backend (nunca antes, nunca por separado) -- ver CancelarCfdiResponseDto.OperacionDesvinculada.
        /// </summary>
        public async Task<CancelarCfdiResponseDto?> CancelarCfdiAsync(FacturaResumenDto factura, CancelarCfdiRequestDto request)
        {
            if (!factura.PuedeCancelarCfdi)
            {
                ErrorMessage = "Esta factura no se puede cancelar desde aquí.";
                return null;
            }

            try
            {
                IsLoading = true;
                ErrorMessage = null;
                SuccessMessage = null;

                var resultado = await _facturaService.CancelarCfdiAsync(factura.IdFactura, request);
                if (resultado.Success && resultado.Cancelada)
                {
                    SuccessMessage = resultado.OperacionDesvinculada
                        ? $"Factura {factura.FolioTitulo} cancelada ante el SAT. La operación queda disponible para volver a facturarse."
                        : $"Factura {factura.FolioTitulo} cancelada ante el SAT.";
                    await CargarFacturasAsync();
                }
                else
                {
                    ErrorMessage = resultado.MensajeResultado
                        ?? resultado.Message
                        ?? "Bilkon no confirmó la cancelación del CFDI.";
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

        /// <summary>Candidatas a "factura que sustituye" para motivo 01: propias (Serie+Folio), vigentes, excluyendo la que se está cancelando.</summary>
        public IReadOnlyList<FacturaResumenDto> ObtenerCandidatasSustitucion(FacturaResumenDto facturaExcluir, string? texto)
        {
            return _todasLasFacturas
                .Where(f => f.IdFactura != facturaExcluir.IdFactura)
                .Where(f => f.PermiteGestionInterna && !f.Cancelada)
                .Where(f => Coincide(f, texto))
                .Take(8)
                .ToList();
        }

        public async Task<RegistrarAbonoFacturaResponseDto> RegistrarComplementoPagoAsync(RegistrarAbonoFacturaRequestDto request)
        {
            var factura = _todasLasFacturas.FirstOrDefault(f => f.IdFactura == request.IdFactura);
            if (factura != null && !factura.PermiteGestionInterna)
            {
                ErrorMessage = "Esta factura no fue generada por el software; captura su pago desde el portal de Bilkon.";
                return new RegistrarAbonoFacturaResponseDto { Success = false, Message = ErrorMessage };
            }

            try
            {
                IsLoading = true;
                ErrorMessage = null;
                SuccessMessage = null;

                var resultado = await _facturaService.RegistrarAbonoAsync(request);
                if (resultado.Success)
                {
                    SuccessMessage = string.IsNullOrWhiteSpace(resultado.Message)
                        ? "Abono registrado correctamente."
                        : resultado.Message;
                    await CargarFacturasAsync();
                }
                else
                {
                    ErrorMessage = string.IsNullOrWhiteSpace(resultado.Message)
                        ? "No se pudo registrar el abono."
                        : resultado.Message;
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

        /// <summary>Abonos de facturas PPD propias de un receptor que todavía no entraron a ningún Complemento de Pago real.</summary>
        public Task<List<AbonoPendienteComplementoDto>> ObtenerAbonosPendientesComplementoAsync(string receptorRfc)
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
                    SuccessMessage = string.IsNullOrWhiteSpace(resultado.Message)
                        ? "Complemento de pago generado y timbrado correctamente."
                        : resultado.Message;
                    await CargarFacturasAsync();
                }
                else
                {
                    ErrorMessage = string.IsNullOrWhiteSpace(resultado.Message)
                        ? "No se pudo generar el complemento de pago."
                        : resultado.Message;
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
    }
}
