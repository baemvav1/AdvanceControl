using System;
using System.Text.Json.Serialization;

namespace Advance_Control.Models
{
    public class GuardarEstadoCuentaMovimientoDto
    {
        [JsonPropertyName("fecha")]
        public DateTime Fecha { get; set; }

        [JsonPropertyName("descripcion")]
        public string Descripcion { get; set; } = string.Empty;

        [JsonPropertyName("referencia")]
        public string? Referencia { get; set; }

        [JsonPropertyName("cargo")]
        public decimal? Cargo { get; set; }

        [JsonPropertyName("abono")]
        public decimal? Abono { get; set; }

        [JsonPropertyName("saldo")]
        public decimal Saldo { get; set; }

        [JsonPropertyName("tipoOperacion")]
        public string? TipoOperacion { get; set; }
    }
}
