namespace Advance_Control.Models
{
    /// <summary>
    /// DTO para los parámetros de búsqueda del endpoint de equipos
    /// </summary>
    public class EquipoQueryDto
    {
        /// <summary>
        /// Búsqueda parcial por marca (LIKE)
        /// </summary>
        public string? Marca { get; set; }

        /// <summary>
        /// Filtro exacto por año de creación
        /// </summary>
        public int? Creado { get; set; }

        /// <summary>
        /// Búsqueda parcial en descripción (LIKE)
        /// </summary>
        public string? Descripcion { get; set; }

        /// <summary>
        /// Búsqueda parcial por identificador (LIKE)
        /// </summary>
        public string? Identificador { get; set; }
    }
}
