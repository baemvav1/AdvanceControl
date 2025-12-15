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
        /// <param name="idOperacion">ID de la operación a eliminar</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Resultado de la operación</returns>
        Task<bool> DeleteOperacionAsync(int idOperacion, CancellationToken cancellationToken = default);
    }
}
