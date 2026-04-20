using System;

namespace Advance_Control.Services.Notificacion
{
    /// <summary>
    /// Mensajero estático para notificaciones in-app de respaldo.
    /// Se usa cuando AppNotificationManager no está disponible (app no desplegada como MSIX).
    /// </summary>
    public static class InAppNotificacionMessenger
    {
        /// <summary>
        /// Se dispara cuando hay una notificación que mostrar in-app.
        /// El primer parámetro es el título, el segundo es la nota opcional.
        /// </summary>
        public static event EventHandler<(string Titulo, string? Nota)>? NotificacionSolicitada;

        internal static void Enviar(string titulo, string? nota)
            => NotificacionSolicitada?.Invoke(null, (titulo, nota));
    }
}
