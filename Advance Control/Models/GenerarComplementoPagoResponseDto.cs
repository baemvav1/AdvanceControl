namespace Advance_Control.Models
{
    public class GenerarComplementoPagoResponseDto
    {
        public bool Success { get; set; }
        public int IdFacturaComplemento { get; set; }
        public string? Uuid { get; set; }
        public int PagosProcesados { get; set; }
        public int DoctosProcesados { get; set; }
        public string? Message { get; set; }
    }
}
