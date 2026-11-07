using System;
using System.Collections.Generic;

namespace Advance_Control.Models
{
    // Espejo de AdvanceApi.DTOs.ConciliacionVariasAUnoDtos (api/conciliacion/varias-a-uno/*).

    public class ConciliacionCombinacionDto
    {
        public int IdMovimiento { get; set; }
        public List<int> IdsFacturas { get; set; } = new();
    }

    public class ConciliacionVariasAUnoPropuestasRequestDto
    {
        public bool Dtc { get; set; } = true;
        public bool Rfc { get; set; }
        public List<ConciliacionCombinacionDto> CombinacionesVetadas { get; set; } = new();
    }

    public class ConciliacionFacturaRefDto
    {
        public int IdFactura { get; set; }
        public string FolioTitulo { get; set; } = string.Empty;
        public decimal SaldoPendiente { get; set; }
    }

    public class ConciliacionVariasAUnoPropuestaDto
    {
        public int IdMovimiento { get; set; }
        public decimal Monto { get; set; }
        public DateTime FechaMovimiento { get; set; }
        public string? ReceptorRfc { get; set; }
        public string? ReceptorNombre { get; set; }
        public List<ConciliacionFacturaRefDto> Facturas { get; set; } = new();
        public int CombinacionesCandidatas { get; set; }
        public string Observaciones { get; set; } = string.Empty;
    }

    public class ConciliacionPagoSinResolverDto
    {
        public int IdMovimiento { get; set; }
        public DateTime FechaMovimiento { get; set; }
        public decimal Monto { get; set; }
        public string Motivo { get; set; } = string.Empty;
    }

    public class ConciliacionVariasAUnoPropuestasResponseDto
    {
        public List<ConciliacionVariasAUnoPropuestaDto> Propuestas { get; set; } = new();
        public List<ConciliacionPagoSinResolverDto> SinResolver { get; set; } = new();
    }

    public class ConciliacionVariasAUnoValidarRequestDto
    {
        public int IdMovimiento { get; set; }
        public List<int> IdsFacturas { get; set; } = new();
        public List<string> Folios { get; set; } = new();
        public bool Dtc { get; set; } = true;
        public bool Rfc { get; set; }
    }

    public class ConciliacionVariasAUnoValidarResponseDto
    {
        public bool Valido { get; set; }
        public string? Motivo { get; set; }
        public List<ConciliacionFacturaRefDto> Facturas { get; set; } = new();
    }

    public class ConciliacionVariasAUnoAplicarRequestDto
    {
        public bool Dtc { get; set; } = true;
        public bool Rfc { get; set; }
        public List<ConciliacionCombinacionDto> Combinaciones { get; set; } = new();
    }

    public class ConciliacionVariasAUnoResultadoDto
    {
        public int IdMovimiento { get; set; }
        public List<int> IdsFacturas { get; set; } = new();
        public bool Aplicado { get; set; }
        public string? Motivo { get; set; }
    }

    public class ConciliacionVariasAUnoAplicarResponseDto
    {
        public int Aplicados { get; set; }
        public int Rechazados { get; set; }
        public int OperacionesConciliacionPendientes { get; set; }
        public List<ConciliacionVariasAUnoResultadoDto> Resultados { get; set; } = new();
    }
}
