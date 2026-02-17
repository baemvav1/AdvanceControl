namespace Advance_Control.Models
{
    /// <summary>
    /// Modelo para representar una transacción del estado de cuenta XML
    /// </summary>
    public class Transaccion
    {
        /// <summary>
        /// ID Transaccion
        /// Almacenado como string para permitir diferentes formatos de XML
        /// </summary>
        public int? Id { get; set; }
        /// <summary>
        /// Fecha de la transacción
        /// Almacenado como string para permitir diferentes formatos de XML
        /// </summary>
        public string? Fecha { get; set; }

        /// <summary>
        /// Descripción de la transacción
        /// </summary>
        public string? Descripcion { get; set; }

        /// <summary>
        /// Monto de la transacción (puede ser positivo o negativo)
        /// </summary>
        public decimal? Monto { get; set; }

        /// <summary>
        /// Tipo de transacción (cargo o abono)
        /// </summary>
        public string? Tipo { get; set; }

        /// <summary>
        /// Saldo después de la transacción
        /// </summary>
        public decimal? Saldo { get; set; }

        /// <summary>
        /// Referencia o número de transacción
        /// </summary>
        public string? Referencia { get; set; }
    }
}
