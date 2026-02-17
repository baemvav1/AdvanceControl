using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Advance_Control.Models
{
    public class EstadoCuentaBancario
    {
        public int IdEstadoCuentaBancario { get; set; }
        public string Titular { get; set; } = string.Empty;
        public string RfcTitular { get; set; } = string.Empty;
        public string NumeroCliente { get; set; } = string.Empty;
        public string DireccionTitular { get; set; } = string.Empty;
        public string NumeroCuenta { get; set; }
        public string Clabe { get; set; }
        public string TipoCuenta { get; set; }
        public string TipoMoneda { get; set; }
        //Resumen
        public string Periodo { get; set; } = string.Empty;
        public DateTime PeriodoInicio { get; set; }
        public DateTime PeriodoFin { get; set; }
        public DateTime FechaCorte { get; set; }
        public decimal? SaldoInicial { get; set; }
        public int? TotalTransacciones { get; set; }

        public decimal? TotalCargos { get; set; }
        public decimal? TotalAbonos { get; set; }
        public decimal? SaldoFinal { get; set; }
        public decimal? SaldoPromedio { get; set; }
        public decimal? TotalComisiones { get; set; }
        public decimal? TotalISR { get; set; }
        public decimal? TotalIVA { get; set; }
        //Banco
        public string NombreBanco { get; set; }
        public string RfcBanco { get; set; }
        public string NombreSucursal { get; set; }
        public string DireccionSucursal { get; set; }
        //Cuenta Habiente
        public string NombreCuentaHabiente { get; set; }
        public string RfcCuentaHabiente { get; set; }
        public string DireccionCuentaHabiente { get; set; }
        public DateTime FechaCarga { get; set; }

        // CFDI Estado Cuenta
        public string FolioFiscal {  get; set; } = string.Empty;
        public string CertificadoEmisor {  get; set; } = string.Empty;
        public string FechaEmisionCert {  get; set; } = string.Empty;
        public string CertificadoSAT {  get; set; } = string.Empty;
        public string FechaCertificacionSAT {  get; set; } = string.Empty;

        //Info Fiscal

        public string RegimenFiscal { get; set; } = string.Empty;
        public string MetodoPago { get; set; } = string.Empty;
        public string FormaPago { get; set; } = string.Empty;
        public string UsoCFDI { get; set; } = string.Empty;
        public string ClaveProdServ { get; set; } = string.Empty;
        public string LugarExpedicion { get; set; } = string.Empty;

        // Navegación
        public virtual ICollection<MovimientoBancario> Movimientos { get; set; }
        public virtual ICollection<ComplementoFiscalBancario> ComplementosFiscales { get; set; }

        public EstadoCuentaBancario()
        {
            Movimientos = new HashSet<MovimientoBancario>();
            ComplementosFiscales = new HashSet<ComplementoFiscalBancario>();
            FechaCarga = DateTime.Now;
        }
    }
}
