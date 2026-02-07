using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;

namespace Advance_Control.Services.Contactos
{
    /// <summary>
    /// Servicio para gestionar operaciones con contactos
    /// </summary>
    public interface IContactoService
    {
        /// <summary>
        /// Obtiene una lista de contactos según los criterios de búsqueda proporcionados
        /// </summary>
        /// <param name="query">Parámetros de búsqueda opcionales</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Lista de contactos que cumplen con los criterios</returns>
        Task<List<ContactoDto>> GetContactosAsync(ContactoQueryDto? query = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Crea un nuevo contacto
        /// </summary>
        /// <param name="query">Datos del contacto a crear</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Resultado de la operación</returns>
        Task<ContactoOperationResponse> CreateContactoAsync(ContactoEditDto query, CancellationToken cancellationToken = default);

        /// <summary>
        /// Actualiza un contacto por su ID
        /// </summary>
        /// <param name="query">Datos del contacto a actualizar</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Resultado de la operación</returns>
        Task<ContactoOperationResponse> UpdateContactoAsync(ContactoEditDto query, CancellationToken cancellationToken = default);

        /// <summary>
        /// Elimina (soft delete) un contacto por su ID
        /// </summary>
        /// <param name="contactoId">ID del contacto a eliminar</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Resultado de la operación</returns>
        Task<ContactoOperationResponse> DeleteContactoAsync(long contactoId, CancellationToken cancellationToken = default);
    }
}
