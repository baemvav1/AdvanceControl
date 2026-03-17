using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Advance_Control.Models
{
    public class GuardarEstadoCuentaGrupoDto
    {
        [JsonPropertyName("ordenGrupo")]
        public int OrdenGrupo { get; set; }

        [JsonPropertyName("grupoId")]
        public string GrupoId { get; set; } = string.Empty;

        [JsonPropertyName("dia")]
        public int? Dia { get; set; }

        [JsonPropertyName("tipo")]
        public string? Tipo { get; set; }

        [JsonPropertyName("transaccionPrincipal")]
        public GuardarEstadoCuentaMovimientoDto TransaccionPrincipal { get; set; } = new();

        [JsonPropertyName("movimientosRelacionados")]
        public List<GuardarEstadoCuentaMovimientoRelacionadoDto> MovimientosRelacionados { get; set; } = new();
    }
}
