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
        public int IdArea { get; set; }

        /// <summary>
        /// Nombre del área
        /// </summary>
        public string Nombre { get; set; } = string.Empty;

        /// <summary>
        /// Tipo de geometría
        /// </summary>
        public string TipoGeometria { get; set; } = string.Empty;

        /// <summary>
        /// Indica si el punto está dentro del área
        /// </summary>
        public bool DentroDelArea { get; set; }
    }
}
