using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Advance_Control.Models;

namespace Advance_Control.Services.Reportes
{
    public interface IReporteFinancieroFacturacionExportService
    {
        Task<string> GenerarReportePdfAsync(
            IReadOnlyList<ReporteFinancieroFacturacionCabeceraDto> cabeceras,
            IReadOnlyList<ReporteFinancieroFacturacionDetalleDto> detalles,
            string? receptorRfcFiltro,
            string? referenciaFiltro,
            DateTimeOffset? fechaInicioFiltro,
            DateTimeOffset? fechaFinFiltro,
            bool? finiquitoFiltro,
            int movimientosNcCount,
            decimal movimientosNcTotal,
            bool mostrarMovimientosNc = true);

        Task<string> GenerarReporteSimplificadoPdfAsync(
            IReadOnlyList<ReporteFinancieroFacturacionCabeceraDto> cabeceras,
            IReadOnlyList<ReporteFinancieroFacturacionDetalleDto> detalles,
            string? receptorRfcFiltro,
            string? referenciaFiltro,
            DateTimeOffset? fechaInicioFiltro,
            DateTimeOffset? fechaFinFiltro,
            bool? finiquitoFiltro,
            int movimientosNcCount,
            decimal movimientosNcTotal,
            bool mostrarMovimientosNc = true);
    }
}
