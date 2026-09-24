using System;
using System.Collections.Generic;

namespace Advance_Control.Models
{
    public class GenerarComplementoPagoRequestDto
    {
        public string ReceptorRfc { get; set; } = string.Empty;
        public List<GrupoPagoComplementoDto> Pagos { get; set; } = new();
    }

    public class GrupoPagoComplementoDto
    {
        public DateTime FechaPago { get; set; }
        public string FormaPago { get; set; } = string.Empty;
        public string Moneda { get; set; } = "MXN";
        public decimal TipoCambio { get; set; } = 1m;
        public string? NumOperacion { get; set; }
        public List<DoctoRelacionadoComplementoDto> Doctos { get; set; } = new();
    }

    public class DoctoRelacionadoComplementoDto
    {
        public int IdAbonoFactura { get; set; }
        public int IdFacturaPagada { get; set; }
        public int NumParcialidad { get; set; }
        public decimal ImpSaldoAnt { get; set; }
        public decimal ImpPagado { get; set; }
        public decimal ImpSaldoInsoluto { get; set; }
        public string ObjetoImp { get; set; } = "02";
    }
}
