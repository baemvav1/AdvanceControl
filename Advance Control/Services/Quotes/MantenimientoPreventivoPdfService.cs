using Advance_Control.Services.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Advance_Control.Services.Quotes
{
    /// <summary>
    /// Genera el PDF de Mantenimiento Preventivo a partir del estado capturado en
    /// <see cref="Advance_Control.Views.Formularios.MantenimientoPreventivoWindow"/>.
    /// </summary>
    public class MantenimientoPreventivoPdfService : IMantenimientoPreventivoPdfService
    {
        private static readonly string[] ColumnasChecklist =
            { "No aplica", "Verificación", "Ajuste", "Limpieza", "Lubricación", "Recorrido" };

        private readonly ILoggingService _logger;
        private readonly IFirmaService _firmaService;

        public MantenimientoPreventivoPdfService(ILoggingService logger, IFirmaService firmaService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _firmaService = firmaService ?? throw new ArgumentNullException(nameof(firmaService));

            QuestPDF.Settings.License = LicenseType.Community;
        }

        private static string GetOperacionFolder(int idOperacion)
        {
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var folder = Path.Combine(documentsPath, "Advance Control", $"Operacion_{idOperacion}");
            Directory.CreateDirectory(folder);
            return folder;
        }

        public async Task<string> GeneratePdfAsync(MantenimientoPreventivoPdfData datos)
        {
            if (datos == null) throw new ArgumentNullException(nameof(datos));

            try
            {
                await _logger.LogInformationAsync($"Generando PDF de mantenimiento preventivo para operación {datos.IdOperacion}", "MantenimientoPreventivoPdfService", "GeneratePdfAsync");

                var fecha = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var fileName = $"MttoPreventivo_{datos.IdOperacion}_{fecha}.pdf";
                var folder = GetOperacionFolder(datos.IdOperacion);
                var filePath = Path.Combine(folder, fileName);

                var firmaTecnicoPath = datos.IdAtiende.HasValue
                    ? _firmaService.GetFirmaOperadorPath(datos.IdAtiende.Value)
                    : null;
                var hasFirmaTecnico = firmaTecnicoPath != null && File.Exists(firmaTecnicoPath);

                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.Letter);
                        page.Margin(2, Unit.Centimetre);
                        page.PageColor(Colors.White);
                        page.DefaultTextStyle(x => x.FontSize(10));

                        page.Header().Column(header =>
                        {
                            header.Item().Text("Mantenimiento Preventivo").FontSize(18).SemiBold();
                            header.Item().Text($"Operación {datos.IdOperacion}").FontSize(10).FontColor(Colors.Grey.Darken1);
                            if (!string.IsNullOrWhiteSpace(datos.DirigidoA))
                                header.Item().PaddingTop(2).Text(datos.DirigidoA).FontSize(10).SemiBold();
                            header.Item().PaddingTop(6).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                        });

                        page.Content().PaddingTop(10).Column(column =>
                        {
                            column.Spacing(14);

                            column.Item().Element(c => AddDatosGenerales(c, datos));

                            foreach (var seccion in datos.Secciones)
                                column.Item().Element(c => AddSeccionChecklist(c, seccion));

                            column.Item().Element(c => AddResumen(c, datos));

                            column.Item().Element(c => AddConfirmaciones(c, datos, firmaTecnicoPath, hasFirmaTecnico));
                        });

                        page.Footer().AlignCenter().Text(x =>
                        {
                            x.Span($"Mantenimiento Preventivo {datos.IdOperacion}, Hoja ");
                            x.CurrentPageNumber();
                            x.Span("/");
                            x.TotalPages();
                        });
                    });
                });

                document.GeneratePdf(filePath);

                await _logger.LogInformationAsync($"PDF de mantenimiento preventivo generado exitosamente: {filePath}", "MantenimientoPreventivoPdfService", "GeneratePdfAsync");
                return filePath;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error al generar PDF de mantenimiento preventivo", ex, "MantenimientoPreventivoPdfService", "GeneratePdfAsync");
                throw;
            }
        }

        private static void AddDatosGenerales(IContainer container, MantenimientoPreventivoPdfData d)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                void Celda(string label, string? valor)
                {
                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Column(col =>
                    {
                        col.Item().Text(label).FontSize(8).FontColor(Colors.Grey.Darken1);
                        col.Item().Text(string.IsNullOrWhiteSpace(valor) ? "—" : valor).FontSize(10);
                    });
                }

                Celda("Proyecto", d.Proyecto);
                Celda("Dirección", d.Direccion);
                Celda("Ruta", d.Ruta);
                Celda("No. Equipo", d.NoEquipo);
                Celda("Refe. Equipo", d.RefeEquipo);
                Celda("Fecha", d.Fecha);
                Celda("Hora de entrada", d.HoraEntrada);
                Celda("Hora de salida", d.HoraSalida);
            });
        }

        private static void AddSeccionChecklist(IContainer container, MantenimientoPreventivoSeccion seccion)
        {
            container.Column(col =>
            {
                col.Item().Text(seccion.Nombre).FontSize(12).SemiBold();
                col.Item().PaddingTop(4).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(3);
                        foreach (var _ in ColumnasChecklist)
                            columns.RelativeColumn();
                    });

                    table.Header(h =>
                    {
                        h.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Ítem").FontSize(8).SemiBold();
                        foreach (var columna in ColumnasChecklist)
                            h.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignCenter().Text(columna).FontSize(7).SemiBold();
                    });

                    foreach (var item in seccion.Items)
                    {
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(item.Texto).FontSize(9);

                        var marcados = new[] { item.NoAplica, item.Verificacion, item.Ajuste, item.Limpieza, item.Lubricacion, item.Recorrido };
                        foreach (var marcado in marcados)
                        {
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignCenter()
                                .Text(marcado ? "X" : "").FontSize(9).SemiBold();
                        }
                    }
                });
            });
        }

        private static void AddResumen(IContainer container, MantenimientoPreventivoPdfData d)
        {
            container.Column(col =>
            {
                col.Item().Text("Resumen de asistencia y observaciones").FontSize(12).SemiBold();
                col.Item().PaddingTop(4).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(6).MinHeight(50)
                    .Text(string.IsNullOrWhiteSpace(d.Observaciones) ? "—" : d.Observaciones).FontSize(9);
                col.Item().PaddingTop(4)
                    .Text($"Situación final del equipo: {(string.IsNullOrWhiteSpace(d.SituacionFinal) ? "—" : d.SituacionFinal)}")
                    .FontSize(10).SemiBold();
            });
        }

        private static void AddConfirmaciones(IContainer container, MantenimientoPreventivoPdfData d, string? firmaTecnicoPath, bool hasFirmaTecnico)
        {
            container.PaddingTop(10).Column(col =>
            {
                col.Item().Text("Confirmaciones").FontSize(12).SemiBold();
                col.Item().PaddingTop(6).Row(row =>
                {
                    row.RelativeItem().Column(tecnicoCol =>
                    {
                        tecnicoCol.Item().Text(string.IsNullOrWhiteSpace(d.TecnicoNombre) ? "Técnico" : d.TecnicoNombre).FontSize(9);
                        var box = tecnicoCol.Item().PaddingTop(4).Height(80).Width(220).Border(1).BorderColor(Colors.Grey.Lighten2);
                        if (hasFirmaTecnico)
                            box.Image(firmaTecnicoPath!).FitArea();
                        else
                            box.AlignCenter().AlignMiddle().Text("Sin firma registrada").FontSize(8).FontColor(Colors.Grey.Darken1);
                    });

                    row.RelativeItem().Column(clienteCol =>
                    {
                        clienteCol.Item().Text(string.IsNullOrWhiteSpace(d.ClienteNombre) ? "Cliente" : d.ClienteNombre).FontSize(9);
                        if (!string.IsNullOrWhiteSpace(d.ClienteCorreo))
                            clienteCol.Item().Text(d.ClienteCorreo).FontSize(8).FontColor(Colors.Grey.Darken1);
                        clienteCol.Item().PaddingTop(4).Height(80).Width(220).Border(1).BorderColor(Colors.Grey.Lighten2)
                            .AlignCenter().AlignMiddle().Text("Firma del cliente").FontSize(8).FontColor(Colors.Grey.Darken1);
                    });
                });
            });
        }
    }
}
