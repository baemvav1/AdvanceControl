using System;
using System.Threading.Tasks;

namespace Advance_Control.Services.Notificacion
{
    /// <summary>
    /// Servicio para mostrar notificaciones del sistema usando Windows AppNotification.
    /// </summary>
    public interface INotificacionService
    {
        /// <summary>
        /// Muestra una notificación de Windows. Las notificaciones con "Error" o "Validación"
        /// en el título son persistentes; el resto se auto-descartan.
        /// </summary>
        /// <param name="argumentos">Argumentos clave-valor opcionales para la activación al hacer clic.</param>
        Task MostrarNotificacionAsync(
            string titulo,
            string? nota = null,
            DateTime? fechaHoraInicio = null,
            DateTime? fechaHoraFinal = null,
            int? tiempoDeVidaSegundos = null,
            System.Collections.Generic.Dictionary<string, string>? argumentos = null);
    }
}
