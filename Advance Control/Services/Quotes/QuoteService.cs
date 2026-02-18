using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.Logging;
using Advance_Control.Services.LocalStorage;
using Advance_Control.Services.Notificacion;
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
        private readonly INotificacionService _notificacionService;

        public QuoteService(ILoggingService logger, IOperacionImageService operacionImageService, INotificacionService notificacionService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _operacionImageService = operacionImageService ?? throw new ArgumentNullException(nameof(operacionImageService));
            _notificacionService = notificacionService ?? throw new ArgumentNullException(nameof(notificacionService));
            
            // Configure QuestPDF license
            QuestPDF.Settings.License = LicenseType.Community;
        }

        /// <summary>
        /// Obtiene la ruta de la carpeta de firmas
        /// </summary>
        private string GetFirmasFolder()
        {
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            return Path.Combine(documentsPath, "Advance Control", "Firmas");
        }

        /// <summary>
        /// Obtiene la ruta de la carpeta de cabeceras
        /// </summary>
        private string GetCabecerasFolder()
        {
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            return Path.Combine(documentsPath, "Advance Control", "Cabeceras");
        }

        /// <summary>
        /// Busca la firma del operador por su idAtiende en la carpeta de Firmas.
        /// Los archivos tienen formato {id}_{nombre}.png
        /// </summary>
        private string? FindFirmaOperador(string firmasFolder, int idAtiende)
        {
            if (!Directory.Exists(firmasFolder))
                return null;

            foreach (var file in Directory.EnumerateFiles(firmasFolder, "*.png"))
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                var parts = fileName.Split('_');
                if (parts.Length >= 2 && int.TryParse(parts[0], out int fileId) && fileId == idAtiende)
                {
                    return file;
                }
            }
            return null;
        }

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
        public async Task<string> GenerateQuotePdfAsync(OperacionDto operacion, IEnumerable<CargoDto> cargos, string? ubicacionNombre = null, string? nombreEmpresa = null, string? apoderadoNombre = null)
        {
            if (operacion == null)
                throw new ArgumentNullException(nameof(operacion));

            if (cargos == null)
                throw new ArgumentNullException(nameof(cargos));

            try
            {
                await _logger.LogInformationAsync($"Generando cotización PDF para operación {operacion.IdOperacion}", "QuoteService", "GenerateQuotePdfAsync");

                // Generate filename: {idOperacion}{fechaActual}Cotizacion.pdf
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var fileName = $"{operacion.IdOperacion}{timestamp}Cotizacion.pdf";
                var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                var operacionFolder = Path.Combine(documentsPath, "Advance Control", $"Operacion_{operacion.IdOperacion}");
                
                // Create directory if it doesn't exist
                Directory.CreateDirectory(operacionFolder);
                
                var filePath = Path.Combine(operacionFolder, fileName);

                var quotationTitle = $"Cotización {operacion.IdOperacion}";
                var cotizacionImagePath = Path.Combine(GetCabecerasFolder(), "Cotizacion.png");

                // Generate PDF
                var document = Document.Create(container =>
                {
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
                                            text.Span(GetTipoOperacion(operacion.IdTipo));
                                        });
                                    });
                                });

                                // Charges table
                                column.Item().Text("Desglose de Cargos").Bold().FontSize(14);
                                
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
                                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5)
                                            .Text(cargo.Cantidad.ToString() ?? "N/A");
                                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5)
                                            .Text(cargo.TipoCargo ?? "N/A");
                                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5)
                                            .Text(cargo.DetalleRelacionado ?? "N/A");
                                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5)
                                            .Text($"${cargo.Unitario ?? 0:N2}");
                                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5)
                                            .AlignRight().PaddingLeft(20).Text($"${cargo.Monto ?? 0:N2}");
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
                                });

                                // Signature images section
                                AddFirmasSection(column.Item(), operacion.IdAtiende);
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

                // Generate filename: {idOperacion}{fechaActual}Reporte.pdf
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var fileName = $"{operacion.IdOperacion}{timestamp}Reporte.pdf";
                var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                var operacionFolder = Path.Combine(documentsPath, "Advance Control", $"Operacion_{operacion.IdOperacion}");
                
                // Create directory if it doesn't exist
                Directory.CreateDirectory(operacionFolder);
                
                var filePath = Path.Combine(operacionFolder, fileName);

                var reportTitle = $"Reporte {operacion.IdOperacion}";
                var cargosList = cargos.ToList();
                var reporteImagePath = Path.Combine(GetCabecerasFolder(), "Reporte.png");

                // Use current date for the report
                var reportDate = DateTime.Now;

                // Load Prefactura and Hoja Servicio images
                List<OperacionImageDto> prefacturas = new List<OperacionImageDto>();
                List<OperacionImageDto> hojasServicio = new List<OperacionImageDto>();
                
                if (operacion.IdOperacion.HasValue)
                {
                    prefacturas = await _operacionImageService.GetPrefacturasAsync(operacion.IdOperacion.Value);
                    hojasServicio = await _operacionImageService.GetHojasServicioAsync(operacion.IdOperacion.Value);
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

                        // Header - only show on first page
                        page.Header()
                            .ShowOnce()
                            .Column(column =>
                            {
                                if (File.Exists(reporteImagePath))
                                {
                                    column.Item().Image(reporteImagePath).FitWidth();
                                }

                                column.Item().AlignRight().Text(reportTitle)
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

                                // Add Hoja Servicio images if they exist
                                if (hojasServicio != null && hojasServicio.Count > 0)
                                {
                                    column.Item().Column(hojaServicioCol =>
                                    {
                                        hojaServicioCol.Item().Text("Hojas de Servicio").Bold().FontSize(16).FontColor(Colors.Blue.Darken2);
                                        hojaServicioCol.Item().PaddingTop(10);
                                        
                                        foreach (var hojaServicio in hojasServicio)
                                        {
                                            if (!string.IsNullOrWhiteSpace(hojaServicio.Url) && File.Exists(hojaServicio.Url))
                                            {
                                                try
                                                {
                                                    hojaServicioCol.Item().PaddingBottom(10).Image(hojaServicio.Url).FitWidth();
                                                }
                                                catch
                                                {
                                                    // Skip images that can't be loaded
                                                    hojaServicioCol.Item().Text($"[Error al cargar: {hojaServicio.FileName}]").FontSize(10).Italic();
                                                }
                                            }
                                        }
                                    });
                                    
                                    // Add separator after hojas de servicio
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

                                // Signature images section
                                AddFirmasSection(column.Item(), operacion.IdAtiende);
                            });

                        page.Footer()
                            .AlignCenter()
                            .Text(x =>
                            {
                                x.Span($"Reporte {operacion.IdOperacion}, Hoja ");
                                x.CurrentPageNumber();
                                x.Span("/");
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
