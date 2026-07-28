using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;

namespace Advance_Control.Services.VisorMundial
{
    public interface IVisorMundialService
    {
        Task<List<VisorMundialUbicacionDto>> ObtenerUbicacionesAsync(CancellationToken cancellationToken = default);
        Task<List<VisorMundialEquipoDto>> ObtenerEquiposPorUbicacionAsync(int idUbicacion, CancellationToken cancellationToken = default);
    }
}
