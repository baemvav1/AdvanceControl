using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;

namespace Advance_Control.Services.EstadoCuenta
{
    /// <summary>
    /// Servicio para gestionar operaciones con estados de cuenta y depósitos
    /// </summary>
    public interface IEstadoCuentaService
    {
        /// <summary>
        /// Obtiene la lista completa de estados de cuenta
        /// </summary>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Lista de estados de cuenta</returns>
        Task<List<EstadoCuentaDto>> GetEstadosCuentaAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Crea un nuevo estado de cuenta con los datos del periodo
        /// </summary>
        /// <param name="fechaCorte">Fecha de corte del estado de cuenta</param>
        /// <param name="periodoDesde">Inicio del periodo</param>
        /// <param name="periodoHasta">Fin del periodo</param>
        /// <param name="saldoInicial">Saldo inicial del periodo</param>
        /// <param name="saldoCorte">Saldo al momento del corte</param>
        /// <param name="totalDepositos">Total de depósitos del periodo</param>
        /// <param name="totalRetiros">Total de retiros del periodo</param>
        /// <param name="comisiones">Comisiones aplicadas (opcional)</param>
        /// <param name="nombreArchivo">Nombre del archivo PDF/documento (opcional)</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Respuesta con el ID del estado de cuenta creado</returns>
        Task<EstadoCuentaOperationResponse> CreateEstadoCuentaAsync(
            DateTime fechaCorte,
            DateTime periodoDesde,
            DateTime periodoHasta,
            decimal saldoInicial,
            decimal saldoCorte,
            decimal totalDepositos,
            decimal totalRetiros,
            decimal? comisiones = null,
            string? nombreArchivo = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtiene todos los depósitos asociados a un estado de cuenta específico
        /// </summary>
        /// <param name="estadoCuentaId">ID del estado de cuenta (debe ser > 0)</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Lista de depósitos del estado de cuenta</returns>
        Task<List<DepositoDto>> GetDepositosAsync(int estadoCuentaId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Agrega un nuevo depósito a un estado de cuenta existente
        /// </summary>
        /// <param name="estadoCuentaId">ID del estado de cuenta (debe ser > 0)</param>
        /// <param name="fechaDeposito">Fecha del depósito</param>
        /// <param name="descripcionDeposito">Descripción del depósito (no puede estar vacío)</param>
        /// <param name="montoDeposito">Monto del depósito (debe ser > 0)</param>
        /// <param name="tipoDeposito">Tipo de depósito (ej: "Transferencia", "Efectivo", "Cheque")</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Respuesta con el ID del depósito creado</returns>
        Task<EstadoCuentaOperationResponse> AddDepositoAsync(
            int estadoCuentaId,
            DateTime fechaDeposito,
            string descripcionDeposito,
            decimal montoDeposito,
            string tipoDeposito,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtiene un resumen de los depósitos agrupados por tipo para un estado de cuenta
        /// </summary>
        /// <param name="estadoCuentaId">ID del estado de cuenta (debe ser > 0)</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Lista con el resumen de depósitos por tipo</returns>
        Task<List<ResumenDepositoDto>> GetResumenDepositosAsync(int estadoCuentaId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Verifica si un depósito específico existe en el estado de cuenta
        /// </summary>
        /// <param name="estadoCuentaId">ID del estado de cuenta (debe ser > 0)</param>
        /// <param name="fechaDeposito">Fecha del depósito a verificar</param>
        /// <param name="descripcionDeposito">Descripción del depósito (no puede estar vacío)</param>
        /// <param name="montoDeposito">Monto del depósito a verificar</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Resultado de la verificación</returns>
        Task<DepositoVerificacionDto> VerificarDepositoAsync(
            int estadoCuentaId,
            DateTime fechaDeposito,
            string descripcionDeposito,
            decimal montoDeposito,
            CancellationToken cancellationToken = default);
    }
}
