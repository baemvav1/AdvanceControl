using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Advance_Control.Models
{
    public class PagoServicioBancario
    {
        public int IdPagoServicio { get; set; }
        public int IdMovimiento { get; set; }
        public string TipoServicio { get; set; } // 'IMSS', 'IMSS-VIV-RCV', 'SAT', 'IMP'
        public string ReferenciaPago { get; set; }
        public decimal MontoPago { get; set; }
        public string MedioPago { get; set; } // 'AFIRMENET', etc.

        // Navegación
        public virtual MovimientoBancario MovimientoBancario { get; set; }
    }
}
