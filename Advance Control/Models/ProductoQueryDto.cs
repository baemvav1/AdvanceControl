namespace Advance_Control.Models
{
    /// <summary>
    /// DTO para los parámetros de consulta de productos
    /// </summary>
    public class ProductoQueryDto
    {
        /// <summary>Concepto del producto (búsqueda parcial)</summary>
        public string? Concepto { get; set; }

        /// <summary>Descripción del producto (búsqueda parcial)</summary>
        public string? Descripcion { get; set; }

        /// <summary>Costo directo del producto (búsqueda exacta)</summary>
        public double? CostoDirecto { get; set; }

        /// <summary>% de utilidad sobre el costo directo</summary>
        public double? PorcentajeUtilidad { get; set; }

        /// <summary>Estatus del producto (por defecto true)</summary>
        public bool Estatus { get; set; } = true;
    }
}
