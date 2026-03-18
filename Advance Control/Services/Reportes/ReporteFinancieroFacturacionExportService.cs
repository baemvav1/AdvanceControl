using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Advance_Control.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Advance_Control.Services.Reportes
{
    public class ReporteFinancieroFacturacionExportService : IReporteFinancieroFacturacionExportService
    {
        private static readonly CultureInfo Cultura = new("es-MX");

        public ReporteFinancieroFacturacionExportService()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public Task<string> GenerarReportePdfAsync(
            IReadOnlyList<ReporteFinancieroFacturacionCabeceraDto> cabeceras,
            IReadOnlyList<ReporteFinancieroFacturacionDetalleDto> detalles,
            string? receptorRfcFiltro,
            string? referenciaFiltro,
            DateTimeOffset? fechaInicioFiltro,
            DateTimeOffset? fechaFinFiltro,
            bool? finiquitoFiltro)
        {
            if (cabeceras == null)
            {
                throw new ArgumentNullException(nameof(cabeceras));
            }

            if (detalles == null)
            {
                throw new ArgumentNullException(nameof(detalles));
            }

            if (detalles.Count == 0)
            {
                throw new InvalidOperationException("No hay registros visibles para generar el reporte.");
            }

            var carpeta = ObtenerCarpetaReportes();
            Directory.CreateDirectory(carpeta);

            var rutaArchivo = Path.Combine(carpeta, ConstruirNombreArchivo(receptorRfcFiltro));
            var resumenFiltros = ConstruirResumenFiltros(receptorRfcFiltro, referenciaFiltro, fechaInicioFiltro, fechaFinFiltro, finiquitoFiltro);
            var totalFacturado = cabeceras.Sum(item => item.TotalFacturado);
            var totalFiniquitado = cabeceras.Sum(item => item.TotalAbonadoMovimientos);
            var cabeceraPath = Path.Combine(ObtenerCarpetaCabeceras(), "EstadoCuenta.png");

            var documento = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.Letter);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().ShowOnce().Column(column =>
                    {
                        if (File.Exists(cabeceraPath))
                        {
                            column.Item().Image(cabeceraPath).FitWidth();
                        }

                        column.Item().Text("Reporte Financiero de Facturacion")
                            .FontSize(18)
                            .SemiBold()
                            .FontColor(Colors.Blue.Darken2);

                        column.Item().Text($"Generado: {DateTime.Now.ToString("dd/MM/yyyy HH:mm", Cultura)}")
                            .FontSize(9)
                            .FontColor(Colors.Grey.Darken1);
                    });

                    page.Content().Column(column =>
                    {
                        column.Spacing(8);

                        column.Item().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(8).Column(filtros =>
                        {
                            filtros.Spacing(2);
                            filtros.Item().Text("Filtros aplicados").SemiBold();
                            foreach (var linea in resumenFiltros)
                            {
                                filtros.Item().Text(linea);
                            }
                        });

                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(6).Column(resumen =>
                            {
                                resumen.Item().Text("Resumen").SemiBold();
                                resumen.Item().Text($"{cabeceras.Sum(item => item.NumeroFacturas)} factura(s) en {cabeceras.Count} cliente(s)");
                                resumen.Item().Text($"Total facturado: {totalFacturado.ToString("C2", Cultura)}");
                                resumen.Item().Text($"Total finiquitado: {totalFiniquitado.ToString("C2", Cultura)}");
                            });
                        });

                        foreach (var cabecera in cabeceras)
                        {
                            var detallesCliente = detalles
                                .Where(item => string.Equals(item.ReceptorRfc, cabecera.ReceptorRfc, StringComparison.OrdinalIgnoreCase))
                                .ToList();

                            column.Item().Border(1).BorderColor(Colors.Blue.Lighten2).Padding(8).Column(cliente =>
                            {
                                cliente.Spacing(6);
                                cliente.Item().Text($"{cabecera.ReceptorNombreTexto} ({cabecera.ReceptorRfcTexto})")
                                    .SemiBold()
                                    .FontSize(12)
                                    .FontColor(Colors.Blue.Darken2);

                                cliente.Item().Text(cabecera.ResumenTexto);
                                cliente.Item().Text($"Facturado: {cabecera.TotalFacturadoTexto}   |   Finiquitado: {cabecera.TotalAbonadoMovimientosTexto}");

                                foreach (var detalle in detallesCliente)
                                {
                                    cliente.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(6).Column(detalleItem =>
                                    {
                                        detalleItem.Spacing(4);

                                        detalleItem.Item().Table(tabla =>
                                        {
                                            tabla.ColumnsDefinition(columnas =>
                                            {
                                                columnas.RelativeColumn(1.45f);
                                                columnas.RelativeColumn(1.2f);
                                                columnas.RelativeColumn(0.8f);
                                            });

                                            tabla.Cell().Element(EstiloCampoDetalle).Text(text =>
                                            {
                                                text.Span("Folio").SemiBold();
                                                text.Span(": ");
                                                text.Span(detalle.FolioTexto);
                                            });
                                            tabla.Cell().Element(EstiloCampoDetalle).Text(text =>
                                            {
                                                text.Span("Fecha").SemiBold();
                                                text.Span(": ");
                                                text.Span(detalle.FechaTimbradoTexto);
                                            });
                                            tabla.Cell().Element(EstiloCampoDetalle).Text(text =>
                                            {
                                                text.Span("Estado").SemiBold();
                                                text.Span(": ");
                                                text.Span(detalle.EstadoTexto);
                                            });
                                        });

                                        detalleItem.Item().Table(tabla =>
                                        {
                                            tabla.ColumnsDefinition(columnas =>
                                            {
                                                columnas.RelativeColumn(0.9f);
                                                columnas.RelativeColumn(0.9f);
                                                columnas.RelativeColumn(1.8f);
                                            });

                                            tabla.Cell().Element(EstiloCampoDetalle).Text(text =>
                                            {
                                                text.Span("Total").SemiBold();
                                                text.Span(": ");
                                                text.Span(detalle.TotalTexto);
                                            });
                                            tabla.Cell().Element(EstiloCampoDetalle).Text(text =>
                                            {
                                                text.Span("Abono").SemiBold();
                                                text.Span(": ");
                                                text.Span(detalle.AbonoTexto);
                                            });
                                            tabla.Cell().Element(EstiloCampoDetalle).Text(text =>
                                            {
                                                text.Span("Referencia").SemiBold();
                                                text.Span(": ");
                                                text.Span(detalle.ReferenciaTexto);
                                            });
                                        });
                                    });
                                }
                            });
                        }
                    });

                    page.Footer().AlignRight().Text(text =>
                    {
                        text.Span("Pagina ");
                        text.CurrentPageNumber();
                        text.Span(" de ");
                        text.TotalPages();
                    });
                });
            });

            documento.GeneratePdf(rutaArchivo);
            return Task.FromResult(rutaArchivo);
        }

        private static string ObtenerCarpetaReportes()
        {
            var documentos = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            return Path.Combine(documentos, "Advance Control", "ReportesFinancieros");
        }

        private static string ObtenerCarpetaCabeceras()
        {
            var documentos = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            return Path.Combine(documentos, "Advance Control", "Cabeceras");
        }

        private static string ConstruirNombreArchivo(string? receptorRfcFiltro)
        {
            var rfc = string.IsNullOrWhiteSpace(receptorRfcFiltro)
                ? "General"
                : LimpiarNombreArchivo(receptorRfcFiltro.Trim().ToUpperInvariant());

            return $"ReporteFinancieroFacturacion_{rfc}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
        }

        private static string LimpiarNombreArchivo(string valor)
        {
            return string.Concat(valor.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
        }

        private static List<string> ConstruirResumenFiltros(
            string? receptorRfcFiltro,
            string? referenciaFiltro,
            DateTimeOffset? fechaInicioFiltro,
            DateTimeOffset? fechaFinFiltro,
            bool? finiquitoFiltro)
        {
            return new List<string>
            {
                $"RFC receptor: {(string.IsNullOrWhiteSpace(receptorRfcFiltro) ? "Todos" : receptorRfcFiltro.Trim().ToUpperInvariant())}",
                $"Referencia: {(string.IsNullOrWhiteSpace(referenciaFiltro) ? "Todas" : referenciaFiltro.Trim())}",
                $"Fecha inicio: {(fechaInicioFiltro.HasValue ? fechaInicioFiltro.Value.ToString("dd/MM/yyyy", Cultura) : "Sin limite")}",
                $"Fecha fin: {(fechaFinFiltro.HasValue ? fechaFinFiltro.Value.ToString("dd/MM/yyyy", Cultura) : "Sin limite")}",
                $"Estado: {ObtenerDescripcionFiniquito(finiquitoFiltro)}"
            };
        }

        private static string ObtenerDescripcionFiniquito(bool? finiquitoFiltro)
        {
            if (finiquitoFiltro == true)
            {
                return "Solo finiquitadas";
            }

            if (finiquitoFiltro == false)
            {
                return "No finiquitadas";
            }

            return "Todas";
        }

        private static IContainer EstiloCampoDetalle(IContainer container)
        {
            return container
                .PaddingHorizontal(2)
                .DefaultTextStyle(x => x.FontSize(9));
        }

    }
}
