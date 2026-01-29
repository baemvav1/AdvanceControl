using System.Text.Json.Serialization;

namespace Advance_Control.Models
{
    /// <summary>
    /// DTO para la respuesta del endpoint check-proveedor de refacciones
    /// </summary>
    public class CheckProveedorResponseDto
    {
        /// <summary>
        /// Indica si existe relación con proveedores
        /// </summary>
        [JsonPropertyName("exists")]
        public bool Exists { get; set; }

        /// <summary>
        /// Resultado numérico (0 o 1) de la verificación
        /// </summary>
        [JsonPropertyName("result")]
        public int Result { get; set; }
    }
}
