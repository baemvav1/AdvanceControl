using System;
using System.Collections.Generic;

namespace Advance_Control.Models
{
    public class ConciliacionAutomaticaFacturaDto
    {
        public int IdFactura { get; set; }
        public decimal MontoAbono { get; set; }
    }

    public class ConciliacionAutomaticaRequestDto
    {
        public int IdFactura { get; set; }
        public List<ConciliacionAutomaticaFacturaDto> Facturas { get; set; } = new();
        public int IdMovimiento { get; set; }
        public DateTime FechaAbono { get; set; }
        public decimal MontoAbono { get; set; }
        public string? Referencia { get; set; }
        public string? Observaciones { get; set; }
        public bool RegistrarEnBitacoraConciliacion { get; set; }
        public string? TipoOperacionBitacoraConciliacion { get; set; }
    }
}
