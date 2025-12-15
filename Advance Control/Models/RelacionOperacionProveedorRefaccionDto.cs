using System.Text.Json.Serialization;

namespace Advance_Control.Models
{
    /// <summary>
    /// DTO para los datos de relación operación-proveedor-refacción que se reciben desde la API
    /// Incluye información de la refacción y proveedor para mostrar en la UI
    /// </summary>
    public class RelacionOperacionProveedorRefaccionDto
    {
        [JsonPropertyName("idRelacionOperacion_ProveedorRefaccion")]
        public int IdRelacionOperacionProveedorRefaccion { get; set; }

        [JsonPropertyName("idProveedorRefaccion")]
        public int IdProveedorRefaccion { get; set; }

        [JsonPropertyName("precio")]
        public double? Precio { get; set; }

        [JsonPropertyName("nota")]
        public string? Nota { get; set; }

        // Campos adicionales que pueden venir del join en la base de datos
        // para mostrar información de la refacción y proveedor
        [JsonPropertyName("marca")]
        public string? Marca { get; set; }

        [JsonPropertyName("serie")]
        public string? Serie { get; set; }

        [JsonPropertyName("razonSocial")]
        public string? RazonSocial { get; set; }

        [JsonPropertyName("nombreComercial")]
        public string? NombreComercial { get; set; }
    }
}
