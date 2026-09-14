using System;
using System.Text.Json.Serialization;

namespace Advance_Control.Models
{
    /// <summary>
    /// Vista de una operación en el Portal de Cliente: 3 señales de estado
    /// independientes (una operación puede estar en cualquier combinación).
    /// </summary>
    public class OperacionClientePortalDto
    {
        [JsonPropertyName("idOperacion")]
        public int IdOperacion { get; set; }

        [JsonPropertyName("idCliente")]
        public int? IdCliente { get; set; }

        [JsonPropertyName("razonSocial")]
        public string? RazonSocial { get; set; }

        [JsonPropertyName("identificador")]
        public string? Identificador { get; set; }

        [JsonPropertyName("nota")]
        public string? Nota { get; set; }

        [JsonPropertyName("fechaInicio")]
        public DateTime? FechaInicio { get; set; }

        [JsonPropertyName("fechaFinal")]
        public DateTime? FechaFinal { get; set; }

        [JsonPropertyName("pendiente")]
        public bool Pendiente { get; set; }

        [JsonPropertyName("tecnicamenteFinalizada")]
        public bool TecnicamenteFinalizada { get; set; }

        [JsonPropertyName("facturada")]
        public bool Facturada { get; set; }

        [JsonIgnore]
        public bool HasNota => !string.IsNullOrWhiteSpace(Nota);
    }
}
