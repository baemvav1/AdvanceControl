using System.Text.Json.Serialization;

namespace Advance_Control.Models
{
    /// <summary>
    /// DTO para los tipos de mantenimiento (Correctivo, Preventivo, etc.)
    /// </summary>
    public class TipoMantenimientoDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("tipoMantenimiento")]
        public string TipoMantenimiento { get; set; } = string.Empty;

        public override string ToString() => TipoMantenimiento;
    }
}
