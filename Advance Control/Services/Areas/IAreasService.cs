using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;

namespace Advance_Control.Services.Areas
{
    /// <summary>
    /// Interfaz para el servicio de áreas geográficas
    /// </summary>
    public interface IAreasService
    {
        /// <summary>
        /// Obtiene áreas con filtros opcionales
        /// </summary>
        Task<List<AreaDto>> GetAreasAsync(
            int? idArea = null,
            string? nombre = null,
            bool? activo = null,
            string? tipoGeometria = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtiene áreas en formato optimizado para Google Maps JavaScript API
        /// </summary>
        Task<List<GoogleMapsAreaDto>> GetAreasForGoogleMapsAsync(
            int? idArea = null,
            bool? activo = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Valida si un punto está dentro de un área
        /// </summary>
        Task<List<AreaValidationResultDto>> ValidatePointAsync(
            decimal latitud,
            decimal longitud,
            int? idArea = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Crea una nueva área geográfica
        /// </summary>
        Task<ApiResponse> CreateAreaAsync(
            AreaDto area,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Actualiza un área geográfica existente
        /// </summary>
        Task<ApiResponse> UpdateAreaAsync(
            int idArea,
            AreaDto area,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Elimina un área geográfica
        /// </summary>
        Task<ApiResponse> DeleteAreaAsync(
            int idArea,
            CancellationToken cancellationToken = default);
    }
}
