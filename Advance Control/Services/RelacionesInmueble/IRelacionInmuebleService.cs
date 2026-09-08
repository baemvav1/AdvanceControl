using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;

namespace Advance_Control.Services.RelacionesInmueble
{
    /// <summary>
    /// Servicio para gestionar operaciones con relaciones inmueble-cliente
    /// </summary>
    public interface IRelacionInmuebleService
    {
        /// <summary>
        /// Obtiene una lista de relaciones cliente para un identificador de inmueble
        /// </summary>
        Task<List<RelacionClienteDto>> GetRelacionesAsync(string identificador, int idCliente = 0, CancellationToken cancellationToken = default);

        /// <summary>
        /// Elimina (soft delete) una relación inmueble-cliente
        /// </summary>
        Task<bool> DeleteRelacionAsync(string identificador, int idCliente, CancellationToken cancellationToken = default);

        /// <summary>
        /// Actualiza la nota de una relación inmueble-cliente
        /// </summary>
        Task<bool> UpdateNotaAsync(string identificador, int idCliente, string? nota, CancellationToken cancellationToken = default);

        /// <summary>
        /// Crea una nueva relación inmueble-cliente
        /// </summary>
        Task<bool> CreateRelacionAsync(string identificador, int idCliente, string? nota, CancellationToken cancellationToken = default);
    }
}
