using Advance_Control.Models;
using Advance_Control.Services.LocalStorage;
using Advance_Control.Services.Logging;
using Advance_Control.Services.Notificacion;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage.Streams;

namespace Advance_Control.Services.Quotes
{
    /// <summary>
    /// Servicio para generar cotizaciones en PDF usando QuestPDF
    /// </summary>
    public class QuoteService : IQuoteService
    {
        /// <summary>
        /// IVA rate (16%) for Mexican tax calculation
        /// </summary>
        private const double IVA_RATE = 0.16;

        private readonly ILoggingService _logger;
        private readonly IOperacionImageService _operacionImageService;
        private readonly INotificacionService _notificacionService;
        private readonly IFirmaService _firmaService;

        public QuoteService(ILoggingService logger, IOperacionImageService operacionImageService, INotificacionService notificacionService, IFirmaService firmaService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _operacionImageService = operacionImageService ?? throw new ArgumentNullException(nameof(operacionImageService));
            _notificacionService = notificacionService ?? throw new ArgumentNullException(nameof(notificacionService));
            _firmaService = firmaService ?? throw new ArgumentNullException(nameof(firmaService));

            // Configure QuestPDF license
            QuestPDF.Settings.License = LicenseType.Community;
        }

        /// <summary>
        /// Obtiene la ruta de la carpeta de firmas
        /// </summary>
        private string GetFirmasFolder() => _firmaService.GetFirmasFolder();

        /// <summary>
        /// Obtiene la ruta de la carpeta de cabeceras
        /// </summary>
        private string GetCabecerasFolder()
        {
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            return Path.Combine(documentsPath, "Advance Control", "Cabeceras");
        }

        /// <summary>
        /// Obtiene la ruta de la carpeta de una operación
        /// </summary>
        private static string GetOperacionFolder(int idOperacion)
        {
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            return Path.Combine(documentsPath, "Advance Control", $"Operacion_{idOperacion}");
        }

        /// <inheritdoc/>
        public string? FindExistingPdf(int idOperacion, string tipo)
        {
            var folder = GetOperacionFolder(idOperacion);
            if (!Directory.Exists(folder)) return null;
            return Directory.GetFiles(folder, $"{tipo}_{idOperacion}_*.pdf").FirstOrDefault();
        }

        /// <inheritdoc/>
        public void DeleteOperacionPdfs(int idOperacion, string tipo)
        {
            var folder = GetOperacionFolder(idOperacion);
            if (!Directory.Exists(folder)) return;
            var pattern = tipo == "*" ? "*.pdf" : $"{tipo}_{idOperacion}_*.pdf";
            foreach (var file in Directory.GetFiles(folder, pattern))
            {
                try { File.Delete(file); } catch { /* ignorar si está en uso */ }
            }
        }

        /// <summary>
        /// Carga un archivo de imagen como array de bytes de forma segura.
        /// Devuelve null si el archivo no existe o no se puede leer.
        /// </summary>
        private static byte[]? TryLoadBytes(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return null;
            try { return File.ReadAllBytes(path); }
            catch { return null; }
        }

        /// <summary>
        /// Busca la firma del operador por su idAtiende usando IFirmaService.
        /// </summary>
        private string? FindFirmaOperador(string _, int idAtiende)
            => _firmaService.GetFirmaOperadorPath(idAtiende);

