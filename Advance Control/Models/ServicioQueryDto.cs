namespace Advance_Control.Models
{
    /// <summary>
    /// DTO para los parámetros de consulta de servicios
    /// </summary>
    public class ServicioQueryDto
    {
        /// <summary>
        /// Concepto del servicio (búsqueda parcial)
        /// </summary>
        public string? Concepto { get; set; }

        /// <summary>
        /// Descripción del servicio (búsqueda parcial)
        /// </summary>
        public string? Descripcion { get; set; }

        /// <summary>
        /// Costo del servicio (búsqueda exacta)
        /// </summary>
        public double? Costo { get; set; }

        /// <summary>
        /// Estatus del servicio (por defecto true)
        /// </summary>
        public bool Estatus { get; set; } = true;
    }
}
