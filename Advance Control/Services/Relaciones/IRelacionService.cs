using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;

namespace Advance_Control.Services.Relaciones
{
    /// <summary>
    /// Servicio para gestionar operaciones con relaciones equipo-cliente
    /// </summary>
    public interface IRelacionService
    {
        /// <summary>
        /// Obtiene una lista de relaciones cliente para un identificador de equipo
        /// </summary>
        /// <param name="identificador">Identificador del equipo</param>
        /// <param name="idCliente">ID del cliente para filtrar (0 para no filtrar)</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Lista de relaciones cliente</returns>
        Task<List<RelacionClienteDto>> GetRelacionesAsync(string identificador, int idCliente = 0, CancellationToken cancellationToken = default);

        /// <summary>
        /// Elimina (soft delete) una relación equipo-cliente
        /// </summary>
        /// <param name="identificador">Identificador del equipo</param>
        /// <param name="idCliente">ID del cliente</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>True si la eliminación fue exitosa, False en caso contrario</returns>
        Task<bool> DeleteRelacionAsync(string identificador, int idCliente, CancellationToken cancellationToken = default);

        /// <summary>
        /// Actualiza la nota de una relación equipo-cliente
        /// </summary>
        /// <param name="identificador">Identificador del equipo</param>
        /// <param name="idCliente">ID del cliente</param>
        /// <param name="nota">Nueva nota (puede ser null o vacía)</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>True si la actualización fue exitosa, False en caso contrario</returns>
        Task<bool> UpdateNotaAsync(string identificador, int idCliente, string? nota, CancellationToken cancellationToken = default);

        /// <summary>
        /// Crea una nueva relación equipo-cliente
        /// </summary>
        /// <param name="identificador">Identificador del equipo</param>
        /// <param name="idCliente">ID del cliente</param>
        /// <param name="nota">Nota asociada a la relación (opcional)</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>True si la creación fue exitosa, False en caso contrario</returns>
        Task<bool> CreateRelacionAsync(string identificador, int idCliente, string? nota, CancellationToken cancellationToken = default);
    }
}
