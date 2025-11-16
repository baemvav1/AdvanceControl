using System;

namespace Advance_Control.Models
{
    /// <summary>
    /// Representa una notificación en el sistema.
    /// </summary>
    public class NotificacionDto
    {
        /// <summary>
        /// Título de la notificación (requerido).
        /// </summary>
        public string Titulo { get; set; } = string.Empty;

        /// <summary>
        /// Nota o contenido de la notificación (opcional).
        /// </summary>
        public string? Nota { get; set; }

        /// <summary>
        /// Fecha y hora de inicio (opcional).
        /// </summary>
        public DateTime? FechaHoraInicio { get; set; }

        /// <summary>
        /// Fecha y hora final (opcional).
        /// </summary>
        public DateTime? FechaHoraFinal { get; set; }

        /// <summary>
        /// Fecha y hora de creación de la notificación.
        /// </summary>
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        /// <summary>
        /// Identificador único de la notificación.
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Tiempo de vida de la notificación en segundos (opcional).
        /// Si es null, la notificación será estática hasta que el usuario la elimine.
        /// </summary>
        public int? TiempoDeVidaSegundos { get; set; }
    }
}
