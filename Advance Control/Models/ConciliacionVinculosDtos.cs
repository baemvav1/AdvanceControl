using System;
using System.Collections.Generic;

namespace Advance_Control.Models
{
    /// <summary>
    /// Par pago documentado–movimiento. IdPago es el id del docto de complemento (Complementos)
    /// o del abono capturado a mano (Ingresos manuales).
    /// </summary>
    public class ConciliacionVinculoParDto
    {
        public int IdPago { get; set; }
        public int IdMovimiento { get; set; }
    }

    /// <summary>POST api/conciliacion/complementos|ingresos-manuales/propuestas</summary>
    public class ConciliacionVinculosPropuestasRequestDto
    {
        public bool Dtc { get; set; } = true;
        public bool Rfc { get; set; }
        public bool Cheques { get; set; }
        public List<ConciliacionVinculoParDto> ParesVetados { get; set; } = new();
    }

    public class ConciliacionVinculoPropuestaDto
    {
        public int IdPago { get; set; }
        public int IdFactura { get; set; }
        public int IdMovimiento { get; set; }
        public decimal Monto { get; set; }
        public DateTime FechaPago { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public int MovimientosCandidatos { get; set; }
        public string Observaciones { get; set; } = string.Empty;
    }

    /// <summary>POST api/conciliacion/complementos|ingresos-manuales/aplicar</summary>
    public class ConciliacionVinculosAplicarRequestDto
    {
        public bool Dtc { get; set; } = true;
        public bool Rfc { get; set; }
        public bool Cheques { get; set; }
        public List<ConciliacionVinculoParDto> Pares { get; set; } = new();
    }

    public class ConciliacionVinculoResultadoDto
    {
        public int IdPago { get; set; }
        public int IdMovimiento { get; set; }
        public bool Aplicado { get; set; }
        public string? Motivo { get; set; }
    }

    public class ConciliacionVinculosAplicarResponseDto
    {
        public int Aplicados { get; set; }
        public int Rechazados { get; set; }
        public List<ConciliacionVinculoResultadoDto> Resultados { get; set; } = new();
    }
}
