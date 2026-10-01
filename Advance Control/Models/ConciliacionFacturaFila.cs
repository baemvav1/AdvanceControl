using System;
using System.Globalization;

namespace Advance_Control.Models
{
    /// <summary>
    /// Fila plana del listado de facturas pendientes de Conciliación. Propiedades
    /// simples para que TableView ordene y filtre por valor.
    /// </summary>
    public class ConciliacionFacturaFila
    {
        public int IdFactura { get; set; }
        /// <summary>Posición de carga (fecha, id); desempate estable al ordenar en la tabla.</summary>
        public int Orden { get; set; }
        public string Folio { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
        public decimal Total { get; set; }
        public string RazonSocial { get; set; } = string.Empty;
        public string Rfc { get; set; } = string.Empty;

        /// <summary>Factura original, para abrirla en el visor.</summary>
        public FacturaResumenDto Factura { get; set; } = null!;

        public string FechaTexto => Fecha.ToString("dd/MM/yyyy");
        /// <summary>Saldo por cobrar (menor al total si ya tiene abonos).</summary>
        public decimal SaldoPendiente => Factura?.SaldoPendiente ?? Total;

        public string TotalTexto => Total.ToString("C2", new CultureInfo("es-MX"));
    }
}
