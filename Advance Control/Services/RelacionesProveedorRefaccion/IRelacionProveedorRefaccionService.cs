using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;

namespace Advance_Control.Services.RelacionesProveedorRefaccion
{
    /// <summary>
    /// Servicio para gestionar operaciones con relaciones proveedor-refacción
    /// </summary>
    public interface IRelacionProveedorRefaccionService
    {
        /// <summary>
        /// Obtiene una lista de relaciones refacción para un ID de proveedor
        /// </summary>
        /// <param name="idProveedor">ID del proveedor</param>
        /// <param name="idRefaccion">ID de la refacción para filtrar (0 para no filtrar)</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Lista de relaciones refacción</returns>
        Task<List<RelacionProveedorRefaccionDto>> GetRelacionesAsync(int idProveedor, int idRefaccion = 0, CancellationToken cancellationToken = default);

        /// <summary>
        /// Elimina (soft delete) una relación proveedor-refacción
        /// </summary>
        /// <param name="idRelacionProveedor">ID de la relación proveedor</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>True si la eliminación fue exitosa, False en caso contrario</returns>
        Task<bool> DeleteRelacionAsync(int idRelacionProveedor, CancellationToken cancellationToken = default);

        /// <summary>
        /// Actualiza la nota de una relación proveedor-refacción
        /// </summary>
        /// <param name="idRelacionProveedor">ID de la relación proveedor</param>
        /// <param name="nota">Nueva nota (puede ser null o vacía)</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>True si la actualización fue exitosa, False en caso contrario</returns>
        Task<bool> UpdateNotaAsync(int idRelacionProveedor, string? nota, CancellationToken cancellationToken = default);

        /// <summary>
        /// Actualiza el precio de una relación proveedor-refacción
        /// </summary>
        /// <param name="idRelacionProveedor">ID de la relación proveedor</param>
        /// <param name="precio">Nuevo precio</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>True si la actualización fue exitosa, False en caso contrario</returns>
        Task<bool> UpdatePrecioAsync(int idRelacionProveedor, double precio, CancellationToken cancellationToken = default);

        /// <summary>
        /// Crea una nueva relación proveedor-refacción
        /// </summary>
        /// <param name="idProveedor">ID del proveedor</param>
        /// <param name="idRefaccion">ID de la refacción</param>
        /// <param name="precio">Precio de la refacción</param>
        /// <param name="nota">Nota asociada a la relación (opcional)</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>True si la creación fue exitosa, False en caso contrario</returns>
        Task<bool> CreateRelacionAsync(int idProveedor, int idRefaccion, double precio, string? nota, CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtiene proveedores que tienen una refacción específica con sus precios
        /// </summary>
        /// <param name="idRefaccion">ID de la refacción</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Lista de proveedores con sus precios para la refacción</returns>
        Task<List<ProveedorPorRefaccionDto>> GetProveedoresByRefaccionAsync(int idRefaccion, CancellationToken cancellationToken = default);
    }
}
