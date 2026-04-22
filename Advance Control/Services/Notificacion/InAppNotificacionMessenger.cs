using System;

namespace Advance_Control.Services.Notificacion
{
    public sealed record InAppNotificacionPayload(string Titulo, string? Nota, string TextoCopiable);

    /// <summary>
    /// Mensajero estático para notificaciones in-app de respaldo.
    /// Se usa cuando AppNotificationManager no está disponible (app no desplegada como MSIX).
    /// </summary>
    public static class InAppNotificacionMessenger
    {
        /// <summary>
        /// Se dispara cuando hay una notificación que mostrar in-app.
        /// El payload incluye el texto mostrado y el contenido copiable.
        /// </summary>
        public static event EventHandler<InAppNotificacionPayload>? NotificacionSolicitada;

        internal static void Enviar(string titulo, string? nota, string textoCopiable)
            => NotificacionSolicitada?.Invoke(null, new InAppNotificacionPayload(titulo, nota, textoCopiable));
    }
}
