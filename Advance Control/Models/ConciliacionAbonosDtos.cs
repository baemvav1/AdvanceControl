using System;
using System.Collections.Generic;

namespace Advance_Control.Models
{
    // Espejo de AdvanceApi.DTOs.ConciliacionAbonosDtos (api/conciliacion/abonos/*).

    public class ConciliacionAbonosCombinacionDto
    {
        public int IdFactura { get; set; }
        public List<int> IdsMovimientos { get; set; } = new();
    }

    /// <summary>Sin RFC: en Abonos la regla RFC siempre aplica en el servidor.</summary>
    public class ConciliacionAbonosPropuestasRequestDto
    {
        public bool Dtc { get; set; } = true;
        public List<ConciliacionAbonosCombinacionDto> CombinacionesVetadas { get; set; } = new();
    }

    public class ConciliacionMovimientoRefDto
    {
        public int IdMovimiento { get; set; }
        public string GrupoId { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
        public string? Referencia { get; set; }
        public string? Rfc { get; set; }
        public decimal Monto { get; set; }
    }

    public class ConciliacionAbonosPropuestaDto
    {
        public int IdFactura { get; set; }
        public string FolioTitulo { get; set; } = string.Empty;
        public DateTime FechaFactura { get; set; }
        public decimal SaldoPendiente { get; set; }
        public string? ReceptorRfc { get; set; }
        public string? ReceptorNombre { get; set; }
        public List<ConciliacionMovimientoRefDto> Movimientos { get; set; } = new();
        public int CombinacionesCandidatas { get; set; }
        public string Observaciones { get; set; } = string.Empty;
    }

    public class ConciliacionFacturaSinResolverDto
    {
        public int IdFactura { get; set; }
        public string FolioTitulo { get; set; } = string.Empty;
        public DateTime FechaFactura { get; set; }
        public decimal SaldoPendiente { get; set; }
        public string Motivo { get; set; } = string.Empty;
    }

    public class ConciliacionAbonosPropuestasResponseDto
    {
        public List<ConciliacionAbonosPropuestaDto> Propuestas { get; set; } = new();
        public List<ConciliacionFacturaSinResolverDto> SinResolver { get; set; } = new();
    }

    public class ConciliacionAbonosAplicarRequestDto
    {
        public bool Dtc { get; set; } = true;
        public List<ConciliacionAbonosCombinacionDto> Combinaciones { get; set; } = new();
    }

    public class ConciliacionAbonosResultadoDto
    {
        public int IdFactura { get; set; }
        public List<int> IdsMovimientos { get; set; } = new();
        public bool Aplicado { get; set; }
        public string? Motivo { get; set; }
    }

    public class ConciliacionAbonosAplicarResponseDto
    {
        public int Aplicados { get; set; }
        public int Rechazados { get; set; }
        public int OperacionesConciliacionPendientes { get; set; }
        public List<ConciliacionAbonosResultadoDto> Resultados { get; set; } = new();
    }
}
