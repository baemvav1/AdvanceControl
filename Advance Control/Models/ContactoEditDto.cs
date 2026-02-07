namespace Advance_Control.Models
{
    /// <summary>
    /// DTO para los parámetros de creación/edición de contactos
    /// </summary>
    public class ContactoEditDto
    {
        /// <summary>
        /// Tipo de operación: 'select', 'delete', 'update', 'create'
        /// </summary>
        public string Operacion { get; set; } = string.Empty;

        /// <summary>
        /// ID del contacto (requerido para delete y update)
        /// </summary>
        public long ContactoId { get; set; } = 0;

        /// <summary>
        /// ID de credencial asociada
        /// </summary>
        public long? CredencialId { get; set; }

        /// <summary>
        /// Nombre del contacto
        /// </summary>
        public string? Nombre { get; set; }

        /// <summary>
        /// Apellido del contacto
        /// </summary>
        public string? Apellido { get; set; }

        /// <summary>
        /// Correo electrónico del contacto
        /// </summary>
        public string? Correo { get; set; }

        /// <summary>
        /// Teléfono del contacto
        /// </summary>
        public string? Telefono { get; set; }

        /// <summary>
        /// Departamento del contacto
        /// </summary>
        public string? Departamento { get; set; }

        /// <summary>
        /// Código interno del contacto
        /// </summary>
        public string? CodigoInterno { get; set; }

        /// <summary>
        /// Indica si el contacto está activo
        /// </summary>
        public bool? Activo { get; set; }

        /// <summary>
        /// Notas adicionales
        /// </summary>
        public string? Notas { get; set; }

        /// <summary>
        /// ID del proveedor asociado
        /// </summary>
        public int? IdProveedor { get; set; }

        /// <summary>
        /// Cargo del contacto
        /// </summary>
        public string? Cargo { get; set; }

        /// <summary>
        /// ID del cliente asociado
        /// </summary>
        public int? IdCliente { get; set; }

        /// <summary>
        /// Estatus del contacto
        /// </summary>
        public bool? Estatus { get; set; }
    }
}
