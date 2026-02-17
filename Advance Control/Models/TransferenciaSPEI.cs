using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Advance_Control.Models
{
    public class TransferenciaSPEI
    {
        public int IdTransferenciaSPEI { get; set; }
        public int IdMovimiento { get; set; }
        public string TipoTransferencia { get; set; } // 'RECIBIDA' o 'ENVIADA'
        public string CodigoBanco { get; set; }
        public string NombreBanco { get; set; }
        public string CuentaOrigen { get; set; }
        public string CuentaDestino { get; set; }
        public string NombreEmisor { get; set; }
        public string NombreBeneficiario { get; set; }
        public string RfcEmisor { get; set; }
        public string RfcBeneficiario { get; set; }
        public string ClaveRastreo { get; set; }
        public string ConceptoPago { get; set; }
        public TimeSpan? HoraOperacion { get; set; }
        public decimal MontoTransferencia { get; set; }

        // Navegación
        public virtual MovimientoBancario MovimientoBancario { get; set; }
    }
}
