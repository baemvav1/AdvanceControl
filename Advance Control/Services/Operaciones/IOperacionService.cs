using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;

namespace Advance_Control.Services.Operaciones
{
    /// <summary>
    /// Servicio para gestionar operaciones del sistema
    /// </summary>
    public interface IOperacionService
    {
        /// <summary>
        /// Obtiene una lista de operaciones según los criterios de búsqueda proporcionados
        /// </summary>
        /// <param name="query">Parámetros de búsqueda opcionales</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Lista de operaciones que cumplen con los criterios</returns>
        Task<List<OperacionDto>> GetOperacionesAsync(OperacionQueryDto? query = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Elimina (soft delete) una operación por su ID
        /// </summary>
        Task<bool> DeleteOperacionAsync(int idOperacion, CancellationToken cancellationToken = default);

        /// <summary>
        /// Actualiza una operación (monto, fechaFinal, etc.)
        /// </summary>
        Task<bool> UpdateOperacionAsync(int idOperacion, int idTipo = 0, int idCliente = 0, int idEquipo = 0, int idAtiende = 0, decimal monto = 0, string? nota = null, DateTime? fechaFinal = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Reabre una operación limpiando su fechaFinal
        /// </summary>
        Task<bool> ReopenOperacionAsync(int idOperacion, CancellationToken cancellationToken = default);

        /// <summary>
        /// Marca el trabajo técnico de una operación como finalizado (t_finalizado = TRUE)
        /// </summary>
        Task<bool> FinalizarTrabajoAsync(int idOperacion, CancellationToken cancellationToken = default);

        /// <summary>
        /// Desmarca el trabajo técnico de una operación como finalizado (t_finalizado = FALSE)
        /// </summary>
        Task<bool> DesfinalizarTrabajoAsync(int idOperacion, CancellationToken cancellationToken = default);
    }
}
