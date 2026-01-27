namespace Advance_Control.Models
{
    /// <summary>
    /// DTO para los parámetros del procedimiento almacenado sp_cliente_edit
    /// </summary>
    public class ClienteEditDto
    {
        /// <summary>
        /// Tipo de operación: 'select', 'delete', 'update', 'create'
        /// </summary>
        public string Operacion { get; set; } = string.Empty;

        /// <summary>
        /// ID del cliente (requerido para delete y update)
        /// </summary>
        public int IdCliente { get; set; } = 0;

        /// <summary>
        /// RFC del cliente
        /// </summary>
        public string? Rfc { get; set; }

        /// <summary>
        /// Razón social del cliente
        /// </summary>
        public string? RazonSocial { get; set; }

        /// <summary>
        /// Nombre comercial del cliente
        /// </summary>
        public string? NombreComercial { get; set; }

        /// <summary>
        /// Régimen fiscal
        /// </summary>
        public string? RegimenFiscal { get; set; }

        /// <summary>
        /// Uso de CFDI
        /// </summary>
        public string? UsoCfdi { get; set; }

        /// <summary>
        /// Días de crédito
        /// </summary>
        public int? DiasCredito { get; set; }

        /// <summary>
        /// Límite de crédito
        /// </summary>
        public decimal? LimiteCredito { get; set; }

        /// <summary>
        /// Prioridad del cliente
        /// </summary>
        public int? Prioridad { get; set; }

        /// <summary>
        /// Estatus del cliente (activo/inactivo)
        /// </summary>
        public bool Estatus { get; set; } = true;

        /// <summary>
        /// ID de credencial asociada
        /// </summary>
        public int? CredencialId { get; set; }

        /// <summary>
        /// Notas adicionales
        /// </summary>
        public string? Notas { get; set; }

        /// <summary>
        /// ID del usuario que realiza la operación
        /// </summary>
        public int? IdUsuario { get; set; }
    }
}
