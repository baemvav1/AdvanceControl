using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Advance_Control.Models
{
    public class EstadoCuentaGrupoDetalleDto
    {
        public int IdMovimiento { get; set; }
        public string GrupoId { get; set; } = string.Empty;
        public int? Dia { get; set; }
        public DateTime Fecha { get; set; }
        public string? TipoGrupo { get; set; }
        public string? TipoOperacion { get; set; }
        public string? SubtipoOperacion { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public string? Referencia { get; set; }
        public decimal Cargo { get; set; }
        public decimal Abono { get; set; }
        public decimal Saldo { get; set; }
        public bool Conciliado { get; set; }
        public Dictionary<string, string?> Metadatos { get; set; } = new();
        public string? RfcEmisor { get; set; }
        public List<EstadoCuentaMovimientoRelacionadoDetalleDto> MovimientosRelacionados { get; set; } = new();

        public string FechaTexto => Fecha == default ? string.Empty : Fecha.ToString("dd/MM/yyyy");
        public string CargoTexto => Cargo == 0m ? "-" : Cargo.ToString("C2", new CultureInfo("es-MX"));
        public string AbonoTexto => Abono == 0m ? "-" : Abono.ToString("C2", new CultureInfo("es-MX"));
        public string SaldoTexto => Saldo.ToString("C2", new CultureInfo("es-MX"));
        public string TipoTitulo => string.IsNullOrWhiteSpace(SubtipoOperacion)
            ? (TipoOperacion ?? TipoGrupo ?? "MOVIMIENTO")
            : $"{TipoOperacion ?? TipoGrupo ?? "MOVIMIENTO"} · {SubtipoOperacion}";
        public string ReferenciaTexto => string.IsNullOrWhiteSpace(Referencia) ? "Sin referencia" : $"Ref: {Referencia}";
        public string ConciliadoTexto => Conciliado ? "Conciliado" : "Pendiente";
        public string RelacionadosResumen => MovimientosRelacionados.Count == 0
            ? "Sin movimientos relacionados"
            : $"{MovimientosRelacionados.Count} movimientos relacionados";
        public string MetadatosResumen => Metadatos.Count == 0
            ? string.Empty
            : string.Join(" | ", Metadatos
                .Where(par => !string.IsNullOrWhiteSpace(par.Value))
                .Select(par => $"{FormatearClave(par.Key)}: {par.Value}"));
        public string MetadatosTexto => string.IsNullOrWhiteSpace(MetadatosResumen)
            ? "Sin metadatos adicionales."
            : MetadatosResumen;

        private static string FormatearClave(string clave)
        {
            if (string.IsNullOrWhiteSpace(clave))
            {
                return string.Empty;
            }

            return clave.Replace("_", " ").Replace(".", " ");
        }
    }
}
