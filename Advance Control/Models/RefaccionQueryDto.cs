namespace Advance_Control.Models
{
    /// <summary>
    /// DTO para los parámetros de búsqueda del endpoint de refacciones
    /// </summary>
    public class RefaccionQueryDto
    {
        /// <summary>
        /// ID de la refacción (requerido para delete y update)
        /// </summary>
        public int IdRefaccion { get; set; } = 0;

        /// <summary>
        /// Búsqueda parcial por marca (LIKE)
        /// </summary>
        public string? Marca { get; set; }

        /// <summary>
        /// Búsqueda parcial por serie (LIKE)
        /// </summary>
        public string? Serie { get; set; }

        /// <summary>
        /// Costo de la refacción
        /// </summary>
        public double? Costo { get; set; }

        /// <summary>
        /// Búsqueda parcial en descripción (LIKE)
        /// </summary>
        public string? Descripcion { get; set; }

        /// <summary>
        /// Estatus de la refacción (por defecto true)
        /// </summary>
        public bool Estatus { get; set; } = true;
    }
}
