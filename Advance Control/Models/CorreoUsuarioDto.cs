using System;
using System.Text.Json.Serialization;

namespace Advance_Control.Models
{
    public class CorreoUsuarioDto
    {
        [JsonPropertyName("credencialId")]
        public long CredencialId { get; set; }

        [JsonPropertyName("usuario")]
        public string Usuario { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("password")]
        public string Password { get; set; } = string.Empty;

        [JsonPropertyName("creadoEn")]
        public DateTime? CreadoEn { get; set; }

        [JsonPropertyName("actualizadoEn")]
        public DateTime? ActualizadoEn { get; set; }

        [JsonPropertyName("actualizadoPor")]
        public string? ActualizadoPor { get; set; }
    }

    public class CorreoUsuarioEditDto
    {
        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("password")]
        public string? Password { get; set; }
    }

    public class CorreoUsuarioOperationResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("credencialId")]
        public long CredencialId { get; set; }
    }
}
