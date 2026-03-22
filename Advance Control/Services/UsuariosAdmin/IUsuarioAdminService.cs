using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;

namespace Advance_Control.Services.UsuariosAdmin
{
    public interface IUsuarioAdminService
    {
        Task<List<UsuarioAdminDto>> GetUsuariosAsync(UsuarioAdminQueryDto? query = null, CancellationToken cancellationToken = default);
        Task<UsuarioAdminDto?> GetUsuarioAsync(long credencialId, CancellationToken cancellationToken = default);
        Task<UsuarioAdminOperationResponse> CreateUsuarioAsync(UsuarioAdminEditDto request, CancellationToken cancellationToken = default);
        Task<UsuarioAdminOperationResponse> UpdateUsuarioAsync(long credencialId, UsuarioAdminEditDto request, CancellationToken cancellationToken = default);
        Task<UsuarioAdminOperationResponse> DeleteUsuarioAsync(long credencialId, CancellationToken cancellationToken = default);
    }
}
