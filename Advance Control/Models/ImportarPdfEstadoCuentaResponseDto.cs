namespace Advance_Control.Models
{
    public class ImportarPdfEstadoCuentaResponseDto
    {
        public GuardarEstadoCuentaRequestDto Request { get; set; } = new();
        public AdvertenciasImportacionPdfDto Advertencias { get; set; } = new();
    }
}