        /// <summary>
        /// Agrega la sección de firmas al final del contenido del PDF.
        /// FirmaDireccion.png a la izquierda y firma del operador a la derecha.
        /// Si no existe firma del operador, FirmaDireccion.png se centra y se notifica error.
        /// </summary>
        private void AddFirmasSection(QuestPDF.Infrastructure.IContainer container, int? idAtiende)
        {
            var firmasFolder = GetFirmasFolder();
            var firmaDireccionPath = Path.Combine(firmasFolder, "FirmaDireccion.png");
            string? firmaOperadorPath = null;

            if (idAtiende.HasValue)
            {
                firmaOperadorPath = FindFirmaOperador(firmasFolder, idAtiende.Value);
            }

            bool hasFirmaDireccion = File.Exists(firmaDireccionPath);
            bool hasFirmaOperador = firmaOperadorPath != null && File.Exists(firmaOperadorPath);

            if (!hasFirmaOperador)
            {
                try
                {
                    _ = _notificacionService.MostrarNotificacionAsync("No existe firma para operador");
                }
                catch
                {
                    // Notification failure should not prevent PDF generation
                }
            }

            if (!hasFirmaDireccion)
            {
                try
                {
                    _ = _notificacionService.MostrarNotificacionAsync("No existe firma de dirección");
                }
                catch
                {
                    // Notification failure should not prevent PDF generation
                }
            }

            container.PaddingTop(25).Column(firmasCol =>
            {
                if (hasFirmaDireccion && hasFirmaOperador)
                {
                    // Both signatures: direction on left, operator on right
                    firmasCol.Item().Row(row =>
                    {
                        row.RelativeItem().AlignLeft().Height(100).Image(firmaDireccionPath).FitArea();
                        row.RelativeItem().AlignRight().Height(100).Image(firmaOperadorPath).FitArea();
                    });
                }
                else if (hasFirmaDireccion)
                {
                    // Only direction signature: center it
                    firmasCol.Item().AlignCenter().Height(100).Image(firmaDireccionPath).FitArea();
                }
            });
        }

