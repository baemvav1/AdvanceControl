using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Advance_Control.Models
{
    /// <summary>
    /// Cliente creado a partir de una factura por fn_clientes_importar_de_facturas
    /// </summary>
    public class ClienteImportadoDto
    {
        [JsonPropertyName("idCliente")]
        public int IdCliente { get; set; }

        [JsonPropertyName("rfc")]
        public string? Rfc { get; set; }

        [JsonPropertyName("nombreComercial")]
        public string? NombreComercial { get; set; }
    }

    /// <summary>
    /// Respuesta de POST /api/Clientes/importar-de-facturas
    /// </summary>
    public class ImportarClientesResponseDto
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("cantidad")]
        public int Cantidad { get; set; }

        [JsonPropertyName("clientes")]
        public List<ClienteImportadoDto> Clientes { get; set; } = new();
    }
}
