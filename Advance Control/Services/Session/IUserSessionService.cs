using System.Threading;
using System.Threading.Tasks;

namespace Advance_Control.Services.Session
{
    /// <summary>
    /// Servicio singleton que almacena la información del usuario autenticado en memoria.
    /// Se carga una sola vez después del login y es accesible desde cualquier parte del código.
    /// </summary>
    public interface IUserSessionService
    {
        /// <summary>ID del usuario autenticado (equivalente a CredencialId)</summary>
        int IdUsuario { get; }

        /// <summary>ID de credencial del usuario autenticado</summary>
        int CredencialId { get; }

        /// <summary>ID del proveedor asociado al usuario</summary>
        int IdProveedor { get; }

        /// <summary>Nombre completo del usuario</summary>
        string? NombreCompleto { get; }

        /// <summary>Correo electrónico del usuario</summary>
        string? Correo { get; }

        /// <summary>Teléfono del usuario</summary>
        string? Telefono { get; }

        /// <summary>Nivel de acceso del usuario</summary>
        int Nivel { get; }

        /// <summary>Tipo de usuario</summary>
        string? TipoUsuario { get; }

        /// <summary>Indica si la sesión ya fue cargada desde el API</summary>
        bool IsLoaded { get; }

        /// <summary>
        /// Carga la información del usuario desde el API.
        /// Debe llamarse una sola vez después del login exitoso o de restaurar la sesión.
        /// </summary>
        Task LoadAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Limpia la información de la sesión.
        /// Debe llamarse al cerrar sesión.
        /// </summary>
        void Clear();
    }
}
