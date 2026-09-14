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

        /// <summary>
        /// Lista las empresas (Cliente) vinculadas a un contacto (universo,
        /// no solo las visibles en el portal).
        /// </summary>
        Task<List<ClientePortalSeleccionDto>> ObtenerClientesVinculadosAsync(long contactoId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Vincula un contacto a una empresa (Cliente). No lo desvincula de
        /// ninguna otra empresa.
        /// </summary>
        Task<ContactoOperationResponse> VincularClienteAsync(long contactoId, int idCliente, CancellationToken cancellationToken = default);

        /// <summary>
        /// Desvincula un contacto de UNA empresa puntual.
        /// </summary>
        Task<ContactoOperationResponse> DesvincularClienteAsync(long contactoId, int idCliente, CancellationToken cancellationToken = default);
    }
}
