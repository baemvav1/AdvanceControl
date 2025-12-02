using System.Text.Json.Serialization;

namespace Advance_Control.Models
{
    /// <summary>
    /// Generic API response model that wraps success status and message from the API
    /// </summary>
    public class ApiResponse
    {
        /// <summary>
        /// Indicates whether the operation was successful
        /// </summary>
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        /// <summary>
        /// Message from the API (error message when success is false)
        /// </summary>
        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }
}
