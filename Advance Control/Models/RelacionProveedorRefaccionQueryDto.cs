namespace Advance_Control.Models
{
    /// <summary>
    /// DTO para los parámetros de consulta de relaciones proveedor-refacción
    /// </summary>
    public class RelacionProveedorRefaccionQueryDto
    {
        /// <summary>
        /// Tipo de operación: 'select', 'delete', 'put', 'update_nota', 'update_precio'
        /// </summary>
        public string Operacion { get; set; } = string.Empty;

        /// <summary>
        /// ID de la relación proveedor-refacción (para delete, update_nota, update_precio)
        /// </summary>
        public int IdRelacionProveedor { get; set; } = 0;

        /// <summary>
        /// ID del proveedor (0 para no filtrar en select)
        /// </summary>
        public int IdProveedor { get; set; } = 0;

        /// <summary>
        /// ID de la refacción (0 para no filtrar en select)
        /// </summary>
        public int IdRefaccion { get; set; } = 0;

        /// <summary>
        /// Nota asociada a la relación
        /// </summary>
        public string? Nota { get; set; }

        /// <summary>
        /// Precio de la refacción por el proveedor
        /// </summary>
        public double Precio { get; set; } = 0.0;
    }
}
