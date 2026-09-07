using System;
using System.Text.Json.Serialization;

namespace Advance_Control.Models
{
    public class RelacionUsuarioRubroDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("credencialId")]
        public long CredencialId { get; set; }

        [JsonPropertyName("idRubro")]
        public int IdRubro { get; set; }

        [JsonPropertyName("nombreRubro")]
        public string? NombreRubro { get; set; }

        [JsonPropertyName("activo")]
        public bool Activo { get; set; }

        [JsonPropertyName("creadoEn")]
        public DateTime? CreadoEn { get; set; }
    }
}
