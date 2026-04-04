namespace Advance_Control.Models
{
    /// <summary>
    /// DTO para los parámetros de búsqueda del endpoint de órdenes de servicio
    /// </summary>
    public class OrdenServicioQueryDto
    {
        /// <summary>
        /// Búsqueda parcial por identificador del equipo
        /// </summary>
        public string? Identificador { get; set; }

        /// <summary>
        /// ID del cliente (0 para no filtrar)
        /// </summary>
        public int IdCliente { get; set; } = 0;
    }
}
