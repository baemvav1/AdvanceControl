using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;

namespace Advance_Control.Services.PermisosUi
{
    public interface IPermisoUiScanner
    {
        Task<List<PermisoModuloSyncDto>> ScanAsync(CancellationToken cancellationToken = default);
    }
}
