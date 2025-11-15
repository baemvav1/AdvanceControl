using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Advance_Control.Models;

namespace Advance_Control.Services.Notificacion
{
    /// <summary>
    /// Servicio para gestionar notificaciones en el sistema.
    /// </summary>
    public interface INotificacionService
    {
        /// <summary>
        /// Muestra una notificación en el sistema.
        /// </summary>
        /// <param name="titulo">Título de la notificación (requerido).</param>
        /// <param name="nota">Nota o contenido de la notificación (opcional).</param>
        /// <param name="fechaHoraInicio">Fecha y hora de inicio (opcional).</param>
        /// <param name="fechaHoraFinal">Fecha y hora final (opcional).</param>
        /// <param name="tiempoDeVidaSegundos">Tiempo de vida en segundos (opcional). Si es null, la notificación será estática.</param>
        /// <returns>La notificación creada.</returns>
        Task<NotificacionDto> MostrarNotificacionAsync(
            string titulo, 
            string? nota = null, 
            DateTime? fechaHoraInicio = null, 
            DateTime? fechaHoraFinal = null,
            int? tiempoDeVidaSegundos = null);

        /// <summary>
        /// Obtiene todas las notificaciones.
        /// </summary>
        /// <returns>Lista de notificaciones.</returns>
        IEnumerable<NotificacionDto> ObtenerNotificaciones();

        /// <summary>
        /// Limpia todas las notificaciones.
        /// </summary>
        void LimpiarNotificaciones();

        /// <summary>
        /// Elimina una notificación específica por su ID.
        /// </summary>
        /// <param name="id">ID de la notificación a eliminar.</param>
        /// <returns>True si se eliminó correctamente, false si no se encontró.</returns>
        bool EliminarNotificacion(Guid id);
    }
}
