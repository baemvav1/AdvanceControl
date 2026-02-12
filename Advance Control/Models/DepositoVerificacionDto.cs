using System.Text.Json.Serialization;

namespace Advance_Control.Models
{
    /// <summary>
    /// DTO para el resultado de la verificación de un depósito
    /// </summary>
    public class DepositoVerificacionDto
    {
        /// <summary>
        /// Indica si el depósito existe
        /// </summary>
        [JsonPropertyName("existe")]
        public bool Existe { get; set; }

        /// <summary>
        /// ID del depósito si existe, null si no existe
        /// </summary>
        [JsonPropertyName("depositoID")]
        public int? DepositoID { get; set; }

        /// <summary>
        /// Mensaje descriptivo del resultado
        /// </summary>
        [JsonPropertyName("mensaje")]
        public string? Mensaje { get; set; }
    }
}
