using System;

namespace Advance_Control.Models
{
    /// <summary>
    /// Notificación persistente generada por el sistema de alertas inteligentes.
    /// Se almacena en la BD y se muestra en el panel de notificaciones hasta ser descartada.
    /// </summary>
    public class NotificacionAlerta
    {
        public int      IdNotificacion { get; set; }
        public int      CredencialId   { get; set; }
        public string   Origen         { get; set; } = string.Empty;
        public string   Titulo         { get; set; } = string.Empty;
        public DateTime Hora           { get; set; }
        public bool     Estatus        { get; set; }

        /// <summary>Icono según el origen de la alerta.</summary>
        public string Icono => Origen switch
        {
            "OrdenServicio" => "\uE90F",   // Repair/Wrench
            "Operaciones"   => "\uE7BA",   // Warning
            "Equipos"       => "\uE7F4",   // AllApps/Devices
            "Cotizaciones"  => "\uE7BC",   // Document
            "Emergencias"   => "\uE7E7",   // Alerta (emergencia del portal)
            "Mantenimiento" => "\uE9D5",   // Checklist (hoja de mantenimiento)
            _               => "\uEA8F",   // Bell
        };
    }
}
