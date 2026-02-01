namespace Advance_Control.Models
{
    /// <summary>
    /// Área en formato optimizado para Google Maps JavaScript API
    /// </summary>
    public class GoogleMapsAreaDto
    {
        /// <summary>
        /// Identificador del área
        /// </summary>
        public int IdArea { get; set; }

        /// <summary>
        /// Nombre del área
        /// </summary>
        public string Nombre { get; set; } = string.Empty;

        /// <summary>
        /// Tipo de geometría (Polygon, Circle, Rectangle, Polyline)
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Array JSON de coordenadas para polígonos/polilíneas (formato: [{"lat":19.4326,"lng":-99.1332}, ...])
        /// </summary>
        public string? Path { get; set; }

        /// <summary>
        /// Objeto JSON con opciones de estilo para Google Maps
        /// </summary>
        public string Options { get; set; } = string.Empty;

        /// <summary>
        /// Objeto JSON con el punto central (formato: {"lat":19.4326,"lng":-99.1332})
        /// </summary>
        public string? Center { get; set; }

        /// <summary>
        /// Objeto JSON con bounding box (formato: {"north":19.4350,"east":-99.1300,"south":19.4300,"west":-99.1364})
        /// </summary>
        public string? Bounds { get; set; }

        /// <summary>
        /// Radio en metros (solo para círculos)
        /// </summary>
        public decimal? Radius { get; set; }
    }
}
