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
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Xml.Linq;

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
        private readonly IEntidadService _entidadService;
        private readonly ISatCatalogoService _satCatalogoService;

        public FacturaPdfService(ILoggingService logger, IEntidadService entidadService, ISatCatalogoService satCatalogoService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _entidadService = entidadService ?? throw new ArgumentNullException(nameof(entidadService));
            _satCatalogoService = satCatalogoService ?? throw new ArgumentNullException(nameof(satCatalogoService));

            QuestPDF.Settings.License = LicenseType.Community;
        }

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

        private static string GetCabecerasFolder()
            => Path.Combine(AppContext.BaseDirectory, "Assets", "Cabeceras");

        private static string GetLogoPath()
            => Path.Combine(AppContext.BaseDirectory, "Assets", "Logos", "AdvanceElevadoresLogo.png");

        private static string GetFacturasFolder()
        {
            var documentos = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            return Path.Combine(documentos, "Advance Control", "Facturas");
        }

        public async Task<string> GenerarFacturaPdfAsync(FacturaDetalleDto detalle)
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
                var logoPath = GetLogoPath();

                var qrBytes = ConstruirQrVerificacion(factura);
                var cadenaOriginal = ConstruirCadenaOriginal(factura);

                // Domicilios completos (no vienen en el CFDI -- el XML solo trae el CP de cada uno)
                // y descripciones de catálogo SAT, para replicar el formato del portal web.
                var entidadEmisor = await _entidadService.GetActiveEntidadAsync();
                var entidadesReceptor = await _entidadService.GetEntidadesAsync(new EntidadQueryDto { RFC = factura.ReceptorRfc });
                var entidadReceptor = entidadesReceptor.FirstOrDefault(e => string.Equals(e.RFC, factura.ReceptorRfc, StringComparison.OrdinalIgnoreCase));
                var regimenesFiscales = await _satCatalogoService.ListarRegimenFiscalAsync(incluirInactivos: true);
                var usosCfdi = await _satCatalogoService.ListarUsoCfdiAsync(incluirInactivos: true);

                var direccionEmisor = FormatearDireccion(entidadEmisor);
                var direccionReceptor = FormatearDireccion(entidadReceptor);
                var regimenEmisorTexto = DescribirClaveSat(regimenesFiscales, factura.EmisorRegimenFiscal);
                var regimenReceptorTexto = DescribirClaveSat(regimenesFiscales, factura.ReceptorRegimenFiscal);
                var usoCfdiTexto = DescribirClaveSat(usosCfdi, factura.ReceptorUsoCfdi);

                var documento = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.Letter);
                        page.Margin(1.5f, Unit.Centimetre);
                        page.PageColor(Colors.White);
                        // Fuente explícita: sin esto, QuestPDF puede caer a una fuente del sistema cuya
                        // tabla de ligaduras se "come" la combinación "ti" (certificado->cerficado,
                        // timbrado->mbrado) y cuyas métricas de ancho no coinciden con las usadas para
                        // calcular el layout -- eso recorta los valores monetarios pegados al borde
                        // derecho de su columna (Subtotal/Impuestos/Total). Segoe UI es fuente estándar
                        // de Windows, sin este problema.
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

                            // Emisor / Receptor
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
                                    receptor.Item().Text($"Uso CFDI: {usoCfdiTexto}");
                                    receptor.Item().Text($"Domicilio fiscal: {factura.ReceptorDomicilioFiscal ?? "-"}");
                                    receptor.Item().Text($"Régimen fiscal: {regimenReceptorTexto}");
                                    if (direccionReceptor != null)
                                    {
                                        receptor.Item().Text(direccionReceptor);
                                    }
                                });
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
                                    header.Cell().Element(EstiloEncabezado).Text(t => { t.AlignRight(); t.Span("P.U."); });
                                    header.Cell().Element(EstiloEncabezado).Text(t => { t.AlignRight(); t.Span("Importe"); });
                                });

                                foreach (var concepto in detalle.Conceptos.OrderBy(c => c.Orden))
                                {
                                    tabla.Cell().Element(EstiloCelda).Text(concepto.CantidadTexto);
                                    tabla.Cell().Element(EstiloCelda).Text(concepto.UnidadTexto);
                                    tabla.Cell().Element(EstiloCelda).Text(concepto.ClaveProdServ ?? "-");
                                    tabla.Cell().Element(EstiloCelda).Text(concepto.Descripcion);
                                    tabla.Cell().Element(EstiloCelda).Text(t => { t.AlignRight(); t.Span(concepto.ValorUnitarioTexto); });
                                    tabla.Cell().Element(EstiloCelda).Text(t => { t.AlignRight(); t.Span(concepto.ImporteTexto); });
                                }
                            });

                            // Totales
                            column.Item().AlignRight().Width(220).Column(totales =>
                            {
                                totales.Item().Row(row =>
                                {
                                    row.RelativeItem().Text("Subtotal:");
                                    row.RelativeItem().Text(t => { t.AlignRight(); t.Span(factura.SubTotalTexto); });
                                });
                                totales.Item().Row(row =>
                                {
                                    row.RelativeItem().Text("Impuestos trasladados:");
                                    row.RelativeItem().Text(t => { t.AlignRight(); t.Span(factura.TotalImpuestosTexto); });
                                });
                                totales.Item().PaddingTop(3).BorderTop(1).BorderColor(Colors.Grey.Lighten1).Row(row =>
                                {
                                    row.RelativeItem().Text("Total:").SemiBold().FontSize(11);
                                    row.RelativeItem().Text(t =>
                                    {
                                        t.AlignRight();
                                        t.DefaultTextStyle(s => s.SemiBold().FontSize(11).FontColor(Colors.Blue.Darken2));
                                        t.Span(factura.TotalTexto);
                                    });
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
                                        header.Cell().Element(EstiloEncabezado).Text(t => { t.AlignRight(); t.Span("Base"); });
                                        header.Cell().Element(EstiloEncabezado).Text(t => { t.AlignRight(); t.Span("Tasa"); });
                                        header.Cell().Element(EstiloEncabezado).Text(t => { t.AlignRight(); t.Span("Importe"); });
                                    });

                                    foreach (var traslado in detalle.TrasladosGlobales)
                                    {
                                        tabla.Cell().Element(EstiloCelda).Text(traslado.ImpuestoResumen);
                                        tabla.Cell().Element(EstiloCelda).Text(t => { t.AlignRight(); t.Span(traslado.BaseTexto); });
                                        tabla.Cell().Element(EstiloCelda).Text(t => { t.AlignRight(); t.Span(traslado.TasaTexto); });
                                        tabla.Cell().Element(EstiloCelda).Text(t => { t.AlignRight(); t.Span(traslado.ImporteTexto); });
                                    }
                                });
                            }

                            // Datos generales del comprobante
                            column.Item().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(8).Row(row =>
                            {
                                row.RelativeItem().Column(col =>
                                {
                                    col.Item().Text(t => { t.Span("Método de pago: ").SemiBold(); t.Span(factura.MetodoPago ?? "-"); });
                                    col.Item().Text(t => { t.Span("Forma de pago: ").SemiBold(); t.Span(factura.FormaPagoTexto); });
                                });
                                row.RelativeItem().Column(col =>
                                {
                                    col.Item().Text(t => { t.Span("Moneda: ").SemiBold(); t.Span(factura.Moneda); });
                                    col.Item().Text(t => { t.Span("Condiciones de pago: ").SemiBold(); t.Span(factura.CondicionesDePago ?? "-"); });
                                });
                            });

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
                                "Este documento es una representación impresa de un CFDI. Puede verificar su autenticidad " +
                                "en el portal del SAT capturando el folio fiscal (UUID), el RFC del emisor y del receptor.")
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
                _ = _logger.LogInformationAsync($"Factura PDF generada: {rutaArchivo}", "FacturaPdfService", "GenerarFacturaPdfAsync");

                return rutaArchivo;
            }
            catch (Exception ex)
            {
                _ = _logger.LogErrorAsync("Error al generar el PDF de la factura", ex, "FacturaPdfService", "GenerarFacturaPdfAsync");
                throw;
            }
        }

        public Task<string> GenerarAcuseCancelacionPdfAsync(FacturaDetalleDto detalle)
        {
            if (detalle == null) throw new ArgumentNullException(nameof(detalle));
            if (detalle.Factura == null) throw new InvalidOperationException("El detalle de la factura no trae la información de encabezado.");

            var factura = detalle.Factura;
            if (!factura.Cancelada)
                throw new InvalidOperationException("Esta factura no está cancelada; no hay acuse de cancelación que generar.");

            try
            {
                var carpeta = GetFacturasFolder();
                Directory.CreateDirectory(carpeta);

                var nombreArchivo = $"AcuseCancelacion_{factura.IdFactura}_{LimpiarNombreArchivo(factura.FolioTitulo)}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                var rutaArchivo = Path.Combine(carpeta, nombreArchivo);
                var cabeceraPath = Path.Combine(GetCabecerasFolder(), "Factura.png");

                var acuse = ParsearAcuseCancelacion(factura.AcuseCancelacionXml);
                var qrBytes = ConstruirQrVerificacion(factura);

                var documento = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.Letter);
                        page.Margin(1.5f, Unit.Centimetre);
                        page.PageColor(Colors.White);
                        page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Segoe UI"));

                        page.Header().ShowOnce().Column(column =>
                        {
                            if (File.Exists(cabeceraPath))
                            {
                                column.Item().Image(cabeceraPath).FitWidth();
                            }

                            column.Item().Row(row =>
                            {
                                row.RelativeItem().Text("Acuse de Cancelación de CFDI")
                                    .FontSize(18)
                                    .SemiBold()
                                    .FontColor(Colors.Red.Darken2);

                                row.ConstantItem(200).Text(text =>
                                {
                                    text.AlignRight();
                                    text.DefaultTextStyle(s => s.FontSize(10));
                                    text.Span("Folio: ").SemiBold();
                                    text.Span(factura.FolioTitulo);
                                });
                            });
                        });

                        page.Content().PaddingVertical(0.5f, Unit.Centimetre).Column(column =>
                        {
                            column.Spacing(8);

                            // Factura cancelada
                            column.Item().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(8).Column(col =>
                            {
                                col.Item().Text("Factura cancelada").SemiBold().FontColor(Colors.Blue.Darken2);
                                col.Item().Text(t => { t.Span("Folio fiscal (UUID): ").SemiBold(); t.Span(factura.Uuid ?? "-"); });
                                col.Item().Text(t => { t.Span("Receptor: ").SemiBold(); t.Span($"{factura.ReceptorNombre ?? "-"} (RFC: {factura.ReceptorRfc ?? "-"})"); });
                                col.Item().Text(t => { t.Span("Total: ").SemiBold(); t.Span(factura.TotalTexto); });
                                col.Item().Text(t => { t.Span("Fecha de emisión: ").SemiBold(); t.Span(factura.FechaTexto); });
                                col.Item().Text(t => { t.Span("Fecha de timbrado: ").SemiBold(); t.Span(factura.FechaTimbrado?.ToString("dd/MM/yyyy HH:mm:ss") ?? "-"); });
                            });

                            // Datos de la cancelación
                            column.Item().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(8).Column(col =>
                            {
                                col.Item().Text("Datos de la cancelación").SemiBold().FontColor(Colors.Blue.Darken2);
                                col.Item().Text(t => { t.Span("Motivo: ").SemiBold(); t.Span(MotivoCancelacionTexto(factura.MotivoCancelacion)); });
                                if (!string.IsNullOrWhiteSpace(factura.UuidSustitucion))
                                {
                                    col.Item().Text(t => { t.Span("Folio que sustituye: ").SemiBold(); t.Span(factura.UuidSustitucion); });
                                }
                                col.Item().Text(t => { t.Span("Fecha de cancelación: ").SemiBold(); t.Span(factura.FechaCancelacion?.ToString("dd/MM/yyyy HH:mm:ss") ?? "-"); });
                            });

                            // Confirmación del SAT (parseada del XML del acuse)
                            column.Item().Background(Colors.Green.Lighten5).Border(1).BorderColor(Colors.Green.Lighten1).Padding(8).Row(row =>
                            {
                                row.RelativeItem(3).Column(col =>
                                {
                                    col.Item().Text("Confirmación del SAT").SemiBold().FontColor(Colors.Green.Darken2);
                                    col.Item().Text(t => { t.Span("Fecha del acuse: ").SemiBold(); t.Span(acuse.Fecha?.ToString("dd/MM/yyyy HH:mm:ss") ?? "-"); });
                                    col.Item().Text(t => { t.Span("RFC emisor: ").SemiBold(); t.Span(acuse.RfcEmisor ?? factura.EmisorRfc ?? "-"); });
                                    col.Item().Text(t =>
                                    {
                                        t.Span("Estatus: ").SemiBold();
                                        t.Span(acuse.EstatusUuid == "201" ? "201 · Folio Fiscal Cancelado" : acuse.EstatusUuid ?? "-");
                                    });
                                    col.Item().Text(t => { t.Span("No. certificado SAT: ").SemiBold(); t.Span(acuse.NoCertificadoSat ?? "-"); });
                                });

                                row.ConstantItem(110).AlignCenter().Column(qrCol =>
                                {
                                    if (qrBytes != null)
                                    {
                                        qrCol.Item().Width(100).Image(qrBytes);
                                    }
                                });
                            });

                            column.Item().PaddingTop(4).Text(
                                "Este acuse no tiene validez fiscal por sí mismo; es la representación de la confirmación de " +
                                "cancelación emitida por el SAT. La cancelación del CFDI referido arriba sí es válida y definitiva " +
                                "ante el SAT. Puede verificar el estatus del folio fiscal en el portal del SAT capturando el UUID, " +
                                "el RFC del emisor y del receptor.")
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
                _ = _logger.LogInformationAsync($"Acuse de cancelación PDF generado: {rutaArchivo}", "FacturaPdfService", "GenerarAcuseCancelacionPdfAsync");

                return Task.FromResult(rutaArchivo);
            }
            catch (Exception ex)
            {
                _ = _logger.LogErrorAsync("Error al generar el PDF del acuse de cancelación", ex, "FacturaPdfService", "GenerarAcuseCancelacionPdfAsync");
                throw;
            }
        }

        /// <summary>
        /// Extrae Fecha/RfcEmisor/EstatusUUID del &lt;Acuse&gt; que regresa Bilkon, y el número de
        /// serie del certificado del SAT embebido en la firma XML-DSig (Signature/KeyInfo/
        /// X509Data/X509Certificate) -- mismo patrón que ya usamos para leer certificados X.509 en
        /// otras partes del proyecto. Si el XML no se puede parsear (o falta algún dato), se
        /// devuelven los campos en null en vez de lanzar -- el PDF debe poder generarse igual,
        /// mostrando "-" donde falte información.
        /// </summary>
        private static (DateTime? Fecha, string? RfcEmisor, string? EstatusUuid, string? NoCertificadoSat) ParsearAcuseCancelacion(string? xmlAcuse)
        {
            if (string.IsNullOrWhiteSpace(xmlAcuse))
            {
                return (null, null, null, null);
            }

            try
            {
                var doc = XDocument.Parse(xmlAcuse);
                var acuseEl = doc.Root;

                DateTime? fecha = DateTime.TryParse(acuseEl?.Attribute("Fecha")?.Value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var f) ? f : null;
                var rfcEmisor = acuseEl?.Attribute("RfcEmisor")?.Value;
                var estatusUuid = acuseEl?.Descendants().FirstOrDefault(e => e.Name.LocalName == "EstatusUUID")?.Value;

                string? noCertificadoSat = null;
                var x509CertEl = acuseEl?.Descendants().FirstOrDefault(e => e.Name.LocalName == "X509Certificate");
                if (x509CertEl != null)
                {
                    try
                    {
                        var certBytes = Convert.FromBase64String(x509CertEl.Value.Trim());
                        using var cert = new X509Certificate2(certBytes);
                        noCertificadoSat = cert.SerialNumber;
                    }
                    catch
                    {
                        // Certificado embebido no legible -- se deja el número de certificado en null.
                    }
                }

                return (fecha, rfcEmisor, estatusUuid, noCertificadoSat);
            }
            catch
            {
                return (null, null, null, null);
            }
        }

        private static string MotivoCancelacionTexto(string? motivo) => motivo switch
        {
            "01" => "01 · Comprobante emitido con errores con relación",
            "02" => "02 · Comprobante emitido con errores sin relación",
            "03" => "03 · No se llevó a cabo la operación",
            "04" => "04 · Operación nominativa relacionada en una factura global",
            _ => motivo ?? "-"
        };

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

            // Formato oficial del SAT (Anexo 20): NO se codifica como URL, en particular "fe" -- es
            // base64 y puede terminar en "=="; si se percent-encodea ("%3D%3D") el portal del SAT no
            // carga los datos fiscales aunque la página cargue. El "?&id=" (con el "&" pegado al "?")
            // también es tal cual como lo emite el propio SAT en sus PDFs oficiales.
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
