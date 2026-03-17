using System;
using System.Globalization;

namespace Advance_Control.Models
{
    public class AbonoFacturaDto
    {
        public int IdAbonoFactura { get; set; }
        public int IdFactura { get; set; }
        public int? IdMovimiento { get; set; }
        public DateTime FechaAbono { get; set; }
        public decimal MontoAbono { get; set; }
        public string? Referencia { get; set; }
        public string? Observaciones { get; set; }
        public decimal SaldoPosterior { get; set; }

        public string FechaAbonoTexto => FechaAbono == default ? string.Empty : FechaAbono.ToString("dd/MM/yyyy HH:mm");
        public string MontoAbonoTexto => MontoAbono.ToString("C2", new CultureInfo("es-MX"));
        public string SaldoPosteriorTexto => SaldoPosterior.ToString("C2", new CultureInfo("es-MX"));
        public string MovimientoTexto => IdMovimiento.HasValue ? $"Movimiento #{IdMovimiento.Value}" : "Sin movimiento relacionado";
        public string ReferenciaTexto => string.IsNullOrWhiteSpace(Referencia) ? "Sin referencia" : Referencia!;
        public string ObservacionesTexto => string.IsNullOrWhiteSpace(Observaciones) ? "Sin observaciones" : Observaciones!;
    }
}
