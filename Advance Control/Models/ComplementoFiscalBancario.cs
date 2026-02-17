using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Advance_Control.Models
{
    public class ComplementoFiscalBancario
    {
        public int IdComplementoFiscal { get; set; }
        public int IdEstadoCuentaBancario { get; set; }
        public string Uuid { get; set; }
        public DateTime FechaTimbrado { get; set; }
        public string NumeroProveedor { get; set; }
        public string FormaPago { get; set; }
        public string MetodoPago { get; set; }
        public string UsoCFDI { get; set; }
        public string ClaveProducto { get; set; }
        public string CodigoPostal { get; set; }

        // Navegación
        public virtual EstadoCuentaBancario EstadoCuentaBancario { get; set; }
    }
}
