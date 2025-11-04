using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Advance_Control.Services.Auth
{
    public interface IAuthService
    {
        /// <summary>
        /// Realiza autenticación con credenciales y almacena tokens seguros.
        /// </summary>
        Task<bool> AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default);

        /// <summary>
        /// Devuelve el access token válido (refresca si es necesario). Puede devolver null si no está autenticado.
        /// </summary>
        Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Intenta refrescar el token mediante refresh token. Devuelve true si se obtuvo nuevo access token.
        /// </summary>
        Task<bool> RefreshTokenAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Valida que el token actual sea válido (opcionalmente llamando al servidor).
        /// </summary>
        Task<bool> ValidateTokenAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Limpia cualquier token y estado de autenticación almacenado.
        /// </summary>
        Task ClearTokenAsync();

        /// <summary>
        /// Indica si actualmente hay un token válido.
        /// </summary>
        bool IsAuthenticated { get; }
    }
}
