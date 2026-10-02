using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.Cargos;
using Advance_Control.Services.LocalStorage;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Advance_Control.Services.Reportes
{
    /// <summary>
    /// "Reporte Ejecutivo de Operaciones": qué se realizó en el periodo filtrado. Portada con periodo,
    /// filtros, resumen e índice; después una sección por operación con sus datos, hojas de servicio,
    /// cargos (cantidad, tipo, detalle y nota, SIN precios) con sus fotos, levantamientos y checklists
    /// de mantenimiento preventivo. No incluye prefacturas, órdenes de compra ni facturas (traen
    /// importes). Página carta con márgenes de 2 cm, igual que el reporte de QuoteService, para que
    /// PdfDocumentosHelper dimensione bien las imágenes.
    /// </summary>
    public class ReporteEjecutivoOperacionesService : IReporteEjecutivoOperacionesService
    {
        private static readonly CultureInfo Cultura = new("es-MX");
        private const int ConcurrenciaDescargas = 3;

        private readonly IOperacionImageService _operacionImageService;
        private readonly ICargoService _cargoService;
        private readonly ICargoImageService _cargoImageService;

        public ReporteEjecutivoOperacionesService(
            IOperacionImageService operacionImageService,
            ICargoService cargoService,
            ICargoImageService cargoImageService)
        {
            _operacionImageService = operacionImageService ?? throw new ArgumentNullException(nameof(operacionImageService));
            _cargoService = cargoService ?? throw new ArgumentNullException(nameof(cargoService));
            _cargoImageService = cargoImageService ?? throw new ArgumentNullException(nameof(cargoImageService));
            QuestPDF.Settings.License = LicenseType.Community;
        }

        private sealed class CargoConFotos
        {
            public required CargoDto Cargo { get; init; }
            public List<string> Fotos { get; init; } = new();
        }

        private sealed class EvidenciasOperacion
        {
            public required OperacionDto Operacion { get; init; }
            public List<CargoConFotos> Cargos { get; } = new();
            public List<string> HojasServicio { get; set; } = new();
            public List<string> Levantamientos { get; set; } = new();
            public List<string> MantenimientoPreventivo { get; set; } = new();
            public string? Error { get; set; }

            public bool SinEvidencias => Cargos.Count == 0 && HojasServicio.Count == 0
                && Levantamientos.Count == 0 && MantenimientoPreventivo.Count == 0;
        }

        public async Task<string> GenerarAsync(
            IReadOnlyList<OperacionDto> operaciones,
            OperacionesReporteFiltrosDto filtros,
            IProgress<string>? progreso = null,
            CancellationToken cancellationToken = default)
        {
            if (operaciones == null) throw new ArgumentNullException(nameof(operaciones));
            if (filtros == null) throw new ArgumentNullException(nameof(filtros));
            if (operaciones.Count == 0) throw new InvalidOperationException("No hay operaciones para el conjunto de filtros actual.");

            var ordenadas = operaciones
                .OrderBy(o => o.FechaInicio ?? DateTime.MaxValue)
                .ThenBy(o => o.IdOperacion)
                .ToList();

            var temporales = new List<string>();
            try
            {
                var evidencias = await RecolectarEvidenciasAsync(ordenadas, temporales, progreso, cancellationToken);

                progreso?.Report("Armando PDF…");
                var carpeta = OperacionesReporteExportService.ObtenerCarpetaReportes();
                Directory.CreateDirectory(carpeta);
                var rutaArchivo = Path.Combine(carpeta, ConstruirNombreArchivo(filtros));

                await Task.Run(() => ConstruirDocumento(evidencias, filtros).GeneratePdf(rutaArchivo), cancellationToken);
                return rutaArchivo;
            }
            finally
            {
                foreach (var temporal in temporales)
                {
                    try { File.Delete(temporal); } catch { }
                }
            }
        }

        // ---------------- Recolección (fuera de QuestPDF) ----------------

        private async Task<List<EvidenciasOperacion>> RecolectarEvidenciasAsync(
            List<OperacionDto> operaciones,
            List<string> temporales,
            IProgress<string>? progreso,
            CancellationToken ct)
        {
            using var limite = new SemaphoreSlim(ConcurrenciaDescargas);
            var terminadas = 0;

            var tareas = operaciones.Select(async operacion =>
            {
                await limite.WaitAsync(ct);
                try
                {
                    return await RecolectarOperacionAsync(operacion, temporales, ct);
                }
                finally
                {
                    limite.Release();
                    var n = Interlocked.Increment(ref terminadas);
                    progreso?.Report($"Cargando {n} de {operaciones.Count}…");
                }
            });

            return (await Task.WhenAll(tareas)).ToList();
        }

        /// <summary>
        /// Un fallo al cargar algo de una operación no tumba el reporte: lo que sí se obtuvo se
        /// muestra y la sección avisa que faltan adjuntos.
        /// </summary>
        private async Task<EvidenciasOperacion> RecolectarOperacionAsync(OperacionDto operacion, List<string> temporales, CancellationToken ct)
        {
            var evidencias = new EvidenciasOperacion { Operacion = operacion };
            if (operacion.IdOperacion is not int idOperacion)
            {
                return evidencias;
            }

            try
            {
                var cargos = await _cargoService.GetCargosAsync(new CargoEditDto { IdOperacion = idOperacion }, cancellationToken: ct);
                foreach (var cargo in cargos)
                {
                    var fotos = new List<string>();
                    try
                    {
                        var imagenes = await _cargoImageService.GetImagesAsync(idOperacion, cargo.IdCargo, cancellationToken: ct);
                        fotos = imagenes
                            .Where(img => !string.IsNullOrWhiteSpace(img.Url) && File.Exists(img.Url))
                            .Select(img => img.Url)
                            .ToList();
                    }
                    catch (OperationCanceledException) { throw; }
                    catch
                    {
                        evidencias.Error = "No se pudieron cargar todas las fotos de los cargos.";
                    }

                    evidencias.Cargos.Add(new CargoConFotos { Cargo = cargo, Fotos = fotos });
                }

                evidencias.HojasServicio = await ExpandirSeguroAsync(
                    () => _operacionImageService.GetHojasServicioAsync(idOperacion, cancellationToken: ct), temporales, evidencias);
                evidencias.Levantamientos = await ExpandirSeguroAsync(
                    () => _operacionImageService.GetLevantamientosAsync(idOperacion, cancellationToken: ct), temporales, evidencias);
                evidencias.MantenimientoPreventivo = await ExpandirSeguroAsync(
                    () => _operacionImageService.GetMantenimientoPreventivosAsync(idOperacion, cancellationToken: ct), temporales, evidencias);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                evidencias.Error = $"No se pudieron cargar los adjuntos de esta operación: {ex.Message}";
            }

            return evidencias;
        }

        private static async Task<List<string>> ExpandirSeguroAsync(
            Func<Task<List<OperacionImageDto>>> obtener,
            List<string> temporales,
            EvidenciasOperacion evidencias)
        {
            try
            {
                return await PdfDocumentosHelper.ExpandirDocumentosAsync(await obtener(), temporales);
            }
            catch (OperationCanceledException) { throw; }
            catch
            {
                evidencias.Error ??= "No se pudieron cargar todos los adjuntos de esta operación.";
                return new List<string>();
            }
        }

        // ---------------- Documento ----------------

        private static Document ConstruirDocumento(List<EvidenciasOperacion> evidencias, OperacionesReporteFiltrosDto filtros)
        {
            var cabeceraPath = Path.Combine(OperacionesReporteExportService.ObtenerCarpetaCabeceras(), "Reporte.png");
            var operaciones = evidencias.Select(e => e.Operacion).ToList();

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.Letter);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().ShowOnce().Column(column =>
                    {
                        if (File.Exists(cabeceraPath))
                        {
                            column.Item().Image(cabeceraPath).FitWidth();
                        }
                    });

                    page.Content().Column(column =>
                    {
                        AgregarPortada(column, operaciones, filtros);

                        foreach (var evidencia in evidencias)
                        {
                            column.Item().PageBreak();
                            AgregarSeccionOperacion(column, evidencia);
                        }
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.DefaultTextStyle(s => s.FontSize(8).FontColor(Colors.Grey.Darken1));
                        x.Span("Reporte ejecutivo  ·  Hoja ");
                        x.CurrentPageNumber();
                        x.Span(" de ");
                        x.TotalPages();
                    });
                });
            });
        }

        private static void AgregarPortada(ColumnDescriptor column, List<OperacionDto> operaciones, OperacionesReporteFiltrosDto filtros)
        {
            column.Spacing(6);

            column.Item().PaddingTop(6).Text("Reporte Ejecutivo de Operaciones")
                .FontSize(20).SemiBold().FontColor(Colors.Blue.Darken2);
            column.Item().Text(text =>
            {
                text.Span("Periodo: ").SemiBold();
                text.Span(TextoPeriodo(filtros));
            });
            column.Item().Text($"Generado: {DateTime.Now.ToString("dd/MM/yyyy HH:mm", Cultura)}"
                    + (string.IsNullOrWhiteSpace(filtros.GeneradoPor) ? string.Empty : $"  |  Por: {filtros.GeneradoPor}"))
                .FontSize(9).FontColor(Colors.Grey.Darken1);

            // Resumen
            var abiertas = operaciones.Count(o => !o.IsFinalized);
            var porTipo = operaciones.GroupBy(o => string.IsNullOrWhiteSpace(o.TipoMantenimiento) ? "Sin tipo" : o.TipoMantenimiento!)
                .OrderByDescending(g => g.Count()).Select(g => $"{g.Key}: {g.Count()}");
            var porTecnico = operaciones.GroupBy(o => string.IsNullOrWhiteSpace(o.Atiende) ? "Sin asignar" : o.Atiende!)
                .OrderByDescending(g => g.Count()).Select(g => $"{g.Key}: {g.Count()}");

            column.Item().PaddingTop(6).Border(1).BorderColor(Colors.Grey.Lighten1).Padding(8).Column(resumen =>
            {
                resumen.Spacing(3);
                resumen.Item().Text("Resumen").SemiBold();
                resumen.Item().Text($"{operaciones.Count} operación(es)  |  Finalizadas: {operaciones.Count - abiertas}  |  Abiertas: {abiertas}");
                resumen.Item().Text(text =>
                {
                    text.Span("Por tipo: ").SemiBold();
                    text.Span(string.Join("  ·  ", porTipo));
                });
                resumen.Item().Text(text =>
                {
                    text.Span("Por técnico: ").SemiBold();
                    text.Span(string.Join("  ·  ", porTecnico));
                });
            });

            column.Item().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(8).Column(f =>
            {
                f.Spacing(2);
                f.Item().Text("Filtros aplicados").SemiBold().FontSize(9);
                foreach (var linea in OperacionesReporteExportService.ConstruirResumenFiltros(filtros))
                {
                    f.Item().Text(linea).FontSize(8);
                }
            });

            // Índice
            column.Item().PaddingTop(8).Text("Operaciones incluidas").SemiBold().FontSize(12).FontColor(Colors.Blue.Darken2);
            column.Item().Table(tabla =>
            {
                tabla.ColumnsDefinition(c =>
                {
                    c.ConstantColumn(34);    // ID
                    c.RelativeColumn(2.2f);  // Cliente / Equipo
                    c.RelativeColumn(1);     // Tipo
                    c.RelativeColumn(1.3f);  // Atiende
                    c.ConstantColumn(46);    // Inicio
                    c.ConstantColumn(46);    // Fin
                    c.RelativeColumn(1.1f);  // Estado
                });

                tabla.Header(h =>
                {
                    foreach (var titulo in new[] { "ID", "Cliente / Equipo", "Tipo", "Atiende", "Inicio", "Fin", "Estado" })
                    {
                        h.Cell().Element(OperacionesReporteExportService.EstiloCeldaEncabezado).Text(titulo);
                    }
                });

                foreach (var op in operaciones)
                {
                    tabla.Cell().Element(OperacionesReporteExportService.EstiloCeldaDatos).Text(op.IdOperacion?.ToString() ?? "—").FontSize(8);
                    tabla.Cell().Element(OperacionesReporteExportService.EstiloCeldaDatos).Column(c =>
                    {
                        c.Item().Text(op.RazonSocial ?? "—").FontSize(8);
                        c.Item().Text(op.Identificador ?? string.Empty).FontSize(7).FontColor(Colors.Grey.Darken1);
                    });
                    tabla.Cell().Element(OperacionesReporteExportService.EstiloCeldaDatos).Text(op.TipoMantenimiento ?? "—").FontSize(8);
                    tabla.Cell().Element(OperacionesReporteExportService.EstiloCeldaDatos).Text(string.IsNullOrWhiteSpace(op.Atiende) ? "Sin asignar" : op.Atiende).FontSize(8);
                    tabla.Cell().Element(OperacionesReporteExportService.EstiloCeldaDatos).Text(op.FechaInicioCorta).FontSize(8);
                    tabla.Cell().Element(OperacionesReporteExportService.EstiloCeldaDatos).Text(op.IsFinalized ? op.FechaFinCorta : "—").FontSize(8);
                    tabla.Cell().Element(OperacionesReporteExportService.EstiloCeldaDatos).Text(OperacionesReporteExportService.TextoEstado(op)).FontSize(8);
                }
            });
        }

        private static void AgregarSeccionOperacion(ColumnDescriptor column, EvidenciasOperacion evidencia)
        {
            var op = evidencia.Operacion;

            // Encabezado de la operación
            column.Item().Background(Colors.Blue.Darken2).Padding(8).Text(text =>
            {
                text.DefaultTextStyle(s => s.FontColor(Colors.White).FontSize(14).SemiBold());
                text.Span($"Operación {op.IdOperacion}");
                text.Span($"  ·  {op.RazonSocial ?? "Sin cliente"}").FontSize(11).NormalWeight();
            });

            column.Item().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(8).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Spacing(2);
                    Dato(c, "Equipo", op.Identificador);
                    Dato(c, "Tipo", op.TipoMantenimiento);
                    Dato(c, "Atendió", string.IsNullOrWhiteSpace(op.Atiende) ? "Sin asignar" : op.Atiende);
                });
                row.RelativeItem().Column(c =>
                {
                    c.Spacing(2);
                    Dato(c, "Inicio", op.FechaInicioCorta);
                    Dato(c, "Fin", op.IsFinalized ? op.FechaFinCorta : "—");
                    Dato(c, "Estado", OperacionesReporteExportService.TextoEstado(op));
                });
            });

            if (!string.IsNullOrWhiteSpace(op.Nota))
            {
                column.Item().PaddingTop(2).Text(text =>
                {
                    text.Span("Nota: ").SemiBold();
                    text.Span(op.Nota);
                });
            }

            if (!string.IsNullOrWhiteSpace(evidencia.Error))
            {
                column.Item().Background(Colors.Orange.Lighten4).Padding(6)
                    .Text(evidencia.Error).FontSize(9).FontColor(Colors.Orange.Darken4);
            }

            if (evidencia.SinEvidencias)
            {
                column.Item().PaddingTop(8).Text("Sin evidencias cargadas para esta operación.")
                    .Italic().FontColor(Colors.Grey.Darken1);
                return;
            }

            // La primera hoja de servicio comparte página con el encabezado de la operación.
            AgregarDocumentos(column, "Hojas de servicio", evidencia.HojasServicio, AltoPrimeraHojaCm);
            AgregarCargos(column, evidencia.Cargos);
            AgregarDocumentos(column, "Levantamientos", evidencia.Levantamientos);
            AgregarDocumentos(column, "Mantenimiento preventivo", evidencia.MantenimientoPreventivo);
        }

        private static void Dato(ColumnDescriptor c, string etiqueta, string? valor) =>
            c.Item().Text(text =>
            {
                text.Span($"{etiqueta}: ").SemiBold();
                text.Span(string.IsNullOrWhiteSpace(valor) ? "—" : valor);
            });

        private static void TituloSeccion(IContainer container, string titulo) =>
            container.PaddingTop(10)
                .Background(Colors.Blue.Lighten4).Padding(6)
                .Text(titulo).Bold().FontSize(12).FontColor(Colors.Blue.Darken3);

        /// <summary>Alto de la primera hoja de servicio para que quepa en la misma página que el encabezado.</summary>
        private const float AltoPrimeraHojaCm = 17f;

        /// <summary>
        /// El título va en el mismo bloque indivisible que la primera imagen, para que no quede
        /// solo al pie de una hoja con su imagen en la siguiente. <paramref name="altoPrimeraCm"/>
        /// limita la primera imagen (null = alto normal de página).
        /// </summary>
        private static void AgregarDocumentos(ColumnDescriptor column, string titulo, List<string> rutas, float? altoPrimeraCm = null)
        {
            if (rutas.Count == 0)
            {
                return;
            }

            column.Item().ShowEntire().Column(bloque =>
            {
                TituloSeccion(bloque.Item(), titulo);
                if (altoPrimeraCm is float alto)
                {
                    PdfDocumentosHelper.AgregarImagenAdaptativa(bloque.Item(), rutas[0], alto);
                }
                else
                {
                    PdfDocumentosHelper.AgregarImagenAdaptativa(bloque.Item(), rutas[0]);
                }
            });

            foreach (var ruta in rutas.Skip(1))
            {
                PdfDocumentosHelper.AgregarImagenAdaptativa(column.Item(), ruta);
            }
        }

        /// <summary>Cargos sin precios: cantidad, tipo, detalle y nota, seguidos de sus fotos.</summary>
        private static void AgregarCargos(ColumnDescriptor column, List<CargoConFotos> cargos)
        {
            if (cargos.Count == 0)
            {
                return;
            }

            for (var indice = 0; indice < cargos.Count; indice++)
            {
                var item = cargos[indice];
                var cargo = item.Cargo;
                var esPrimero = indice == 0;

                // Bloque indivisible: (título de la sección si es el primer cargo) + tabla del cargo +
                // su primer renglón de fotos. Así ni el título ni la tabla quedan huérfanos al pie.
                column.Item().ShowEntire().Column(bloque =>
                {
                    if (esPrimero)
                    {
                        TituloSeccion(bloque.Item(), "Trabajos realizados");
                    }

                    bloque.Item().PaddingTop(6).Table(tabla =>
                    {
                        tabla.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(60);   // Cantidad
                            c.RelativeColumn(1);    // Tipo
                            c.RelativeColumn(3);    // Detalle
                        });

                        tabla.Header(h =>
                        {
                            h.Cell().Background(Colors.Grey.Lighten2).Padding(4).Text("Cantidad").Bold().FontSize(9);
                            h.Cell().Background(Colors.Grey.Lighten2).Padding(4).Text("Tipo").Bold().FontSize(9);
                            h.Cell().Background(Colors.Grey.Lighten2).Padding(4).Text("Detalle").Bold().FontSize(9);
                        });

                        tabla.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4)
                            .Text(cargo.Cantidad?.ToString("0.##", Cultura) ?? "—");
                        tabla.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4)
                            .Text(cargo.TipoCargo ?? "—");
                        tabla.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Column(c =>
                        {
                            c.Item().Text(cargo.DetalleLinea1 ?? "—");
                            if (cargo.TieneSubdetalle)
                            {
                                c.Item().PaddingTop(2).Text(cargo.DetalleLinea2 ?? string.Empty).FontSize(9).FontColor(Colors.Grey.Medium);
                                c.Item().Text(cargo.DetalleLinea3 ?? string.Empty).FontSize(9).FontColor(Colors.Grey.Darken1);
                            }
                            if (!string.IsNullOrWhiteSpace(cargo.Nota))
                            {
                                c.Item().PaddingTop(3).Text(cargo.Nota).FontSize(8).Italic().FontColor(Colors.Grey.Darken2);
                            }
                        });
                });

                if (item.Fotos.Count > 0)
                {
                    AgregarRenglonFotos(bloque.Item(), item.Fotos, 0);
                }
                });

                for (var i = 2; i < item.Fotos.Count; i += 2)
                {
                    AgregarRenglonFotos(column.Item(), item.Fotos, i);
                }
            }
        }

        /// <summary>
        /// Dos fotos lado a lado con alto fijo: así un renglón siempre cabe en una página (ShowEntire no
        /// puede fallar por una foto muy alta) y las verticales no ocupan una hoja entera.
        /// </summary>
        private static void AgregarRenglonFotos(IContainer container, List<string> fotos, int inicio)
        {
            var izquierda = fotos[inicio];
            var derecha = inicio + 1 < fotos.Count ? fotos[inicio + 1] : null;

            container.ShowEntire().PaddingTop(4).Height(8, Unit.Centimetre).Row(row =>
            {
                row.Spacing(4);
                row.RelativeItem().AlignCenter().Image(izquierda).FitArea();
                if (derecha is not null)
                {
                    row.RelativeItem().AlignCenter().Image(derecha).FitArea();
                }
                else
                {
                    row.RelativeItem();
                }
            });
        }

        private static string TextoPeriodo(OperacionesReporteFiltrosDto filtros)
        {
            var desde = filtros.FechaInicialFiltro?.ToString("dd/MM/yyyy", Cultura);
            var hasta = filtros.FechaFinalFiltro?.ToString("dd/MM/yyyy", Cultura);
            return (desde, hasta) switch
            {
                (null, null) => "Sin límite de fechas",
                (not null, null) => $"Desde {desde}",
                (null, not null) => $"Hasta {hasta}",
                _ => $"{desde} al {hasta}"
            };
        }

        private static string ConstruirNombreArchivo(OperacionesReporteFiltrosDto filtros)
        {
            var desde = filtros.FechaInicialFiltro?.ToString("yyyyMMdd") ?? "inicio";
            var hasta = filtros.FechaFinalFiltro?.ToString("yyyyMMdd") ?? "hoy";
            return $"ReporteEjecutivo_{desde}_{hasta}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
        }
    }
}
