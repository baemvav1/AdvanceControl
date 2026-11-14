using System;

namespace Advance_Control.Models
{
    /// <summary>
    /// "Borrar por fechas" en Conciliación: facturas emitidas entre dos fechas y/o el mes de un
    /// estado de cuenta. Con los dos se borra lo que cumpla cualquiera. Simular solo cuenta.
    /// </summary>
    public class ConciliacionBorrarRequestDto
    {
        public DateTime? FacturaDesde { get; set; }
        public DateTime? FacturaHasta { get; set; }
        public DateTime? MesEstadoCuenta { get; set; }
        public bool Simular { get; set; } = true;
    }

    public class ConciliacionBorrarResponseDto
    {
        public bool Simulado { get; set; }
        public int PagosBorrados { get; set; }
        public int IngresosDesligados { get; set; }
        public int ComplementosSoltados { get; set; }
        public int FacturasAfectadas { get; set; }
        public int MovimientosAfectados { get; set; }
        public decimal MontoTotal { get; set; }
    }

    /// <summary>Mes de la factura más vieja (GET api/factura/importacion-automatica/primer-mes).</summary>
    public class ImportacionAutomaticaPrimerMesDto
    {
        public int? Anio { get; set; }
        public int? Mes { get; set; }
    }
}
