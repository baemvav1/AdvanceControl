using AdvanceApi.Models;

namespace AdvanceApi.Services
{
    /// <summary>
    /// Interface para el servicio que obtiene información de contacto del usuario
    /// </summary>
    public interface IContactoUsuarioService
    {
        /// <summary>
        /// Obtiene la información de contacto del usuario por su nombre de usuario
        /// </summary>
        /// <param name="username">Nombre de usuario</param>
        /// <returns>Información del usuario o null si no se encuentra</returns>
        Task<UserInfoDto?> GetContactoUsuarioAsync(string username);
    }
}
