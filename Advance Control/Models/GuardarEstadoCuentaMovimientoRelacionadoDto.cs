using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Advance_Control.Models
{
    public class GuardarEstadoCuentaMovimientoRelacionadoDto
    {
        [JsonPropertyName("tipo")]
        public string? Tipo { get; set; }

        [JsonPropertyName("orden")]
        public int? Orden { get; set; }

        [JsonPropertyName("descripcion")]
        public string Descripcion { get; set; } = string.Empty;

        [JsonPropertyName("rfc")]
        public string? Rfc { get; set; }

        [JsonPropertyName("monto")]
        public decimal? Monto { get; set; }

        [JsonPropertyName("saldo")]
        public decimal? Saldo { get; set; }

        [JsonPropertyName("metadatos")]
        public Dictionary<string, string?> Metadatos { get; set; } = new();
    }
}
