using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;

namespace Advance_Control.Services.Refacciones
{
    /// <summary>
    /// Servicio para gestionar operaciones con refacciones
    /// </summary>
    public interface IRefaccionService
    {
        /// <summary>
        /// Obtiene una lista de refacciones según los criterios de búsqueda proporcionados
        /// </summary>
        /// <param name="query">Parámetros de búsqueda opcionales</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Lista de refacciones que cumplen con los criterios</returns>
        Task<List<RefaccionDto>> GetRefaccionesAsync(RefaccionQueryDto? query = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Elimina (soft delete) una refacción por su ID
        /// </summary>
        /// <param name="id">ID de la refacción a eliminar</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Resultado de la operación</returns>
        Task<bool> DeleteRefaccionAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Actualiza una refacción existente
        /// </summary>
        /// <param name="id">ID de la refacción a actualizar</param>
        /// <param name="query">Datos a actualizar</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Resultado de la operación</returns>
        Task<bool> UpdateRefaccionAsync(int id, RefaccionQueryDto query, CancellationToken cancellationToken = default);

        /// <summary>
        /// Crea una nueva refacción
        /// </summary>
        /// <param name="marca">Marca de la refacción</param>
        /// <param name="serie">Serie de la refacción</param>
        /// <param name="costo">Costo de la refacción</param>
        /// <param name="descripcion">Descripción de la refacción</param>
        /// <param name="estatus">Estatus de la refacción (opcional, default true)</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Resultado de la operación</returns>
        Task<bool> CreateRefaccionAsync(string? marca, string? serie, double? costo, string? descripcion, bool estatus = true, CancellationToken cancellationToken = default);
    }
}
