using System;

namespace Advance_Control.Models
{
    /// <summary>
    /// Representa un ítem de actividad reciente para el dashboard.
    /// Mapeado desde el endpoint GET /api/Logging/actividad.
    /// </summary>
    public class ActivityItem
    {
        public string? Id { get; set; }
        public int Level { get; set; }
        public string? LevelLabel { get; set; }
        public string? Message { get; set; }
        public string? Source { get; set; }
        public string? Method { get; set; }
        public string? Page { get; set; }
        public string? Categoria { get; set; }
        public int? CredencialId { get; set; }
        public string? Username { get; set; }
        public DateTime Timestamp { get; set; }
        public string? Exception { get; set; }

        // ── Propiedades computadas para XAML x:Bind ──────────────────────

        public bool IsError => Level >= 4;
        public bool IsWarning => Level == 3;

        /// <summary>Indica si hay nombre de página para mostrar en la UI</summary>
        public bool HasPage => !string.IsNullOrEmpty(Page);

        /// <summary>Color del indicador según nivel</summary>
        public Windows.UI.Color LevelColor => Level switch
        {
            >= 5 => Windows.UI.Color.FromArgb(255, 196, 43, 28),   // Critical — rojo fuerte
            4    => Windows.UI.Color.FromArgb(255, 210, 60, 60),   // Error — rojo
            3    => Windows.UI.Color.FromArgb(255, 200, 130, 0),   // Warning — naranja
            2    => Windows.UI.Color.FromArgb(255, 16, 137, 62),   // Info — verde
            _    => Windows.UI.Color.FromArgb(255, 130, 130, 130), // Debug/Trace — gris
        };

        /// <summary>Timestamp formateado para la UI (hora de hoy, o fecha si es de otro día)</summary>
        public string TimestampLabel
        {
            get
            {
                var local = Timestamp.ToLocalTime();
                return local.Date == DateTime.Today
                    ? local.ToString("HH:mm")
                    : local.ToString("dd/MM HH:mm");
            }
        }
    }
}

