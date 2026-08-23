using System.Text.Json.Serialization;

namespace Advance_Control.Models
{
    /// <summary>Clave SAT y tasa de IVA recordadas para un tipo de cargo (Refacción/Servicio).</summary>
    public class TipoCargoDefaultDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("concepto")]
        public string Concepto { get; set; } = string.Empty;

        [JsonPropertyName("descripcionCorta")]
        public string DescripcionCorta { get; set; } = string.Empty;

        [JsonPropertyName("claveProdServDefault")]
        public string? ClaveProdServDefault { get; set; }

        [JsonPropertyName("claveUnidadDefault")]
        public string? ClaveUnidadDefault { get; set; }

        [JsonPropertyName("tasaIvaDefault")]
        public decimal? TasaIvaDefault { get; set; }
    }
}
