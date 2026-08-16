namespace Advance_Control.Models
{
    /// <summary>
    /// Punto ya calculado (con abanico anti-solape aplicado) para un colaborador
    /// en el modo "Colaboradores por área" del Visor Mundial.
    /// </summary>
    public class VisorMundialColaboradorPuntoDto
    {
        public long CredencialId { get; set; }
        public string? NombreCompleto { get; set; }
        public string? Cargo { get; set; }
        public string? Glyph { get; set; }
        public int IdArea { get; set; }
        public string? NombreArea { get; set; }
        public decimal Latitud { get; set; }
        public decimal Longitud { get; set; }
    }
}
