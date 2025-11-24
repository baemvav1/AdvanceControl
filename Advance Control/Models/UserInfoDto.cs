using System.Text.Json.Serialization;

namespace Advance_Control.Models
{
    public class UserInfoDto
    {
        [JsonPropertyName("credencialId")]
        public int CredencialId { get; set; }
        
        [JsonPropertyName("nombreCompleto")]
        public string? NombreCompleto { get; set; }
        
        [JsonPropertyName("correo")]
        public string? Correo { get; set; }
        
        [JsonPropertyName("telefono")]
        public string? Telefono { get; set; }
        
        [JsonPropertyName("nivel")]
        public int Nivel { get; set; }
        
        [JsonPropertyName("tipoUsuario")]
        public string? TipoUsuario { get; set; }
    }
}
