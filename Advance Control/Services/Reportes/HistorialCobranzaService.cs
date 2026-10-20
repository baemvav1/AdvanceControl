using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.Clientes;
using Advance_Control.Services.Cargos;
using Advance_Control.Services.Entidades;
using Advance_Control.Services.Equipos;
using Advance_Control.Services.LocalStorage;
using Advance_Control.Services.Logging;
using Advance_Control.Services.Operaciones;
using Advance_Control.Services.Quotes;
using Advance_Control.Services.Ubicaciones;

namespace Advance_Control.Services.Reportes
{
    public class HistorialCobranzaService : IHistorialCobranzaService
    {
        private readonly IOperacionService _operacionService;
        private readonly ICargoService _cargoService;
        private readonly ICargoImageService _cargoImageService;
        private readonly IQuoteService _quoteService;
        private readonly Facturas.IFacturaService _facturaService;
        private readonly Facturas.IFacturaPdfService _facturaPdfService;
        private readonly IReporteFinancieroFacturacionService _reporteService;
        private readonly IReporteFinancieroFacturacionExportService _exportService;
        private readonly IEntidadService _entidadService;
        private readonly IEquipoService _equipoService;
        private readonly IUbicacionService _ubicacionService;
        private readonly IClienteService _clienteService;
        private readonly ILoggingService _logger;

