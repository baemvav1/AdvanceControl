using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;

namespace Advance_Control.Services.Portal
{
    /// <summary>
    /// Cliente HTTP del Portal de Cliente (api/PortalCliente/*). El servidor
    /// resuelve por completo qué empresas ve este login; el cliente nunca
    /// manda un IdCliente.
    /// </summary>
    public interface IPortalClienteService
    {
        Task<List<OperacionClientePortalDto>> ObtenerOperacionesAsync(CancellationToken cancellationToken = default);

        Task<List<MantenimientoPreventivoHojaDto>?> ObtenerHojasMantenimientoAsync(int idOperacion, CancellationToken cancellationToken = default);

        Task<MantenimientoPreventivoHojaDto?> ObtenerHojaMantenimientoAsync(long idHoja, CancellationToken cancellationToken = default);

        /// <summary>Null si la firma falló (ver mensaje de error vía logging).</summary>
        Task<MantenimientoPreventivoFirmarResponseDto?> FirmarHojaMantenimientoAsync(long idHoja, CancellationToken cancellationToken = default);
    }
}
