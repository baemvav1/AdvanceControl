using System.Threading;
using System.Threading.Tasks;

namespace Advance_Control.Services.GoogleCloudStorage
{
    /// <summary>
    /// Interfaz para el servicio de autenticación OAuth 2.0 con Google Cloud Storage.
    /// Maneja la obtención y renovación de tokens de acceso para operaciones de almacenamiento.
    /// </summary>
    public interface IGoogleCloudStorageAuthService
    {
        /// <summary>
        /// Obtiene un token de acceso válido para Google Cloud Storage.
        /// Si el token actual ha expirado, lo renueva automáticamente.
        /// </summary>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Token de acceso válido, o null si no se puede obtener</returns>
        Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Indica si el usuario está autenticado con Google Cloud Storage
        /// </summary>
        bool IsAuthenticated { get; }

        /// <summary>
        /// Inicia el flujo de autenticación OAuth 2.0 con Google.
        /// Abre el navegador para que el usuario autorice la aplicación.
        /// </summary>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>True si la autenticación fue exitosa</returns>
        Task<bool> AuthenticateAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Limpia los tokens almacenados y cierra la sesión de Google Cloud Storage
        /// </summary>
        Task ClearAuthenticationAsync();

        /// <summary>
        /// Intenta restaurar la sesión desde tokens almacenados
        /// </summary>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>True si la sesión fue restaurada exitosamente</returns>
        Task<bool> TryRestoreSessionAsync(CancellationToken cancellationToken = default);
    }
}
