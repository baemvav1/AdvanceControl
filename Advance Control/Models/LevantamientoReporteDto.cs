using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Advance_Control.Models
{
    public sealed class LevantamientoReporteDto
    {
        [JsonPropertyName("tipo")]
        public string Tipo { get; init; } = "ElevadorDeTraccion";

        [JsonPropertyName("etiqueta")]
        public string Etiqueta { get; init; } = "Levantamiento de elevador de traccion";

        [JsonPropertyName("secciones")]
        public List<LevantamientoNodoDto> Secciones { get; init; } = new();
    }

    public sealed class LevantamientoNodoDto
    {
        [JsonPropertyName("clave")]
        public required string Clave { get; init; }

        [JsonPropertyName("etiqueta")]
        public required string Etiqueta { get; init; }

        [JsonPropertyName("descripcionFalla")]
        public string? DescripcionFalla { get; set; }

        [JsonPropertyName("tieneFalla")]
        public bool TieneFalla { get; set; }

        [JsonPropertyName("hijos")]
        public List<LevantamientoNodoDto> Hijos { get; init; } = new();
    }
}
