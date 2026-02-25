using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;

namespace Advance_Control.Services.Activity
{
    /// <summary>
    /// Servicio que obtiene la actividad reciente del sistema para mostrar en el dashboard.
    /// Consume el endpoint GET /api/Logging/actividad.
    /// </summary>
    public interface IActivityService
    {
        /// <summary>
        /// Obtiene los últimos registros de actividad de negocio.
        /// </summary>
        /// <param name="credencialId">Filtrar por usuario. Null = todos.</param>
        /// <param name="categoria">Filtrar por categoría: Operacion | Mantenimiento | Cliente | Equipo | Proveedor | Autenticacion. Null = todas.</param>
        /// <param name="soloErrores">True = solo Warning/Error/Critical.</param>
        /// <param name="top">Máximo de registros. Default 30.</param>
        Task<IReadOnlyList<ActivityItem>> GetActividadRecienteAsync(
            int? credencialId = null,
            string? categoria = null,
            bool soloErrores = false,
            int top = 30,
            CancellationToken cancellationToken = default);
    }
}
