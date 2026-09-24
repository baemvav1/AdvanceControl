using System.Collections.Generic;

namespace Advance_Control.Models
{
    public class FacturaDetalleDto
    {
        public FacturaResumenDto? Factura { get; set; }
        public List<FacturaConceptoDto> Conceptos { get; set; } = new();
        public List<FacturaTrasladoDto> TrasladosGlobales { get; set; } = new();
        public List<AbonoFacturaDto> Abonos { get; set; } = new();

        /// <summary>Complementos de Pago que citan esta factura como documento pagado (relación inversa).</summary>
        public List<ComplementoPagoRelacionadoDto> ComplementosRelacionados { get; set; } = new();
    }
}
