using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;

namespace Advance_Control.Services.Productos
{
    /// <summary>
    /// Interfaz para el servicio de productos (materiales/insumos suministrados al cliente final)
    /// </summary>
    public interface IProductoService
    {
        /// <summary>
        /// Obtiene una lista de productos según los criterios de búsqueda proporcionados
        /// </summary>
        Task<List<ProductoDto>> GetProductosAsync(ProductoQueryDto? query = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Elimina (soft delete) un producto por su ID
        /// </summary>
        Task<bool> DeleteProductoAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Actualiza un producto existente
        /// </summary>
        Task<bool> UpdateProductoAsync(int id, ProductoQueryDto query, CancellationToken cancellationToken = default);

        /// <summary>
        /// Crea un nuevo producto
        /// </summary>
        Task<bool> CreateProductoAsync(string concepto, string descripcion, double costoDirecto, double porcentajeUtilidad, bool estatus = true, CancellationToken cancellationToken = default);
    }
}
