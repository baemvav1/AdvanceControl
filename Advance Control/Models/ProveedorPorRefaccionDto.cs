using System.Text.Json.Serialization;

namespace Advance_Control.Models
{
    /// <summary>
    /// DTO para la respuesta de la operación select_by_refaccion
    /// Representa un proveedor que tiene una refacción específica con su precio
    /// </summary>
    public class ProveedorPorRefaccionDto
    {
        /// <summary>
        /// ID del proveedor
        /// </summary>
        [JsonPropertyName("idProveedor")]
        public int? IdProveedor { get; set; }

        /// <summary>
        /// Nombre comercial del proveedor
        /// </summary>
        [JsonPropertyName("nombreComercial")]
        public string? NombreComercial { get; set; }

        /// <summary>
        /// Costo/Precio de la refacción para este proveedor
        /// </summary>
        [JsonPropertyName("costo")]
        public double? Costo { get; set; }
    }
}
