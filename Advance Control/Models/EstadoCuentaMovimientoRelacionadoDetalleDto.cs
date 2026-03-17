using System.Globalization;

namespace Advance_Control.Models
{
    public class EstadoCuentaMovimientoRelacionadoDetalleDto
    {
        public int IdMovimientoRelacionado { get; set; }
        public int IdMovimiento { get; set; }
        public string? TipoRelacion { get; set; }
        public int? Orden { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public string? Rfc { get; set; }
        public decimal Monto { get; set; }
        public decimal? Saldo { get; set; }

        public string Titulo => string.IsNullOrWhiteSpace(TipoRelacion) ? "RELACIONADO" : TipoRelacion!;
        public string TipoRelacionTexto => string.IsNullOrWhiteSpace(TipoRelacion) ? "Tipo no especificado" : TipoRelacion!;
        public string OrdenTexto => Orden.HasValue ? $"Orden: {Orden}" : "Orden no especificado";
        public string MontoTexto => Monto == 0m ? "-" : Monto.ToString("C2", new CultureInfo("es-MX"));
        public string SaldoTexto => !Saldo.HasValue ? "No aplica" : Saldo.Value.ToString("C2", new CultureInfo("es-MX"));
        public string RfcTexto => string.IsNullOrWhiteSpace(Rfc) ? "Sin RFC" : $"RFC: {Rfc}";
    }
}
