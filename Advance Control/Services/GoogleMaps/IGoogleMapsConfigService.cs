using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;

namespace Advance_Control.Services.GoogleMaps
{
    /// <summary>
    /// Interfaz para el servicio de configuración de Google Maps
    /// </summary>
    public interface IGoogleMapsConfigService
    {
        /// <summary>
        /// Obtiene la clave de API de Google Maps
        /// </summary>
        Task<string?> GetApiKeyAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtiene toda la configuración de Google Maps (clave API, centro predeterminado y zoom)
        /// </summary>
        Task<GoogleMapsConfigDto?> GetConfigAsync(CancellationToken cancellationToken = default);
    }
}
