using System;
using System.Text.Json.Serialization;

namespace Advance_Control.Models
{
    /// <summary>
    /// Hoja de mantenimiento preventivo persistida (mantenimiento_preventivo_hojas).
    /// </summary>
    public class MantenimientoPreventivoHojaDto
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("idOperacion")]
        public int IdOperacion { get; set; }

        [JsonPropertyName("tipoMaquina")]
        public string? TipoMaquina { get; set; }

        [JsonPropertyName("situacionFinal")]
        public string? SituacionFinal { get; set; }

        [JsonPropertyName("horaEntrada")]
        public TimeSpan? HoraEntrada { get; set; }

        [JsonPropertyName("horaSalida")]
        public TimeSpan? HoraSalida { get; set; }

        [JsonPropertyName("observaciones")]
        public string? Observaciones { get; set; }

        /// <summary>Checklist completo como JSON crudo (ver formato en la migración 115).</summary>
        [JsonPropertyName("checklistJson")]
        public string ChecklistJson { get; set; } = "{}";

        [JsonPropertyName("idAtiende")]
        public long? IdAtiende { get; set; }

        [JsonPropertyName("idContactoDirigido")]
        public long? IdContactoDirigido { get; set; }

        /// <summary>Borrador | Completada | Firmada</summary>
        [JsonPropertyName("estado")]
        public string Estado { get; set; } = "Borrador";

        [JsonPropertyName("pdfUrl")]
        public string? PdfUrl { get; set; }

        [JsonPropertyName("creadoEn")]
        public DateTime CreadoEn { get; set; }

        [JsonPropertyName("actualizadoEn")]
        public DateTime? ActualizadoEn { get; set; }

        [JsonPropertyName("completadaEn")]
        public DateTime? CompletadaEn { get; set; }

        [JsonPropertyName("firmadaEn")]
        public DateTime? FirmadaEn { get; set; }

        [JsonPropertyName("firmadoPorContactoId")]
        public long? FirmadoPorContactoId { get; set; }

        [JsonPropertyName("firmadoIp")]
        public string? FirmadoIp { get; set; }

        [JsonIgnore]
        public string CreadoEnTexto => CreadoEn.ToString("dd/MM/yyyy HH:mm");

        [JsonIgnore]
        public bool PuedeFirmarse => Estado == "Completada";

        [JsonIgnore]
        public bool YaFirmada => Estado == "Firmada";
    }

    /// <summary>Datos para crear/actualizar una hoja (guardar avance o completar).</summary>
    public class MantenimientoPreventivoGuardarRequestDto
    {
        [JsonPropertyName("idOperacion")]
        public int IdOperacion { get; set; }

        [JsonPropertyName("tipoMaquina")]
        public string? TipoMaquina { get; set; }

        [JsonPropertyName("situacionFinal")]
        public string? SituacionFinal { get; set; }

        [JsonPropertyName("horaEntrada")]
        public TimeSpan? HoraEntrada { get; set; }

        [JsonPropertyName("horaSalida")]
        public TimeSpan? HoraSalida { get; set; }

        [JsonPropertyName("observaciones")]
        public string? Observaciones { get; set; }

        [JsonPropertyName("checklistJson")]
        public string? ChecklistJson { get; set; }

        [JsonPropertyName("idAtiende")]
        public long? IdAtiende { get; set; }

        [JsonPropertyName("idContactoDirigido")]
        public long? IdContactoDirigido { get; set; }

        /// <summary>Borrador | Completada</summary>
        [JsonPropertyName("estado")]
        public string? Estado { get; set; }

        [JsonPropertyName("pdfUrl")]
        public string? PdfUrl { get; set; }
    }
}
