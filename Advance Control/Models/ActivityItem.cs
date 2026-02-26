using System;

namespace Advance_Control.Models
{
    /// <summary>
    /// Representa un ítem de actividad de usuario para el dashboard.
    /// Mapeado desde la tabla actividad via GET /api/Actividad.
    /// </summary>
    public class ActivityItem
    {
        public int IdActividad { get; set; }
        public int CredencialId { get; set; }
        public string Origen { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;
        public DateTime Hora { get; set; }

        // ── Propiedades computadas para XAML x:Bind ──────────────────────

        /// <summary>Icono según el origen de la actividad.</summary>
        public string IconoGlyph => Origen switch
        {
            "Clientes"     => "\uE716",
            "Operaciones"  => "\uE9F9",
            "Mantenimiento"=> "\uE90F",
            "Equipos"      => "\uE7F4",
            "Proveedores"  => "\uE8D7",
            "Servicios"    => "\uE7BA",
            "Sesion"       => "\uE72E",
            _              => "\uE823"
        };

        /// <summary>Fecha formateada: hora si es hoy, fecha si es otro día.</summary>
        public string HoraLabel
        {
            get
            {
                return Hora.Date == DateTime.Today
                    ? "Hoy"
                    : Hora.ToString("dd/MM/yyyy");
            }
        }
    }
}

