using System.Globalization;

namespace Advance_Control.Models
{
    public class MovimientoRelacionadoBancario
    {
        public string Tipo { get; set; } = string.Empty;
        public int? Orden { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public string? Rfc { get; set; }
        public decimal? Monto { get; set; }
        public decimal? Saldo { get; set; }

        public string MontoTexto => !Monto.HasValue || Monto.Value == 0m ? "-" : Monto.Value.ToString("C2", new CultureInfo("es-MX"));
    }
}
