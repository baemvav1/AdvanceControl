using System.Collections.Generic;

namespace Advance_Control.Models
{
    public class ConciliacionAutomaticaResponseDto
    {
        public bool Success { get; set; }
        public int IdFactura { get; set; }
        public int IdMovimiento { get; set; }
        public int IdAbonoFactura { get; set; }
        public decimal TotalAbonado { get; set; }
        public decimal SaldoPendiente { get; set; }
        public bool? Finiquito { get; set; }
        public bool Conciliado { get; set; }
        public int TotalFacturasProcesadas { get; set; }
        public decimal MontoAplicado { get; set; }
        public List<int> IdFacturas { get; set; } = new();
        public List<int> IdAbonosFactura { get; set; } = new();
        public string? Message { get; set; }
    }
}
