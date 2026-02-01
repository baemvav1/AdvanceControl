using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;

namespace Advance_Control.Services.Ubicaciones
{
    /// <summary>
    /// Interfaz para el servicio de ubicaciones de Google Maps
    /// </summary>
    public interface IUbicacionService
    {
        /// <summary>
        /// Obtiene todas las ubicaciones activas
        /// </summary>
        Task<List<UbicacionDto>> GetUbicacionesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtiene una ubicación específica por su ID
        /// </summary>
        Task<UbicacionDto?> GetUbicacionByIdAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Busca una ubicación por nombre exacto
        /// </summary>
        Task<UbicacionDto?> GetUbicacionByNombreAsync(string nombre, CancellationToken cancellationToken = default);

        /// <summary>
        /// Crea una nueva ubicación
        /// </summary>
        Task<ApiResponse> CreateUbicacionAsync(UbicacionDto ubicacion, CancellationToken cancellationToken = default);

        /// <summary>
        /// Actualiza una ubicación existente
        /// </summary>
        Task<ApiResponse> UpdateUbicacionAsync(int id, UbicacionDto ubicacion, CancellationToken cancellationToken = default);

        /// <summary>
        /// Elimina una ubicación
        /// </summary>
        Task<ApiResponse> DeleteUbicacionAsync(int id, CancellationToken cancellationToken = default);
    }
}
