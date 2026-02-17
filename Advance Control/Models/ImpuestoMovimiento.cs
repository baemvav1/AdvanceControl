using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Advance_Control.Models
{
    public class ImpuestoMovimiento
    {
        public int IdImpuestoMovimiento { get; set; }
        public int IdMovimiento { get; set; }
        public string TipoImpuesto { get; set; } // 'IVA', 'ISR'
        public string RfcRelacionado { get; set; }
        public decimal MontoImpuesto { get; set; }

        // Navegación
        public virtual MovimientoBancario MovimientoBancario { get; set; }
    }
}
