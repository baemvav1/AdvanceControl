using System;
using System.Text.Json.Serialization;

namespace Advance_Control.Models
{
    /// <summary>
    /// DTO para los datos del estado de cuenta que se reciben desde la API
    /// </summary>
    public class EstadoCuentaDto
    {
        /// <summary>
        /// ID del estado de cuenta
        /// </summary>
        [JsonPropertyName("estadoCuentaID")]
        public int EstadoCuentaID { get; set; }

        /// <summary>
        /// Fecha de corte del estado de cuenta
        /// </summary>
        [JsonPropertyName("fechaCorte")]
        public DateTime FechaCorte { get; set; }

        /// <summary>
        /// Fecha inicio del periodo
        /// </summary>
        [JsonPropertyName("periodoDesde")]
        public DateTime PeriodoDesde { get; set; }

        /// <summary>
        /// Fecha fin del periodo
        /// </summary>
        [JsonPropertyName("periodoHasta")]
        public DateTime PeriodoHasta { get; set; }

        /// <summary>
        /// Saldo inicial del periodo
        /// </summary>
        [JsonPropertyName("saldoInicial")]
        public decimal SaldoInicial { get; set; }

        /// <summary>
        /// Saldo al momento del corte
        /// </summary>
        [JsonPropertyName("saldoCorte")]
        public decimal SaldoCorte { get; set; }

        /// <summary>
        /// Total de depósitos del periodo
        /// </summary>
        [JsonPropertyName("totalDepositos")]
        public decimal TotalDepositos { get; set; }

        /// <summary>
        /// Total de retiros del periodo
        /// </summary>
        [JsonPropertyName("totalRetiros")]
        public decimal TotalRetiros { get; set; }

        /// <summary>
        /// Comisiones aplicadas
        /// </summary>
        [JsonPropertyName("comisiones")]
        public decimal Comisiones { get; set; }

        /// <summary>
        /// Nombre del archivo PDF/documento
        /// </summary>
        [JsonPropertyName("nombreArchivo")]
        public string? NombreArchivo { get; set; }
    }
}
