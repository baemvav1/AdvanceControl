using Microsoft.Extensions.Options;
using System;
using System.Linq;

namespace Advance_Control.Services.EndPointProvider
{
    public class ApiEndpointProvider : IApiEndpointProvider
    {
        private readonly ExternalApiOptions _options;
        private readonly string _baseUrlNormalized;

        public ApiEndpointProvider(IOptions<ExternalApiOptions> options)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            if (string.IsNullOrWhiteSpace(_options.BaseUrl))
                throw new ArgumentException("ExternalApi:BaseUrl must be configured in appsettings.json");

            // Validar que BaseUrl sea una URL válida
            if (!Uri.TryCreate(_options.BaseUrl, UriKind.Absolute, out var uri))
                throw new ArgumentException($"ExternalApi:BaseUrl is not a valid absolute URI: {_options.BaseUrl}");
            
            // SEGURIDAD: Validar que use HTTPS en producción (permitir HTTP solo para localhost/desarrollo)
            if (uri.Scheme != "https" && uri.Scheme != "http")
                throw new ArgumentException($"ExternalApi:BaseUrl must use HTTP or HTTPS scheme: {_options.BaseUrl}");
            
            // Advertencia si se usa HTTP en un host que no es localhost
            if (uri.Scheme == "http" && !uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) && !uri.Host.StartsWith("127."))
            {
                // En un entorno real, esto debería ser un error. Por ahora solo advertencia.
                System.Diagnostics.Debug.WriteLine($"ADVERTENCIA DE SEGURIDAD: BaseUrl usa HTTP en lugar de HTTPS para host no-local: {_options.BaseUrl}");
            }

            // Normalize base URL (remove trailing slash)
            _baseUrlNormalized = _options.BaseUrl.Trim();
            if (_baseUrlNormalized.EndsWith("/"))
                _baseUrlNormalized = _baseUrlNormalized.TrimEnd('/');
        }

        public string GetApiBaseUrl() => _baseUrlNormalized;

        public string GetEndpoint(string routeRelative)
        {
            if (string.IsNullOrWhiteSpace(routeRelative))
                throw new ArgumentException(nameof(routeRelative));

            return Combine(_baseUrlNormalized, routeRelative);
        }

        public string GetEndpoint(params string[] routeParts)
        {
            if (routeParts == null || routeParts.Length == 0)
                throw new ArgumentException(nameof(routeParts));

            var joined = string.Join("/", routeParts.Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p.Trim('/')));
            return Combine(_baseUrlNormalized, joined);
        }

        private static string Combine(string baseUrl, string relative)
        {
            // baseUrl comes normalized without trailing slash
            baseUrl = baseUrl.Trim();

            // Normalize relative part
            relative = (relative ?? string.Empty).Trim();
            if (relative.StartsWith("/")) relative = relative.TrimStart('/');

            // Use Uri to safely combine
            var baseUri = baseUrl.EndsWith("/") ? new Uri(baseUrl) : new Uri(baseUrl + "/");
            return new Uri(baseUri, relative).ToString();
        }
    }
}