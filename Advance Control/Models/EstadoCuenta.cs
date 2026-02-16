using System.Collections.Generic;

namespace Advance_Control.Models
{
    /// <summary>
    /// Modelo para representar un estado de cuenta completo del XML
    /// </summary>
    public class EstadoCuenta
    {
        /// <summary>
        /// Número de cuenta
        /// </summary>
        public string? NumeroCuenta { get; set; }

        /// <summary>
        /// Nombre del titular de la cuenta
        /// </summary>
        public string? Titular { get; set; }

        /// <summary>
        /// Período del estado de cuenta
        /// </summary>
        public string? Periodo { get; set; }

        /// <summary>
        /// Fecha de inicio del período
        /// </summary>
        public string? FechaInicio { get; set; }

        /// <summary>
        /// Fecha de fin del período
        /// </summary>
        public string? FechaFin { get; set; }

        /// <summary>
        /// Saldo inicial del período
        /// </summary>
        public decimal? SaldoInicial { get; set; }

        /// <summary>
        /// Saldo final del período
        /// </summary>
        public decimal? SaldoFinal { get; set; }

        /// <summary>
        /// Total de cargos en el período
        /// </summary>
        public decimal? TotalCargos { get; set; }

        /// <summary>
        /// Total de abonos en el período
        /// </summary>
        public decimal? TotalAbonos { get; set; }

        /// <summary>
        /// Banco emisor del estado de cuenta
        /// </summary>
        public string? Banco { get; set; }

        /// <summary>
        /// Sucursal del banco
        /// </summary>
        public string? Sucursal { get; set; }

        /// <summary>
        /// Lista de transacciones del estado de cuenta
        /// </summary>
        public List<Transaccion> Transacciones { get; set; } = new List<Transaccion>();
    }
}
