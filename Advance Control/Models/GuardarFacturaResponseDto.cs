namespace Advance_Control.Models
{
    public class GuardarFacturaResponseDto
    {
        public bool Success { get; set; }
        public int IdFactura { get; set; }
        public string Accion { get; set; } = string.Empty;
        public int ConceptosProcesados { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
