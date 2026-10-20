using System;
using System.Collections.Generic;

namespace Advance_Control.Models
{
    /// <summary>Mes a importar del portal FEL en Línea y las credenciales del portal (no se guardan en la API).</summary>
    public class ImportacionAutomaticaRequestDto
    {
        public int Anio { get; set; }
        public int Mes { get; set; }
        public string Usuario { get; set; } = string.Empty;
        public string Cuenta { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class ImportacionAutomaticaXmlRequestDto
    {
        public const int MaxUuids = 25;
        public string Usuario { get; set; } = string.Empty;
        public string Cuenta { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public List<string> Uuids { get; set; } = new();
    }

    public class ImportacionAutomaticaComprobanteDto
    {
        public string Uuid { get; set; } = string.Empty;
        public string? EstadoPortal { get; set; }
        public string? RfcReceptor { get; set; }
        public DateTime? FechaTimbrado { get; set; }
    }

    public class ImportacionAutomaticaConsultaResponseDto
    {
        public int TotalPortal { get; set; }
        public int CanceladasPortal { get; set; }
        public int Existentes { get; set; }
        public List<ImportacionAutomaticaComprobanteDto> Faltantes { get; set; } = new();
    }

    public class ImportacionAutomaticaXmlDto
    {
        public string Uuid { get; set; } = string.Empty;
        public string? Xml { get; set; }
        public string? Error { get; set; }
    }

    public class FacturaCanceladaPortalDto
    {
        public int IdFactura { get; set; }
        public string? TipoDeComprobante { get; set; }
        public string? Serie { get; set; }
        public string? Folio { get; set; }
        public string Uuid { get; set; } = string.Empty;
        public string? ReceptorRfc { get; set; }
        public string? ReceptorNombre { get; set; }
        public decimal Total { get; set; }
        public int AbonosEliminados { get; set; }
        public int AbonosDesvinculados { get; set; }
        public int MovimientosDesligados { get; set; }
        public int AbonosSinMovimiento { get; set; }
        public decimal MontoSinMovimiento { get; set; }
        public int? IdOperacionDesvinculada { get; set; }
    }

    public class ImportacionAutomaticaCancelacionesResponseDto
    {
        public int CanceladasPortal { get; set; }
        public int YaCanceladas { get; set; }
        public List<FacturaCanceladaPortalDto> Canceladas { get; set; } = new();
        public List<string> Omitidas { get; set; } = new();
        public List<string> NoEncontradas { get; set; } = new();
    }
}
