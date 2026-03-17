using System;
using System.Globalization;

namespace Advance_Control.Models
{
    public class MovimientoEstadoCuentaDto
    {
        public int IdMovimiento { get; set; }
        public int IdEstadoCuenta { get; set; }
        public DateTime Fecha { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public string? Referencia { get; set; }
        public decimal Cargo { get; set; }
        public decimal Abono { get; set; }
        public decimal Saldo { get; set; }
        public string? TipoOperacion { get; set; }

        public string FechaTexto => Fecha == default ? string.Empty : Fecha.ToString("dd/MM/yyyy");

        public string CargoTexto => Cargo == 0m ? "-" : Cargo.ToString("C2", new CultureInfo("es-MX"));

        public string AbonoTexto => Abono == 0m ? "-" : Abono.ToString("C2", new CultureInfo("es-MX"));

        public string SaldoTexto => Saldo.ToString("C2", new CultureInfo("es-MX"));
    }
}
