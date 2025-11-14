using System;

namespace Advance_Control.Models
{
    /// <summary>
    /// Modelo que representa una notificación en el sistema
    /// </summary>
    public class Notificacion
    {
        /// <summary>
        /// Título de la notificación (requerido)
        /// </summary>
        public string Titulo { get; set; } = string.Empty;

        /// <summary>
        /// Nota o mensaje de la notificación (opcional)
        /// </summary>
        public string? Nota { get; set; }

        /// <summary>
        /// Fecha y hora de inicio de la notificación (opcional)
        /// </summary>
        public DateTime? FechaHoraInicio { get; set; }

        /// <summary>
        /// Fecha y hora final de la notificación (opcional)
        /// </summary>
        public DateTime? FechaHoraFinal { get; set; }

        /// <summary>
        /// Fecha y hora de creación de la notificación
        /// </summary>
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        /// <summary>
        /// Indica si la notificación ha sido leída
        /// </summary>
        public bool Leida { get; set; } = false;
    }
}
