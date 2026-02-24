using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;

namespace Advance_Control.Services.Proveedores
{
    /// <summary>
    /// Servicio para gestionar operaciones con proveedores
    /// </summary>
    public interface IProveedorService
    {
        /// <summary>
        /// Obtiene una lista de proveedores según los criterios de búsqueda proporcionados
        /// </summary>
        /// <param name="query">Parámetros de búsqueda opcionales</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Lista de proveedores que cumplen con los criterios</returns>
        Task<List<ProveedorDto>> GetProveedoresAsync(ProveedorQueryDto? query = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Crea un nuevo proveedor
        /// </summary>
        Task<bool> CreateProveedorAsync(string rfc, string? razonSocial, string? nombreComercial, string? nota, CancellationToken cancellationToken = default);

        /// <summary>
        /// Actualiza un proveedor existente
        /// </summary>
        Task<bool> UpdateProveedorAsync(int id, string? rfc, string? razonSocial, string? nombreComercial, string? nota, CancellationToken cancellationToken = default);

        /// <summary>
        /// Elimina (soft delete) un proveedor por su ID
        /// </summary>
        Task<bool> DeleteProveedorAsync(int id, CancellationToken cancellationToken = default);
    }
}
