using System;
using System.Text.Json.Serialization;

namespace Advance_Control.Models
{
    /// <summary>
    /// DTO para los datos de un depósito asociado a un estado de cuenta
    /// </summary>
    public class DepositoDto
    {
        /// <summary>
        /// ID del depósito
        /// </summary>
        [JsonPropertyName("depositoID")]
        public int DepositoID { get; set; }

        /// <summary>
        /// ID del estado de cuenta al que pertenece el depósito
        /// </summary>
        [JsonPropertyName("estadoCuentaID")]
        public int EstadoCuentaID { get; set; }

        /// <summary>
        /// Fecha del depósito
        /// </summary>
        [JsonPropertyName("fechaDeposito")]
        public DateTime FechaDeposito { get; set; }

        /// <summary>
        /// Descripción del depósito
        /// </summary>
        [JsonPropertyName("descripcion")]
        public string? Descripcion { get; set; }

        /// <summary>
        /// Monto del depósito
        /// </summary>
        [JsonPropertyName("monto")]
        public decimal Monto { get; set; }

        /// <summary>
        /// Tipo de depósito (Transferencia, Efectivo, Cheque, etc.)
        /// </summary>
        [JsonPropertyName("tipo")]
        public string? Tipo { get; set; }
    }
}
