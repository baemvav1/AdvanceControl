using System;
using System.Globalization;

namespace Advance_Control.Models
{
    public class ReporteCobranzaItemDto
    {
        public int OrdenGrupo { get; set; }
        public string? Estado { get; set; }
        public int? IdFactura { get; set; }
        public string? Folio { get; set; }
        public DateTime FechaFactura { get; set; }
        public decimal Total { get; set; }
        public bool Pagada { get; set; }
        public int? IdOperacion { get; set; }
        public string? OperacionIdentificador { get; set; }
        public string? OperacionCliente { get; set; }
        public string? TecnicoAtiende { get; set; }
        public string? MovimientosTexto { get; set; }

        /// <summary>True cuando la fila es una operación finalizada que aún no tiene factura (grupo 2).</summary>
        public bool EsSinFacturar => OrdenGrupo == 2;

        public string FolioTexto => EsSinFacturar
            ? "Sin facturar"
            : string.IsNullOrWhiteSpace(Folio) ? "Sin folio" : Folio;
        public string FechaFacturaTexto => FechaFactura == default ? string.Empty : FechaFactura.ToString("dd/MM/yyyy");
        public string TotalTexto => Total.ToString("C2", new CultureInfo("es-MX"));
        public string PagadaTexto => Pagada ? "Pagada" : "Pendiente";
        public string EstadoTexto => Estado ?? PagadaTexto;
        public string OperacionTexto => IdOperacion.HasValue
            ? $"#{IdOperacion} - {OperacionCliente ?? "Sin cliente"} ({OperacionIdentificador ?? "Sin equipo"})"
            : "Sin operación vinculada";
        public string TecnicoTexto => string.IsNullOrWhiteSpace(TecnicoAtiende) ? "Sin técnico asignado" : TecnicoAtiende;
        public string MovimientosResumenTexto => EsSinFacturar
            ? "No aplica (sin factura)"
            : string.IsNullOrWhiteSpace(MovimientosTexto) ? "Sin movimientos" : MovimientosTexto;
    }
}