        public HistorialCobranzaService(
            IOperacionService operacionService,
            ICargoService cargoService,
            ICargoImageService cargoImageService,
            IQuoteService quoteService,
            Facturas.IFacturaService facturaService,
            Facturas.IFacturaPdfService facturaPdfService,
            IReporteFinancieroFacturacionService reporteService,
            IReporteFinancieroFacturacionExportService exportService,
            IEntidadService entidadService,
            IEquipoService equipoService,
            IUbicacionService ubicacionService,
            IClienteService clienteService,
            ILoggingService logger)
        {
            _operacionService = operacionService ?? throw new ArgumentNullException(nameof(operacionService));
            _cargoService = cargoService ?? throw new ArgumentNullException(nameof(cargoService));
            _cargoImageService = cargoImageService ?? throw new ArgumentNullException(nameof(cargoImageService));
            _quoteService = quoteService ?? throw new ArgumentNullException(nameof(quoteService));
            _facturaService = facturaService ?? throw new ArgumentNullException(nameof(facturaService));
            _facturaPdfService = facturaPdfService ?? throw new ArgumentNullException(nameof(facturaPdfService));
            _reporteService = reporteService ?? throw new ArgumentNullException(nameof(reporteService));
            _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
            _entidadService = entidadService ?? throw new ArgumentNullException(nameof(entidadService));
            _equipoService = equipoService ?? throw new ArgumentNullException(nameof(equipoService));
            _ubicacionService = ubicacionService ?? throw new ArgumentNullException(nameof(ubicacionService));
            _clienteService = clienteService ?? throw new ArgumentNullException(nameof(clienteService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<HistorialCobranzaResultadoDto> GenerarHistorialAsync(
            string rfc,
            string nombreCliente,
            int idCliente,
            DateTimeOffset fechaInicio,
            DateTimeOffset fechaFin,
            string? dirigidoA,
            HistorialCobranzaEstadosDto estados,
            IProgress<string>? progreso = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(rfc))
            {
                throw new ArgumentException("El RFC del cliente es obligatorio.", nameof(rfc));
            }

            if (idCliente <= 0)
            {
                throw new ArgumentException("El cliente es obligatorio.", nameof(idCliente));
            }

            if (estados == null || !estados.AlgunoMarcado)
            {
                throw new ArgumentException("Marca al menos un estado (Abiertas, Abiertas S/Orden, Finalizada C/Orden, Finalizada S/Orden o Pendientes).", nameof(estados));
            }

            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var rfcLimpio = LimpiarNombreArchivo(rfc.Trim().ToUpperInvariant());
            var nombreCarpetaRaiz = $"{rfcLimpio}_{fechaInicio:yyyyMMdd}_a_{fechaFin:yyyyMMdd}_{DateTime.Now:yyyyMMdd_HHmmss}";
            var carpetaRaiz = Path.Combine(documentsPath, global::Advance_Control.Utilities.EntornoApp.CarpetaDocumentos, "HistorialesCobranza", nombreCarpetaRaiz);
            Directory.CreateDirectory(carpetaRaiz);

            var resultado = new HistorialCobranzaResultadoDto { CarpetaHistorial = carpetaRaiz };

            string? nombreEmpresa = null;
            string? apoderadoNombre = null;
            try
            {
                var entidadActiva = await _entidadService.GetActiveEntidadAsync(cancellationToken);
                nombreEmpresa = entidadActiva?.NombreComercial;
                apoderadoNombre = entidadActiva?.Apoderado;
            }
            catch (Exception ex)
            {
                await _logger.LogWarningAsync($"No se pudo obtener la entidad activa: {ex.Message}", "HistorialCobranzaService", "GenerarHistorialAsync");
            }

            decimal? limiteCredito = null;
            try
            {
                var clientes = await _clienteService.GetClienteByIdAsync(idCliente, cancellationToken);
                limiteCredito = clientes?.FirstOrDefault()?.LimiteCredito;
            }
            catch (Exception ex)
            {
                await _logger.LogWarningAsync($"No se pudo obtener el límite de crédito del cliente: {ex.Message}", "HistorialCobranzaService", "GenerarHistorialAsync");
            }

            progreso?.Report("Consultando operaciones del cliente...");
            // IncluirFacturadas: sin él la API oculta las operaciones que ya tienen factura, y el
            // historial salía solo con pendientes / sin facturar.
            var operaciones = await _operacionService.GetOperacionesAsync(
                new OperacionQueryDto { IdCliente = idCliente, IncluirFinalizadas = true, IncluirFacturadas = true },
                cancellationToken);

            var operacionesFiltradas = (operaciones ?? new List<OperacionDto>())
                .Where(o =>
                {
                    var fechaReferencia = o.FechaFinal ?? o.FechaInicio;
                    return fechaReferencia.HasValue
                        && fechaReferencia.Value.Date >= fechaInicio.Date
                        && fechaReferencia.Value.Date <= fechaFin.Date;
                })
                .ToList();

            var operacionesFacturadas = await _facturaService.ObtenerOperacionesFacturadasAsync(cancellationToken);

            // fn_operaciones_facturadas no excluye facturas canceladas: sin esto, una operación con su
            // factura cancelada caería en "Pendientes" (abonado 0 < total).
            var idsCanceladas = (await _facturaService.ObtenerFacturasAsync(cancellationToken))
                .Where(f => f.Cancelada)
                .Select(f => f.IdFactura)
                .ToHashSet();

            var facturasPorOperacion = (operacionesFacturadas ?? new List<OperacionFacturadaDto>())
                .Where(f => !idsCanceladas.Contains(f.IdFactura))
                .GroupBy(f => f.IdOperacion)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var operacion in operacionesFiltradas)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!operacion.IdOperacion.HasValue)
                {
                    resultado.OperacionesOmitidas++;
                    continue;
                }

                var idOperacion = operacion.IdOperacion.Value;

                try
                {
                    progreso?.Report($"Procesando operación #{idOperacion}...");

                    facturasPorOperacion.TryGetValue(idOperacion, out var facturas);
                    var categoria = Clasificar(operacion, facturas);
                    if (categoria is null || !EstaMarcada(categoria.Value, estados))
                    {
                        resultado.OperacionesExcluidas++;
                        continue;
                    }

                    var tieneFactura = facturas is { Count: > 0 };
                    // Si tiene varias facturas se documenta la que falta por cubrir.
                    var idFactura = tieneFactura
                        ? (facturas!.FirstOrDefault(f => f.TotalAbonado < f.Total) ?? facturas![0]).IdFactura
                        : 0;

                    var nombreCarpetaOperacion = categoria switch
                    {
                        CategoriaHistorial.Abierta => $"Operacion_{idOperacion}_abierta",
                        CategoriaHistorial.AbiertaSinOrden => $"Operacion_{idOperacion}_abierta_sin_orden",
                        CategoriaHistorial.FinalizadaConOrden => $"Operacion_{idOperacion}_finalizada_con_orden",
                        CategoriaHistorial.FinalizadaSinOrden => $"Operacion_{idOperacion}_finalizada_sin_orden",
                        _ => $"Operacion_{idOperacion}_facturada_pendiente"
                    };

                    var carpetaOperacion = Path.Combine(carpetaRaiz, nombreCarpetaOperacion);
                    Directory.CreateDirectory(carpetaOperacion);

                    var cargos = await _cargoService.GetCargosAsync(new CargoEditDto { IdOperacion = idOperacion }, cancellationToken: cancellationToken);

                    string? ubicacionNombre = null;
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(operacion.Identificador))
                        {
                            var equipos = await _equipoService.GetEquiposAsync(new EquipoQueryDto { Identificador = operacion.Identificador }, cancellationToken);
                            var equipo = equipos?.FirstOrDefault();
                            if (equipo?.IdUbicacion.HasValue == true && equipo.IdUbicacion.Value > 0)
                            {
                                var ubicacion = await _ubicacionService.GetUbicacionByIdAsync(equipo.IdUbicacion.Value, cancellationToken);
                                ubicacionNombre = ubicacion?.Nombre;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        await _logger.LogWarningAsync($"No se pudo obtener la ubicación del equipo de la operación {idOperacion}: {ex.Message}", "HistorialCobranzaService", "GenerarHistorialAsync");
                    }

                    var rutaCotizacion = await _quoteService.GenerateQuotePdfAsync(operacion, cargos, ubicacionNombre, nombreEmpresa, apoderadoNombre, limiteCredito, dirigidoA);
                    CopiarArchivo(rutaCotizacion, carpetaOperacion);

                    foreach (var cargo in cargos)
                    {
                        try
                        {
                            var imagenes = await _cargoImageService.GetImagesAsync(idOperacion, cargo.IdCargo, cancellationToken: cancellationToken);
                            cargo.Images = new ObservableCollection<CargoImageDto>(imagenes ?? new List<CargoImageDto>());
                        }
                        catch (Exception ex)
                        {
                            await _logger.LogWarningAsync($"No se pudieron cargar imágenes del cargo {cargo.IdCargo} (operación {idOperacion}): {ex.Message}", "HistorialCobranzaService", "GenerarHistorialAsync");
                        }
                    }

                    var rutaReporte = await _quoteService.GenerateReportePdfAsync(operacion, cargos, ubicacionNombre, nombreEmpresa, dirigidoA);
                    CopiarArchivo(rutaReporte, carpetaOperacion);

                    if (tieneFactura)
                    {
                        var detalle = await _facturaService.ObtenerDetalleFacturaAsync(idFactura, cancellationToken);
                        if (detalle != null)
                        {
                            var rutaFacturaPdf = await _facturaPdfService.GenerarFacturaPdfAsync(detalle);
                            CopiarArchivo(rutaFacturaPdf, carpetaOperacion);
                        }

                        var xml = await _facturaService.ObtenerXmlFacturaAsync(idFactura, cancellationToken);
                        if (!string.IsNullOrWhiteSpace(xml))
                        {
                            var rutaXml = Path.Combine(carpetaOperacion, $"Factura_{idFactura}_{DateTime.Now:yyyyMMdd}.xml");
                            await File.WriteAllTextAsync(rutaXml, xml, cancellationToken);
                        }

                    }

                    switch (categoria)
                    {
                        case CategoriaHistorial.Abierta: resultado.OperacionesAbiertas++; break;
                        case CategoriaHistorial.AbiertaSinOrden: resultado.OperacionesAbiertasSinOrden++; break;
                        case CategoriaHistorial.FinalizadaConOrden: resultado.OperacionesFinalizadasConOrden++; break;
                        case CategoriaHistorial.FinalizadaSinOrden: resultado.OperacionesFinalizadasSinOrden++; break;
                        default: resultado.OperacionesFacturadasPendientes++; break;
                    }
                }
                catch (Exception ex)
                {
                    resultado.OperacionesOmitidas++;
                    resultado.Errores.Add($"Operación #{idOperacion}: {ex.Message}");
                    await _logger.LogErrorAsync($"Error procesando operación {idOperacion} en historial de cobranza", ex, "HistorialCobranzaService", "GenerarHistorialAsync");
                }
            }

