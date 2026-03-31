using Advance_Control.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Advance_Control.Services.TipoMantenimiento
{
    public interface ITipoMantenimientoService
    {
        Task<List<TipoMantenimientoDto>> GetTiposMantenimientoAsync(CancellationToken cancellationToken = default);
    }
}
