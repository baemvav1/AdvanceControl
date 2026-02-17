using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.Logging;
using Advance_Control.Services.LocalStorage;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

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

        public QuoteService(ILoggingService logger, IOperacionImageService operacionImageService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _operacionImageService = operacionImageService ?? throw new ArgumentNullException(nameof(operacionImageService));
            
            // Configure QuestPDF license
            QuestPDF.Settings.License = LicenseType.Community;
        }

        /// <summary>
        /// Genera un PDF de cotización a partir de una operación y sus cargos
        /// </summary>
        public async Task<string> GenerateQuotePdfAsync(OperacionDto operacion, IEnumerable<CargoDto> cargos, string? ubicacionNombre = null, string? nombreEmpresa = null, string? apoderadoNombre = null)
        {
            if (operacion == null)
                throw new ArgumentNullException(nameof(operacion));

            if (cargos == null)
                throw new ArgumentNullException(nameof(cargos));

            try
            {
                await _logger.LogInformationAsync($"Generando cotización PDF para operación {operacion.IdOperacion}", "QuoteService", "GenerateQuotePdfAsync");

                // Generate filename with sanitized client name
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var clientName = operacion.RazonSocial ?? "Cliente";
                
                // Sanitize the client name to remove invalid filename characters
                var invalidChars = Path.GetInvalidFileNameChars();
                var sanitizedClientName = string.Join("_", clientName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
                
                var fileName = $"Cotizacion_{sanitizedClientName}_{timestamp}.pdf";
                var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                var quotesFolder = Path.Combine(documentsPath, "Advance Control", "Cotizaciones");
                
                // Create directory if it doesn't exist
                Directory.CreateDirectory(quotesFolder);
                
                var filePath = Path.Combine(quotesFolder, fileName);

                // Use company name from entity or default
                var companyTitle = !string.IsNullOrWhiteSpace(nombreEmpresa) ? nombreEmpresa.ToUpperInvariant() : "ADVANCE CONTROL";
                var quotationTitle = $"Cotización {operacion.IdOperacion}";

                // Generate PDF
                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.Letter);
                        page.Margin(2, Unit.Centimetre);
                        page.PageColor(Colors.White);
                        page.DefaultTextStyle(x => x.FontSize(11));

                        page.Header()
                            .Height(120)
                            .Background(Colors.Blue.Lighten3)
                            .Padding(20)
                            .Column(column =>
                            {
                                column.Item().AlignCenter().Text(companyTitle)
                                    .FontSize(24)
                                    .Bold()
                                    .FontColor(Colors.Blue.Darken3);
                                
                                column.Item().AlignCenter().Text(quotationTitle)
                                    .FontSize(16)
                                    .FontColor(Colors.Blue.Darken2);
                                
                                // Add Referencia with operation note
                                if (!string.IsNullOrWhiteSpace(operacion.Nota))
                                {
                                    column.Item().PaddingTop(5).AlignCenter().Text($"Referencia: {operacion.Nota}")
                                        .FontSize(12)
                                        .FontColor(Colors.Blue.Darken1);
                                }
                            });

                        page.Content()
                            .PaddingVertical(1, Unit.Centimetre)
                            .Column(column =>
                            {
                                column.Spacing(20);

                                // Use consistent date throughout the document
                                var quoteDate = operacion.FechaInicio ?? DateTime.Now;

                                // Information section
                                column.Item().Row(row =>
                                {
                                    row.RelativeItem().Column(col =>
                                    {
                                        col.Item().Text("Información del Cliente").Bold().FontSize(14);
                                        col.Item().PaddingTop(5);
                                        col.Item().Text($"Cliente: {operacion.RazonSocial ?? "N/A"}");
                                        col.Item().Text($"Equipo: {operacion.Identificador ?? "N/A"}");
                                        if (!string.IsNullOrWhiteSpace(ubicacionNombre))
                                        {
                                            col.Item().Text($"Ubicación: {ubicacionNombre}");
                                        }
                                    });

                                    row.RelativeItem().Column(col =>
                                    {
                                        col.Item().Text("Información de la Operación").Bold().FontSize(14);
                                        col.Item().PaddingTop(5);
                                        col.Item().Text($"Fecha: {quoteDate:dd/MM/yyyy}");
                                        col.Item().Text($"Atendido por: {operacion.Atiende ?? "N/A"}");
                                        col.Item().Text($"Tipo: {GetTipoOperacion(operacion.IdTipo)}");
                                    });
                                });

                                // Charges table
                                column.Item().Text("Desglose de Cargos").Bold().FontSize(14);
                                
                                var cargosList = cargos.ToList();
                                
                                column.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(2);  // Tipo
                                        columns.RelativeColumn(3);  // Detalle
                                        columns.RelativeColumn(2);  // Proveedor
                                        columns.RelativeColumn(3);  // Nota
                                        columns.RelativeColumn(1);  // Monto
                                    });

                                    // Header
                                    table.Header(header =>
                                    {
                                        header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Tipo").Bold();
                                        header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Detalle").Bold();
                                        header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Proveedor").Bold();
                                        header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Nota").Bold();
                                        header.Cell().Background(Colors.Grey.Lighten2).Padding(5).AlignRight().Text("Monto").Bold();
                                    });

                                    // Rows
                                    foreach (var cargo in cargosList)
                                    {
                                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5)
                                            .Text(cargo.TipoCargo ?? "N/A");
                                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5)
                                            .Text(cargo.DetalleRelacionado ?? "N/A");
                                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5)
                                            .Text(cargo.Proveedor ?? "N/A");
                                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5)
                                            .Text(cargo.Nota ?? "-");
                                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5)
                                            .AlignRight().Text($"${cargo.Monto ?? 0:N2}");
                                    }
                                });

                                // Totals section with IVA
                                var subtotal = cargosList.Sum(c => c.Monto ?? 0);
                                var iva = subtotal * IVA_RATE;
                                var total = subtotal + iva;

                                column.Item().PaddingTop(10).AlignRight().Column(totalsCol =>
                                {
                                    totalsCol.Item().Row(row =>
                                    {
                                        row.AutoItem().Text("Subtotal: ").FontSize(12);
                                        row.AutoItem().Text($"${subtotal:N2}").FontSize(12);
                                    });
                                    totalsCol.Item().PaddingTop(3).Row(row =>
                                    {
                                        row.AutoItem().Text("IVA (16%): ").FontSize(12);
                                        row.AutoItem().Text($"${iva:N2}").FontSize(12);
                                    });
                                    totalsCol.Item().PaddingTop(5).Row(row =>
                                    {
                                        row.AutoItem().Text("TOTAL: ").Bold().FontSize(14);
                                        row.AutoItem().Text($"${total:N2}").Bold().FontSize(14).FontColor(Colors.Blue.Darken2);
                                    });
                                });

                                // Payment terms and validity notes section
                                column.Item().PaddingTop(25).Border(1).BorderColor(Colors.Grey.Lighten1).Background(Colors.Grey.Lighten4).Padding(15).Column(notesCol =>
                                {
                                    notesCol.Item().Text("Condiciones de pago:").Bold().FontSize(11).FontColor(Colors.Blue.Darken2);
                                    notesCol.Item().PaddingTop(8).Text("• Se solicita anticipo del 70% para realizar este trabajo").FontSize(10);
                                    notesCol.Item().PaddingTop(4).Text("• El 30% restante, al término de este").FontSize(10);
                                    notesCol.Item().PaddingTop(4).Text("• Vigencia de esta cotización: treinta días naturales").FontSize(10);
                                });

                                // Closing salutation section
                                column.Item().PaddingTop(25).Column(closingCol =>
                                {
                                    closingCol.Item().Text("En la confianza de recibir su pronta respuesta, le envío un cordial saludo.")
                                        .FontSize(11).Italic();
                                    
                                    closingCol.Item().PaddingTop(20).Text("Atentamente,").FontSize(11);
                                    
                                    var apoderado = !string.IsNullOrWhiteSpace(apoderadoNombre) ? apoderadoNombre : "La Dirección";
                                    closingCol.Item().PaddingTop(25).Text(apoderado).Bold().FontSize(12);
                                    closingCol.Item().Text("Director General").FontSize(11).FontColor(Colors.Grey.Darken1);
                                });
                            });

                        page.Footer()
                            .AlignCenter()
                            .Text(x =>
                            {
                                x.Span("Página ");
                                x.CurrentPageNumber();
                                x.Span(" de ");
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

        private string GetTipoOperacion(int? idTipo)
        {
            return idTipo switch
            {
                1 => "Correctivo",
                2 => "Preventivo",
                _ => "N/A"
            };
        }

        /// <summary>
        /// Genera un PDF de reporte de cotización con fotos de cargos
        /// </summary>
        public async Task<string> GenerateReportePdfAsync(OperacionDto operacion, IEnumerable<CargoDto> cargos, string? ubicacionNombre = null, string? nombreEmpresa = null)
        {
            if (operacion == null)
                throw new ArgumentNullException(nameof(operacion));

            if (cargos == null)
                throw new ArgumentNullException(nameof(cargos));

            try
            {
                await _logger.LogInformationAsync($"Generando reporte PDF para operación {operacion.IdOperacion}", "QuoteService", "GenerateReportePdfAsync");

                // Generate filename
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var fileName = $"Reporte_Cotizacion_{operacion.IdOperacion}_{timestamp}.pdf";
                var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                var reportsFolder = Path.Combine(documentsPath, "Advance Control", "Reportes");
                
                // Create directory if it doesn't exist
                Directory.CreateDirectory(reportsFolder);
                
                var filePath = Path.Combine(reportsFolder, fileName);

                var reportTitle = $"Reporte Cotización {operacion.IdOperacion}";
                var cargosList = cargos.ToList();

                // Use company name from entity or default
                var companyTitle = !string.IsNullOrWhiteSpace(nombreEmpresa) ? nombreEmpresa.ToUpperInvariant() : "ADVANCE CONTROL";

                // Use current date for the report
                var reportDate = DateTime.Now;

                // Load Prefactura and Orden Compra images
                List<OperacionImageDto> prefacturas = new List<OperacionImageDto>();
                List<OperacionImageDto> ordenesCompra = new List<OperacionImageDto>();
                
                if (operacion.IdOperacion.HasValue)
                {
                    prefacturas = await _operacionImageService.GetPrefacturasAsync(operacion.IdOperacion.Value);
                    ordenesCompra = await _operacionImageService.GetOrdenesCompraAsync(operacion.IdOperacion.Value);
                }

                // Generate PDF
                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.Letter);
                        page.Margin(2, Unit.Centimetre);
                        page.PageColor(Colors.White);
                        page.DefaultTextStyle(x => x.FontSize(11));

                        page.Header()
                            .Height(100)
                            .Background(Colors.Blue.Lighten3)
                            .Padding(15)
                            .Column(column =>
                            {
                                column.Item().AlignCenter().Text(companyTitle)
                                    .FontSize(24)
                                    .Bold()
                                    .FontColor(Colors.Blue.Darken3);
                                
                                column.Item().AlignCenter().Text(reportTitle)
                                    .FontSize(16)
                                    .FontColor(Colors.Blue.Darken2);
                            });

                        page.Content()
                            .PaddingVertical(1, Unit.Centimetre)
                            .Column(column =>
                            {
                                column.Spacing(15);

                                // Add Prefactura images at the beginning if they exist
                                if (prefacturas != null && prefacturas.Count > 0)
                                {
                                    column.Item().Column(prefacturaCol =>
                                    {
                                        prefacturaCol.Item().Text("Prefacturas").Bold().FontSize(16).FontColor(Colors.Blue.Darken2);
                                        prefacturaCol.Item().PaddingTop(10);
                                        
                                        foreach (var prefactura in prefacturas)
                                        {
                                            if (!string.IsNullOrWhiteSpace(prefactura.Url) && File.Exists(prefactura.Url))
                                            {
                                                try
                                                {
                                                    prefacturaCol.Item().PaddingBottom(10).Image(prefactura.Url).FitWidth();
                                                }
                                                catch
                                                {
                                                    // Skip images that can't be loaded
                                                    prefacturaCol.Item().Text($"[Error al cargar: {prefactura.FileName}]").FontSize(10).Italic();
                                                }
                                            }
                                        }
                                    });
                                    
                                    // Add separator after prefacturas
                                    column.Item().PaddingTop(10);
                                }

                                // Add Orden Compra images if they exist
                                if (ordenesCompra != null && ordenesCompra.Count > 0)
                                {
                                    column.Item().Column(ordenCompraCol =>
                                    {
                                        ordenCompraCol.Item().Text("Órdenes de Compra").Bold().FontSize(16).FontColor(Colors.Blue.Darken2);
                                        ordenCompraCol.Item().PaddingTop(10);
                                        
                                        foreach (var ordenCompra in ordenesCompra)
                                        {
                                            if (!string.IsNullOrWhiteSpace(ordenCompra.Url) && File.Exists(ordenCompra.Url))
                                            {
                                                try
                                                {
                                                    ordenCompraCol.Item().PaddingBottom(10).Image(ordenCompra.Url).FitWidth();
                                                }
                                                catch
                                                {
                                                    // Skip images that can't be loaded
                                                    ordenCompraCol.Item().Text($"[Error al cargar: {ordenCompra.FileName}]").FontSize(10).Italic();
                                                }
                                            }
                                        }
                                    });
                                    
                                    // Add separator after ordenes de compra
                                    column.Item().PaddingTop(10);
                                }

                                // Information section (Client and Operation info)
                                column.Item().Row(row =>
                                {
                                    row.RelativeItem().Column(col =>
                                    {
                                        col.Item().Text("Información del Cliente").Bold().FontSize(14);
                                        col.Item().PaddingTop(5);
                                        col.Item().Text($"Cliente: {operacion.RazonSocial ?? "N/A"}");
                                        col.Item().Text($"Equipo: {operacion.Identificador ?? "N/A"}");
                                        if (!string.IsNullOrWhiteSpace(ubicacionNombre))
                                        {
                                            col.Item().Text($"Ubicación: {ubicacionNombre}");
                                        }
                                    });

                                    row.RelativeItem().Column(col =>
                                    {
                                        col.Item().Text("Información de la Operación").Bold().FontSize(14);
                                        col.Item().PaddingTop(5);
                                        col.Item().Text($"Fecha: {reportDate:dd/MM/yyyy}");
                                        col.Item().Text($"Atendido por: {operacion.Atiende ?? "N/A"}");
                                        col.Item().Text($"Tipo: {GetTipoOperacion(operacion.IdTipo)}");
                                    });
                                });

                                // Separator before cargo rows
                                column.Item().PaddingTop(10);

                                // Cargo rows
                                foreach (var cargo in cargosList)
                                {
                                    column.Item().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingBottom(10).Column(cargoCol =>
                                    {
                                        // Row 1: Tipo, Detalle, Nota
                                        cargoCol.Item().Background(Colors.Grey.Lighten4).Padding(8).Row(row =>
                                        {
                                            row.RelativeItem(1).Text($"Tipo: {cargo.TipoCargo ?? "N/A"}").Bold();
                                            row.RelativeItem(2).Text($"Detalle: {cargo.DetalleRelacionado ?? "N/A"}");
                                            row.RelativeItem(3).Text($"Nota: {cargo.Nota ?? "-"}");
                                        });

                                        // Row 2: Photos if they exist
                                        if (cargo.Images != null && cargo.Images.Count > 0)
                                        {
                                            cargoCol.Item().PaddingTop(5).Row(imagesRow =>
                                            {
                                                foreach (var image in cargo.Images)
                                                {
                                                    if (!string.IsNullOrWhiteSpace(image.Url) && File.Exists(image.Url))
                                                    {
                                                        try
                                                        {
                                                            imagesRow.AutoItem().Padding(2).Width(100).Image(image.Url);
                                                        }
                                                        catch
                                                        {
                                                            // Skip images that can't be loaded
                                                            imagesRow.AutoItem().Padding(2).Text($"[Imagen: {image.FileName}]").FontSize(9).Italic();
                                                        }
                                                    }
                                                }
                                            });
                                        }
                                    });
                                }
                            });

                        page.Footer()
                            .AlignCenter()
                            .Text(x =>
                            {
                                x.Span("Página ");
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
        }
    }
}