            progreso?.Report("Generando reporte de cobranza...");
            try
            {
                var items = await _reporteService.ObtenerReporteCobranzaAsync(rfc, null, null, fechaInicio, fechaFin, cancellationToken);
                if (items != null && items.Count > 0)
                {
                    var rutaReporteCobranza = await _exportService.GenerarReporteCobranzaPdfAsync(items, rfc, null, fechaInicio, fechaFin, null);
                    CopiarArchivo(rutaReporteCobranza, carpetaRaiz);
                    resultado.ReporteCobranzaGenerado = true;
                }
            }
            catch (Exception ex)
            {
                resultado.Errores.Add($"Reporte de cobranza: {ex.Message}");
                await _logger.LogErrorAsync("Error generando el reporte de cobranza del historial", ex, "HistorialCobranzaService", "GenerarHistorialAsync");
            }

            progreso?.Report("Historial completo.");
            return resultado;
        }

        private enum CategoriaHistorial
        {
            Abierta,
            AbiertaSinOrden,
            FinalizadaConOrden,
            FinalizadaSinOrden,
            FacturadaPendiente
        }

        /// <summary>
        /// Categoría de la operación según los checks de Cobranza, o null si no cae en ninguna
        /// (facturada y ya pagada por completo).
        /// </summary>
        private static CategoriaHistorial? Clasificar(OperacionDto operacion, List<OperacionFacturadaDto>? facturas)
        {
            if (facturas is { Count: > 0 })
            {
                return facturas.Any(f => f.TotalAbonado < f.Total)
                    ? CategoriaHistorial.FacturadaPendiente
                    : null;
            }

            var conOrden = operacion.CkOrdenCompraCargadaApi;
            if (operacion.TFinalizado)
            {
                return conOrden ? CategoriaHistorial.FinalizadaConOrden : CategoriaHistorial.FinalizadaSinOrden;
            }

            return conOrden ? CategoriaHistorial.Abierta : CategoriaHistorial.AbiertaSinOrden;
        }

        private static bool EstaMarcada(CategoriaHistorial categoria, HistorialCobranzaEstadosDto estados) => categoria switch
        {
            CategoriaHistorial.Abierta => estados.Abiertas,
            CategoriaHistorial.AbiertaSinOrden => estados.AbiertasSinOrden,
            CategoriaHistorial.FinalizadaConOrden => estados.FinalizadaConOrden,
            CategoriaHistorial.FinalizadaSinOrden => estados.FinalizadaSinOrden,
            _ => estados.Pendientes
        };

        private static void CopiarArchivo(string? rutaOrigen, string carpetaDestino)
        {
            if (string.IsNullOrWhiteSpace(rutaOrigen) || !File.Exists(rutaOrigen))
            {
                return;
            }

            var destino = Path.Combine(carpetaDestino, Path.GetFileName(rutaOrigen));
            File.Copy(rutaOrigen, destino, overwrite: true);
        }

        private static string LimpiarNombreArchivo(string valor)
        {
            return string.Concat(valor.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
        }
    }
}
