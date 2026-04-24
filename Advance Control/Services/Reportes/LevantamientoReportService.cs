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

namespace Advance_Control.Services.Reportes
{
    public class LevantamientoReportService : ILevantamientoReportService
    {
        private readonly ILoggingService _logger;

        public LevantamientoReportService(ILoggingService logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public async Task<string> GenerarReportePdfAsync(
            int idLevantamiento,
            string? equipoIdentificador,
            string? equipoMarca,
            string? introduccion,
            string? conclusion,
            IReadOnlyList<LevantamientoTreeItemModel> nodos)
        {
            var fecha = DateTime.Now.ToString("yyyyMMdd");
            var safeEquipo = SanitizeFileName(equipoIdentificador ?? "SinEquipo");
            var fileName = $"Reporte_Levantamiento_{idLevantamiento}_{safeEquipo}_{fecha}.pdf";

            var folder = GetLevantamientoFolder(idLevantamiento);
            Directory.CreateDirectory(folder);
            var filePath = Path.Combine(folder, fileName);

            var cabeceraPath = GetCabeceraPath();
            var firmaPath = GetFirmaDireccionPath();

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.Letter);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    // Header
                    page.Header().ShowOnce().Column(column =>
                    {
                        if (File.Exists(cabeceraPath))
                            column.Item().Image(cabeceraPath).FitWidth();

                        column.Item().AlignRight()
                            .Text($"Levantamiento #{idLevantamiento}")
                            .FontSize(16).Bold().FontColor(Colors.Blue.Darken2);
                    });

                    // Content
                    page.Content().PaddingVertical(1, Unit.Centimetre).Column(column =>
                    {
                        // Info general
                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text(text =>
                                {
                                    text.Span("Equipo: ").Bold();
                                    text.Span(equipoIdentificador ?? "N/A");
                                });
                                col.Item().Text(text =>
                                {
                                    text.Span("Marca: ").Bold();
                                    text.Span(equipoMarca ?? "N/A");
                                });
                            });
                            row.RelativeItem().AlignRight().Column(col =>
                            {
                                col.Item().Text(text =>
                                {
                                    text.Span("Fecha: ").Bold();
                                    text.Span(DateTime.Now.ToString("dd/MM/yyyy"));
                                });
                                col.Item().Text(text =>
                                {
                                    text.Span("Tipo: ").Bold();
                                    text.Span("Elevador de Traccion");
                                });
                            });
                        });

                        column.Item().PaddingTop(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        // Introduccion
                        if (!string.IsNullOrWhiteSpace(introduccion))
                        {
                            column.Item().PaddingTop(10).Text("Introduccion")
                                .Bold().FontSize(13).FontColor(Colors.Blue.Darken3);
                            column.Item().PaddingTop(4).Text(introduccion);
                            column.Item().PaddingTop(6).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                        }

                        // Fallas detectadas
                        column.Item().PaddingTop(10)
                            .Background(Colors.Blue.Lighten4).Padding(6)
                            .Text("Fallas Detectadas")
                            .Bold().FontSize(13).FontColor(Colors.Blue.Darken3);

                        // Nodos del arbol
                        foreach (var nodo in nodos)
                        {
                            AddNodoAlReporte(column, nodo, nivel: 0);
                        }

                        column.Item().PaddingTop(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        // Conclusion
                        if (!string.IsNullOrWhiteSpace(conclusion))
                        {
                            column.Item().PaddingTop(10).Text("Conclusion")
                                .Bold().FontSize(13).FontColor(Colors.Blue.Darken3);
                            column.Item().PaddingTop(4).Text(conclusion);
                            column.Item().PaddingTop(6).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                        }

                        // Firma
                        if (File.Exists(firmaPath))
                        {
                            column.Item().PaddingTop(25).AlignCenter()
                                .Height(100).Image(firmaPath).FitArea();
                        }
                    });

                    // Footer
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span($"Levantamiento {idLevantamiento}  ·  Hoja ");
                        x.CurrentPageNumber();
                        x.Span(" de ");
                        x.TotalPages();
                    });
                });
            });

            document.GeneratePdf(filePath);

            await _logger.LogInformationAsync(
                $"Reporte PDF generado: {filePath}",
                nameof(LevantamientoReportService), nameof(GenerarReportePdfAsync));

            return filePath;
        }

        private static void AddNodoAlReporte(ColumnDescriptor column, LevantamientoTreeItemModel nodo, int nivel)
        {
            var indent = nivel * 15;

            // Seccion / nodo padre
            column.Item().PaddingTop(6).PaddingLeft(indent)
                .Background(nivel == 0 ? Colors.Grey.Lighten4 : Colors.White)
                .BorderTop(1).BorderColor(Colors.Grey.Lighten2)
                .Padding(6).Column(nodoCol =>
                {
                    // Etiqueta del nodo
                    nodoCol.Item().Text(nodo.Etiqueta)
                        .Bold().FontSize(nivel == 0 ? 12 : 11);

                    // Descripcion de falla si tiene
                    if (nodo.TieneFalla && !string.IsNullOrWhiteSpace(nodo.DescripcionFalla))
                    {
                        nodoCol.Item().PaddingTop(2)
                            .Text(nodo.DescripcionFalla)
                            .FontSize(10).FontColor(Colors.Grey.Darken1);
                    }

                    // Imagenes del nodo (3 por fila, tamaño reducido)
                    var imagePaths = nodo.Imagenes
                        .Where(img => File.Exists(img.FilePath))
                        .Select(img => img.FilePath)
                        .ToList();

                    if (imagePaths.Count > 0)
                    {
                        for (int i = 0; i < imagePaths.Count; i += 3)
                        {
                            int idx = i;
                            nodoCol.Item().PaddingTop(4).Row(row =>
                            {
                                row.RelativeItem().Padding(2).MaxHeight(120).Image(imagePaths[idx]).FitArea();
                                if (idx + 1 < imagePaths.Count)
                                    row.RelativeItem().Padding(2).MaxHeight(120).Image(imagePaths[idx + 1]).FitArea();
                                else
                                    row.RelativeItem();
                                if (idx + 2 < imagePaths.Count)
                                    row.RelativeItem().Padding(2).MaxHeight(120).Image(imagePaths[idx + 2]).FitArea();
                                else
                                    row.RelativeItem();
                            });
                        }
                    }
                });

            // Hijos recursivos
            foreach (var hijo in nodo.Hijos)
            {
                AddNodoAlReporte(column, hijo, nivel + 1);
            }
        }

        private static string GetLevantamientoFolder(int idLevantamiento)
        {
            var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            return Path.Combine(docs, "Advance Control", "Levantamientos", $"Levantamiento{idLevantamiento}");
        }

        private static string GetCabeceraPath()
        {
            return Path.Combine(AppContext.BaseDirectory, "Assets", "Cabeceras", "Reporte.png");
        }

        private static string GetFirmaDireccionPath()
        {
            var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            return Path.Combine(docs, "Advance Control", "Firmas", "FirmaDireccion.png");
        }

        private static string SanitizeFileName(string value)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            return new string(value.Select(ch => invalidChars.Contains(ch) ? '_' : ch).ToArray());
        }
    }
}
