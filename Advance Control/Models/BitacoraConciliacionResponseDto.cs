namespace Advance_Control.Models
{
    public class BitacoraConciliacionResponseDto
    {
        public bool Success { get; set; }
        public int? IdAbonoFactura { get; set; }
        public int? IdFactura { get; set; }
        public int? IdMovimiento { get; set; }
        public int OperacionesPendientes { get; set; }
        public int OperacionesRevertidas { get; set; }
        public string? TipoOperacion { get; set; }
        public string? Message { get; set; }
    }
}
