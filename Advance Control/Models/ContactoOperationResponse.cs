using System.Text.Json.Serialization;

namespace Advance_Control.Models
{
    /// <summary>
    /// Response model for contacto CRUD operations
    /// </summary>
    public class ContactoOperationResponse
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
        /// ID of the contacto (returned from create operations)
        /// </summary>
        [JsonPropertyName("contactoId")]
        public long? ContactoId { get; set; }
    }
}
