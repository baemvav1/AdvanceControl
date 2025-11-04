using System;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;

namespace Advance_Control.Services.Logging
{
    /// <summary>
    /// Servicio de logging que envía logs al servidor
    /// </summary>
    public interface ILoggingService
    {
        /// <summary>
        /// Registra un log de nivel Trace
        /// </summary>
        Task LogTraceAsync(string message, string? source = null, string? method = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Registra un log de nivel Debug
        /// </summary>
        Task LogDebugAsync(string message, string? source = null, string? method = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Registra un log de nivel Information
        /// </summary>
        Task LogInformationAsync(string message, string? source = null, string? method = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Registra un log de nivel Warning
        /// </summary>
        Task LogWarningAsync(string message, string? source = null, string? method = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Registra un log de nivel Error con excepción
        /// </summary>
        Task LogErrorAsync(string message, Exception? exception = null, string? source = null, string? method = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Registra un log de nivel Critical con excepción
        /// </summary>
        Task LogCriticalAsync(string message, Exception? exception = null, string? source = null, string? method = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Registra una entrada de log personalizada
        /// </summary>
        Task LogAsync(LogEntry logEntry, CancellationToken cancellationToken = default);
    }
}
