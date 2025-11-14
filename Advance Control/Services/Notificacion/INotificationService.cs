using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Advance_Control.Models;

namespace Advance_Control.Services.Notificacion
{
    /// <summary>
    /// Servicio para gestionar notificaciones en la aplicación
    /// </summary>
    public interface INotificationService
    {
        /// <summary>
        /// Colección observable de notificaciones actuales
        /// </summary>
        ObservableCollection<Models.Notificacion> Notificaciones { get; }

        /// <summary>
        /// Agrega una nueva notificación al sistema
        /// </summary>
        /// <param name="titulo">Título de la notificación (requerido)</param>
        /// <param name="nota">Nota o mensaje de la notificación (opcional)</param>
        /// <param name="fechaHoraInicio">Fecha y hora de inicio (opcional)</param>
        /// <param name="fechaHoraFinal">Fecha y hora final (opcional)</param>
        /// <returns>Task completado cuando la notificación se agregó</returns>
        Task AgregarNotificacionAsync(
            string titulo,
            string? nota = null,
            DateTime? fechaHoraInicio = null,
            DateTime? fechaHoraFinal = null);

        /// <summary>
        /// Marca una notificación como leída
        /// </summary>
        /// <param name="notificacion">La notificación a marcar como leída</param>
        void MarcarComoLeida(Models.Notificacion notificacion);

        /// <summary>
        /// Elimina una notificación del sistema
        /// </summary>
        /// <param name="notificacion">La notificación a eliminar</param>
        void EliminarNotificacion(Models.Notificacion notificacion);

        /// <summary>
        /// Limpia todas las notificaciones
        /// </summary>
        void LimpiarNotificaciones();
    }
}
