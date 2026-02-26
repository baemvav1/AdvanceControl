using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;

namespace Advance_Control.Services.Nivel
{
    public interface INivelService
    {
        Task<List<NivelDto>> GetNivelesAsync(int idNivel = 0, CancellationToken cancellationToken = default);
    }
}
