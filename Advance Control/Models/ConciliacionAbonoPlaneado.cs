using System.Globalization;

namespace Advance_Control.Models
{
    /// <summary>Un abono por registrar desde Conciliación ("Abonar"): movimiento → factura por un monto.</summary>
    public sealed record ConciliacionAbonoPlaneado(
        ConciliacionFacturaFila Factura,
        ConciliacionMovimientoFila Movimiento,
        decimal Monto)
    {
        public string MontoTexto => Monto.ToString("C2", new CultureInfo("es-MX"));
    }
}
