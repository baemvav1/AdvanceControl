using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Advance_Control.Models
{
    public class GuardarEstadoCuentaRequestDto
    {
        [JsonPropertyName("numeroCuenta")]
        public string NumeroCuenta { get; set; } = string.Empty;

        [JsonPropertyName("clabe")]
        public string Clabe { get; set; } = string.Empty;

        [JsonPropertyName("tipoCuenta")]
        public string? TipoCuenta { get; set; }

        [JsonPropertyName("tipoMoneda")]
        public string? TipoMoneda { get; set; }

        [JsonPropertyName("fechaInicio")]
        public DateTime FechaInicio { get; set; }

        [JsonPropertyName("fechaFin")]
        public DateTime FechaFin { get; set; }

        [JsonPropertyName("fechaCorte")]
        public DateTime FechaCorte { get; set; }

        [JsonPropertyName("saldoInicial")]
        public decimal SaldoInicial { get; set; }

        [JsonPropertyName("totalCargos")]
        public decimal TotalCargos { get; set; }

        [JsonPropertyName("totalAbonos")]
        public decimal TotalAbonos { get; set; }

        [JsonPropertyName("saldoFinal")]
        public decimal SaldoFinal { get; set; }

        [JsonPropertyName("totalComisiones")]
        public decimal TotalComisiones { get; set; }

        [JsonPropertyName("totalISR")]
        public decimal TotalISR { get; set; }

        [JsonPropertyName("totalIVA")]
        public decimal TotalIVA { get; set; }

        [JsonPropertyName("movimientos")]
        public List<GuardarEstadoCuentaMovimientoDto> Movimientos { get; set; } = new();
    }
}
