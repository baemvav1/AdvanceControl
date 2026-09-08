using System;
using System.Text.Json.Serialization;

namespace Advance_Control.Models
{
    /// <summary>
    /// Datos fiscales propios (emisor) usados para timbrar CFDI directamente desde el ERP.
    /// </summary>
    public class ConfiguracionEmisorDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("rfc")]
        public string Rfc { get; set; } = string.Empty;

        [JsonPropertyName("razonSocial")]
        public string RazonSocial { get; set; } = string.Empty;

        [JsonPropertyName("regimenFiscal")]
        public string RegimenFiscal { get; set; } = string.Empty;

        [JsonPropertyName("lugarExpedicion")]
        public string LugarExpedicion { get; set; } = string.Empty;

        [JsonPropertyName("actualizadoEn")]
        public DateTime ActualizadoEn { get; set; }
    }

    /// <summary>
    /// Estado del CSD cargado, sin exponer la llave privada ni su password.
    /// </summary>
    public class CsdEmisorEstadoDto
    {
        [JsonPropertyName("cargado")]
        public bool Cargado { get; set; }

        [JsonPropertyName("numeroCertificado")]
        public string? NumeroCertificado { get; set; }

        [JsonPropertyName("rfc")]
        public string? Rfc { get; set; }

        [JsonPropertyName("fechaVigenciaDesde")]
        public DateTime? FechaVigenciaDesde { get; set; }

        [JsonPropertyName("fechaVigenciaHasta")]
        public DateTime? FechaVigenciaHasta { get; set; }

        [JsonPropertyName("vigente")]
        public bool Vigente { get; set; }

        [JsonPropertyName("cargadoEn")]
        public DateTime? CargadoEn { get; set; }
    }

    /// <summary>Resultado de intentar cargar un CSD nuevo.</summary>
    public class CsdUploadResultDto
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public CsdEmisorEstadoDto? Estado { get; set; }
    }

    /// <summary>
    /// Estado de la FIEL/PFX cargado, sin exponer el PFX ni su password. El PFX generado a partir
    /// de la FIEL se usa para la cancelación de CFDI ante Bilkon, no para timbrar (eso usa el CSD).
    /// </summary>
    public class FielEmisorEstadoDto
    {
        [JsonPropertyName("cargado")]
        public bool Cargado { get; set; }

        [JsonPropertyName("numeroCertificado")]
        public string? NumeroCertificado { get; set; }

        [JsonPropertyName("rfc")]
        public string? Rfc { get; set; }

        [JsonPropertyName("fechaVigenciaDesde")]
        public DateTime? FechaVigenciaDesde { get; set; }

        [JsonPropertyName("fechaVigenciaHasta")]
        public DateTime? FechaVigenciaHasta { get; set; }

        [JsonPropertyName("vigente")]
        public bool Vigente { get; set; }

        [JsonPropertyName("cargadoEn")]
        public DateTime? CargadoEn { get; set; }
    }

    /// <summary>Resultado de intentar generar y guardar un PFX nuevo a partir de la FIEL.</summary>
    public class FielUploadResultDto
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public FielEmisorEstadoDto? Estado { get; set; }
    }
}
