using System;

namespace Advance_Control.Models
{
    public class RegistrarAbonoFacturaRequestDto
    {
        public int IdFactura { get; set; }
        public int? IdMovimiento { get; set; }
        public DateTime FechaAbono { get; set; } = DateTime.Now;
        public decimal MontoAbono { get; set; }
        public string? Referencia { get; set; }
        public string? Observaciones { get; set; }
    }
}
