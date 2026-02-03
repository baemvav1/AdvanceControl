using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.Logging;
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

        public QuoteService(ILoggingService logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Configure QuestPDF license
            QuestPDF.Settings.License = LicenseType.Community;
        }

        /// <summary>
        /// Genera un PDF de cotización a partir de una operación y sus cargos
        /// </summary>
        public async Task<string> GenerateQuotePdfAsync(OperacionDto operacion, IEnumerable<CargoDto> cargos, string? ubicacionNombre = null)
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
                            .Padding(20)
                            .Column(column =>
                            {
                                column.Item().AlignCenter().Text("ADVANCE CONTROL")
                                    .FontSize(24)
                                    .Bold()
                                    .FontColor(Colors.Blue.Darken3);
                                
                                column.Item().AlignCenter().Text("Cotización de Servicio")
                                    .FontSize(16)
                                    .FontColor(Colors.Blue.Darken2);
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

                                // Additional notes
                                if (!string.IsNullOrWhiteSpace(operacion.Nota))
                                {
                                    column.Item().PaddingTop(20).Column(col =>
                                    {
                                        col.Item().Text("Notas Adicionales").Bold().FontSize(12);
                                        col.Item().PaddingTop(5).Text(operacion.Nota);
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
    }
}
