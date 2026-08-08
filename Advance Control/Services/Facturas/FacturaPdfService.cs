using Advance_Control.Models;
using Advance_Control.Services.Logging;
using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Advance_Control.Services.Facturas
{
    /// <summary>
    /// Genera la representación impresa oficial de una factura (CFDI) usando QuestPDF,
    /// con el mismo patrón de encabezado que QuoteService (Cotización/Nota/Reporte).
    /// </summary>
    public class FacturaPdfService : IFacturaPdfService
    {
        private static readonly CultureInfo Cultura = new("es-MX");
        private const string SatVerificacionBaseUrl = "https://verificacfdi.facturaelectronica.sat.gob.mx/default.aspx";

        private readonly ILoggingService _logger;

        public FacturaPdfService(ILoggingService logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            QuestPDF.Settings.License = LicenseType.Community;
        }

        private static string GetCabecerasFolder()
            => Path.Combine(AppContext.BaseDirectory, "Assets", "Cabeceras");

        private static string GetFacturasFolder()
        {
            var documentos = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            return Path.Combine(documentos, "Advance Control", "Facturas");
        }

        public Task<string> GenerarFacturaPdfAsync(FacturaDetalleDto detalle)
        {
            if (detalle == null) throw new ArgumentNullException(nameof(detalle));
            if (detalle.Factura == null) throw new InvalidOperationException("El detalle de la factura no trae la información de encabezado.");

            var factura = detalle.Factura;

            try
            {
                var carpeta = GetFacturasFolder();
                Directory.CreateDirectory(carpeta);

                var nombreArchivo = $"Factura_{factura.IdFactura}_{LimpiarNombreArchivo(factura.FolioTitulo)}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                var rutaArchivo = Path.Combine(carpeta, nombreArchivo);
                var cabeceraPath = Path.Combine(GetCabecerasFolder(), "Factura.png");

                var qrBytes = ConstruirQrVerificacion(factura);
                var cadenaOriginal = ConstruirCadenaOriginal(factura);

                var documento = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.Letter);
                        page.Margin(1.5f, Unit.Centimetre);
                        page.PageColor(Colors.White);
                        page.DefaultTextStyle(x => x.FontSize(9));

                        page.Header().ShowOnce().Column(column =>
                        {
                            if (File.Exists(cabeceraPath))
                            {
                                column.Item().Image(cabeceraPath).FitWidth();
                            }

                            column.Item().Row(row =>
                            {
                                row.RelativeItem().Text("Factura")
                                    .FontSize(18)
                                    .SemiBold()
                                    .FontColor(Colors.Blue.Darken2);

                                row.ConstantItem(200).AlignRight().Text(text =>
                                {
                                    text.DefaultTextStyle(s => s.FontSize(10));
                                    text.Span("Folio: ").SemiBold();
                                    text.Span(factura.FolioTitulo);
                                });
                            });
                        });

                        page.Content().PaddingVertical(0.5f, Unit.Centimetre).Column(column =>
                        {
                            column.Spacing(8);

                            // Emisor / Receptor
                            column.Item().Row(row =>
                            {
                                row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(8).Column(emisor =>
                                {
                                    emisor.Item().Text("Emisor").SemiBold().FontColor(Colors.Blue.Darken2);
                                    emisor.Item().Text(factura.EmisorNombre ?? "-");
                                    emisor.Item().Text($"RFC: {factura.EmisorRfc ?? "-"}");
                                    emisor.Item().Text($"Régimen fiscal: {factura.EmisorRegimenFiscal ?? "-"}");
                                    emisor.Item().Text(factura.LugarExpedicionTexto);
                                });

                                row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(8).Column(receptor =>
                                {
                                    receptor.Item().Text("Receptor").SemiBold().FontColor(Colors.Blue.Darken2);
                                    receptor.Item().Text(factura.ReceptorNombre ?? "-");
                                    receptor.Item().Text($"RFC: {factura.ReceptorRfc ?? "-"}");
                                    receptor.Item().Text($"Régimen fiscal: {factura.ReceptorRegimenFiscal ?? "-"}");
                                    receptor.Item().Text($"Uso CFDI: {factura.ReceptorUsoCfdi ?? "-"}");
                                    receptor.Item().Text($"Domicilio fiscal (CP): {factura.ReceptorDomicilioFiscal ?? "-"}");
                                });
                            });

                            // Datos generales del comprobante
                            column.Item().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(8).Row(row =>
                            {
                                row.RelativeItem().Column(col =>
                                {
                                    col.Item().Text(t => { t.Span("Fecha de emisión: ").SemiBold(); t.Span(factura.FechaTexto); });
                                    col.Item().Text(t => { t.Span("Fecha de timbrado: ").SemiBold(); t.Span(factura.FechaTimbrado?.ToString("dd/MM/yyyy HH:mm:ss") ?? "-"); });
                                    col.Item().Text(t => { t.Span("Método de pago: ").SemiBold(); t.Span(factura.MetodoPago ?? "-"); });
                                    col.Item().Text(t => { t.Span("Forma de pago: ").SemiBold(); t.Span(factura.FormaPago ?? "-"); });
                                });
                                row.RelativeItem().Column(col =>
                                {
                                    col.Item().Text(t => { t.Span("Moneda: ").SemiBold(); t.Span(factura.Moneda); });
                                    col.Item().Text(t => { t.Span("Condiciones de pago: ").SemiBold(); t.Span(factura.CondicionesDePago ?? "-"); });
                                    col.Item().Text(t => { t.Span("Tipo de comprobante: ").SemiBold(); t.Span(factura.TipoDeComprobante ?? "-"); });
                                    col.Item().Text(t => { t.Span("No. de certificado: ").SemiBold(); t.Span(factura.NoCertificado ?? "-"); });
                                });
                            });

                            // Folio fiscal (UUID) destacado
                            column.Item().Background(Colors.Blue.Lighten5).Padding(6).Text(t =>
                            {
                                t.AlignCenter();
                                t.Span("Folio fiscal (UUID): ").SemiBold();
                                t.Span(factura.Uuid ?? "Sin timbrar");
                            });

                            // Conceptos
                            column.Item().Table(tabla =>
                            {
                                tabla.ColumnsDefinition(columnas =>
                                {
                                    columnas.ConstantColumn(40);   // Cantidad
                                    columnas.ConstantColumn(50);   // Unidad
                                    columnas.ConstantColumn(65);   // Clave
                                    columnas.RelativeColumn(3);    // Descripción
                                    columnas.ConstantColumn(70);   // P.U.
                                    columnas.ConstantColumn(75);   // Importe
                                });

                                tabla.Header(header =>
                                {
                                    header.Cell().Element(EstiloEncabezado).Text("Cant.");
                                    header.Cell().Element(EstiloEncabezado).Text("Unidad");
                                    header.Cell().Element(EstiloEncabezado).Text("Clave");
                                    header.Cell().Element(EstiloEncabezado).Text("Descripción");
                                    header.Cell().Element(EstiloEncabezado).AlignRight().Text("P.U.");
                                    header.Cell().Element(EstiloEncabezado).AlignRight().Text("Importe");
                                });

                                foreach (var concepto in detalle.Conceptos.OrderBy(c => c.Orden))
                                {
                                    tabla.Cell().Element(EstiloCelda).Text(concepto.CantidadTexto);
                                    tabla.Cell().Element(EstiloCelda).Text(concepto.UnidadTexto);
                                    tabla.Cell().Element(EstiloCelda).Text(concepto.ClaveProdServ ?? "-");
                                    tabla.Cell().Element(EstiloCelda).Text(concepto.Descripcion);
                                    tabla.Cell().Element(EstiloCelda).AlignRight().Text(concepto.ValorUnitarioTexto);
                                    tabla.Cell().Element(EstiloCelda).AlignRight().Text(concepto.ImporteTexto);
                                }
                            });

                            // Totales
                            column.Item().AlignRight().Width(220).Column(totales =>
                            {
                                totales.Item().Row(row =>
                                {
                                    row.RelativeItem().Text("Subtotal:");
                                    row.RelativeItem().AlignRight().Text(factura.SubTotalTexto);
                                });
                                totales.Item().Row(row =>
                                {
                                    row.RelativeItem().Text("Impuestos trasladados:");
                                    row.RelativeItem().AlignRight().Text(factura.TotalImpuestosTexto);
                                });
                                totales.Item().PaddingTop(3).BorderTop(1).BorderColor(Colors.Grey.Lighten1).Row(row =>
                                {
                                    row.RelativeItem().Text("Total:").SemiBold().FontSize(11);
                                    row.RelativeItem().AlignRight().Text(factura.TotalTexto).SemiBold().FontSize(11).FontColor(Colors.Blue.Darken2);
                                });
                            });

                            if (detalle.TrasladosGlobales.Count > 0)
                            {
                                column.Item().Text("Impuestos trasladados").SemiBold().FontSize(9);
                                column.Item().Table(tabla =>
                                {
                                    tabla.ColumnsDefinition(columnas =>
                                    {
                                        columnas.RelativeColumn(2);
                                        columnas.RelativeColumn(1);
                                        columnas.RelativeColumn(1);
                                        columnas.RelativeColumn(1);
                                    });

                                    tabla.Header(header =>
                                    {
                                        header.Cell().Element(EstiloEncabezado).Text("Impuesto");
                                        header.Cell().Element(EstiloEncabezado).AlignRight().Text("Base");
                                        header.Cell().Element(EstiloEncabezado).AlignRight().Text("Tasa");
                                        header.Cell().Element(EstiloEncabezado).AlignRight().Text("Importe");
                                    });

                                    foreach (var traslado in detalle.TrasladosGlobales)
                                    {
                                        tabla.Cell().Element(EstiloCelda).Text(traslado.ImpuestoResumen);
                                        tabla.Cell().Element(EstiloCelda).AlignRight().Text(traslado.BaseTexto);
                                        tabla.Cell().Element(EstiloCelda).AlignRight().Text(traslado.TasaTexto);
                                        tabla.Cell().Element(EstiloCelda).AlignRight().Text(traslado.ImporteTexto);
                                    }
                                });
                            }

                            // Timbre fiscal: sellos + cadena original + QR
                            column.Item().PaddingTop(6).Border(1).BorderColor(Colors.Grey.Lighten1).Padding(8).Row(row =>
                            {
                                row.RelativeItem(3).Column(sellos =>
                                {
                                    sellos.Spacing(4);
                                    sellos.Item().Text("Datos del comprobante fiscal digital").SemiBold().FontSize(8);
                                    sellos.Item().Text(t =>
                                    {
                                        t.DefaultTextStyle(s => s.FontSize(7));
                                        t.Span("RFC proveedor de certificación: ").SemiBold();
                                        t.Span(factura.RfcProvCertif ?? "-");
                                    });
                                    sellos.Item().Text(t =>
                                    {
                                        t.DefaultTextStyle(s => s.FontSize(7));
                                        t.Span("No. certificado SAT: ").SemiBold();
                                        t.Span(factura.NoCertificadoSat ?? "-");
                                    });
                                    sellos.Item().Text("Cadena original del complemento de certificación:").SemiBold().FontSize(7);
                                    sellos.Item().Text(cadenaOriginal).FontSize(6).FontFamily("Consolas");
                                    sellos.Item().Text("Sello digital del CFDI:").SemiBold().FontSize(7);
                                    sellos.Item().Text(factura.Sello ?? "-").FontSize(6).FontFamily("Consolas");
                                    sellos.Item().Text("Sello del SAT:").SemiBold().FontSize(7);
                                    sellos.Item().Text(factura.SelloSat ?? "-").FontSize(6).FontFamily("Consolas");
                                });

                                row.ConstantItem(110).AlignCenter().Column(qrCol =>
                                {
                                    if (qrBytes != null)
                                    {
                                        qrCol.Item().Width(100).Image(qrBytes);
                                    }
                                    else
                                    {
                                        qrCol.Item().Text("Sin datos suficientes para generar el QR de verificación.").FontSize(6);
                                    }
                                });
                            });

                            column.Item().PaddingTop(4).Text(
                                "Este documento es una representación impresa de un CFDI. Puede verificar su autenticidad " +
                                "en el portal del SAT capturando el folio fiscal (UUID), el RFC del emisor y del receptor.")
                                .FontSize(7)
                                .Italic()
                                .FontColor(Colors.Grey.Darken1);
                        });

                        page.Footer().AlignRight().Text(text =>
                        {
                            text.Span("Página ");
                            text.CurrentPageNumber();
                            text.Span(" de ");
                            text.TotalPages();
                        });
                    });
                });

                documento.GeneratePdf(rutaArchivo);
                _ = _logger.LogInformationAsync($"Factura PDF generada: {rutaArchivo}", "FacturaPdfService", "GenerarFacturaPdfAsync");

                return Task.FromResult(rutaArchivo);
            }
            catch (Exception ex)
            {
                _ = _logger.LogErrorAsync("Error al generar el PDF de la factura", ex, "FacturaPdfService", "GenerarFacturaPdfAsync");
                throw;
            }
        }

        /// <summary>
        /// URL de verificación oficial del SAT (id/re/rr/tt/fe), codificada como QR PNG.
        /// Null si falta algún dato del timbre (factura sin timbrar, o cargada antes de que
        /// el detalle expusiera los sellos).
        /// </summary>
        private static byte[]? ConstruirQrVerificacion(FacturaResumenDto factura)
        {
            if (string.IsNullOrWhiteSpace(factura.Uuid)
                || string.IsNullOrWhiteSpace(factura.EmisorRfc)
                || string.IsNullOrWhiteSpace(factura.ReceptorRfc)
                || string.IsNullOrWhiteSpace(factura.Sello))
            {
                return null;
            }

            var fe = factura.Sello!.Length >= 8 ? factura.Sello[^8..] : factura.Sello;
            var totalTexto = factura.Total.ToString("F6", CultureInfo.InvariantCulture);

            var url = $"{SatVerificacionBaseUrl}?" +
                       $"id={Uri.EscapeDataString(factura.Uuid)}" +
                       $"&re={Uri.EscapeDataString(factura.EmisorRfc)}" +
                       $"&rr={Uri.EscapeDataString(factura.ReceptorRfc)}" +
                       $"&tt={totalTexto}" +
                       $"&fe={Uri.EscapeDataString(fe)}";

            var qrGenerator = new QRCodeGenerator();
            var qrData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.M);
            var pngQr = new PngByteQRCode(qrData);
            return pngQr.GetGraphic(20);
        }

        /// <summary>
        /// Cadena original del complemento de certificación digital del SAT (TimbreFiscalDigital
        /// v1.1): ||1.1|UUID|FechaTimbrado|SelloCFD|NoCertificadoSAT||
        /// </summary>
        private static string ConstruirCadenaOriginal(FacturaResumenDto factura)
        {
            if (string.IsNullOrWhiteSpace(factura.Uuid) || !factura.FechaTimbrado.HasValue)
            {
                return "Sin datos de timbre disponibles.";
            }

            var fecha = factura.FechaTimbrado.Value.ToString("yyyy-MM-ddTHH:mm:ss");
            return $"||1.1|{factura.Uuid}|{fecha}|{factura.SelloCfd}|{factura.NoCertificadoSat}||";
        }

        private static string LimpiarNombreArchivo(string valor)
            => string.Concat(valor.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));

        private static IContainer EstiloEncabezado(IContainer container)
        {
            return container
                .Border(1)
                .BorderColor(Colors.Grey.Medium)
                .Background(Colors.Blue.Darken2)
                .PaddingVertical(4)
                .PaddingHorizontal(4)
                .DefaultTextStyle(x => x.FontSize(8).SemiBold().FontColor(Colors.White));
        }

        private static IContainer EstiloCelda(IContainer container)
        {
            return container
                .BorderBottom(1)
                .BorderColor(Colors.Grey.Lighten2)
                .PaddingVertical(3)
                .PaddingHorizontal(4)
                .DefaultTextStyle(x => x.FontSize(8));
        }
    }
}
