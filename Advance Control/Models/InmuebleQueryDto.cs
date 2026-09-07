namespace Advance_Control.Models
{
    /// <summary>
    /// DTO para los parámetros de búsqueda del endpoint de inmuebles
    /// </summary>
    public class InmuebleQueryDto
    {
        /// <summary>
        /// Búsqueda parcial en descripción (LIKE)
        /// </summary>
        public string? Descripcion { get; set; }

        /// <summary>
        /// Búsqueda parcial por identificador (LIKE)
        /// </summary>
        public string? Identificador { get; set; }

        /// <summary>
        /// Filtro exacto por ID de tipo de inmueble
        /// </summary>
        public int? IdTipoInmueble { get; set; }

        /// <summary>
        /// Superficie en metros cuadrados (opcional)
        /// </summary>
        public double? SuperficieM2 { get; set; }

        /// <summary>
        /// Filtro exacto por código postal
        /// </summary>
        public string? CodigoPostal { get; set; }

        /// <summary>
        /// Filtro exacto por ID de ubicación
        /// </summary>
        public int? IdUbicacion { get; set; }
    }
}
