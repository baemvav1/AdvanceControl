using System;

namespace Advance_Control.Models
{
    /// <summary>
    /// Modelo de datos para un área geográfica
    /// </summary>
    public class AreaDto
    {
        public int IdArea { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public string ColorMapa { get; set; } = "#FF0000";
        public decimal? Opacidad { get; set; }
        public string ColorBorde { get; set; } = "#000000";
        public int? AnchoBorde { get; set; }
        public bool? Activo { get; set; }
        public string TipoGeometria { get; set; } = string.Empty;
        public decimal? CentroLatitud { get; set; }
        public decimal? CentroLongitud { get; set; }
        public decimal? Radio { get; set; }
        public decimal? BoundingBoxNE_Lat { get; set; }
        public decimal? BoundingBoxNE_Lng { get; set; }
        public decimal? BoundingBoxSW_Lat { get; set; }
        public decimal? BoundingBoxSW_Lng { get; set; }
        public bool? EtiquetaMostrar { get; set; }
        public string? EtiquetaTexto { get; set; }
        public int? NivelZoom { get; set; }
        public string? MetadataJSON { get; set; }
        public DateTime? FechaCreacion { get; set; }
        public DateTime? FechaModificacion { get; set; }
        public string? UsuarioCreacion { get; set; }
        public string? UsuarioModificacion { get; set; }
        public int? TotalCoordenadas { get; set; }
        public int? TotalMarcadores { get; set; }
    }
}
