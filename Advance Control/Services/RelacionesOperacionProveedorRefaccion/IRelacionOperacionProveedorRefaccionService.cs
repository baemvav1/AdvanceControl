using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;

namespace Advance_Control.Services.RelacionesOperacionProveedorRefaccion
{
    /// <summary>
    /// Servicio para gestionar operaciones con relaciones operación-proveedor-refacción
    /// </summary>
    public interface IRelacionOperacionProveedorRefaccionService
    {
        /// <summary>
        /// Obtiene una lista de relaciones refacción para un ID de operación
        /// </summary>
        /// <param name="idOperacion">ID de la operación</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Lista de relaciones refacción</returns>
        Task<List<RelacionOperacionProveedorRefaccionDto>> GetRelacionesAsync(int idOperacion, CancellationToken cancellationToken = default);

        /// <summary>
        /// Elimina (soft delete) una relación operación-proveedor-refacción
        /// </summary>
        /// <param name="idRelacionOperacionProveedorRefaccion">ID de la relación</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>True si la eliminación fue exitosa, False en caso contrario</returns>
        Task<bool> DeleteRelacionAsync(int idRelacionOperacionProveedorRefaccion, CancellationToken cancellationToken = default);

        /// <summary>
        /// Actualiza la nota de una relación operación-proveedor-refacción
        /// </summary>
        /// <param name="idRelacionOperacionProveedorRefaccion">ID de la relación</param>
        /// <param name="nota">Nueva nota (puede ser null o vacía)</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>True si la actualización fue exitosa, False en caso contrario</returns>
        Task<bool> UpdateNotaAsync(int idRelacionOperacionProveedorRefaccion, string? nota, CancellationToken cancellationToken = default);

        /// <summary>
        /// Crea una nueva relación operación-proveedor-refacción
        /// </summary>
        /// <param name="idOperacion">ID de la operación</param>
        /// <param name="idProveedorRefaccion">ID del proveedor refacción</param>
        /// <param name="precio">Precio de la refacción</param>
        /// <param name="nota">Nota asociada a la relación (opcional)</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>True si la creación fue exitosa, False en caso contrario</returns>
        Task<bool> CreateRelacionAsync(int idOperacion, int idProveedorRefaccion, double precio, string? nota, CancellationToken cancellationToken = default);
    }
}
