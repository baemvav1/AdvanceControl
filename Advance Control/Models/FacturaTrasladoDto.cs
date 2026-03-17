using System.Globalization;

namespace Advance_Control.Models
{
    public class FacturaTrasladoDto
    {
        public int IdTraslado { get; set; }
        public int? IdFacturaConcepto { get; set; }
        public int Orden { get; set; }
        public decimal Base { get; set; }
        public string? Impuesto { get; set; }
        public string? TipoFactor { get; set; }
        public decimal TasaOCuota { get; set; }
        public decimal Importe { get; set; }

        public string BaseTexto => Base.ToString("C2", new CultureInfo("es-MX"));
        public string ImporteTexto => Importe.ToString("C2", new CultureInfo("es-MX"));
        public string TasaTexto => TasaOCuota == 0m ? "-" : TasaOCuota.ToString("P2", CultureInfo.InvariantCulture);
        public string ImpuestoResumen => string.IsNullOrWhiteSpace(Impuesto)
            ? "Impuesto no especificado"
            : $"{Impuesto} · {TipoFactor ?? "Sin tipo"}";
    }
}
