using System;

namespace Advance_Control.Models
{
    /// <summary>
    /// Espejo de AdvanceApi.DTOs.ConciliacionFiltroFechasDto: rango de la cinta de Conciliación
    /// (botón Rangos). Facturas emitidas entre FacturaDesde y FacturaHasta y movimientos del
    /// estado de cuenta del mes de MesEstadoCuenta. Null = sin filtro.
    /// </summary>
    public class ConciliacionFiltroFechasDto
    {
        public DateTime? FacturaDesde { get; set; }
        public DateTime? FacturaHasta { get; set; }
        public DateTime? MesEstadoCuenta { get; set; }

        public void CopiarA(ConciliacionFiltroFechasDto destino)
        {
            destino.FacturaDesde = FacturaDesde;
            destino.FacturaHasta = FacturaHasta;
            destino.MesEstadoCuenta = MesEstadoCuenta;
        }
    }
}
