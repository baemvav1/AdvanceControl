using Advance_Control.Services.EndPointProvider;
using Advance_Control.Services.Logging;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Advance_Control.Services.OnlineCheck
{
    public class OnlineCheck : IOnlineCheck
    {
        private readonly HttpClient _httpClient;
        private readonly IApiEndpointProvider _endpointProvider;
        private readonly ILoggingService? _logger;
        private const string _relativeEndpoint = "Online"; // ruta relativa que describe el recurso

        public OnlineCheck(HttpClient httpClient, IApiEndpointProvider endpointProvider, ILoggingService? logger = null)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _endpointProvider = endpointProvider ?? throw new ArgumentNullException(nameof(endpointProvider));
            _logger = logger;
        }

        public async Task<OnlineCheckResult> CheckAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var endpoint = _endpointProvider.GetEndpoint(_relativeEndpoint);

                // Intento HEAD por ser más ligero; si no está soportado, fallback a GET
                using (var req = new HttpRequestMessage(HttpMethod.Head, endpoint))
                {
                    var resp = await _httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

                    if (resp.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed)
                    {
                        resp.Dispose();
                        resp = await _httpClient.GetAsync(endpoint, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
                    }

                    var status = (int)resp.StatusCode;
                    if (status >= 200 && status <= 299)
                        return OnlineCheckResult.Success();

                    return OnlineCheckResult.FromHttpStatus(status, $"API responded with status code {status}");
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger?.LogWarningAsync("Verificación de conectividad cancelada", "OnlineCheck", "CheckAsync");
                return OnlineCheckResult.FromException("Operation cancelled");
            }
            catch (Exception ex)
            {
                await _logger?.LogErrorAsync("Error al verificar conectividad con la API", ex, "OnlineCheck", "CheckAsync");
                // Ejemplos: DNS, connection refused, TLS/SSL, timeout, etc.
                return OnlineCheckResult.FromException(ex.Message);
            }
        }
    }
}