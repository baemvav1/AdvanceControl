using System.Collections.Generic;
using System.Threading.Tasks;
using Advance_Control.Models;

namespace Advance_Control.Services.Reportes
{
    public interface ILevantamientoReportService
    {
        /// <summary>
        /// Genera un reporte PDF del levantamiento y retorna la ruta del archivo generado.
        /// </summary>
        Task<string> GenerarReportePdfAsync(
            int idLevantamiento,
            string? equipoIdentificador,
            string? equipoMarca,
            string? introduccion,
            string? conclusion,
            IReadOnlyList<LevantamientoTreeItemModel> nodos);
    }
}
