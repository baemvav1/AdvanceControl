using System;
using System.Globalization;

namespace Advance_Control.Models
{
    public class ReporteCobranzaItemDto
    {
        public int IdFactura { get; set; }
        public string? Folio { get; set; }
        public DateTime FechaFactura { get; set; }
        public decimal Total { get; set; }
        public bool Pagada { get; set; }
        public int? IdOperacion { get; set; }
        public string? OperacionIdentificador { get; set; }
        public string? OperacionCliente { get; set; }
        public string? TecnicoAtiende { get; set; }
        public string? MovimientosTexto { get; set; }

        public string FolioTexto => string.IsNullOrWhiteSpace(Folio) ? "Sin folio" : Folio;
        public string FechaFacturaTexto => FechaFactura == default ? string.Empty : FechaFactura.ToString("dd/MM/yyyy");
        public string TotalTexto => Total.ToString("C2", new CultureInfo("es-MX"));
        public string PagadaTexto => Pagada ? "Pagada" : "Pendiente";
        public string OperacionTexto => IdOperacion.HasValue
            ? $"#{IdOperacion} - {OperacionCliente ?? "Sin cliente"} ({OperacionIdentificador ?? "Sin equipo"})"
            : "Sin operación vinculada";
        public string TecnicoTexto => string.IsNullOrWhiteSpace(TecnicoAtiende) ? "Sin técnico asignado" : TecnicoAtiende;
        public string MovimientosResumenTexto => string.IsNullOrWhiteSpace(MovimientosTexto) ? "Sin movimientos" : MovimientosTexto;
    }
}
