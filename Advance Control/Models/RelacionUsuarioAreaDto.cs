using System;
using System.Text.Json.Serialization;

namespace Advance_Control.Models
{
    public class RelacionUsuarioAreaDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("credencialId")]
        public long CredencialId { get; set; }

        [JsonPropertyName("idArea")]
        public int IdArea { get; set; }

        [JsonPropertyName("nombreArea")]
        public string? NombreArea { get; set; }

        [JsonPropertyName("colorMapa")]
        public string? ColorMapa { get; set; }

        [JsonPropertyName("nota")]
        public string? Nota { get; set; }

        [JsonPropertyName("activo")]
        public bool Activo { get; set; }

        [JsonPropertyName("creadoEn")]
        public DateTime? CreadoEn { get; set; }
    }
}
