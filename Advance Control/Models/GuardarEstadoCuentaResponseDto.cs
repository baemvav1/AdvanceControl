using System.Text.Json.Serialization;

namespace Advance_Control.Models
{
    public class GuardarEstadoCuentaResponseDto : ApiResponse
    {
        [JsonPropertyName("idEstadoCuenta")]
        public int IdEstadoCuenta { get; set; }

        [JsonPropertyName("accion")]
        public string? Accion { get; set; }

        [JsonPropertyName("movimientosProcesados")]
        public int MovimientosProcesados { get; set; }
    }
}
