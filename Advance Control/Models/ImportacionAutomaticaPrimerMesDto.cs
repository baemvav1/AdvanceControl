namespace Advance_Control.Models
{
    /// <summary>Mes de la factura más vieja (GET api/factura/importacion-automatica/primer-mes).</summary>
    public class ImportacionAutomaticaPrimerMesDto
    {
        public int? Anio { get; set; }
        public int? Mes { get; set; }
    }
}
