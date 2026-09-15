using System.Collections.Generic;
using System.Threading.Tasks;
using Advance_Control.Models;

namespace Advance_Control.Services.Reportes
{
    public interface IOperacionesReporteExportService
    {
        /// <summary>
        /// Genera un PDF tabular con las operaciones dadas (todo el conjunto filtrado,
        /// no solo la página en pantalla), citando qué checks de checks_operacion
        /// tiene marcados cada una, junto con los filtros que produjeron ese conjunto.
        /// </summary>
        Task<string> GenerarReporteOperacionesPdfAsync(
            IReadOnlyList<OperacionDto> operaciones,
            OperacionesReporteFiltrosDto filtros);
    }
}
