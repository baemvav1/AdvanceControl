namespace Advance_Control.Models
{
    /// <summary>
    /// DTO para los parámetros de búsqueda de proveedores
    /// </summary>
    public class ProveedorQueryDto
    {
        /// <summary>
        /// Búsqueda parcial por RFC
        /// </summary>
        public string? Rfc { get; set; }

        /// <summary>
        /// Búsqueda parcial por razón social
        /// </summary>
        public string? RazonSocial { get; set; }

        /// <summary>
        /// Búsqueda parcial por nombre comercial
        /// </summary>
        public string? NombreComercial { get; set; }

        /// <summary>
        /// Búsqueda parcial en notas
        /// </summary>
        public string? Nota { get; set; }
    }
}
