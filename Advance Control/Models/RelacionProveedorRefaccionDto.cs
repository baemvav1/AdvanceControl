using System.Text.Json.Serialization;

namespace Advance_Control.Models
{
    /// <summary>
    /// DTO para los datos de relación proveedor-refacción que se reciben desde la API
    /// </summary>
    public class RelacionProveedorRefaccionDto
    {
        [JsonPropertyName("idRelacionProveedor")]
        public int IdRelacionProveedor { get; set; }

        [JsonPropertyName("idRefaccion")]
        public int IdRefaccion { get; set; }

        [JsonPropertyName("marca")]
        public string? Marca { get; set; }

        [JsonPropertyName("serie")]
        public string? Serie { get; set; }

        [JsonPropertyName("costo")]
        public double? Precio { get; set; }

        [JsonPropertyName("nota")]
        public string? Nota { get; set; }

        [JsonPropertyName("creado")]
        public int? Creado { get; set; }
    }
}
