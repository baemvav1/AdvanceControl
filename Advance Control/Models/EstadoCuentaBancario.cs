using System;
using System.Collections.Generic;

namespace Advance_Control.Models
{
    public class EstadoCuentaBancario
    {
        public int IdEstadoCuentaBancario { get; set; }
        public string VersionXml { get; set; } = "2.0";
        public string Titular { get; set; } = string.Empty;
        public string RfcTitular { get; set; } = string.Empty;
        public string NumeroCliente { get; set; } = string.Empty;
        public string DireccionTitular { get; set; } = string.Empty;
        public string NumeroCuenta { get; set; } = string.Empty;
        public string Clabe { get; set; } = string.Empty;
        public string TipoCuenta { get; set; } = string.Empty;
        public string TipoMoneda { get; set; } = string.Empty;
        public string Periodo { get; set; } = string.Empty;
        public DateTime PeriodoInicio { get; set; }
        public DateTime PeriodoFin { get; set; }
        public DateTime FechaCorte { get; set; }
        public decimal? SaldoInicial { get; set; }
        public int? TotalTransacciones { get; set; }
        public int? TotalGrupos { get; set; }
        public decimal? TotalCargos { get; set; }
        public decimal? TotalAbonos { get; set; }
        public decimal? SaldoFinal { get; set; }
        public decimal? SaldoPromedio { get; set; }
        public decimal? TotalComisiones { get; set; }
        public decimal? TotalISR { get; set; }
        public decimal? TotalIVA { get; set; }
        public string NombreBanco { get; set; } = string.Empty;
        public string RfcBanco { get; set; } = string.Empty;
        public string NombreSucursal { get; set; } = string.Empty;
        public string DireccionSucursal { get; set; } = string.Empty;
        public DateTime FechaCarga { get; set; }
        public string FolioFiscal { get; set; } = string.Empty;
        public string CertificadoEmisor { get; set; } = string.Empty;
        public string FechaEmisionCert { get; set; } = string.Empty;
        public string CertificadoSAT { get; set; } = string.Empty;
        public string FechaCertificacionSAT { get; set; } = string.Empty;
        public string RegimenFiscal { get; set; } = string.Empty;
        public string MetodoPago { get; set; } = string.Empty;
        public string FormaPago { get; set; } = string.Empty;
        public string UsoCFDI { get; set; } = string.Empty;
        public string ClaveProdServ { get; set; } = string.Empty;
        public string LugarExpedicion { get; set; } = string.Empty;
        public virtual ICollection<MovimientoBancario> Movimientos { get; set; }

        public EstadoCuentaBancario()
        {
            Movimientos = new List<MovimientoBancario>();
            FechaCarga = DateTime.Now;
        }
    }
}
