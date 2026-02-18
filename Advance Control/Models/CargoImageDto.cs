using System.Text.Json.Serialization;

namespace Advance_Control.Models
{
    /// <summary>
    /// DTO para representar una imagen asociada a un cargo
    /// </summary>
    public class CargoImageDto
    {
        /// <summary>
        /// Nombre del archivo de la imagen
        /// Formato: {idOperacion}_{idCargo}_{numeroImagen}_Cargo.extension
        /// </summary>
        [JsonPropertyName("fileName")]
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// Ruta local para acceder a la imagen
        /// </summary>
        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// ID del cargo al que pertenece la imagen
        /// </summary>
        [JsonPropertyName("idCargo")]
        public int IdCargo { get; set; }

        /// <summary>
        /// Número secuencial de la imagen para este cargo
        /// </summary>
        [JsonPropertyName("imageNumber")]
        public int ImageNumber { get; set; }
    }
}
