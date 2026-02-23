using System;

namespace Advance_Control.Models
{
    /// <summary>
    /// DTO para los parámetros de búsqueda del procedimiento almacenado sp_cliente_select
    /// </summary>
    public class ClienteQueryDto
    {
        /// <summary>
        /// Búsqueda parcial por RFC (LIKE)
        /// </summary>
        public string? Rfc { get; set; }

        /// <summary>
        /// Búsqueda parcial en notas (LIKE)
        /// </summary>
        public string? Notas { get; set; }

        /// <summary>
        /// Coincidencia exacta de prioridad
        /// </summary>
        public int? Prioridad { get; set; }
    }
}
