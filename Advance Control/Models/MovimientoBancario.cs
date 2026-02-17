using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Advance_Control.Models
{
    public class MovimientoBancario
    {
        public int IdMovimiento { get; set; }
        public int IdEstadoCuentaBancario { get; set; }
        public DateTime FechaMovimiento { get; set; }
        public string Descripcion { get; set; }
        public string Referencia { get; set; }
        public decimal? MontoCargo { get; set; }
        public decimal? MontoAbono { get; set; }
        public decimal SaldoResultante { get; set; }
        public string TipoMovimiento { get; set; }
        public DateTime FechaRegistro { get; set; }

        // Navegación
        public virtual EstadoCuentaBancario EstadoCuentaBancario { get; set; }
        public virtual ICollection<TransferenciaSPEI> TransferenciasSPEI { get; set; }
        public virtual ICollection<ImpuestoMovimiento> Impuestos { get; set; }
        public virtual ICollection<ComisionBancaria> Comisiones { get; set; }
        public virtual ICollection<PagoServicioBancario> PagosServicios { get; set; }
        public virtual ICollection<DepositoBancario> Depositos { get; set; }
        public virtual ICollection<OperacionInternacional> OperacionesInternacionales { get; set; }

        public MovimientoBancario()
        {
            TransferenciasSPEI = new HashSet<TransferenciaSPEI>();
            Impuestos = new HashSet<ImpuestoMovimiento>();
            Comisiones = new HashSet<ComisionBancaria>();
            PagosServicios = new HashSet<PagoServicioBancario>();
            Depositos = new HashSet<DepositoBancario>();
            OperacionesInternacionales = new HashSet<OperacionInternacional>();
            FechaRegistro = DateTime.Now;
        }
    }
}
