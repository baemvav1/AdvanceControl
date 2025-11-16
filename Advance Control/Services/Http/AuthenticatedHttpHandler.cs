using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Services.Auth;
using Advance_Control.Services.EndPointProvider;
using Advance_Control.Services.Logging;

namespace Advance_Control.Services.Http
{
    /// <summary>
    /// DelegatingHandler que:
    /// - adjunta Authorization: Bearer &lt;token&gt; a las requests dirigidas a la API configurada,
    /// - intenta un refresh (a través de IAuthService.RefreshTokenAsync) al recibir 401 y reintenta la petición una vez con el nuevo token.
    /// 
    /// Nota importante:
    /// - Usa Lazy&lt;IAuthService&gt; para romper la dependencia circular:
    ///   AuthService → AuthenticatedHttpHandler → IAuthService
    /// - El Lazy&lt;T&gt; permite que el servicio se resuelva solo cuando se necesita (lazy loading).
    /// 
    /// Recomendaciones:
    /// - Registrar en DI como transient y añadir a la pipeline del HttpClient que usas para la API.
    /// - No adjunta token a dominios externos (comprueba host contra el proveedor de endpoints).
    /// </summary>
    public class AuthenticatedHttpHandler : DelegatingHandler
    {
        private readonly Lazy<IAuthService> _authService;
        private readonly IApiEndpointProvider _endpointProvider;
        private readonly ILoggingService? _logger;
        private readonly string? _apiHost; // normalized host (lowercase) or null

        public AuthenticatedHttpHandler(Lazy<IAuthService> authService, IApiEndpointProvider endpointProvider, ILoggingService? logger = null)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _endpointProvider = endpointProvider ?? throw new ArgumentNullException(nameof(endpointProvider));
            _logger = logger;

            // Try to normalize the API host now for quicker comparisons later.
            try
            {
                var baseUrl = _endpointProvider.GetApiBaseUrl();
                if (Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri))
                {
                    _apiHost = baseUri.Host?.ToLowerInvariant();
                }
            }
            catch
            {
                _ = _logger?.LogWarningAsync("No se pudo obtener el host de la API para AuthenticatedHttpHandler", "AuthenticatedHttpHandler", ".ctor");
                _apiHost = null;
            }
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            // Only attach token for requests that target the API host (prevents token leakage to external domains).
            if (ShouldAttachToken(request.RequestUri))
            {
                var token = await _authService.Value.GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(token))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }
            }

            var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

            // If unauthorized, attempt a single refresh + retry
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                // Dispose original response early
                response.Dispose();

                var refreshed = await _authService.Value.RefreshTokenAsync(cancellationToken).ConfigureAwait(false);
                if (!refreshed)
                {
                    // refresh failed -> return 401 to caller
                    return new HttpResponseMessage(HttpStatusCode.Unauthorized)
                    {
                        RequestMessage = request
                    };
                }

                // Get new token and retry once
                var newToken = await _authService.Value.GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);
                if (string.IsNullOrEmpty(newToken))
                {
                    return new HttpResponseMessage(HttpStatusCode.Unauthorized)
                    {
                        RequestMessage = request
                    };
                }

                // Clone the original request to retry (we must not reuse the same HttpRequestMessage after sending)
                var retryRequest = await CloneHttpRequestMessageAsync(request).ConfigureAwait(false);

                // Attach new token if appropriate
                if (ShouldAttachToken(retryRequest.RequestUri))
                {
                    retryRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newToken);
                }

                return await base.SendAsync(retryRequest, cancellationToken).ConfigureAwait(false);
            }

            return response;
        }

        private bool ShouldAttachToken(Uri? requestUri)
        {
            if (requestUri == null) return false;
            // SEGURIDAD: Si no pudimos determinar el API host, ser restrictivo por defecto
            // para prevenir fuga de tokens a dominios no autorizados
            if (!_apiHost.HasValue()) 
            {
                _ = _logger?.LogWarningAsync("No se pudo determinar el host de la API. No se adjuntará token por seguridad.", "AuthenticatedHttpHandler", "ShouldAttachToken");
                return false;
            }
            try
            {
                var host = requestUri.Host?.ToLowerInvariant();
                return host == _apiHost;
            }
            catch
            {
                _ = _logger?.LogWarningAsync($"Error al verificar si se debe adjuntar token a la URI: {requestUri}", "AuthenticatedHttpHandler", "ShouldAttachToken");
                return false;
            }
        }

        private static async Task<HttpRequestMessage> CloneHttpRequestMessageAsync(HttpRequestMessage req)
        {
            var clone = new HttpRequestMessage(req.Method, req.RequestUri)
            {
                Version = req.Version
            };

            // Copy headers
            foreach (var header in req.Headers)
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

            // Copy properties (Options/Properties may vary by platform; copy common ones)
#if NET5_0_OR_GREATER
            foreach (var prop in req.Options)
            {
                // HttpRequestOptionsKey is not trivially clonable; skip for simplicity.
            }
#endif

            // Copy content if present
            if (req.Content != null)
            {
                // Buffer content to memory stream
                var ms = new MemoryStream();
                await req.Content.CopyToAsync(ms).ConfigureAwait(false);
                ms.Position = 0;
                clone.Content = new StreamContent(ms);

                // Copy content headers
                if (req.Content.Headers != null)
                {
                    foreach (var h in req.Content.Headers)
                        clone.Content.Headers.TryAddWithoutValidation(h.Key, h.Value);
                }
            }

            return clone;
        }
    }

    // Small extension to check for null/empty strings on nullable types
    internal static class StringExtensions
    {
        public static bool HasValue(this string? s) => !string.IsNullOrEmpty(s);
    }
}