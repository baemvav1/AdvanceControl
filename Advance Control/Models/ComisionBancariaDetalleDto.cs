using System;
using System.Globalization;

namespace Advance_Control.Models
{
    public class ComisionBancariaDetalleDto
    {
        public int IdComision { get; set; }
        public int IdMovimiento { get; set; }
        public string? TipoComision { get; set; }
        public decimal Monto { get; set; }
        public decimal? Iva { get; set; }
        public string? Referencia { get; set; }
        public DateTime? Fecha { get; set; }
        public string? Descripcion { get; set; }

        public string FechaTexto => Fecha.HasValue ? Fecha.Value.ToString("dd/MM/yyyy") : string.Empty;

        public string MontoTexto => Monto.ToString("C2", new CultureInfo("es-MX"));

        public string IvaTexto => (Iva ?? 0m).ToString("C2", new CultureInfo("es-MX"));
    }
}
