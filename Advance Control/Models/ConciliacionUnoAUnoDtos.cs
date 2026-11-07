using System;
using System.Collections.Generic;

namespace Advance_Control.Models
{
    // Espejo de AdvanceApi.DTOs.ConciliacionUnoAUnoDtos (api/conciliacion/uno-a-uno/*).

    public class ConciliacionParDto
    {
        public int IdFactura { get; set; }
        public int IdMovimiento { get; set; }
    }

    public class ConciliacionUnoAUnoPropuestasRequestDto
    {
        public bool Dtc { get; set; } = true;
        public bool Rfc { get; set; }
        public List<ConciliacionParDto> ParesVetados { get; set; } = new();
    }

    public class ConciliacionUnoAUnoPropuestaDto
    {
        public int IdFactura { get; set; }
        public int IdMovimiento { get; set; }
        public decimal Monto { get; set; }
        public string FolioTitulo { get; set; } = string.Empty;
        public string? ReceptorRfc { get; set; }
        public string? ReceptorNombre { get; set; }
        public DateTime FechaMovimiento { get; set; }
        public int FacturasCandidatas { get; set; }
        public string Observaciones { get; set; } = string.Empty;
    }

    public class ConciliacionUnoAUnoValidarRequestDto
    {
        public int IdMovimiento { get; set; }
        public int? IdFactura { get; set; }
        public string? Folio { get; set; }
        public bool Dtc { get; set; } = true;
        public bool Rfc { get; set; }
    }

    public class ConciliacionUnoAUnoValidarResponseDto
    {
        public bool Valido { get; set; }
        public string? Motivo { get; set; }
        public int? IdFactura { get; set; }
        public string? FolioTitulo { get; set; }
    }

    public class ConciliacionUnoAUnoAplicarRequestDto
    {
        public bool Dtc { get; set; } = true;
        public bool Rfc { get; set; }
        public List<ConciliacionParDto> Pares { get; set; } = new();
    }

    public class ConciliacionUnoAUnoResultadoParDto
    {
        public int IdFactura { get; set; }
        public int IdMovimiento { get; set; }
        public bool Aplicado { get; set; }
        public int? IdAbonoFactura { get; set; }
        public string? Motivo { get; set; }
    }

    public class ConciliacionUnoAUnoAplicarResponseDto
    {
        public int Aplicados { get; set; }
        public int Rechazados { get; set; }
        public int OperacionesConciliacionPendientes { get; set; }
        public List<ConciliacionUnoAUnoResultadoParDto> Resultados { get; set; } = new();
    }
}
