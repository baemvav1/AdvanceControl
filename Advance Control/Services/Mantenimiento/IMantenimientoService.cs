using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;

namespace Advance_Control.Services.Mantenimiento
{
    /// <summary>
    /// Servicio para gestionar operaciones con mantenimientos
    /// </summary>
    public interface IMantenimientoService
    {
        /// <summary>
        /// Obtiene una lista de mantenimientos según los criterios de búsqueda proporcionados
        /// </summary>
        /// <param name="query">Parámetros de búsqueda opcionales</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Lista de mantenimientos que cumplen con los criterios</returns>
        Task<List<MantenimientoDto>> GetMantenimientosAsync(MantenimientoQueryDto? query = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Elimina (soft delete) un mantenimiento por su ID
        /// </summary>
        /// <param name="idMantenimiento">ID del mantenimiento a eliminar</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Resultado de la operación</returns>
        Task<bool> DeleteMantenimientoAsync(int idMantenimiento, CancellationToken cancellationToken = default);

        /// <summary>
        /// Crea un nuevo mantenimiento
        /// </summary>
        /// <param name="idTipoMantenimiento">ID del tipo de mantenimiento (obligatorio)</param>
        /// <param name="idCliente">ID del cliente (obligatorio)</param>
        /// <param name="idEquipo">ID del equipo (obligatorio)</param>
        /// <param name="nota">Nota asociada al mantenimiento (opcional)</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Resultado de la operación</returns>
        Task<bool> CreateMantenimientoAsync(int idTipoMantenimiento, int idCliente, int idEquipo, string? nota = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Actualiza el estado de atendido de un mantenimiento
        /// </summary>
        /// <param name="idMantenimiento">ID del mantenimiento (obligatorio)</param>
        /// <param name="idAtendio">ID del usuario que atendió (obligatorio)</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Resultado de la operación</returns>
        Task<bool> UpdateAtendidoAsync(int idMantenimiento, int idAtendio, CancellationToken cancellationToken = default);
    }
}