        /// <summary>
        /// Genera un PDF de cotización a partir de una operación y sus cargos
        /// </summary>
        public async Task<string> GenerateQuotePdfAsync(OperacionDto operacion, IEnumerable<CargoDto> cargos, string? ubicacionNombre = null, string? nombreEmpresa = null, string? apoderadoNombre = null, decimal? limiteCredito = null, string? dirigidoA = null)
        {
            if (operacion == null)
                throw new ArgumentNullException(nameof(operacion));

            if (cargos == null)
                throw new ArgumentNullException(nameof(cargos));

            try
            {
                await _logger.LogInformationAsync($"Generando cotización PDF para operación {operacion.IdOperacion}", "QuoteService", "GenerateQuotePdfAsync");

                // Generate filename: Cotizacion_{idOperacion}_{identificador}_{fechaActual}.pdf
                var fecha = DateTime.Now.ToString("yyyyMMdd");
                var idStr = operacion.Identificador ?? "sin-equipo";
                var safeId = string.Concat(idStr.Split(Path.GetInvalidFileNameChars()));
                var fileName = $"Cotizacion_{operacion.IdOperacion}_{safeId}_{fecha}.pdf";
                var operacionFolder = GetOperacionFolder(operacion.IdOperacion ?? 0);

                // Create directory if it doesn't exist
                Directory.CreateDirectory(operacionFolder);

                var filePath = Path.Combine(operacionFolder, fileName);
                var quotationTitle = $"Cotización {operacion.IdOperacion}";
                var cotizacionImagePath = Path.Combine(GetCabecerasFolder(), "Cotizacion.png");

                // Generate PDF
                var document = Document.Create(container =>
                {
                    var usCulture = new CultureInfo("en-US");
                    container.Page(page =>
                    {
                        page.Size(PageSizes.Letter);
                        page.Margin(2, Unit.Centimetre);
                        page.PageColor(Colors.White);
                        page.DefaultTextStyle(x => x.FontSize(11));

                        // Header - only show on first page
                        page.Header()
                            .ShowOnce()
                            .Column(column =>
                            {
                                if (File.Exists(cotizacionImagePath))
                                {
                                    column.Item().Image(cotizacionImagePath).FitWidth();
                                }

                                column.Item().AlignRight().Text(quotationTitle)
                                    .FontSize(16)
                                    .FontColor(Colors.Blue.Darken2);

                                
                            });

                        page.Content()
                            .PaddingVertical(1, Unit.Centimetre)
                            .Column(column =>
                            {

                                column.Spacing(5);

                                // Use consistent date throughout the document
                                var quoteDate = operacion.FechaInicio ?? DateTime.Now;

                                // Information section
                                column.Item().Row(row =>
                                {
                                    row.RelativeItem().Column(col =>
                                    {
                                        col.Item().Text(text =>
                                        {
                                            text.Span("Cliente: ").Bold();
                                            text.Span(operacion.RazonSocial ?? "N/A");
                                        });
                                        col.Item().Text(text =>
                                        {
                                            text.Span("Equipo: ").Bold();
                                            text.Span(operacion.Identificador ?? "N/A");
                                        });
                                        if (!string.IsNullOrWhiteSpace(dirigidoA))
                                        {
                                            col.Item().Text(text =>
                                            {
                                                text.Span("Dirigido a: ").Bold();
                                                text.Span(dirigidoA);
                                            });
                                        }
                                    });

                                    row.RelativeItem().PaddingLeft(70).Column(col =>
                                    {
                                        col.Item().AlignRight().PaddingLeft(20).Text(text =>
                                        {
                                            text.Span("Fecha: ").Bold();
                                            text.Span($"{quoteDate:dd/MM/yyyy}");
                                        });

                                        col.Item().AlignRight().PaddingLeft(20).Text(text =>
                                        {
                                            text.Span("Atendido por: ").Bold();
                                            text.Span(operacion.Atiende ?? "N/A");
                                        });

                                        col.Item().AlignRight().PaddingLeft(20).Text(text =>
                                        {
                                            text.Span("Tipo: ").Bold();
                                            text.Span((operacion.TipoMantenimiento ?? "N/A"));
                                        });
                                    });
                                });

                                // Leyenda de identificación — renglón completo (dos columnas)
                                column.Item().Text(text =>
                                {
                                    text.AlignCenter();
                                    text.DefaultTextStyle(s => s.FontSize(13));
                                    text.Span("Cotización No: ").Bold();
                                    text.Span($"{operacion.IdOperacion}");
                                });

                                if (!string.IsNullOrWhiteSpace(ubicacionNombre))
                                {
                                    column.Item().Text(text =>
                                    {
                                        text.Span("Ubicación: ").Bold();
                                        text.Span(ubicacionNombre);
                                    });
                                }

                                var cargosList = cargos.ToList();

                                column.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {

                                        columns.RelativeColumn(1);  // Cantidad
                                        columns.RelativeColumn(1);  // Tipo
                                        columns.RelativeColumn(2);  // Detalle
                                        columns.RelativeColumn(1);  // P.U.
                                        columns.RelativeColumn(1);  // SubTotal
                                    });

                                    // Header
                                    table.Header(header =>
                                    {
                                        header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Cantidad").Bold();
                                        header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Tipo").Bold();
                                        header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Detalle").Bold();
                                        header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("P.U.").Bold();
                                        header.Cell().Background(Colors.Grey.Lighten2).Padding(5).AlignRight().PaddingLeft(20).Text("Monto").Bold();
                                    });

                                    // Rows
                                    foreach (var cargo in cargosList)
                                    {
                                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignMiddle()
                                            .Text(cargo.Cantidad.ToString() ?? "N/A");
                                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignMiddle()
                                            .Text(cargo.TipoCargo ?? "N/A");
                                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5)
                                            .Column(col =>
                                            {
                                                col.Item().Text(cargo.DetalleLinea1 ?? "N/A");
                                                if (cargo.TieneSubdetalle)
                                                {
                                                    col.Item().PaddingTop(2).Text(cargo.DetalleLinea2 ?? "").FontSize(9).FontColor(Colors.Grey.Medium);
                                                    col.Item().Text(cargo.DetalleLinea3 ?? "").FontSize(9).FontColor(Colors.Grey.Darken1);
                                                }
                                            });
                                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignMiddle()
                                            .Text($"${(cargo.Unitario ?? 0).ToString("N2", usCulture)}");
                                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignMiddle()
                                            .AlignRight().PaddingLeft(20).Text($"${(cargo.Monto ?? 0).ToString("N2", usCulture)}");
                                    }
                                });

                                // Totals section with IVA
                                var subtotal = cargosList.Sum(c => c.Monto ?? 0);
                                var iva = subtotal * IVA_RATE;
                                var total = subtotal + iva;

                                

                                column.Item().PaddingTop(10).AlignRight().Width(140).Column(totalsCol =>
                                {
                                    totalsCol.Item().Row(row =>
                                    {
                                        row.RelativeItem().Text("SUBTOTAL: ").FontSize(10).AlignLeft();
                                        row.RelativeItem().Text($"${subtotal.ToString("N2", usCulture)}").FontSize(10).AlignRight();
                                    });
                                    totalsCol.Item().PaddingTop(3).Row(row =>
                                    {
                                        row.RelativeItem().Text("IVA (16%): ").FontSize(10).AlignLeft();
                                        row.RelativeItem().Text($"${iva.ToString("N2", usCulture)}").FontSize(10).AlignRight();
                                    });
                                    totalsCol.Item().PaddingTop(5).Row(row =>
                                    {
                                        row.RelativeItem().Text("TOTAL: ").Bold().FontSize(10).AlignLeft();
                                        row.RelativeItem().Text($"${total.ToString("N2", usCulture)}").Bold().FontSize(10).FontColor(Colors.Blue.Darken2).AlignRight();
                                    });
                                });

                                // Payment terms and validity notes section
                                column.Item().Border(1).BorderColor(Colors.Grey.Lighten1).Background(Colors.Grey.Lighten4).Padding(15).Column(notesCol =>
                                {
                                    notesCol.Item().Text("Condiciones de pago:").Bold().FontSize(11).FontColor(Colors.Blue.Darken2);
                                    if (limiteCredito.HasValue && (decimal)total >= limiteCredito.Value)
                                    {
                                        notesCol.Item().PaddingTop(8).Text("• Se solicita anticipo del 70% para realizar este trabajo").FontSize(10);
                                        notesCol.Item().PaddingTop(4).Text("• El 30% restante, al término de este").FontSize(10);
                                    }
                                   
                                    notesCol.Item().PaddingTop(4).Text("• Vigencia de esta cotización: treinta días naturales").FontSize(10);
                                });

                                // Closing salutation section
                                column.Item().PaddingTop(25).Column(closingCol =>
                                {
                                    AddFirmasSection(column.Item(), operacion.IdAtiende);
                                });

                                // Signature images section

                            });

                        page.Footer()
                            .AlignCenter()
                            .Text(x =>
                            {
                                x.Span($"Cotización {operacion.IdOperacion}, Hoja ");
                                x.CurrentPageNumber();
                                x.Span("/");
                                x.TotalPages();
                            });
                    });
                });

                // Generate and save PDF
                document.GeneratePdf(filePath);

                await _logger.LogInformationAsync($"Cotización PDF generada exitosamente: {filePath}", "QuoteService", "GenerateQuotePdfAsync");

                return filePath;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error al generar cotización PDF", ex, "QuoteService", "GenerateQuotePdfAsync");
                throw;
            }
        }

        /// <summary>
        /// Genera un PDF de reporte de cotización con fotos de cargos
        /// </summary>
        public async Task<string> GenerateReportePdfAsync(OperacionDto operacion, IEnumerable<CargoDto> cargos, string? ubicacionNombre = null, string? nombreEmpresa = null, string? dirigidoA = null)
        {
            if (operacion == null)
                throw new ArgumentNullException(nameof(operacion));

            if (cargos == null)
                throw new ArgumentNullException(nameof(cargos));

            var tempImageFiles = new List<string>();
            try
            {
                await _logger.LogInformationAsync($"Generando reporte PDF para operación {operacion.IdOperacion}", "QuoteService", "GenerateReportePdfAsync");

                // Generate filename: Reporte_{idOperacion}_{identificador}_{fechaActual}.pdf
                var fecha = DateTime.Now.ToString("yyyyMMdd");
                var idStr = operacion.Identificador ?? "sin-equipo";
                var safeId = string.Concat(idStr.Split(Path.GetInvalidFileNameChars()));
                var fileName = $"Reporte_{operacion.IdOperacion}_{safeId}_{fecha}.pdf";
                var operacionFolder = GetOperacionFolder(operacion.IdOperacion ?? 0);

                // Create directory if it doesn't exist
                Directory.CreateDirectory(operacionFolder);

                var filePath = Path.Combine(operacionFolder, fileName);

                var reportTitle = $"Reporte {operacion.IdOperacion}";
                var cargosList = cargos.ToList();
                var reporteImagePath = Path.Combine(GetCabecerasFolder(), "Reporte.png");
                var reportDate = DateTime.Now;

                // Cargar listas de imágenes (async, fuera del lambda de QuestPDF)
                List<OperacionImageDto> prefacturas = new List<OperacionImageDto>();
                List<OperacionImageDto> hojasServicio = new List<OperacionImageDto>();
                List<OperacionImageDto> ordenesCompra = new List<OperacionImageDto>();

                if (operacion.IdOperacion.HasValue)
                {
                    prefacturas = await _operacionImageService.GetPrefacturasAsync(operacion.IdOperacion.Value);
                    hojasServicio = await _operacionImageService.GetHojasServicioAsync(operacion.IdOperacion.Value);
                    try { ordenesCompra = await _operacionImageService.GetOrdenComprasAsync(operacion.IdOperacion.Value); } catch { }
                }

                // Expandir rutas: imágenes directamente, PDFs convertidos a PNGs temporales
                var prefacturasPaths = await ExpandDocumentPathsAsync(prefacturas, tempImageFiles);
                var hojasPaths       = await ExpandDocumentPathsAsync(hojasServicio, tempImageFiles);
                var ordenesPaths     = await ExpandDocumentPathsAsync(ordenesCompra, tempImageFiles);

                // Filtrar rutas de imágenes de cargos (validadas fuera del lambda)
                var cargosConImagenes = cargosList.Select(cargo => new
                {
                    Cargo = cargo,
                    ImagesPaths = cargo.Images
                        .Where(img => !string.IsNullOrWhiteSpace(img.Url) && File.Exists(img.Url))
                        .Select(img => img.Url)
                        .ToList()
                }).ToList();

                // Generate PDF
                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.Letter);
                        page.Margin(2, Unit.Centimetre);
                        page.PageColor(Colors.White);
                        page.DefaultTextStyle(x => x.FontSize(11));

                        // ── Cabecera (sólo primera página) ──────────────────────────
                        page.Header()
                            .ShowOnce()
                            .Column(column =>
                            {
                                if (File.Exists(reporteImagePath))
                                    column.Item().Image(reporteImagePath).FitWidth();

                                column.Item().AlignRight().Text(reportTitle)
                                    .FontSize(16)
                                    .Bold()
                                    .FontColor(Colors.Blue.Darken2);
                            });

                        // ── Contenido ───────────────────────────────────────────────
                        page.Content()
                            .PaddingVertical(1, Unit.Centimetre)
                            .Column(column =>
                            {
                                column.Spacing(6);

                                // ── Datos generales (misma estructura que cotización) ─
                                column.Item().Row(row =>
                                {
                                    row.RelativeItem().Column(col =>
                                    {
                                        col.Item().Text(text =>
                                        {
                                            text.Span("Cliente: ").Bold();
                                            text.Span(operacion.RazonSocial ?? "N/A");
                                        });
                                        col.Item().Text(text =>
                                        {
                                            text.Span("Equipo: ").Bold();
                                            text.Span(operacion.Identificador ?? "N/A");
                                        });
                                        if (!string.IsNullOrWhiteSpace(dirigidoA))
                                        {
                                            col.Item().Text(text =>
                                            {
                                                text.Span("Dirigido a: ").Bold();
                                                text.Span(dirigidoA);
                                            });
                                        }
                                        if (!string.IsNullOrWhiteSpace(ubicacionNombre))
                                        {
                                            col.Item().Text(text =>
                                            {
                                                text.Span("Ubicación: ").Bold();
                                                text.Span(ubicacionNombre);
                                            });
                                        }
                                    });

                                    row.RelativeItem().PaddingLeft(70).Column(col =>
                                    {
                                        col.Item().AlignRight().PaddingLeft(20).Text(text =>
                                        {
                                            text.Span("Fecha: ").Bold();
                                            text.Span($"{reportDate:dd/MM/yyyy}");
                                        });
                                        col.Item().AlignRight().PaddingLeft(20).Text(text =>
                                        {
                                            text.Span("Atendido por: ").Bold();
                                            text.Span(operacion.Atiende ?? "N/A");
                                        });
                                        col.Item().AlignRight().PaddingLeft(20).Text(text =>
                                        {
                                            text.Span("Tipo: ").Bold();
                                            text.Span((operacion.TipoMantenimiento ?? "N/A"));
                                        });
                                    });
                                });

                                // Leyenda de identificación — renglón completo (dos columnas)
                                column.Item().Text(text =>
                                {
                                    text.AlignCenter();
                                    text.DefaultTextStyle(s => s.FontSize(13));
                                    text.Span("Reporte No: ").Bold();
                                    text.Span($"{operacion.IdOperacion}");
                                    text.Span(", Cotización No: ").Bold();
                                    text.Span($"{operacion.IdOperacion}");
                                    text.Span(", Orden de compra No: ").Bold();
                                    text.Span("N/D");
                                });

                                column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                                // ── Órdenes de compra (opcional) ────────────────────
                                if (ordenesPaths.Count > 0)
                                {
                                    column.Item().PaddingTop(4)
                                        .Background(Colors.Blue.Lighten4).Padding(6)
                                        .Text("Órdenes de Compra").Bold().FontSize(13).FontColor(Colors.Blue.Darken3);
                                    foreach (var path in ordenesPaths)
                                        column.Item().PaddingTop(4).Image(path).FitArea();
                                    column.Item().PaddingTop(6).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                                }

                                // ── Prefacturas (opcional) ──────────────────────────
                                if (prefacturasPaths.Count > 0)
                                {
                                    column.Item().PaddingTop(4)
                                        .Background(Colors.Blue.Lighten4).Padding(6)
                                        .Text("Prefacturas").Bold().FontSize(13).FontColor(Colors.Blue.Darken3);
                                    foreach (var path in prefacturasPaths)
                                        column.Item().PaddingTop(4).Image(path).FitArea();
                                    column.Item().PaddingTop(6).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                                }

                                // ── Hojas de servicio (opcional) ────────────────────
                                if (hojasPaths.Count > 0)
                                {
                                    column.Item().PaddingTop(4)
                                        .Background(Colors.Blue.Lighten4).Padding(6)
                                        .Text("Hojas de Servicio").Bold().FontSize(13).FontColor(Colors.Blue.Darken3);
                                    foreach (var path in hojasPaths)
                                        column.Item().PaddingTop(4).Image(path).FitArea();
                                    column.Item().PaddingTop(6).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                                }

                                // ── Detalle de cargos ───────────────────────────────
                                column.Item().PaddingTop(4)
                                    .Background(Colors.Blue.Lighten4).Padding(6)
                                    .Text("Detalle de Cargos").Bold().FontSize(13).FontColor(Colors.Blue.Darken3);

                                foreach (var item in cargosConImagenes)
                                {
                                    var cargo = item.Cargo;

                                    // Encabezado del cargo — fondo gris, no paginable (pequeño)
                                    column.Item().PaddingTop(6)
                                        .Background(Colors.Grey.Lighten4)
                                        .BorderTop(1).BorderColor(Colors.Grey.Lighten2)
                                        .Padding(8)
                                        .Row(row =>
                                        {
                                            row.RelativeItem(1)
                                                .Text($"Tipo: {cargo.TipoCargo ?? "N/A"}")
                                                .Bold().FontSize(10);

                                            row.RelativeItem(3).Column(detCol =>
                                            {
                                                detCol.Item().Text(cargo.DetalleLinea1 ?? "N/A").Bold();
                                                if (cargo.TieneSubdetalle)
                                                {
                                                    detCol.Item().PaddingTop(2)
                                                        .Text(cargo.DetalleLinea2 ?? "")
                                                        .FontSize(9).FontColor(Colors.Grey.Medium);
                                                    detCol.Item()
                                                        .Text(cargo.DetalleLinea3 ?? "")
                                                        .FontSize(9).FontColor(Colors.Grey.Darken1);
                                                }
                                            });
                                        });

                                    // Fotografías del cargo — 2 columnas, FitWidth usa el ancho de cada celda
                                    var imgPaths = item.ImagesPaths;
                                    for (int i = 0; i < imgPaths.Count; i += 2)
                                    {
                                        int idx = i; // captura para lambda
                                        column.Item().PaddingTop(4).Row(row =>
                                        {
                                            row.RelativeItem().Padding(2).Image(imgPaths[idx]).FitWidth();
                                            if (idx + 1 < imgPaths.Count)
                                                row.RelativeItem().Padding(2).Image(imgPaths[idx + 1]).FitWidth();
                                            else
                                                row.RelativeItem(); // celda vacía para mantener alineación
                                        });
                                    }
                                }

                                // ── Sección de firmas ───────────────────────────────
                                AddFirmasSection(column.Item(), operacion.IdAtiende);
                            });

                        // ── Pie de página ───────────────────────────────────────────
                        page.Footer()
                            .AlignCenter()
                            .Text(x =>
                            {
                                x.Span($"Reporte {operacion.IdOperacion}  ·  Hoja ");
                                x.CurrentPageNumber();
                                x.Span(" de ");
                                x.TotalPages();
                            });
                    });
                });

                // Generate and save PDF
                document.GeneratePdf(filePath);

                await _logger.LogInformationAsync($"Reporte PDF generado exitosamente: {filePath}", "QuoteService", "GenerateReportePdfAsync");

                return filePath;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error al generar reporte PDF", ex, "QuoteService", "GenerateReportePdfAsync");
                throw;
            }
            finally
            {
                foreach (var tmp in tempImageFiles)
                    try { File.Delete(tmp); } catch { }
            }
        }

        /// <summary>
        /// Expande las rutas de documentos: imágenes directamente, PDFs convertidos a PNGs temporales (una imagen por página).
        /// </summary>
        private static async Task<List<string>> ExpandDocumentPathsAsync(
            List<OperacionImageDto> docs, List<string> tempFiles)
        {
            var result = new List<string>();
            foreach (var dto in docs.Where(x => !string.IsNullOrWhiteSpace(x.Url) && File.Exists(x.Url)))
            {
                if (dto.IsPdf)
                {
                    var pages = await ConvertPdfPagesToImagesAsync(dto.Url!);
                    result.AddRange(pages);
                    tempFiles.AddRange(pages);
                }
                else
                {
                    result.Add(dto.Url!);
                }
            }
            return result;
        }

        /// <summary>
        /// Renderiza cada página de un PDF a un PNG temporal usando Windows.Data.Pdf (API nativa Windows, sin dependencias extra).
        /// </summary>
        private static async Task<List<string>> ConvertPdfPagesToImagesAsync(string pdfPath)
        {
            var tempFiles = new List<string>();
            try
            {
                var file    = await Windows.Storage.StorageFile.GetFileFromPathAsync(pdfPath);
                var pdfDoc  = await Windows.Data.Pdf.PdfDocument.LoadFromFileAsync(file);
                var tempFolder = Path.GetTempPath();
                var baseName   = Path.GetFileNameWithoutExtension(pdfPath);

                for (uint i = 0; i < pdfDoc.PageCount; i++)
                {
                    using var page     = pdfDoc.GetPage(i);
                    var tempPath       = Path.Combine(tempFolder, $"__acreporte_{baseName}_p{i}.png");
                    using var memStream = new InMemoryRandomAccessStream();
                    await page.RenderToStreamAsync(memStream);
                    memStream.Seek(0);
                    using var fileStream = File.Create(tempPath);
                    await memStream.AsStreamForRead().CopyToAsync(fileStream);
                    tempFiles.Add(tempPath);
                }
            }
            catch { /* Si falla la conversión del PDF, se omite sin romper el reporte */ }
            return tempFiles;
        }
    }
}
