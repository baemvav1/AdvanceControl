using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;

namespace Advance_Control.Services.Equipos
{
    /// <summary>
    /// Servicio para gestionar operaciones con equipos
    /// </summary>
    public interface IEquipoService
    {
        /// <summary>
        /// Obtiene una lista de equipos según los criterios de búsqueda proporcionados
        /// </summary>
        /// <param name="query">Parámetros de búsqueda opcionales</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Lista de equipos que cumplen con los criterios</returns>
        Task<List<EquipoDto>> GetEquiposAsync(EquipoQueryDto? query = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Elimina (soft delete) un equipo por su ID
        /// </summary>
        /// <param name="id">ID del equipo a eliminar</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Resultado de la operación</returns>
        Task<bool> DeleteEquipoAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Actualiza un equipo existente
        /// </summary>
        /// <param name="id">ID del equipo a actualizar</param>
        /// <param name="query">Datos a actualizar</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Resultado de la operación</returns>
        Task<bool> UpdateEquipoAsync(int id, EquipoQueryDto query, CancellationToken cancellationToken = default);
    }
}
