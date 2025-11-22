using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;

namespace Advance_Control.Services.UserInfo
{
    /// <summary>
    /// Servicio para obtener información del usuario autenticado
    /// </summary>
    public interface IUserInfoService
    {
        /// <summary>
        /// Obtiene la información del usuario autenticado actual
        /// </summary>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Información del usuario o null si no se pudo obtener</returns>
        Task<UserInfoDto?> GetUserInfoAsync(CancellationToken cancellationToken = default);
    }
}
