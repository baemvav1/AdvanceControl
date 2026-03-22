using System.Text.Json.Serialization;

namespace Advance_Control.Models
{
    public class TipoUsuarioDto
    {
        [JsonPropertyName("idTipoUsuario")]
        public int IdTipoUsuario { get; set; }

        [JsonPropertyName("nivel")]
        public int Nivel { get; set; }

        [JsonPropertyName("tipoUsuario")]
        public string TipoUsuario { get; set; } = string.Empty;

        [JsonPropertyName("nota")]
        public string? Nota { get; set; }
    }
}
