using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.EndPointProvider;

namespace Advance_Control.Services.Logging
{
    /// <summary>
    /// Implementación del servicio de logging que envía logs al servidor
    /// </summary>
    public class LoggingService : ILoggingService
    {
        private readonly HttpClient _http;
        private readonly IApiEndpointProvider _endpoints;
        private readonly string _machineName;
        private readonly string _appVersion;

        public LoggingService(HttpClient http, IApiEndpointProvider endpoints)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
            _machineName = Environment.MachineName;
            _appVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";
        }

        public Task LogTraceAsync(string message, string? source = null, string? method = null, CancellationToken cancellationToken = default)
        {
            return LogAsync(CreateLogEntry(Models.LogLevel.Trace, message, null, source, method), cancellationToken);
        }

        public Task LogDebugAsync(string message, string? source = null, string? method = null, CancellationToken cancellationToken = default)
        {
            return LogAsync(CreateLogEntry(Models.LogLevel.Debug, message, null, source, method), cancellationToken);
        }

        public Task LogInformationAsync(string message, string? source = null, string? method = null, CancellationToken cancellationToken = default)
        {
            return LogAsync(CreateLogEntry(Models.LogLevel.Information, message, null, source, method), cancellationToken);
        }

        public Task LogWarningAsync(string message, string? source = null, string? method = null, CancellationToken cancellationToken = default)
        {
            return LogAsync(CreateLogEntry(Models.LogLevel.Warning, message, null, source, method), cancellationToken);
        }

        public Task LogErrorAsync(string message, Exception? exception = null, string? source = null, string? method = null, CancellationToken cancellationToken = default)
        {
            return LogAsync(CreateLogEntry(Models.LogLevel.Error, message, exception, source, method), cancellationToken);
        }

        public Task LogCriticalAsync(string message, Exception? exception = null, string? source = null, string? method = null, CancellationToken cancellationToken = default)
        {
            return LogAsync(CreateLogEntry(Models.LogLevel.Critical, message, exception, source, method), cancellationToken);
        }

        public async Task LogAsync(LogEntry logEntry, CancellationToken cancellationToken = default)
        {
            if (logEntry == null)
                return;

            try
            {
                // Construir URL del endpoint de logging
                var url = _endpoints.GetEndpoint("api", "Logging", "log");

                // Enviar log al servidor (fire-and-forget con timeout corto)
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(5));

                var response = await _http.PostAsJsonAsync(url, logEntry, cts.Token);
                
                // No lanzar excepción si falla, para evitar que el logging cause problemas
                // en la aplicación principal
            }
            catch
            {
                // Silenciar errores de logging para no afectar el flujo principal
                // En producción, podríamos guardar en un archivo local o cola
            }
        }

        private LogEntry CreateLogEntry(Models.LogLevel level, string message, Exception? exception, string? source, string? method)
        {
            return new LogEntry
            {
                Id = Guid.NewGuid().ToString(),
                Level = level,
                Message = message,
                Exception = exception?.Message,
                StackTrace = exception?.StackTrace,
                Source = source,
                Method = method,
                MachineName = _machineName,
                AppVersion = _appVersion,
                Timestamp = DateTime.UtcNow,
                Username = null // Se podría obtener del AuthService si está disponible
            };
        }
    }
}
