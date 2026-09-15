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
    /// <summary>
    /// Genera el "Reporte General de Operaciones": una tabla, una fila por
    /// operación del conjunto filtrado, con quién la atiende y qué checks de
    /// checks_operacion tiene marcados — para exigir resultados al operador.
    /// </summary>
    public class OperacionesReporteExportService : IOperacionesReporteExportService
    {
        private static readonly CultureInfo Cultura = new("es-MX");

        public OperacionesReporteExportService()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public Task<string> GenerarReporteOperacionesPdfAsync(
            IReadOnlyList<OperacionDto> operaciones,
            OperacionesReporteFiltrosDto filtros)
        {
            if (operaciones == null) throw new ArgumentNullException(nameof(operaciones));
            if (filtros == null) throw new ArgumentNullException(nameof(filtros));
            if (operaciones.Count == 0) throw new InvalidOperationException("No hay operaciones para el conjunto de filtros actual.");

            var carpeta = ObtenerCarpetaReportes();
            Directory.CreateDirectory(carpeta);

            var rutaArchivo = Path.Combine(carpeta, ConstruirNombreArchivo(filtros));
            var resumenFiltros = ConstruirResumenFiltros(filtros);

            // Orden por operador para que el reporte se pueda leer "por bloques"
            // al exigir resultados a cada uno.
            var filas = operaciones
                .OrderBy(o => string.IsNullOrWhiteSpace(o.Atiende) ? "zzz" : o.Atiende, StringComparer.OrdinalIgnoreCase)
                .ThenBy(o => o.IdOperacion)
                .ToList();

            var totalAbiertas = filas.Count(o => !o.IsFinalized);
            var totalTFinalizado = filas.Count(o => o.TFinalizado);
            var totalFacturadas = filas.Count(o => o.EstaFacturada);
            var cabeceraPath = Path.Combine(ObtenerCarpetaCabeceras(), "Reporte.png");

            var documento = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.Letter.Landscape());
                    page.Margin(1.2f, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    page.Header().ShowOnce().Column(column =>
                    {
                        if (File.Exists(cabeceraPath))
                        {
                            column.Item().Image(cabeceraPath).FitWidth();
                        }

                        column.Item().Text("Reporte General de Operaciones")
                            .FontSize(18)
                            .SemiBold()
                            .FontColor(Colors.Blue.Darken2);

                        column.Item().Text($"Generado: {DateTime.Now.ToString("dd/MM/yyyy HH:mm", Cultura)}" +
                                           (string.IsNullOrWhiteSpace(filtros.GeneradoPor) ? "" : $"  |  Por: {filtros.GeneradoPor}"))
                            .FontSize(9)
                            .FontColor(Colors.Grey.Darken1);

                        column.Item().PaddingTop(4).Border(1).BorderColor(Colors.Grey.Lighten1).Padding(6).Column(f =>
                        {
                            f.Spacing(2);
                            f.Item().Text("Filtros aplicados").SemiBold().FontSize(8);
                            foreach (var linea in resumenFiltros)
                                f.Item().Text(linea).FontSize(8);
                        });

                        column.Item().PaddingTop(4).PaddingBottom(8).Row(row =>
                        {
                            row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(6).Column(resumen =>
                            {
                                resumen.Item().Text("Resumen").SemiBold().FontSize(8);
                                resumen.Item().Text($"{filas.Count} operación(es)  |  Abiertas: {totalAbiertas}  |  T-Finalizado: {totalTFinalizado}  |  Facturadas: {totalFacturadas}")
                                    .FontSize(8);
                                resumen.Item().Text("Checks: CG=Cotización Generada  CE=Cotización Enviada  RG=Reporte Generado  RE=Reporte Enviado  PF=Prefactura  HS=Hoja de Servicio  OC=Orden de Compra  FA=Factura")
                                    .FontSize(7)
                                    .FontColor(Colors.Grey.Darken1);
                            });
                        });
                    });

                    page.Content().Table(tabla =>
                    {
                        tabla.ColumnsDefinition(columnas =>
                        {
                            columnas.ConstantColumn(28);   // ID
                            columnas.RelativeColumn(1.6f); // Cliente/Equipo
                            columnas.RelativeColumn(1.1f); // Atiende
                            columnas.ConstantColumn(52);   // Tipo
                            columnas.ConstantColumn(46);   // F. Inicio
                            columnas.ConstantColumn(46);   // F. Fin
                            columnas.ConstantColumn(78);   // Estado
                            columnas.ConstantColumn(24);   // CG
                            columnas.ConstantColumn(24);   // CE
                            columnas.ConstantColumn(24);   // RG
                            columnas.ConstantColumn(24);   // RE
                            columnas.ConstantColumn(24);   // PF
                            columnas.ConstantColumn(24);   // HS
                            columnas.ConstantColumn(24);   // OC
                            columnas.ConstantColumn(24);   // FA
                        });

                        tabla.Header(header =>
                        {
                            header.Cell().Element(EstiloCeldaEncabezado).Text("ID");
                            header.Cell().Element(EstiloCeldaEncabezado).Text("Cliente / Equipo");
                            header.Cell().Element(EstiloCeldaEncabezado).Text("Atiende");
                            header.Cell().Element(EstiloCeldaEncabezado).Text("Tipo");
                            header.Cell().Element(EstiloCeldaEncabezado).Text("F. Inicio");
                            header.Cell().Element(EstiloCeldaEncabezado).Text("F. Fin");
                            header.Cell().Element(EstiloCeldaEncabezado).Text("Estado");
                            header.Cell().Element(EstiloCeldaEncabezado).Text(t => { t.AlignCenter(); t.Span("CG"); });
                            header.Cell().Element(EstiloCeldaEncabezado).Text(t => { t.AlignCenter(); t.Span("CE"); });
                            header.Cell().Element(EstiloCeldaEncabezado).Text(t => { t.AlignCenter(); t.Span("RG"); });
                            header.Cell().Element(EstiloCeldaEncabezado).Text(t => { t.AlignCenter(); t.Span("RE"); });
                            header.Cell().Element(EstiloCeldaEncabezado).Text(t => { t.AlignCenter(); t.Span("PF"); });
                            header.Cell().Element(EstiloCeldaEncabezado).Text(t => { t.AlignCenter(); t.Span("HS"); });
                            header.Cell().Element(EstiloCeldaEncabezado).Text(t => { t.AlignCenter(); t.Span("OC"); });
                            header.Cell().Element(EstiloCeldaEncabezado).Text(t => { t.AlignCenter(); t.Span("FA"); });
                        });

                        foreach (var op in filas)
                        {
                            tabla.Cell().Element(c => EstiloCeldaDatos(c)).Text(op.IdOperacion?.ToString() ?? "—").FontSize(8);
                            tabla.Cell().Element(c => EstiloCeldaDatos(c)).Text(op.RazonSocial ?? op.Identificador ?? "—").FontSize(8);
                            tabla.Cell().Element(c => EstiloCeldaDatos(c)).Text(string.IsNullOrWhiteSpace(op.Atiende) ? "Sin asignar" : op.Atiende).FontSize(8);
                            tabla.Cell().Element(c => EstiloCeldaDatos(c)).Text(op.TipoMantenimiento ?? "—").FontSize(8);
                            tabla.Cell().Element(c => EstiloCeldaDatos(c)).Text(op.FechaInicioCorta).FontSize(8);
                            tabla.Cell().Element(c => EstiloCeldaDatos(c)).Text(op.IsFinalized ? op.FechaFinCorta : "—").FontSize(8);
                            tabla.Cell().Element(c => EstiloCeldaDatos(c)).Text(TextoEstado(op)).FontSize(8);

                            foreach (var marcado in new[]
                                     {
                                         op.CkCotizacionGenerada, op.CkCotizacionEnviada,
                                         op.CkReporteGenerado, op.CkReporteEnviado,
                                         op.CkPrefacturaCargada, op.CkHojaServicioCargada,
                                         op.CkOrdenCompraCargada, op.CkFacturaCargada
                                     })
                            {
                                tabla.Cell().Element(c => EstiloCeldaDatos(c)).Text(text =>
                                {
                                    text.AlignCenter();
                                    if (marcado)
                                        text.Span("✓").FontColor(Colors.Green.Darken2).SemiBold().FontSize(9);
                                    else
                                        text.Span("✗").FontColor(Colors.Red.Medium).FontSize(9);
                                });
                            }
                        }
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
            return Task.FromResult(rutaArchivo);
        }

        private static string TextoEstado(OperacionDto op)
        {
            if (op.EstaFacturada)
                return op.EstaPagada ? "Facturada (pagada)" : "Facturada (pend. pago)";
            if (op.TFinalizado)
                return "T-Finalizado";
            return op.IsFinalized ? "Finalizada" : "Abierta";
        }

        private static string ObtenerCarpetaReportes()
        {
            var documentos = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            return Path.Combine(documentos, "Advance Control", "ReportesOperaciones");
        }

        private static string ObtenerCarpetaCabeceras()
        {
            return Path.Combine(AppContext.BaseDirectory, "Assets", "Cabeceras");
        }

        private static string ConstruirNombreArchivo(OperacionesReporteFiltrosDto filtros)
        {
            var etiqueta = !string.IsNullOrWhiteSpace(filtros.ClienteFiltro)
                ? filtros.ClienteFiltro
                : !string.IsNullOrWhiteSpace(filtros.EquipoFiltro)
                    ? filtros.EquipoFiltro
                    : "General";
            return $"ReporteOperaciones_{LimpiarNombreArchivo(etiqueta!)}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
        }

        private static string LimpiarNombreArchivo(string valor)
        {
            return string.Concat(valor.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
        }

        private static List<string> ConstruirResumenFiltros(OperacionesReporteFiltrosDto filtros)
        {
            return new List<string>
            {
                $"ID Operación: {(string.IsNullOrWhiteSpace(filtros.IdOperacionFiltro) ? "Todos" : filtros.IdOperacionFiltro)}",
                $"Tipo: {filtros.TipoFiltro}",
                $"Cliente: {(string.IsNullOrWhiteSpace(filtros.ClienteFiltro) ? "Todos" : filtros.ClienteFiltro)}",
                $"Equipo: {(string.IsNullOrWhiteSpace(filtros.EquipoFiltro) ? "Todos" : filtros.EquipoFiltro)}",
                $"Área: {(string.IsNullOrWhiteSpace(filtros.AreaFiltro) ? "Todas" : filtros.AreaFiltro)}",
                $"Nota: {(string.IsNullOrWhiteSpace(filtros.NotaFiltro) ? "Sin filtro" : filtros.NotaFiltro)}",
                $"Fecha inicio: {(filtros.FechaInicialFiltro.HasValue ? filtros.FechaInicialFiltro.Value.ToString("dd/MM/yyyy", Cultura) : "Sin límite")}",
                $"Fecha fin: {(filtros.FechaFinalFiltro.HasValue ? filtros.FechaFinalFiltro.Value.ToString("dd/MM/yyyy", Cultura) : "Sin límite")}",
                $"Mostrar: Abiertas={(filtros.MostrarAbiertas ? "Sí" : "No")}, T-Finalizado={(filtros.MostrarTFinalizadas ? "Sí" : "No")}, Facturadas={(filtros.MostrarFacturadas ? "Sí" : "No")}, Abiertas con OC={(filtros.MostrarAbiertasConOc ? "Sí" : "No")}"
            };
        }

        private static IContainer EstiloCeldaEncabezado(IContainer container)
        {
            return container
                .Border(1)
                .BorderColor(Colors.Grey.Medium)
                .Background(Colors.Blue.Darken2)
                .PaddingVertical(4)
                .PaddingHorizontal(3)
                .DefaultTextStyle(x => x.FontSize(8).SemiBold().FontColor(Colors.White));
        }

        private static IContainer EstiloCeldaDatos(IContainer container)
        {
            return container
                .Border(1)
                .BorderColor(Colors.Grey.Lighten1)
                .PaddingVertical(3)
                .PaddingHorizontal(3);
        }
    }
}
