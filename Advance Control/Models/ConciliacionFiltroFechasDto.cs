using System;
using System.Collections.Generic;

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

        /// <summary>Facturas visibles en el grid con los filtros de columna; null = sin filtro.</summary>
        public List<int>? IdsFacturas { get; set; }

        /// <summary>Movimientos visibles en el grid con los filtros de columna; null = sin filtro.</summary>
        public List<int>? IdsMovimientos { get; set; }

        /// <summary>Razón social sobre la que se concilia (sus facturas y los movimientos de sus cuentas).</summary>
        public int? IdEntidad { get; set; }

        public void CopiarA(ConciliacionFiltroFechasDto destino)
        {
            destino.FacturaDesde = FacturaDesde;
            destino.FacturaHasta = FacturaHasta;
            destino.MesEstadoCuenta = MesEstadoCuenta;
            destino.IdsFacturas = IdsFacturas;
            destino.IdsMovimientos = IdsMovimientos;
            destino.IdEntidad = IdEntidad;
        }
    }
}
