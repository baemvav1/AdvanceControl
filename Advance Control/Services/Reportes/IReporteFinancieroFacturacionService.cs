using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;

namespace Advance_Control.Services.Reportes
{
    public interface IReporteFinancieroFacturacionService
    {
        Task<ReporteFinancieroFacturacionResponseDto> ObtenerReporteAsync(
            string? receptorRfc,
            bool? finiquito,
            string? referencia,
            DateTimeOffset? fechaInicio,
            DateTimeOffset? fechaFin,
            CancellationToken cancellationToken = default);
    }
}
