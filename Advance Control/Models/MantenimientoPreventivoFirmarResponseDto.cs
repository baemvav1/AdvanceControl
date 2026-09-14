using System;
using System.Text.Json.Serialization;

namespace Advance_Control.Models
{
    /// <summary>Resultado de "Aprobar/Firmar" una hoja de mantenimiento desde el Portal de Cliente.</summary>
    public class MantenimientoPreventivoFirmarResponseDto
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("idOperacion")]
        public int IdOperacion { get; set; }

        [JsonPropertyName("estado")]
        public string Estado { get; set; } = string.Empty;

        [JsonPropertyName("firmadaEn")]
        public DateTime? FirmadaEn { get; set; }

        [JsonPropertyName("firmadoPorContactoId")]
        public long? FirmadoPorContactoId { get; set; }

        [JsonPropertyName("firmadoIp")]
        public string? FirmadoIp { get; set; }
    }
}
