using System.Text.Json.Serialization;

namespace Advance_Control.Models
{
    public class OperacionVisorAccessDto
    {
        [JsonPropertyName("operacion")]
        public OperacionDto? Operacion { get; set; }

        [JsonPropertyName("modoAcceso")]
        public string ModoAcceso { get; set; } = "normal";

        [JsonPropertyName("soloLectura")]
        public bool SoloLectura { get; set; }

        [JsonPropertyName("mensajeReferenciaId")]
        public long? MensajeReferenciaId { get; set; }
    }
}
