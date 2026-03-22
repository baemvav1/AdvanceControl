using Advance_Control.Models;
using System.Threading;
using System.Threading.Tasks;

namespace Advance_Control.Services.CorreoUsuario
{
    public interface ICorreoUsuarioService
    {
        Task<CorreoUsuarioDto?> GetCorreoActualAsync(CancellationToken cancellationToken = default);
        Task<CorreoUsuarioDto?> GetCorreoUsuarioAsync(long credencialId, CancellationToken cancellationToken = default);
        Task<CorreoUsuarioOperationResponse> SaveCorreoUsuarioAsync(long credencialId, CorreoUsuarioEditDto request, CancellationToken cancellationToken = default);
        Task<CorreoUsuarioOperationResponse> DeleteCorreoUsuarioAsync(long credencialId, CancellationToken cancellationToken = default);
    }
}
