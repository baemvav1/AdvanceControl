using System;
using System.Globalization;

namespace Advance_Control.Models
{
    public class VisorMundialEquipoFacturaDto
    {
        public int IdFactura { get; set; }
        public string? Folio { get; set; }
        public DateTime? FechaTimbrado { get; set; }
        public decimal Total { get; set; }
        public bool Finiquito { get; set; }
        public decimal Abono { get; set; }
        public DateTime? FechaVencimiento { get; set; }
        public int DiasVencido { get; set; }
        public string? BucketVencimiento { get; set; }
        public int? IdOperacion { get; set; }

        public string FolioTexto => string.IsNullOrWhiteSpace(Folio) ? "Sin folio" : Folio!;
        public string FechaTimbradoTexto => FechaTimbrado.HasValue ? FechaTimbrado.Value.ToString("dd/MM/yyyy HH:mm") : "Sin fecha";
        public string TotalTexto => Total.ToString("C2", new CultureInfo("es-MX"));
        public string EstadoTexto => Finiquito ? "Finiquitada" : "Pendiente";
        public string BucketVencimientoTexto => string.IsNullOrWhiteSpace(BucketVencimiento) ? "Sin vencimiento" : BucketVencimiento!;
        public string DiasVencidoTexto => DiasVencido > 0 ? $"Vencida hace {DiasVencido} día(s)" : BucketVencimientoTexto;
    }
}
