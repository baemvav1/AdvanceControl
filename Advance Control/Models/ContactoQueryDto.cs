namespace Advance_Control.Models
{
    /// <summary>
    /// DTO para los parámetros de búsqueda de contactos
    /// </summary>
    public class ContactoQueryDto
    {
        /// <summary>
        /// ID del contacto específico
        /// </summary>
        public long? ContactoId { get; set; }

        /// <summary>
        /// Búsqueda por credencial ID
        /// </summary>
        public long? CredencialId { get; set; }

        /// <summary>
        /// Búsqueda parcial por nombre
        /// </summary>
        public string? Nombre { get; set; }

        /// <summary>
        /// Búsqueda parcial por apellido
        /// </summary>
        public string? Apellido { get; set; }

        /// <summary>
        /// Búsqueda parcial por correo
        /// </summary>
        public string? Correo { get; set; }

        /// <summary>
        /// Búsqueda parcial por teléfono
        /// </summary>
        public string? Telefono { get; set; }

        /// <summary>
        /// Búsqueda parcial por departamento
        /// </summary>
        public string? Departamento { get; set; }

        /// <summary>
        /// Búsqueda parcial por código interno
        /// </summary>
        public string? CodigoInterno { get; set; }

        /// <summary>
        /// Búsqueda por ID de proveedor
        /// </summary>
        public int? IdProveedor { get; set; }

        /// <summary>
        /// Búsqueda parcial por cargo
        /// </summary>
        public string? Cargo { get; set; }

        /// <summary>
        /// Búsqueda por ID de cliente
        /// </summary>
        public int? IdCliente { get; set; }
    }
}
