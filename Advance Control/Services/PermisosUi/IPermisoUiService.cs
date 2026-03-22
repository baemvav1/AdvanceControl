using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;

namespace Advance_Control.Services.PermisosUi
{
    public interface IPermisoUiService
    {
        Task<List<PermisoModuloDto>> GetCatalogoAsync(bool soloActivos = true, CancellationToken cancellationToken = default);
        Task<PermisoUiSyncResultDto> SyncCatalogoAsync(PermisoUiSyncRequestDto request, CancellationToken cancellationToken = default);
        Task<PermisoModuloDto?> UpdateNivelModuloAsync(PermisoModuloNivelUpdateDto request, CancellationToken cancellationToken = default);
        Task<PermisoAccionModuloDto?> UpdateNivelAccionAsync(PermisoAccionNivelUpdateDto request, CancellationToken cancellationToken = default);
    }
}
