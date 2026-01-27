using System.Text.Json.Serialization;

namespace Advance_Control.Models
{
    /// <summary>
    /// Response model for cliente CRUD operations
    /// </summary>
    public class ClienteOperationResponse
    {
        /// <summary>
        /// Indicates whether the operation was successful
        /// </summary>
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        /// <summary>
        /// Message from the API
        /// </summary>
        [JsonPropertyName("message")]
        public string? Message { get; set; }

        /// <summary>
        /// ID of the cliente (returned from create operations)
        /// </summary>
        [JsonPropertyName("id_cliente")]
        public int? IdCliente { get; set; }
    }
}
