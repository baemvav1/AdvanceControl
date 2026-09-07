using System.Text.Json.Serialization;

namespace Advance_Control.Models
{
    /// <summary>
    /// Catálogo de rubros de negocio (Elevadores, Inmuebles, ...)
    /// </summary>
    public class RubroDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("nombre")]
        public string Nombre { get; set; } = string.Empty;

        [JsonPropertyName("estatus")]
        public bool Estatus { get; set; }
    }
}
