using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;

namespace Advance_Control.Services.Alertas
{
    public interface INotificacionAlertaService
    {
        /// <summary>Genera alertas inteligentes y devuelve las notificaciones activas.</summary>
        Task<IReadOnlyList<NotificacionAlerta>> GenerarYObtenerAsync(int credencialId, CancellationToken cancellationToken = default);

        /// <summary>Obtiene las notificaciones activas sin regenerar.</summary>
        Task<IReadOnlyList<NotificacionAlerta>> GetAsync(int credencialId, CancellationToken cancellationToken = default);

        /// <summary>Marca todas las notificaciones del usuario como vistas.</summary>
        Task MarcarVistasAsync(int credencialId, CancellationToken cancellationToken = default);
    }
}
