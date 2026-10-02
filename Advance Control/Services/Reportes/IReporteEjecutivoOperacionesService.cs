using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;

namespace Advance_Control.Services.Reportes
{
    public interface IReporteEjecutivoOperacionesService
    {
        /// <summary>
        /// Genera el "Reporte Ejecutivo de Operaciones" (portada con resumen e índice + una sección por
        /// operación con hojas de servicio, cargos sin precios y evidencias) y devuelve la ruta del PDF.
        /// </summary>
        Task<string> GenerarAsync(
            IReadOnlyList<OperacionDto> operaciones,
            OperacionesReporteFiltrosDto filtros,
            IProgress<string>? progreso = null,
            CancellationToken cancellationToken = default);
    }
}
