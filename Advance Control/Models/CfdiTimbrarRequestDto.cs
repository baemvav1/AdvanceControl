using System.Collections.Generic;

namespace Advance_Control.Models
{
    /// <summary>Datos para construir y timbrar un CFDI 4.0 directamente desde una operación (POST api/factura/operacion/{id}/timbrar).</summary>
    public class CfdiTimbrarRequestDto
    {
        public string? Serie { get; set; }
        public string? Folio { get; set; }

        public string ReceptorRfc { get; set; } = string.Empty;
        public string ReceptorRazonSocial { get; set; } = string.Empty;
        public string ReceptorCodigoPostal { get; set; } = string.Empty;
        public string ReceptorRegimenFiscal { get; set; } = string.Empty;
        public string ReceptorUsoCfdi { get; set; } = string.Empty;

        public string FormaPago { get; set; } = string.Empty;
        public string MetodoPago { get; set; } = string.Empty;
        public string? CondicionesDePago { get; set; }
        public string Moneda { get; set; } = "MXN";

        public List<CfdiConceptoTimbrarDto> Conceptos { get; set; } = new();

        public string? Referencia { get; set; }
    }

    public class CfdiConceptoTimbrarDto
    {
        public string ClaveProdServ { get; set; } = string.Empty;
        public string ClaveUnidad { get; set; } = string.Empty;
        public string Unidad { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public decimal Cantidad { get; set; } = 1;
        public decimal ValorUnitario { get; set; }
        public decimal? TasaIva { get; set; }
    }

    /// <summary>Resultado de intentar timbrar, con el detalle de error de FEL Bilkon si aplica.</summary>
    public class TimbrarResultadoDto
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? Observacion { get; set; }
        public string? CodigoRespuesta { get; set; }
        public int? IdFactura { get; set; }
    }
}
