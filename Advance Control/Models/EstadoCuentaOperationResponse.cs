using System.Text.Json.Serialization;

namespace Advance_Control.Models
{
    /// <summary>
    /// DTO para las respuestas de operaciones de estado de cuenta (crear, agregar depósito)
    /// </summary>
    public class EstadoCuentaOperationResponse
    {
        /// <summary>
        /// ID del recurso creado
        /// </summary>
        [JsonPropertyName("id")]
        public int Id { get; set; }

        /// <summary>
        /// Mensaje descriptivo de la operación
        /// </summary>
        [JsonPropertyName("message")]
        public string? Message { get; set; }

        /// <summary>
        /// Indica si la operación fue exitosa (campo local, no de la API)
        /// </summary>
        [JsonIgnore]
        public bool Success { get; set; }
    }
}
