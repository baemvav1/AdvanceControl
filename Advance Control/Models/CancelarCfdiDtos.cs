using System;

namespace Advance_Control.Models
{
    public class CancelarCfdiRequestDto
    {
        /// <summary>Clave SAT: "01" (con relación, exige UuidSustitucion), "02", "03" o "04".</summary>
        public string Motivo { get; set; } = string.Empty;

        /// <summary>UUID de la factura que sustituye a esta. Obligatorio solo si Motivo == "01".</summary>
        public string? UuidSustitucion { get; set; }
    }

    public class CancelarCfdiResponseDto
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? Observacion { get; set; }
        public string? CodigoRespuesta { get; set; }

        public int IdFactura { get; set; }
        public string? Uuid { get; set; }
        public bool Cancelada { get; set; }
        public string? CodigoResultado { get; set; }
        public string? MensajeResultado { get; set; }
        public DateTime? FechaCancelacion { get; set; }
        public bool OperacionDesvinculada { get; set; }
    }
}
