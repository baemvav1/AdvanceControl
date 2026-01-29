namespace Advance_Control.Models
{
    /// <summary>
    /// DTO para los parámetros del servicio de cargos
    /// </summary>
    public class CargoEditDto
    {
        /// <summary>
        /// Tipo de operación: 'select', 'delete', 'update', 'create'
        /// </summary>
        public string Operacion { get; set; } = string.Empty;

        /// <summary>
        /// ID del cargo (requerido para delete y update)
        /// </summary>
        public int IdCargo { get; set; } = 0;

        /// <summary>
        /// ID del tipo de cargo
        /// </summary>
        public int? IdTipoCargo { get; set; }

        /// <summary>
        /// ID de la operación
        /// </summary>
        public int? IdOperacion { get; set; }

        /// <summary>
        /// ID de la relación del cargo
        /// </summary>
        public int? IdRelacionCargo { get; set; }

        /// <summary>
        /// Monto del cargo
        /// </summary>
        public double? Monto { get; set; }

        /// <summary>
        /// Nota del cargo
        /// </summary>
        public string? Nota { get; set; }

        /// <summary>
        /// ID del proveedor
        /// </summary>
        public int? IdProveedor { get; set; }
    }
}
