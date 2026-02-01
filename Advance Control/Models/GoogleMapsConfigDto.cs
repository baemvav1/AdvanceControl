namespace Advance_Control.Models
{
    /// <summary>
    /// Configuración de Google Maps obtenida desde el API
    /// </summary>
    public class GoogleMapsConfigDto
    {
        /// <summary>
        /// Clave de API de Google Maps
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// Coordenadas del centro predeterminado (formato: "latitud,longitud")
        /// </summary>
        public string DefaultCenter { get; set; } = string.Empty;

        /// <summary>
        /// Nivel de zoom predeterminado
        /// </summary>
        public int DefaultZoom { get; set; } = 15;
    }
}
