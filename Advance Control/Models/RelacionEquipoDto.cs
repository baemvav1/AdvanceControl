using System.Text.Json.Serialization;

namespace Advance_Control.Models
{
    /// <summary>
    /// DTO para los datos de relación refacción-equipo que se reciben desde la API
    /// </summary>
    public class RelacionEquipoDto
    {
        [JsonPropertyName("idRelacionRefaccion")]
        public int IdRelacionRefaccion { get; set; }

        [JsonPropertyName("idEquipo")]
        public int IdEquipo { get; set; }

        [JsonPropertyName("marca")]
        public string? Marca { get; set; }

        [JsonPropertyName("identificador")]
        public string? Identificador { get; set; }

        [JsonPropertyName("nota")]
        public string? Nota { get; set; }

        [JsonPropertyName("creado")]
        public int? Creado { get; set; }
    }
}
