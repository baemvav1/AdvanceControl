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
    /// Servicio encargado de transformar el modelo del reporte financiero de facturación
    /// en un archivo PDF usando QuestPDF.
    /// </summary>
    public class ReporteFinancieroFacturacionExportService : IReporteFinancieroFacturacionExportService
    {
        // Cultura fija para formatear fechas y montos en formato mexicano.
        private static readonly CultureInfo Cultura = new("es-MX");

        public ReporteFinancieroFacturacionExportService()
        {
            // Se establece la licencia comunitaria de QuestPDF una sola vez.
            QuestPDF.Settings.License = LicenseType.Community;
        }

        /// <summary>
        /// Genera el PDF del reporte financiero tomando las facturas ya visibles en pantalla,
        /// junto con los filtros aplicados por el usuario.
        /// </summary>
        public Task<string> GenerarReportePdfAsync(
            IReadOnlyList<ReporteFinancieroFacturacionCabeceraDto> cabeceras,
            IReadOnlyList<ReporteFinancieroFacturacionDetalleDto> detalles,
            string? receptorRfcFiltro,
            string? referenciaFiltro,
            DateTimeOffset? fechaInicioFiltro,
            DateTimeOffset? fechaFinFiltro,
            bool? finiquitoFiltro,
            int movimientosNcCount,
            decimal movimientosNcTotal,
            bool mostrarMovimientosNc = true)
        {
            // Validación defensiva: la colección de cabeceras es obligatoria.
            if (cabeceras == null)
            {
                throw new ArgumentNullException(nameof(cabeceras));
            }

            // Validación defensiva: la colección de detalles es obligatoria.
            if (detalles == null)
            {
                throw new ArgumentNullException(nameof(detalles));
            }

            // Si no hay detalles visibles, no tiene sentido construir el PDF.
            if (detalles.Count == 0)
            {
                throw new InvalidOperationException("No hay registros visibles para generar el reporte.");
            }

            // Se asegura la carpeta destino donde se guardarán los PDFs.
            var carpeta = ObtenerCarpetaReportes();
            Directory.CreateDirectory(carpeta);

            // Se prepara toda la información derivada que usará el documento.
            var rutaArchivo = Path.Combine(carpeta, ConstruirNombreArchivo(receptorRfcFiltro));
            var resumenFiltros = ConstruirResumenFiltros(receptorRfcFiltro, referenciaFiltro, fechaInicioFiltro, fechaFinFiltro, finiquitoFiltro);
            var totalFacturado = cabeceras.Sum(item => item.TotalFacturado);
            var totalFiniquitado = cabeceras.Sum(item => item.TotalAbonadoMovimientos);
            var totalRestante = totalFacturado - totalFiniquitado - movimientosNcTotal;
            var cabeceraPath = Path.Combine(ObtenerCarpetaCabeceras(), "EstadoCuenta.png");

            // Aquí se define toda la estructura visual del PDF.
            var documento = Document.Create(container =>
            {
                container.Page(page =>
                {
                    // Configuración general de la página.
                    page.Size(PageSizes.Letter);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    // Encabezado del documento: imagen institucional, título y fecha de generación.
                    page.Header().ShowOnce().Column(column =>
                    {
                        // Si existe la imagen configurada para este reporte, se usa como cabecera.
                        if (File.Exists(cabeceraPath))
                        {
                            column.Item().Image(cabeceraPath).FitWidth();
                        }

                        // Título principal del PDF.
                        column.Item().Text("Reporte Financiero de Facturacion")
                            .FontSize(18)
                            .SemiBold()
                            .FontColor(Colors.Blue.Darken2);

                        // Fecha y hora exactas de generación del archivo.
                        column.Item().Text($"Generado: {DateTime.Now.ToString("dd/MM/yyyy HH:mm", Cultura)}")
                            .FontSize(9)
                            .FontColor(Colors.Grey.Darken1);
                    });

                    // Contenido central del reporte.
                    page.Content().Column(column =>
                    {
                        column.Spacing(8);

                        // Cuadro con el resumen textual de los filtros aplicados.
                        column.Item().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(8).Column(filtros =>
                        {
                            filtros.Spacing(2);
                            filtros.Item().Text("Filtros aplicados").SemiBold();

                            // Cada filtro se imprime como una línea independiente para facilitar lectura.
                            foreach (var linea in resumenFiltros)
                            {
                                filtros.Item().Text(linea);
                            }
                        });

                        // Bloque de resumen global del reporte.
                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(6).Column(resumen =>
                            {
                                resumen.Item().Text("Resumen").SemiBold();
                                resumen.Item().Text($"{cabeceras.Sum(item => item.NumeroFacturas)} factura(s) en {cabeceras.Count} cliente(s)");
                                resumen.Item().Text($"Total facturado: {totalFacturado.ToString("C2", Cultura)}");
                                resumen.Item().Text($"Total finiquitado: {totalFiniquitado.ToString("C2", Cultura)}");
                                if (mostrarMovimientosNc)
                                {
                                    resumen.Item().Text(text =>
                                    {
                                        text.Span("Movimientos no conciliados: ").Bold();
                                        text.Span($"{movimientosNcTotal.ToString("C2", Cultura)} ({movimientosNcCount} movimiento(s))");
                                    });
                                    resumen.Item().Text(text =>
                                    {
                                        text.Span("Restante: ").Bold();
                                        text.Span(totalRestante.ToString("C2", Cultura));
                                    });
                                }
                            });
                        });

                        // Cada cabecera representa un cliente/receptor dentro del reporte.
                        foreach (var cabecera in cabeceras)
                        {
                            // Se seleccionan únicamente las facturas que pertenecen al receptor actual.
                            var detallesCliente = detalles
                                .Where(item => string.Equals(item.ReceptorRfc, cabecera.ReceptorRfc, StringComparison.OrdinalIgnoreCase))
                                .ToList();

                            // Tarjeta principal del cliente con sus facturas.
                            column.Item().Border(1).BorderColor(Colors.Blue.Lighten2).Padding(8).Column(cliente =>
                            {
                                cliente.Spacing(6);

                                // Nombre y RFC del cliente.
                                cliente.Item().Text($"{cabecera.ReceptorNombreTexto} ({cabecera.ReceptorRfcTexto})")
                                    .SemiBold()
                                    .FontSize(12)
                                    .FontColor(Colors.Blue.Darken2);

                                // Resumen de cantidades de facturas.
                                cliente.Item().Text(cabecera.ResumenTexto);

                                // Totales monetarios del cliente actual.
                                cliente.Item().Text($"Facturado: {cabecera.TotalFacturadoTexto}   |   Finiquitado: {cabecera.TotalAbonadoMovimientosTexto}");

                                // Se imprime una tarjeta por cada factura del cliente.
                                foreach (var detalle in detallesCliente)
                                {
                                    cliente.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(6).Column(detalleItem =>
                                    {
                                        detalleItem.Spacing(4);

                                        // Primera fila de metadatos de la factura: folio, fecha y estado.
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

                                        // Segunda fila de información financiera de la factura.
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
                                                text.Span("Referencia(s)").SemiBold();
                                                text.Span(": ");
                                                text.Span(detalle.ReferenciaTexto);
                                            });
                                        });

                                        // Si la factura tiene abonos relacionados, se listan debajo de ella.
                                        if (detalle.Abonos.Count > 0)
                                        {
                                            detalleItem.Item().PaddingTop(2).Column(abonos =>
                                            {
                                                abonos.Spacing(4);

                                                // Título del sublistado de abonos de la factura.
                                                abonos.Item().Text(detalle.ResumenAbonosTitulo)
                                                    .SemiBold()
                                                    .FontSize(9);

                                                // Cada abono se muestra en su propia tabla para aislar su contenido.
                                                foreach (var abono in detalle.Abonos.OrderBy(item => item.FechaAbono).ThenBy(item => item.IdAbonoFactura))
                                                {
                                                    abonos.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Table(tabla =>
                                                    {
                                                        // Se reservan anchos fijos para fecha, monto y saldo;
                                                        // la referencia consume el espacio restante.
                                                        tabla.ColumnsDefinition(columnas =>
                                                        {
                                                            columnas.ConstantColumn(120);
                                                            columnas.ConstantColumn(120);
                                                            columnas.ConstantColumn(125);
                                                            columnas.ConstantColumn(120);
                                                        });

                                                        tabla.Header(header =>
                                                        {
                                                            header.Cell().Element(EstiloEtiquetaSubtablaAbono).Text("Fecha");
                                                            header.Cell().Element(EstiloEtiquetaSubtablaAbono).Text("Monto");
                                                            header.Cell().Element(EstiloEtiquetaSubtablaAbono).Text("Referencia");
                                                            header.Cell().Element(EstiloEtiquetaSubtablaAbono).Text("Saldo");
                                                        });

                                                        // Fila de valores del abono actual.
                                                        tabla.Cell().Element(EstiloValorSubtablaAbono).Text(abono.FechaAbonoTexto);
                                                        tabla.Cell().Element(EstiloValorSubtablaAbono).Text(abono.MontoAbonoTexto);
                                                        tabla.Cell().Element(EstiloValorSubtablaAbono).Text(abono.ReferenciaTexto);
                                                        tabla.Cell().Element(EstiloValorSubtablaAbono).Text(abono.SaldoPosteriorTexto);
                                                    });
                                                }
                                            });
                                        }
                                    });
                                }
                            });
                        }
                    });

                    // Pie de página con numeración.
                    page.Footer().AlignRight().Text(text =>
                    {
                        text.Span("Pagina ");
                        text.CurrentPageNumber();
                        text.Span(" de ");
                        text.TotalPages();
                    });
                });
            });

            // Finalmente se genera el archivo físico en disco.
            documento.GeneratePdf(rutaArchivo);
            return Task.FromResult(rutaArchivo);
        }

        /// <summary>
        /// Devuelve la carpeta local donde se guardan los reportes PDF.
        /// </summary>
        private static string ObtenerCarpetaReportes()
        {
            var documentos = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            return Path.Combine(documentos, "Advance Control", "ReportesFinancieros");
        }

        /// <summary>
        /// Devuelve la carpeta donde se almacenan las imágenes de cabecera.
        /// </summary>
        private static string ObtenerCarpetaCabeceras()
        {
            var documentos = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            return Path.Combine(documentos, "Advance Control", "Cabeceras");
        }

        /// <summary>
        /// Construye el nombre final del PDF usando el RFC filtrado, o "General" si no hubo filtro.
        /// </summary>
        private static string ConstruirNombreArchivo(string? receptorRfcFiltro)
        {
            var rfc = string.IsNullOrWhiteSpace(receptorRfcFiltro)
                ? "General"
                : LimpiarNombreArchivo(receptorRfcFiltro.Trim().ToUpperInvariant());

            return $"ReporteFinancieroFacturacion_{rfc}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
        }

        /// <summary>
        /// Quita caracteres no válidos para nombres de archivo.
        /// </summary>
        private static string LimpiarNombreArchivo(string valor)
        {
            return string.Concat(valor.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
        }

        /// <summary>
        /// Convierte los filtros actuales en un listado de líneas legibles para imprimir en el PDF.
        /// </summary>
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

        /// <summary>
        /// Traduce el filtro nullable de finiquito a texto legible.
        /// </summary>
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

        /// <summary>
        /// Estilo base de las celdas de detalle de factura.
        /// </summary>
        private static IContainer EstiloCampoDetalle(IContainer container)
        {
            return container
                .PaddingHorizontal(2)
                .DefaultTextStyle(x => x.FontSize(9));
        }

        /// <summary>
        /// Estilo para los encabezados de la subtabla de abonos.
        /// </summary>
        private static IContainer EstiloEtiquetaSubtablaAbono(IContainer container)
        {
            return container
                .BorderBottom(1)
                .BorderColor(Colors.Grey.Lighten1)
                .PaddingVertical(2)
                .PaddingHorizontal(2)
                .DefaultTextStyle(x => x.FontSize(8).SemiBold());
        }

        /// <summary>
        /// Estilo para las celdas de valores de la subtabla de abonos.
        /// </summary>
        private static IContainer EstiloValorSubtablaAbono(IContainer container)
        {
            return container
                .PaddingVertical(3)
                .PaddingHorizontal(2)
                .DefaultTextStyle(x => x.FontSize(8));
        }

        /// <summary>
        /// Genera el PDF del reporte simplificado: tabla tipo Excel con
        /// Folio, Fecha Emisión, Conceptos y Observación (Pendiente o fechas de abonos).
        /// </summary>
        public Task<string> GenerarReporteSimplificadoPdfAsync(
            IReadOnlyList<ReporteFinancieroFacturacionCabeceraDto> cabeceras,
            IReadOnlyList<ReporteFinancieroFacturacionDetalleDto> detalles,
            string? receptorRfcFiltro,
            string? referenciaFiltro,
            DateTimeOffset? fechaInicioFiltro,
            DateTimeOffset? fechaFinFiltro,
            bool? finiquitoFiltro,
            int movimientosNcCount,
            decimal movimientosNcTotal,
            bool mostrarMovimientosNc = true)
        {
            if (cabeceras == null) throw new ArgumentNullException(nameof(cabeceras));
            if (detalles == null) throw new ArgumentNullException(nameof(detalles));
            if (detalles.Count == 0) throw new InvalidOperationException("No hay registros visibles para generar el reporte.");

            var carpeta = ObtenerCarpetaReportes();
            Directory.CreateDirectory(carpeta);

            var rutaArchivo = Path.Combine(carpeta, ConstruirNombreArchivoSimplificado(receptorRfcFiltro));
            var resumenFiltros = ConstruirResumenFiltros(receptorRfcFiltro, referenciaFiltro, fechaInicioFiltro, fechaFinFiltro, finiquitoFiltro);
            var totalFacturado = cabeceras.Sum(item => item.TotalFacturado);
            var totalFiniquitado = cabeceras.Sum(item => item.TotalAbonadoMovimientos);
            var cabeceraPath = Path.Combine(ObtenerCarpetaCabeceras(), "EstadoCuenta.png");

            // Construir las filas del reporte simplificado
            var filas = ConstruirFilasSimplificadas(detalles);

            var documento = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.Letter.Landscape());
                    page.Margin(1.5f, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    // Encabezado: misma estructura que el reporte actual
                    page.Header().ShowOnce().Column(column =>
                    {
                        if (File.Exists(cabeceraPath))
                        {
                            column.Item().Image(cabeceraPath).FitWidth();
                        }

                        column.Item().Text("Reporte Simplificado de Facturación")
                            .FontSize(18)
                            .SemiBold()
                            .FontColor(Colors.Blue.Darken2);

                        column.Item().Text($"Generado: {DateTime.Now.ToString("dd/MM/yyyy HH:mm", Cultura)}")
                            .FontSize(9)
                            .FontColor(Colors.Grey.Darken1);

                        // Filtros aplicados
                        column.Item().PaddingTop(4).Border(1).BorderColor(Colors.Grey.Lighten1).Padding(6).Column(filtros =>
                        {
                            filtros.Spacing(2);
                            filtros.Item().Text("Filtros aplicados").SemiBold().FontSize(8);
                            foreach (var linea in resumenFiltros)
                            {
                                filtros.Item().Text(linea).FontSize(8);
                            }
                        });

                        // Resumen
                        column.Item().PaddingTop(4).PaddingBottom(8).Row(row =>
                        {
                            row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(6).Column(resumen =>
                            {
                                resumen.Item().Text("Resumen").SemiBold().FontSize(8);
                                resumen.Item().Text($"{cabeceras.Sum(item => item.NumeroFacturas)} factura(s) en {cabeceras.Count} cliente(s)").FontSize(8);
                                resumen.Item().Text($"Total facturado: {totalFacturado.ToString("C2", Cultura)}  |  Total finiquitado: {totalFiniquitado.ToString("C2", Cultura)}").FontSize(8);
                                if (mostrarMovimientosNc)
                                {
                                    resumen.Item().Text($"Movimientos no conciliados: {movimientosNcTotal.ToString("C2", Cultura)} ({movimientosNcCount})").FontSize(8);
                                }
                            });
                        });
                    });

                    // Contenido: tabla tipo Excel
                    page.Content().Table(tabla =>
                    {
                        tabla.ColumnsDefinition(columnas =>
                        {
                            columnas.ConstantColumn(80);   // Folio
                            columnas.ConstantColumn(85);   // Fecha Emisión
                            columnas.RelativeColumn();     // Conceptos
                            columnas.ConstantColumn(100);  // Observación
                        });

                        // Encabezado de tabla
                        tabla.Header(header =>
                        {
                            header.Cell().Element(EstiloCeldaEncabezado).Text("Folio");
                            header.Cell().Element(EstiloCeldaEncabezado).Text("Fecha Emisión");
                            header.Cell().Element(EstiloCeldaEncabezado).Text("Conceptos");
                            header.Cell().Element(EstiloCeldaEncabezado).Text("Observación");
                        });

                        // Filas de datos
                        foreach (var fila in filas)
                        {
                            tabla.Cell().Element(c => EstiloCeldaDatos(c, fila.EsPrimerAbono)).Text(fila.Folio).FontSize(8);
                            tabla.Cell().Element(c => EstiloCeldaDatos(c, fila.EsPrimerAbono)).Text(fila.FechaEmision).FontSize(8);
                            tabla.Cell().Element(c => EstiloCeldaDatos(c, fila.EsPrimerAbono)).Text(fila.Conceptos).FontSize(8);
                            tabla.Cell().Element(c => EstiloCeldaDatos(c, fila.EsPrimerAbono)).Text(fila.Observacion).FontSize(8);
                        }
                    });

                    page.Footer().AlignRight().Text(text =>
                    {
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

        /// <summary>
        /// Construye las filas del reporte simplificado a partir de los detalles.
        /// Para facturas con múltiples abonos, duplica la fila con diferente Observación.
        /// </summary>
        private static List<FilaReporteSimplificado> ConstruirFilasSimplificadas(
            IReadOnlyList<ReporteFinancieroFacturacionDetalleDto> detalles)
        {
            var filas = new List<FilaReporteSimplificado>();

            foreach (var detalle in detalles)
            {
                var folio = detalle.FolioTexto;
                var fecha = detalle.FechaTimbrado.HasValue
                    ? detalle.FechaTimbrado.Value.ToString("dd/MM/yyyy")
                    : "Sin fecha";
                var conceptos = string.IsNullOrWhiteSpace(detalle.ConceptosTexto)
                    ? "Sin conceptos"
                    : detalle.ConceptosTexto;

                if (detalle.Abonos.Count == 0)
                {
                    // Sin abonos → "Pendiente"
                    filas.Add(new FilaReporteSimplificado
                    {
                        Folio = folio,
                        FechaEmision = fecha,
                        Conceptos = conceptos,
                        Observacion = "Pendiente",
                        EsPrimerAbono = true
                    });
                }
                else if (detalle.Abonos.Count == 1)
                {
                    // Un solo abono → fecha del abono
                    var abono = detalle.Abonos[0];
                    filas.Add(new FilaReporteSimplificado
                    {
                        Folio = folio,
                        FechaEmision = fecha,
                        Conceptos = conceptos,
                        Observacion = abono.FechaAbono.ToString("dd/MM/yyyy"),
                        EsPrimerAbono = true
                    });
                }
                else
                {
                    // Múltiples abonos → una fila por abono
                    var abonosOrdenados = detalle.Abonos.OrderBy(a => a.FechaAbono).ThenBy(a => a.IdAbonoFactura).ToList();
                    for (int i = 0; i < abonosOrdenados.Count; i++)
                    {
                        filas.Add(new FilaReporteSimplificado
                        {
                            Folio = folio,
                            FechaEmision = fecha,
                            Conceptos = conceptos,
                            Observacion = abonosOrdenados[i].FechaAbono.ToString("dd/MM/yyyy"),
                            EsPrimerAbono = i == 0
                        });
                    }
                }
            }

            return filas;
        }

        private static string ConstruirNombreArchivoSimplificado(string? receptorRfcFiltro)
        {
            var rfc = string.IsNullOrWhiteSpace(receptorRfcFiltro)
                ? "General"
                : LimpiarNombreArchivo(receptorRfcFiltro.Trim().ToUpperInvariant());
            return $"ReporteSimplificado_{rfc}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
        }

        private static IContainer EstiloCeldaEncabezado(IContainer container)
        {
            return container
                .Border(1)
                .BorderColor(Colors.Grey.Medium)
                .Background(Colors.Blue.Darken2)
                .PaddingVertical(4)
                .PaddingHorizontal(4)
                .DefaultTextStyle(x => x.FontSize(9).SemiBold().FontColor(Colors.White));
        }

        private static IContainer EstiloCeldaDatos(IContainer container, bool bordeArriba)
        {
            var c = container
                .BorderBottom(1)
                .BorderLeft(1)
                .BorderRight(1)
                .BorderColor(Colors.Grey.Lighten1)
                .PaddingVertical(3)
                .PaddingHorizontal(4);

            if (bordeArriba)
            {
                c = container
                    .Border(1)
                    .BorderColor(Colors.Grey.Lighten1)
                    .PaddingVertical(3)
                    .PaddingHorizontal(4);
            }

            return c;
        }

        /// <summary>
        /// Fila del reporte simplificado.
        /// </summary>
        private class FilaReporteSimplificado
        {
            public string Folio { get; set; } = string.Empty;
            public string FechaEmision { get; set; } = string.Empty;
            public string Conceptos { get; set; } = string.Empty;
            public string Observacion { get; set; } = string.Empty;
            public bool EsPrimerAbono { get; set; } = true;
        }

    }
}
