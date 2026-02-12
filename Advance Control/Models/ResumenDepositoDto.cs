using System.Text.Json.Serialization;

namespace Advance_Control.Models
{
    /// <summary>
    /// DTO para el resumen de depósitos agrupados por tipo
    /// </summary>
    public class ResumenDepositoDto
    {
        /// <summary>
        /// Tipo de depósito
        /// </summary>
        [JsonPropertyName("tipo")]
        public string? Tipo { get; set; }

        /// <summary>
        /// Cantidad de depósitos de este tipo
        /// </summary>
        [JsonPropertyName("cantidadDepositos")]
        public int CantidadDepositos { get; set; }

        /// <summary>
        /// Total del monto de depósitos de este tipo
        /// </summary>
        [JsonPropertyName("totalMonto")]
        public decimal TotalMonto { get; set; }
    }
}
