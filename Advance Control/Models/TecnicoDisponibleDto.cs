using System.Text.Json.Serialization;

namespace Advance_Control.Models
{
    /// <summary>
    /// DTO para representar un técnico disponible para atender un mantenimiento.
    /// Se obtiene del endpoint GET /api/Mantenimiento/tecnicos
    /// </summary>
    public class TecnicoDisponibleDto
    {
        [JsonPropertyName("credencialId")]
        public long CredencialId { get; set; }

        [JsonPropertyName("nombre")]
        public string Nombre { get; set; } = string.Empty;

        [JsonPropertyName("apellido")]
        public string Apellido { get; set; } = string.Empty;

        [JsonPropertyName("nombreCompleto")]
        public string NombreCompleto { get; set; } = string.Empty;

        [JsonPropertyName("correo")]
        public string? Correo { get; set; }

        [JsonPropertyName("telefono")]
        public string? Telefono { get; set; }

        [JsonPropertyName("cargo")]
        public string? Cargo { get; set; }

        [JsonPropertyName("tipoUsuario")]
        public string TipoUsuario { get; set; } = string.Empty;

        [JsonPropertyName("nivel")]
        public int Nivel { get; set; }
    }
}
