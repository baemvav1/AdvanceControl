using Advance_Control.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Advance_Control.Services.TipoUsuario
{
    public interface ITipoUsuarioService
    {
        Task<List<TipoUsuarioDto>> GetTiposUsuarioAsync(int idTipoUsuario = 0, CancellationToken cancellationToken = default);
    }
}
