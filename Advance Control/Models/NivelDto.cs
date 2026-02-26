using System.Text.Json.Serialization;

namespace Advance_Control.Models
{
    public class NivelDto
    {
        [JsonPropertyName("idNivel")]
        public int IdNivel { get; set; }

        [JsonPropertyName("descripcion")]
        public string Descripcion { get; set; } = string.Empty;

        [JsonPropertyName("nivelAcceso")]
        public int? NivelAcceso { get; set; }
    }
}
