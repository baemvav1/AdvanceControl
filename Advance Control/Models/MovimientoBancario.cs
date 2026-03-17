using System;
using System.Collections.Generic;
using System.Globalization;

namespace Advance_Control.Models
{
    public class MovimientoBancario
    {
        public int IdMovimiento { get; set; }
        public int IdEstadoCuentaBancario { get; set; }
        public string GrupoId { get; set; } = string.Empty;
        public int? Dia { get; set; }
        public DateTime FechaMovimiento { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public string? Referencia { get; set; }
        public decimal? MontoCargo { get; set; }
        public decimal? MontoAbono { get; set; }
        public decimal SaldoResultante { get; set; }
        public bool Conciliado { get; set; }
        public string TipoMovimiento { get; set; } = string.Empty;
        public string? SubtipoMovimiento { get; set; }
        public string? TipoGrupo { get; set; }
        public Dictionary<string, string?> Metadatos { get; set; } = new();
        public List<MovimientoRelacionadoBancario> MovimientosRelacionados { get; set; } = new();
        public DateTime FechaRegistro { get; set; }

        public string FechaTexto => FechaMovimiento == default ? string.Empty : FechaMovimiento.ToString("dd/MM/yyyy");
        public string CargoTexto => !MontoCargo.HasValue || MontoCargo.Value == 0m ? "-" : MontoCargo.Value.ToString("C2", new CultureInfo("es-MX"));
        public string AbonoTexto => !MontoAbono.HasValue || MontoAbono.Value == 0m ? "-" : MontoAbono.Value.ToString("C2", new CultureInfo("es-MX"));
        public string SaldoTexto => SaldoResultante.ToString("C2", new CultureInfo("es-MX"));
        public string TipoDetalle => string.IsNullOrWhiteSpace(SubtipoMovimiento)
            ? (TipoMovimiento ?? TipoGrupo ?? string.Empty)
            : $"{TipoMovimiento ?? TipoGrupo} · {SubtipoMovimiento}";
        public string ConciliadoTexto => Conciliado ? "Conciliado" : "Pendiente";
        public string RelacionadosTexto => MovimientosRelacionados.Count == 0 ? "Sin relacionados" : $"{MovimientosRelacionados.Count} relacionados";

        public MovimientoBancario()
        {
            FechaRegistro = DateTime.Now;
        }
    }
}
