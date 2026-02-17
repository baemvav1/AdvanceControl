using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Advance_Control.Models
{
    public class ComisionBancaria
    {
        public int IdComisionBancaria { get; set; }
        public int IdMovimiento { get; set; }
        public string TipoComision { get; set; }
        public decimal MontoComision { get; set; }
        public decimal? MontoIVA { get; set; }
        public string ReferenciaComision { get; set; }

        // Navegación
        public virtual MovimientoBancario MovimientoBancario { get; set; }
    }
}
