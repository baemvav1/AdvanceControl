using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;

namespace Advance_Control.Services.Dashboard
{
    /// <summary>
    /// Servicio que obtiene los conteos del dashboard desde la API.
    /// </summary>
    public interface IDashboardService
    {
        /// <summary>
        /// Obtiene los conteos de operaciones, órdenes de servicio, clientes y equipos
        /// del usuario autenticado.
        /// </summary>
        Task<DashboardConteoDto?> GetConteosAsync(CancellationToken cancellationToken = default);
    }
}
