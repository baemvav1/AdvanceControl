using System;
using System.Globalization;
using System.Linq;

namespace Advance_Control.Models
{
    public class ConciliacionMovimientoResumenDto
    {
        public int IdEstadoCuenta { get; set; }
        public int IdMovimiento { get; set; }
        public string NumeroCuenta { get; set; } = string.Empty;
        public string? TipoCuenta { get; set; }
        public string? Banco { get; set; }
        public string? Titular { get; set; }
        public string GrupoId { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
        public string? TipoOperacion { get; set; }
        public string? SubtipoOperacion { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public string? Referencia { get; set; }
        public decimal Cargo { get; set; }
        public decimal Abono { get; set; }
        public decimal Saldo { get; set; }
        public int RelacionadosCount { get; set; }
        public string PeriodoTexto { get; set; } = string.Empty;
        public string MetadatosTexto { get; set; } = "Sin metadatos adicionales.";

        public string CuentaTitulo => string.IsNullOrWhiteSpace(TipoCuenta)
            ? NumeroCuenta
            : $"{NumeroCuenta} · {TipoCuenta}";

        public string BancoTitularTexto => string.Join(" · ", new[] { Banco, Titular }.Where(valor => !string.IsNullOrWhiteSpace(valor)));
        public string FechaTexto => Fecha == default ? string.Empty : Fecha.ToString("dd/MM/yyyy");
        public string TipoTitulo => string.IsNullOrWhiteSpace(SubtipoOperacion)
            ? (TipoOperacion ?? "MOVIMIENTO")
            : $"{TipoOperacion ?? "MOVIMIENTO"} · {SubtipoOperacion}";
        public string ReferenciaTexto => string.IsNullOrWhiteSpace(Referencia) ? "Sin referencia" : $"{Referencia}";
        public string CargoTexto => Cargo == 0m ? "-" : Cargo.ToString("C2", new CultureInfo("es-MX"));
        public string AbonoTexto => Abono == 0m ? "-" : Abono.ToString("C2", new CultureInfo("es-MX"));
        public string SaldoTexto => Saldo.ToString("C2", new CultureInfo("es-MX"));
        public string RelacionadosTexto => RelacionadosCount == 0 ? "Sin relacionados" : $"{RelacionadosCount} relacionados";
        public string PendienteTexto => "Pendiente de conciliacion";
    }
}
