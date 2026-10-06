using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;

namespace Advance_Control.Services.Portal
{
    /// <summary>
    /// Logins del Portal de Clientes de una empresa (api/Clientes/{id}/logins).
    /// Si la API rechaza la operación lanza InvalidOperationException con el
    /// mensaje que hay que mostrar al usuario.
    /// </summary>
    public interface IClienteLoginService
    {
        Task<List<ClienteLoginDto>> ObtenerAsync(int idCliente, CancellationToken cancellationToken = default);

        /// <summary>Crea el login; la contraseña la genera el servidor y se devuelve una sola vez.</summary>
        Task<ClienteLoginPasswordDto> CrearAsync(int idCliente, long contactoId, string usuario, CancellationToken cancellationToken = default);

        /// <summary>Genera una contraseña nueva (devuelta una sola vez) y cierra las sesiones abiertas.</summary>
        Task<ClienteLoginPasswordDto> RestablecerAsync(int idCliente, long loginId, CancellationToken cancellationToken = default);

        Task<ClienteLoginDto> MarcarDatosEnviadosAsync(int idCliente, long loginId, CancellationToken cancellationToken = default);

        Task EliminarAsync(int idCliente, long loginId, CancellationToken cancellationToken = default);
    }
}
