using System;
using System.Text.Json.Serialization;

namespace Advance_Control.Models
{
    /// <summary>
    /// DTO para los datos de un contrato de suscripción (Oro/Plata/Bronce)
    /// que se reciben desde la API
    /// </summary>
    public class ContratoSuscripcionDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("idCliente")]
        public int IdCliente { get; set; }

        [JsonPropertyName("nivel")]
        public string Nivel { get; set; } = string.Empty;

        [JsonPropertyName("numeroContrato")]
        public string? NumeroContrato { get; set; }

        [JsonPropertyName("direccionInstalacion")]
        public string? DireccionInstalacion { get; set; }

        [JsonPropertyName("montoMensual")]
        public decimal MontoMensual { get; set; }

        [JsonPropertyName("numeroUnidades")]
        public int NumeroUnidades { get; set; }

        [JsonPropertyName("vigenciaInicio")]
        public DateTime VigenciaInicio { get; set; }

        [JsonPropertyName("vigenciaFin")]
        public DateTime VigenciaFin { get; set; }

        [JsonPropertyName("nombreFirmante")]
        public string? NombreFirmante { get; set; }

        [JsonPropertyName("telefonoFirmante")]
        public string? TelefonoFirmante { get; set; }

        [JsonPropertyName("fechaFirma")]
        public DateTime? FechaFirma { get; set; }

        [JsonPropertyName("estatus")]
        public string Estatus { get; set; } = "generado";

        [JsonPropertyName("pdfGeneradoUrl")]
        public string? PdfGeneradoUrl { get; set; }

        [JsonPropertyName("pdfFirmadoUrl")]
        public string? PdfFirmadoUrl { get; set; }

        [JsonPropertyName("creadoEn")]
        public DateTime? CreadoEn { get; set; }

        [JsonPropertyName("idsEquipos")]
        public int[] IdsEquipos { get; set; } = Array.Empty<int>();

        /// <summary>
        /// Indica si el contrato ya fue firmado (documento escaneado cargado)
        /// </summary>
        [JsonIgnore]
        public bool EstaFirmado => string.Equals(Estatus, "firmado", StringComparison.OrdinalIgnoreCase);

        [JsonIgnore]
        public string MontoMensualTexto => $"${MontoMensual:N2}";

        [JsonIgnore]
        public string VigenciaInicioTexto => VigenciaInicio.ToString("dd/MM/yyyy");

        [JsonIgnore]
        public string VigenciaFinTexto => VigenciaFin.ToString("dd/MM/yyyy");
    }
}
