using System;
using System.Text.Json.Serialization;

namespace Advance_Control.Models
{
    /// <summary>
    /// Login del Portal de Clientes de una empresa (pivot "Logins" de
    /// ClientesPage). Espejo de AdvanceApi.DTOs.ClienteLoginDto.
    /// </summary>
    public class ClienteLoginDto
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("idCliente")]
        public int IdCliente { get; set; }

        [JsonPropertyName("contactoId")]
        public long ContactoId { get; set; }

        [JsonPropertyName("usuario")]
        public string Usuario { get; set; } = string.Empty;

        [JsonPropertyName("contactoNombre")]
        public string ContactoNombre { get; set; } = string.Empty;

        [JsonPropertyName("contactoCorreo")]
        public string? ContactoCorreo { get; set; }

        [JsonPropertyName("creadoEn")]
        public DateTime CreadoEn { get; set; }

        [JsonPropertyName("ultimoAccesoEn")]
        public DateTime? UltimoAccesoEn { get; set; }

        [JsonPropertyName("datosEnviadosEn")]
        public DateTime? DatosEnviadosEn { get; set; }

        [JsonPropertyName("datosEnviadosPor")]
        public string? DatosEnviadosPor { get; set; }

        public string ContactoTexto => string.IsNullOrWhiteSpace(ContactoCorreo)
            ? ContactoNombre
            : $"{ContactoNombre} · {ContactoCorreo}";

        public string UltimoAccesoTexto => UltimoAccesoEn.HasValue
            ? $"Último acceso: {UltimoAccesoEn.Value.ToLocalTime():dd/MM/yyyy HH:mm}"
            : "Nunca ha entrado";

        public string DatosEnviadosTexto => DatosEnviadosEn.HasValue
            ? $"Datos enviados el {DatosEnviadosEn.Value.ToLocalTime():dd/MM/yyyy HH:mm}"
              + (string.IsNullOrWhiteSpace(DatosEnviadosPor) ? string.Empty : $" por {DatosEnviadosPor}")
            : "Datos de acceso no enviados";
    }

    /// <summary>Respuesta de crear/restablecer: la contraseña en claro llega solo esta vez.</summary>
    public class ClienteLoginPasswordDto
    {
        [JsonPropertyName("login")]
        public ClienteLoginDto Login { get; set; } = new();

        [JsonPropertyName("password")]
        public string Password { get; set; } = string.Empty;
    }
}
