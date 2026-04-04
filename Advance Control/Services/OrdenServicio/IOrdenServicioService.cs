using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;

namespace Advance_Control.Services.OrdenServicio
{
    /// <summary>
    /// Servicio para gestionar operaciones con órdenes de servicio
    /// </summary>
    public interface IOrdenServicioService
    {
        /// <summary>
        /// Obtiene una lista de órdenes de servicio según los criterios de búsqueda proporcionados
        /// </summary>
        /// <param name="query">Parámetros de búsqueda opcionales</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Lista de órdenes de servicio que cumplen con los criterios</returns>
        Task<List<OrdenServicioDto>> GetOrdenesServicioAsync(OrdenServicioQueryDto? query = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Elimina (soft delete) una orden de servicio por su ID
        /// </summary>
        /// <param name="idOrdenServicio">ID de la orden de servicio a eliminar</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Resultado de la operación</returns>
        Task<bool> DeleteOrdenServicioAsync(int idOrdenServicio, CancellationToken cancellationToken = default);

        /// <summary>
        /// Crea una nueva orden de servicio
        /// </summary>
        /// <param name="idTipoMantenimiento">ID del tipo de mantenimiento (obligatorio)</param>
        /// <param name="idCliente">ID del cliente (obligatorio)</param>
        /// <param name="idEquipo">ID del equipo (obligatorio)</param>
        /// <param name="nota">Nota asociada a la orden de servicio (opcional)</param>
        /// <param name="credencialId">ID de credencial del usuario que crea la orden</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Resultado de la operación</returns>
        Task<bool> CreateOrdenServicioAsync(int idTipoMantenimiento, int idCliente, int idEquipo, string? nota = null, int credencialId = 0, CancellationToken cancellationToken = default);

        /// <summary>
        /// Actualiza el estado de atendido de una orden de servicio
        /// </summary>
        /// <param name="idOrdenServicio">ID de la orden de servicio (obligatorio)</param>
        /// <param name="idAtendio">ID del usuario que atendió (obligatorio)</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Resultado de la operación</returns>
        Task<bool> UpdateAtendidoAsync(int idOrdenServicio, int idAtendio, CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtiene los técnicos disponibles para atender una orden de servicio,
        /// filtrados por el área del equipo asociado
        /// </summary>
        /// <param name="identificador">Identificador del equipo</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Lista de técnicos disponibles (nivel TecSup o Tecnico)</returns>
        Task<List<TecnicoDisponibleDto>> GetTecnicosDisponiblesAsync(string identificador, CancellationToken cancellationToken = default);
    }
}
