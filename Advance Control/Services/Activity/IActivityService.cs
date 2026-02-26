using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;

namespace Advance_Control.Services.Activity
{
    /// <summary>
    /// Servicio que gestiona la actividad de usuario para el dashboard.
    /// Consume GET /api/Actividad y POST /api/Actividad.
    /// </summary>
    public interface IActivityService
    {
        /// <summary>
        /// Obtiene las actividades registradas del usuario autenticado.
        /// </summary>
        Task<IReadOnlyList<ActivityItem>> GetActividadAsync(
            int credencialId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Registra una nueva actividad realizada por el usuario autenticado.
        /// El credencial_id se obtiene de la sesión activa.
        /// </summary>
        /// <param name="origen">Módulo que origina la acción (ej: "Clientes", "Operaciones").</param>
        /// <param name="titulo">Descripción legible de la acción realizada.</param>
        Task CrearActividadAsync(
            string origen,
            string titulo,
            CancellationToken cancellationToken = default);
    }
}
