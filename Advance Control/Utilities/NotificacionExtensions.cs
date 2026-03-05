using Advance_Control.Services.Notificacion;
using System;
using System.Threading.Tasks;

namespace Advance_Control.Utilities
{
    /// <summary>
    /// Métodos de extensión para INotificacionService.
    /// Elimina el patrón repetido MostrarNotificacionAsync(titulo:..., nota:..., fechaHoraInicio: DateTime.Now).
    /// </summary>
    public static class NotificacionExtensions
    {
        public static Task MostrarAsync(this INotificacionService service, string titulo, string nota)
            => service.MostrarNotificacionAsync(titulo: titulo, nota: nota, fechaHoraInicio: DateTime.Now);
    }
}
