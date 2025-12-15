namespace Advance_Control.Models
{
    /// <summary>
    /// DTO para los parámetros de búsqueda de operaciones
    /// </summary>
    public class OperacionQueryDto
    {
        /// <summary>
        /// Filtro exacto por idTipo (0 para no filtrar)
        /// </summary>
        public int IdTipo { get; set; }

        /// <summary>
        /// Filtro exacto por idCliente (0 para no filtrar)
        /// </summary>
        public int IdCliente { get; set; }

        /// <summary>
        /// Filtro exacto por idEquipo (0 para no filtrar)
        /// </summary>
        public int IdEquipo { get; set; }

        /// <summary>
        /// Filtro exacto por idAtiende (0 para no filtrar)
        /// </summary>
        public int IdAtiende { get; set; }

        /// <summary>
        /// Búsqueda parcial en nota (permite búsqueda por texto)
        /// </summary>
        public string? Nota { get; set; }
    }
}
