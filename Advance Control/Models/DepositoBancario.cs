using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Advance_Control.Models
{
    public class DepositoBancario
    {
        public int IdDeposito { get; set; }
        public int IdMovimiento { get; set; }
        public string TipoDeposito { get; set; } // 'EFECTIVO', 'CHEQUE', 'SBC', 'TRANSFERENCIA'
        public string ReferenciaDeposito { get; set; }
        public decimal MontoDeposito { get; set; }
        public string OrigenDeposito { get; set; } // 'NACIONAL', 'INTERNACIONAL'

        // Navegación
        public virtual MovimientoBancario MovimientoBancario { get; set; }
    }
}
