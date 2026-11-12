using System;

namespace Advance_Control.Models
{
    /// <summary>Un evento del historial de un paso del check (GET api/CheckOperacion/{id}/historial).</summary>
    public class CheckHistorialDto
    {
        /// <summary>Clave del paso en snake_case (ej: "cotizacion_generada").</summary>
        public string Campo { get; set; } = string.Empty;

        /// <summary>True = se marcó; false = se desmarcó (o la aprobación quedó reemplazada).</summary>
        public bool Valor { get; set; }

        /// <summary>Quién lo hizo; null si no se sabe.</summary>
        public string? RealizadoPor { get; set; }
        public DateTimeOffset Fecha { get; set; }
        public string? Detalle { get; set; }
    }
}
