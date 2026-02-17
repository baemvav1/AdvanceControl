using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Advance_Control.Models
{
    public class OperacionInternacional
    {
        public int IdOperacionInternacional { get; set; }
        public int IdMovimiento { get; set; }
        public string TipoOperacion { get; set; } // 'RECEPCION', 'ENVIO'
        public string ReferenciaOperacion { get; set; }
        public decimal MontoOperacion { get; set; }
        public decimal? MontoIVA { get; set; }
        public string Descripcion { get; set; }

        // Navegación
        public virtual MovimientoBancario MovimientoBancario { get; set; }
    }
}
