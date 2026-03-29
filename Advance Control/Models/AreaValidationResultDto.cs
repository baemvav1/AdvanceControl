using System.Text.Json.Serialization;

namespace Advance_Control.Models
{
    /// <summary>
    /// Resultado de la validación de un punto dentro de un área
    /// </summary>
    public class AreaValidationResultDto
    {
        /// <summary>
        /// Identificador del área
        /// </summary>
        [JsonPropertyName("id")]
        public int IdArea { get; set; }

        /// <summary>
        /// Nombre del área
        /// </summary>
        [JsonPropertyName("nombre")]
        public string Nombre { get; set; } = string.Empty;

        /// <summary>
        /// Tipo de geometría
        /// </summary>
        [JsonPropertyName("tipo_geometria")]
        public string TipoGeometria { get; set; } = string.Empty;

        /// <summary>
        /// Indica si el punto está dentro del área
        /// </summary>
        [JsonPropertyName("dentro_del_area")]
        public bool DentroDelArea { get; set; }
    }
}
