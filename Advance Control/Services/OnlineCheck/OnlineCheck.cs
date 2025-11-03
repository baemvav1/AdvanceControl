using Advance_Control.Services.EndPointProvider;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Advance_Control.Services.OnlineCheck
{
    /// <summary>
    /// Servicio para verificar la disponibilidad de la API externa.
    /// Implementa IOnlineCheck realizando peticiones HTTP al endpoint de verificación.
    /// </summary>
    public class OnlineCheck : IOnlineCheck
    {
        private readonly HttpClient _httpClient;
        private readonly IApiEndpointProvider _endpointProvider;
        private const string _relativeEndpoint = "Online"; // ruta relativa que describe el recurso

        /// <summary>
        /// Inicializa una nueva instancia de OnlineCheck.
        /// </summary>
        /// <param name="httpClient">Cliente HTTP para realizar peticiones.</param>
        /// <param name="endpointProvider">Proveedor de endpoints para construir URLs.</param>
        /// <exception cref="ArgumentNullException">Si httpClient o endpointProvider es null.</exception>
        public OnlineCheck(HttpClient httpClient, IApiEndpointProvider endpointProvider)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _endpointProvider = endpointProvider ?? throw new ArgumentNullException(nameof(endpointProvider));
        }

        /// <summary>
        /// Verifica la disponibilidad de la API realizando una petición al endpoint de verificación.
        /// Intenta primero HEAD (más ligero), si no está soportado hace fallback a GET.
        /// </summary>
        /// <param name="cancellationToken">Token para cancelar la operación.</param>
        /// <returns>
        /// OnlineCheckResult con IsOnline = true si la API responde con código 2xx,
        /// IsOnline = false con detalles de error en caso contrario.
        /// </returns>
        /// <remarks>
        /// Este método no lanza excepciones, devuelve un resultado estructurado con información del error.
        /// Maneja errores de red (DNS, timeout, SSL) y errores HTTP (4xx, 5xx).
        /// </remarks>
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
                return OnlineCheckResult.FromException("Operation cancelled");
            }
            catch (Exception ex)
            {
                // Ejemplos: DNS, connection refused, TLS/SSL, timeout, etc.
                return OnlineCheckResult.FromException(ex.Message);
            }
        }
    }
}