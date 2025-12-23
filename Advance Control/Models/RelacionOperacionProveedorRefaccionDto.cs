using System.Text.Json.Serialization;

namespace Advance_Control.Models
{
    /// <summary>
    /// DTO para los datos de relación operación-proveedor-refacción que se reciben desde la API
    /// </summary>
    public class RelacionOperacionProveedorRefaccionDto
    {
        [JsonPropertyName("idRelacionOperacionProveedorRefaccion")]
        public int? IdRelacionOperacionProveedorRefaccion { get; set; }

        [JsonPropertyName("idProveedorRefaccion")]
        public int? IdProveedorRefaccion { get; set; }

        [JsonPropertyName("descripcion")]
        public string? Descripcion { get; set; }

        [JsonPropertyName("marca")]
        public string? Marca { get; set; }

        [JsonPropertyName("serie")]
        public string? Serie { get; set; }

        [JsonPropertyName("precio")]
        public float? Precio { get; set; }

        [JsonPropertyName("nota")]
        public string? Nota { get; set; }
    }
}
