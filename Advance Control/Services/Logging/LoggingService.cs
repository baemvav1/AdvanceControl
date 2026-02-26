using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.EndPointProvider;
using Advance_Control.Services.NotificationSettings;
using Advance_Control.Services.Session;

namespace Advance_Control.Services.Logging
{
    /// <summary>
    /// Implementación del servicio de logging.
    /// Auto-popula CredencialId y Username desde IUserSessionService en cada entrada.
    /// Usa Lazy&lt;IUserSessionService&gt; para romper la dependencia circular con UserSessionService.
    /// </summary>
    public class LoggingService : ILoggingService
    {
        private readonly HttpClient _http;
        private readonly IApiEndpointProvider _endpoints;
        private readonly Lazy<IUserSessionService> _session;
        private readonly INotificationSettingsService _notificationSettings;
        private readonly string _machineName;
        private readonly string _appVersion;

        public LoggingService(HttpClient http, IApiEndpointProvider endpoints,
            Lazy<IUserSessionService> session,
            INotificationSettingsService notificationSettings)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _notificationSettings = notificationSettings ?? throw new ArgumentNullException(nameof(notificationSettings));
            _machineName = Environment.MachineName;
            _appVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";
        }

        public Task LogTraceAsync(string message, string? source = null, string? method = null,
            string? categoria = null, string? page = null, CancellationToken cancellationToken = default)
            => LogAsync(Build(Models.LogLevel.Trace, message, null, source, method, categoria, page), cancellationToken);

        public Task LogDebugAsync(string message, string? source = null, string? method = null,
            string? categoria = null, string? page = null, CancellationToken cancellationToken = default)
            => LogAsync(Build(Models.LogLevel.Debug, message, null, source, method, categoria, page), cancellationToken);

        public Task LogInformationAsync(string message, string? source = null, string? method = null,
            string? categoria = null, string? page = null, CancellationToken cancellationToken = default)
            => LogAsync(Build(Models.LogLevel.Information, message, null, source, method, categoria, page), cancellationToken);

        public Task LogWarningAsync(string message, string? source = null, string? method = null,
            string? categoria = null, string? page = null, CancellationToken cancellationToken = default)
            => LogAsync(Build(Models.LogLevel.Warning, message, null, source, method, categoria, page), cancellationToken);

        public Task LogErrorAsync(string message, Exception? exception = null,
            string? source = null, string? method = null,
            string? categoria = null, string? page = null, CancellationToken cancellationToken = default)
            => LogAsync(Build(Models.LogLevel.Error, message, exception, source, method, categoria, page), cancellationToken);

        public Task LogCriticalAsync(string message, Exception? exception = null,
            string? source = null, string? method = null,
            string? categoria = null, string? page = null, CancellationToken cancellationToken = default)
            => LogAsync(Build(Models.LogLevel.Critical, message, exception, source, method, categoria, page), cancellationToken);

        public async Task LogAsync(LogEntry logEntry, CancellationToken cancellationToken = default)
        {
            if (logEntry == null) return;

            // Auto-completar campos de sesión si están vacíos y la sesión ya fue cargada
            try
            {
                var sess = _session.Value;
                if (sess.IsLoaded)
                {
                    if (logEntry.CredencialId == null && sess.CredencialId > 0)
                        logEntry.CredencialId = sess.CredencialId;
                    if (string.IsNullOrEmpty(logEntry.Username))
                        logEntry.Username = sess.NombreCompleto;
                }
            }
            catch { /* sesión aún no lista — se omite */ }

            try
            {
                // Registrar la categoría en la lista de notificaciones permitidas si es nueva
                await _notificationSettings.EnsureCategoryRegisteredAsync(logEntry.Categoria, logEntry.Page).ConfigureAwait(false);
            }
            catch { /* silenciar */ }

            try
            {
                var url = _endpoints.GetEndpoint("api", "Logging", "log");
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(5));
                _ = await _http.PostAsJsonAsync(url, logEntry, cts.Token);
            }
            catch
            {
                // Silenciar: el logging nunca debe interrumpir el flujo principal
            }
        }

        private LogEntry Build(Models.LogLevel level, string message, Exception? exception,
            string? source, string? method, string? categoria, string? page)
        {
            return new LogEntry
            {
                Id          = Guid.NewGuid().ToString(),
                Level       = level,
                Message     = message,
                Exception   = exception?.Message,
                StackTrace  = exception?.StackTrace,
                Source      = source,
                Method      = method,
                MachineName = _machineName,
                AppVersion  = _appVersion,
                Timestamp   = DateTime.UtcNow,
                Categoria   = categoria,
                Page        = page,
                // CredencialId y Username se completan en LogAsync() si la sesión está disponible
            };
        }
    }
}


