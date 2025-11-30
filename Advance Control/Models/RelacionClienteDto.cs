using System.Text.Json.Serialization;

namespace Advance_Control.Models
{
    /// <summary>
    /// DTO para los datos de relación equipo-cliente que se reciben desde la API
    /// </summary>
    public class RelacionClienteDto
    {
        [JsonPropertyName("idRelacion")]
        public int IdRelacion { get; set; }

        [JsonPropertyName("idCliente")]
        public int IdCliente { get; set; }

        [JsonPropertyName("razonSocial")]
        public string? RazonSocial { get; set; }

        [JsonPropertyName("nombreComercial")]
        public string? NombreComercial { get; set; }
    }
}
