namespace Advance_Control.Models
{
    public class RegistrarAbonoFacturaResponseDto
    {
        public bool Success { get; set; }
        public int IdAbonoFactura { get; set; }
        public int IdFactura { get; set; }
        public int? IdMovimiento { get; set; }
        public decimal TotalAbonado { get; set; }
        public decimal SaldoPendiente { get; set; }
        public bool? Finiquito { get; set; }
        public string? Message { get; set; }
    }
}
