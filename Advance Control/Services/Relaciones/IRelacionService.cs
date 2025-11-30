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
    }
}
