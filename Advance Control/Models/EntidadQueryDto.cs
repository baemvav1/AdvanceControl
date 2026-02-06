namespace Advance_Control.Models
{
    /// <summary>
    /// DTO para los parámetros de búsqueda de entidades
    /// </summary>
    public class EntidadQueryDto
    {
        /// <summary>
        /// Búsqueda parcial por nombre comercial
        /// </summary>
        public string? NombreComercial { get; set; }

        /// <summary>
        /// Búsqueda parcial por razón social
        /// </summary>
        public string? RazonSocial { get; set; }

        /// <summary>
        /// Búsqueda parcial por RFC
        /// </summary>
        public string? RFC { get; set; }

        /// <summary>
        /// Búsqueda parcial por código postal
        /// </summary>
        public string? CP { get; set; }

        /// <summary>
        /// Búsqueda parcial por estado
        /// </summary>
        public string? Estado { get; set; }

        /// <summary>
        /// Búsqueda parcial por ciudad
        /// </summary>
        public string? Ciudad { get; set; }
    }
}
