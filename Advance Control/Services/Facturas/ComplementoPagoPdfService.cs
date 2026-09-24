using Advance_Control.Models;
using Advance_Control.Services.Entidades;
using Advance_Control.Services.Logging;
using Advance_Control.Services.SatCatalogo;
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
    /// Genera la representación impresa de un Complemento de Pago (CFDI Pagos 2.0) usando
    /// QuestPDF -- mismo patrón visual que FacturaPdfService, pero la tabla central lista los
    /// pagos y documentos relacionados en vez de conceptos (un Complemento de Pago no tiene
    /// conceptos con importe real, TipoDeComprobante=P siempre lleva Total=0).
    /// </summary>
    public class ComplementoPagoPdfService : IComplementoPagoPdfService
    {
        private static readonly CultureInfo Cultura = new("es-MX");
        private const string SatVerificacionBaseUrl = "https://verificacfdi.facturaelectronica.sat.gob.mx/default.aspx";

        private readonly ILoggingService _logger;
        private readonly IEntidadService _entidadService;
        private readonly ISatCatalogoService _satCatalogoService;

        public ComplementoPagoPdfService(ILoggingService logger, IEntidadService entidadService, ISatCatalogoService satCatalogoService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _entidadService = entidadService ?? throw new ArgumentNullException(nameof(entidadService));
            _satCatalogoService = satCatalogoService ?? throw new ArgumentNullException(nameof(satCatalogoService));

            QuestPDF.Settings.License = LicenseType.Community;
        }

        private static string GetFacturasFolder()
        {
            var documentos = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            return Path.Combine(documentos, "Advance Control", "Facturas");
        }

        private static string GetLogoPath()
            => Path.Combine(AppContext.BaseDirectory, "Assets", "Logos", "AdvanceElevadoresLogo.png");

        /// <summary>Arma "Av. Calle, Num, Colonia, CP, Ciudad, Estado, País" con las partes disponibles de la entidad; null si no hay ninguna.</summary>
        private static string? FormatearDireccion(EntidadDto? entidad)
        {
            if (entidad == null)
            {
                return null;
            }

            var numero = string.IsNullOrWhiteSpace(entidad.NumInt) ? entidad.NumExt : $"{entidad.NumExt}/{entidad.NumInt}";
            var partes = new[] { entidad.Calle, numero, entidad.Colonia, entidad.CP, entidad.Ciudad, entidad.Estado, entidad.Pais }
                .Where(p => !string.IsNullOrWhiteSpace(p));

            var direccion = string.Join(", ", partes);
            return string.IsNullOrWhiteSpace(direccion) ? null : direccion;
        }

        /// <summary>Busca la clave en el catálogo SAT y arma "601 - General de Ley Personas Morales"; si no la encuentra, deja la clave sola.</summary>
        private static string DescribirClaveSat(System.Collections.Generic.List<SatCatalogoItemDto> catalogo, string? clave)
        {
            if (string.IsNullOrWhiteSpace(clave))
            {
                return "-";
            }

            var item = catalogo.FirstOrDefault(c => string.Equals(c.Clave, clave, StringComparison.OrdinalIgnoreCase));
            return item?.ToString() ?? clave;
        }

        public async Task<string> GenerarComplementoPagoPdfAsync(ComplementoPagoDetalleDto detalle)
        {
            if (detalle == null) throw new ArgumentNullException(nameof(detalle));
            if (detalle.Factura == null) throw new InvalidOperationException("El detalle del complemento no trae la información de encabezado.");

            var factura = detalle.Factura;

            try
            {
                var carpeta = GetFacturasFolder();
                Directory.CreateDirectory(carpeta);

                var nombreArchivo = $"ComplementoPago_{factura.IdFactura}_{LimpiarNombreArchivo(factura.FolioTitulo)}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                var rutaArchivo = Path.Combine(carpeta, nombreArchivo);
                var logoPath = GetLogoPath();

                var qrBytes = ConstruirQrVerificacion(factura);
                var cadenaOriginal = ConstruirCadenaOriginal(factura);

                // Domicilios completos (no vienen en el CFDI) y descripciones de catálogo SAT,
                // mismo mecanismo que FacturaPdfService.
                var entidadEmisor = await _entidadService.GetActiveEntidadAsync();
                var entidadesReceptor = await _entidadService.GetEntidadesAsync(new EntidadQueryDto { RFC = factura.ReceptorRfc });
                var entidadReceptor = entidadesReceptor.FirstOrDefault(e => string.Equals(e.RFC, factura.ReceptorRfc, StringComparison.OrdinalIgnoreCase));
                var regimenesFiscales = await _satCatalogoService.ListarRegimenFiscalAsync(incluirInactivos: true);

                var direccionEmisor = FormatearDireccion(entidadEmisor);
                var direccionReceptor = FormatearDireccion(entidadReceptor);
                var regimenEmisorTexto = DescribirClaveSat(regimenesFiscales, factura.EmisorRegimenFiscal);
                var regimenReceptorTexto = DescribirClaveSat(regimenesFiscales, factura.ReceptorRegimenFiscal);

                var documento = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.Letter);
                        page.Margin(1.5f, Unit.Centimetre);
                        page.PageColor(Colors.White);
                        page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Segoe UI"));

                        page.Header().ShowOnce().Row(row =>
                        {
                            if (File.Exists(logoPath))
                            {
                                row.ConstantItem(230).Height(72).AlignMiddle().Image(logoPath).FitArea();
                            }

                            row.RelativeItem().PaddingLeft(12).Column(datos =>
                            {
                                datos.Spacing(1);

                                void Renglon(string etiqueta, string valor)
                                {
                                    datos.Item().Text(t =>
                                    {
                                        t.AlignRight();
                                        t.DefaultTextStyle(s => s.FontSize(7.5f));
                                        t.Span($"{etiqueta}: ").SemiBold().FontColor(Colors.Blue.Darken2);
                                        t.Span(valor);
                                    });
                                }

                                Renglon("Folio", factura.FolioTitulo);
                                Renglon("Folio fiscal (UUID)", factura.Uuid ?? "Sin timbrar");
                                Renglon("No. de serie del certificado del SAT", factura.NoCertificadoSat ?? "-");
                                Renglon("No. de serie del certificado del emisor", factura.NoCertificado ?? "-");
                                Renglon("Fecha y hora de certificación", factura.FechaTimbrado?.ToString("dd/MM/yyyy HH:mm:ss") ?? "-");
                                Renglon("Fecha y hora de emisión de CFDI", factura.FechaTexto);
                                Renglon("Lugar de expedición", factura.LugarExpedicionTexto);
                            });
                        });

                        page.Content().PaddingVertical(0.5f, Unit.Centimetre).Column(column =>
                        {
                            column.Spacing(8);

                            column.Item().Row(row =>
                            {
                                row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(8).Column(emisor =>
                                {
                                    emisor.Item().Text("Emisor").SemiBold().FontColor(Colors.Blue.Darken2);
                                    emisor.Item().Text(factura.EmisorNombre ?? "-");
                                    emisor.Item().Text(factura.EmisorRfc ?? "-");
                                    emisor.Item().Text($"Régimen fiscal: {regimenEmisorTexto}");
                                    if (direccionEmisor != null)
                                    {
                                        emisor.Item().Text(direccionEmisor);
                                    }
                                });

                                row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(8).Column(receptor =>
                                {
                                    receptor.Item().Text("Receptor").SemiBold().FontColor(Colors.Blue.Darken2);
                                    receptor.Item().Text(factura.ReceptorNombre ?? "-");
                                    receptor.Item().Text(factura.ReceptorRfc ?? "-");
                                    receptor.Item().Text($"Régimen fiscal: {regimenReceptorTexto}");
                                    if (direccionReceptor != null)
                                    {
                                        receptor.Item().Text(direccionReceptor);
                                    }
                                });
                            });

                            foreach (var pago in detalle.Pagos)
                            {
                                column.Item().PaddingTop(4).Text(t =>
                                {
                                    t.Span($"Pago del {pago.FechaPago:dd/MM/yyyy}").SemiBold().FontSize(10);
                                    t.Span($"  ·  Forma de pago: {pago.FormaPagoTexto}");
                                    if (!string.IsNullOrWhiteSpace(pago.NumOperacion))
                                        t.Span($"  ·  Núm. operación: {pago.NumOperacion}");
                                });

                                column.Item().Table(tabla =>
                                {
                                    tabla.ColumnsDefinition(columnas =>
                                    {
                                        columnas.RelativeColumn(2);    // Docto. relacionado
                                        columnas.ConstantColumn(50);   // Parcialidad
                                        columnas.ConstantColumn(75);   // Saldo ant.
                                        columnas.ConstantColumn(75);   // Pagado
                                        columnas.ConstantColumn(75);   // Saldo insoluto
                                        columnas.ConstantColumn(70);   // IVA trasladado
                                    });

                                    tabla.Header(header =>
                                    {
                                        header.Cell().Element(EstiloEncabezado).Text("Docto. relacionado");
                                        header.Cell().Element(EstiloEncabezado).Text(t => { t.AlignRight(); t.Span("Parc."); });
                                        header.Cell().Element(EstiloEncabezado).Text(t => { t.AlignRight(); t.Span("Saldo ant."); });
                                        header.Cell().Element(EstiloEncabezado).Text(t => { t.AlignRight(); t.Span("Pagado"); });
                                        header.Cell().Element(EstiloEncabezado).Text(t => { t.AlignRight(); t.Span("Saldo insol."); });
                                        header.Cell().Element(EstiloEncabezado).Text(t => { t.AlignRight(); t.Span("IVA"); });
                                    });

                                    foreach (var docto in pago.Doctos)
                                    {
                                        tabla.Cell().Element(EstiloCelda).Text(docto.FacturaPagadaTexto);
                                        tabla.Cell().Element(EstiloCelda).Text(t => { t.AlignRight(); t.Span(docto.NumParcialidad.ToString()); });
                                        tabla.Cell().Element(EstiloCelda).Text(t => { t.AlignRight(); t.Span(docto.ImpSaldoAnt.ToString("C2", Cultura)); });
                                        tabla.Cell().Element(EstiloCelda).Text(t => { t.AlignRight(); t.Span(docto.ImpPagado.ToString("C2", Cultura)); });
                                        tabla.Cell().Element(EstiloCelda).Text(t => { t.AlignRight(); t.Span(docto.ImpSaldoInsoluto.ToString("C2", Cultura)); });
                                        tabla.Cell().Element(EstiloCelda).Text(t =>
                                        {
                                            t.AlignRight();
                                            t.Span(docto.IvaImporte.HasValue ? docto.IvaImporte.Value.ToString("C2", Cultura) : "-");
                                        });
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
                                "Representación impresa de un Complemento de Pago CFDI. Verifique el folio fiscal (UUID) " +
                                "en el portal del SAT capturando el RFC del emisor y del receptor.")
                                .FontSize(7)
                                .Italic()
                                .FontColor(Colors.Grey.Darken1);
                        });

                        page.Footer().Text(text =>
                        {
                            text.AlignRight();
                            text.Span("Página ");
                            text.CurrentPageNumber();
                            text.Span(" de ");
                            text.TotalPages();
                        });
                    });
                });

                documento.GeneratePdf(rutaArchivo);
                _ = _logger.LogInformationAsync($"Complemento de pago PDF generado: {rutaArchivo}", "ComplementoPagoPdfService", "GenerarComplementoPagoPdfAsync");

                return rutaArchivo;
            }
            catch (Exception ex)
            {
                _ = _logger.LogErrorAsync("Error al generar el PDF del complemento de pago", ex, "ComplementoPagoPdfService", "GenerarComplementoPagoPdfAsync");
                throw;
            }
        }

        /// <summary>URL de verificación oficial del SAT (id/re/rr/tt/fe), codificada como QR PNG. Null si falta algún dato del timbre.</summary>
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
                       $"&id={factura.Uuid}" +
                       $"&re={factura.EmisorRfc}" +
                       $"&rr={factura.ReceptorRfc}" +
                       $"&tt={totalTexto}" +
                       $"&fe={fe}";

            var qrGenerator = new QRCodeGenerator();
            var qrData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.M);
            var pngQr = new PngByteQRCode(qrData);
            return pngQr.GetGraphic(20);
        }

        /// <summary>Cadena original del complemento de certificación digital del SAT (TimbreFiscalDigital v1.1).</summary>
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
