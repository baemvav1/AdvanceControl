using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;

namespace Advance_Control.Services.RelacionesRefaccionEquipo
{
    /// <summary>
    /// Servicio para gestionar operaciones con relaciones refacción-equipo
    /// </summary>
    public interface IRelacionRefaccionEquipoService
    {
        /// <summary>
        /// Obtiene una lista de relaciones equipo para un ID de refacción
        /// </summary>
        /// <param name="idRefaccion">ID de la refacción</param>
        /// <param name="idEquipo">ID del equipo para filtrar (0 para no filtrar)</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Lista de relaciones equipo</returns>
        Task<List<RelacionEquipoDto>> GetRelacionesAsync(int idRefaccion, int idEquipo = 0, CancellationToken cancellationToken = default);

        /// <summary>
        /// Elimina (soft delete) una relación refacción-equipo
        /// </summary>
        /// <param name="idRelacionRefaccion">ID de la relación refacción</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>True si la eliminación fue exitosa, False en caso contrario</returns>
        Task<bool> DeleteRelacionAsync(int idRelacionRefaccion, CancellationToken cancellationToken = default);

        /// <summary>
        /// Actualiza la nota de una relación refacción-equipo
        /// </summary>
        /// <param name="idRelacionRefaccion">ID de la relación refacción</param>
        /// <param name="nota">Nueva nota (puede ser null o vacía)</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>True si la actualización fue exitosa, False en caso contrario</returns>
        Task<bool> UpdateNotaAsync(int idRelacionRefaccion, string? nota, CancellationToken cancellationToken = default);

        /// <summary>
        /// Crea una nueva relación refacción-equipo
        /// </summary>
        /// <param name="idRefaccion">ID de la refacción</param>
        /// <param name="idEquipo">ID del equipo</param>
        /// <param name="nota">Nota asociada a la relación (opcional)</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>True si la creación fue exitosa, False en caso contrario</returns>
        Task<bool> CreateRelacionAsync(int idRefaccion, int idEquipo, string? nota, CancellationToken cancellationToken = default);
    }
}
