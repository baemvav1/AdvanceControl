using System.Text.Json.Serialization;

namespace Advance_Control.Models
{
    /// <summary>
    /// Conteos del dashboard recibidos desde la API.
    /// </summary>
    public class DashboardConteoDto
    {
        [JsonPropertyName("totalOperaciones")]
        public int TotalOperaciones { get; set; }

        [JsonPropertyName("totalOrdenesServicio")]
        public int TotalOrdenesServicio { get; set; }

        [JsonPropertyName("totalClientes")]
        public int TotalClientes { get; set; }

        [JsonPropertyName("totalEquipos")]
        public int TotalEquipos { get; set; }
    }
}
