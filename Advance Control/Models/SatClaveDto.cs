using System.Text.Json.Serialization;

namespace Advance_Control.Models
{
    /// <summary>Una clave del catálogo SAT c_ClaveProdServ o c_ClaveUnidad.</summary>
    public class SatClaveDto
    {
        [JsonPropertyName("clave")]
        public string Clave { get; set; } = string.Empty;

        [JsonPropertyName("descripcion")]
        public string Descripcion { get; set; } = string.Empty;

        public override string ToString() => $"{Clave} - {Descripcion}";
    }

    /// <summary>Una clave del catálogo SAT c_RegimenFiscal o c_UsoCFDI -- ambos indican si aplican a persona física, moral, o ambas.</summary>
    public class SatCatalogoItemDto
    {
        [JsonPropertyName("clave")]
        public string Clave { get; set; } = string.Empty;

        [JsonPropertyName("descripcion")]
        public string Descripcion { get; set; } = string.Empty;

        [JsonPropertyName("aplicaFisica")]
        public bool AplicaFisica { get; set; }

        [JsonPropertyName("aplicaMoral")]
        public bool AplicaMoral { get; set; }

        [JsonPropertyName("estatus")]
        public bool Estatus { get; set; } = true;

        public override string ToString() => $"{Clave} - {Descripcion}";
    }
}
