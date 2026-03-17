using System;
using System.Collections.Generic;

namespace Advance_Control.Models
{
    public class GuardarFacturaRequestDto
    {
        public string VersionXml { get; set; } = "4.0";
        public string? Folio { get; set; }
        public DateTime Fecha { get; set; }
        public string? FormaPago { get; set; }
        public string? NoCertificado { get; set; }
        public string? Certificado { get; set; }
        public string? Sello { get; set; }
        public string? CondicionesDePago { get; set; }
        public decimal SubTotal { get; set; }
        public string Moneda { get; set; } = "MXN";
        public decimal Total { get; set; }
        public string? TipoDeComprobante { get; set; }
        public string? Exportacion { get; set; }
        public string? MetodoPago { get; set; }
        public string? LugarExpedicion { get; set; }
        public decimal TotalImpuestosTrasladados { get; set; }
        public string? EmisorRfc { get; set; }
        public string? EmisorNombre { get; set; }
        public string? EmisorRegimenFiscal { get; set; }
        public string? ReceptorRfc { get; set; }
        public string? ReceptorNombre { get; set; }
        public string? ReceptorDomicilioFiscal { get; set; }
        public string? ReceptorRegimenFiscal { get; set; }
        public string? ReceptorUsoCfdi { get; set; }
        public string? Uuid { get; set; }
        public DateTime? FechaTimbrado { get; set; }
        public string? RfcProvCertif { get; set; }
        public string? NoCertificadoSat { get; set; }
        public string? SelloCfd { get; set; }
        public string? SelloSat { get; set; }
        public string? XmlContenido { get; set; }
        public List<FacturaConceptoDto> Conceptos { get; set; } = new();
        public List<FacturaTrasladoDto> TrasladosGlobales { get; set; } = new();
    }
}
