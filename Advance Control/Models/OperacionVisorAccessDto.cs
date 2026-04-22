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

        /// <summary>
        /// Indica si el usuario tiene acceso normal (puede mutar) a la operación.
        /// Conveniencia espejo de !SoloLectura — provista por la API.
        /// </summary>
        [JsonPropertyName("tieneAcceso")]
        public bool TieneAcceso { get; set; }
    }
}
