using System;
using System.Globalization;

namespace Advance_Control.Models
{
    public class ImpuestoMovimientoDetalleDto
    {
        public int IdImpuesto { get; set; }
        public int IdMovimiento { get; set; }
        public string? TipoImpuesto { get; set; }
        public string? Rfc { get; set; }
        public decimal Monto { get; set; }
        public DateTime? Fecha { get; set; }
        public string? Descripcion { get; set; }

        public string FechaTexto => Fecha.HasValue ? Fecha.Value.ToString("dd/MM/yyyy") : string.Empty;

        public string MontoTexto => Monto.ToString("C2", new CultureInfo("es-MX"));
    }
}
