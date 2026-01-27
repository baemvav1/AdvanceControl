using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;

namespace Advance_Control.Services.Clientes
{
    /// <summary>
    /// Servicio para gestionar operaciones con clientes
    /// </summary>
    public interface IClienteService
    {
        /// <summary>
        /// Obtiene una lista de clientes según los criterios de búsqueda proporcionados
        /// </summary>
        /// <param name="query">Parámetros de búsqueda opcionales</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Lista de clientes que cumplen con los criterios</returns>
        Task<List<CustomerDto>> GetClientesAsync(ClienteQueryDto? query = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Crea un nuevo cliente usando el procedimiento almacenado sp_cliente_edit
        /// </summary>
        /// <param name="query">Datos del cliente a crear</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Resultado de la operación</returns>
        Task<object> CreateClienteAsync(ClienteEditDto query, CancellationToken cancellationToken = default);

        /// <summary>
        /// Actualiza un cliente por su ID
        /// </summary>
        /// <param name="query">Datos del cliente a actualizar</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Resultado de la operación</returns>
        Task<object> UpdateClienteAsync(ClienteEditDto query, CancellationToken cancellationToken = default);

        /// <summary>
        /// Elimina (soft delete) un cliente por su ID
        /// </summary>
        /// <param name="idCliente">ID del cliente a eliminar</param>
        /// <param name="idUsuario">ID del usuario que realiza la operación</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Resultado de la operación</returns>
        Task<object> DeleteClienteAsync(int idCliente, int? idUsuario, CancellationToken cancellationToken = default);
    }
}
