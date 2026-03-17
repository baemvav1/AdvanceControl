using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Advance_Control.Models
{
    public class FacturaConceptoDto
    {
        public int IdFacturaConcepto { get; set; }
        public int Orden { get; set; }
        public string? ClaveProdServ { get; set; }
        public decimal Cantidad { get; set; }
        public string? ClaveUnidad { get; set; }
        public string? Unidad { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public decimal ValorUnitario { get; set; }
        public decimal Importe { get; set; }
        public string? ObjetoImp { get; set; }
        public List<FacturaTrasladoDto> Traslados { get; set; } = new();

        public string CantidadTexto => Cantidad.ToString("N2", new CultureInfo("es-MX"));
        public string ValorUnitarioTexto => ValorUnitario.ToString("C2", new CultureInfo("es-MX"));
        public string ImporteTexto => Importe.ToString("C2", new CultureInfo("es-MX"));
        public string ClavesTexto => $"{ClaveProdServ ?? "Sin clave"} · {ClaveUnidad ?? "Sin unidad"}";
        public string UnidadTexto => string.IsNullOrWhiteSpace(Unidad) ? "Sin unidad" : Unidad!;
        public string ImpuestosTexto => Traslados.Count == 0
            ? "Sin traslados"
            : string.Join(" | ", Traslados.Select(traslado => $"{traslado.ImpuestoResumen}: {traslado.ImporteTexto}"));
    }
}
