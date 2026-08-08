using System;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;

namespace Advance_Control.Services.Reportes
{
    /// <summary>
    /// Genera en disco el expediente completo de cobranza de un cliente en un rango de
    /// fechas: una subcarpeta por operación (Cotización, Reporte y, si ya se facturó,
    /// la Factura en PDF y XML) más el Reporte de Cobranza del mismo cliente/rango.
    /// </summary>
    public interface IHistorialCobranzaService
    {
        Task<HistorialCobranzaResultadoDto> GenerarHistorialAsync(
            string rfc,
            string nombreCliente,
            int idCliente,
            DateTimeOffset fechaInicio,
            DateTimeOffset fechaFin,
            string? dirigidoA,
            IProgress<string>? progreso = null,
            CancellationToken cancellationToken = default);
    }
}
