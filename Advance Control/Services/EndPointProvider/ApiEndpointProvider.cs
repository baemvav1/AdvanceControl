using Advance_Control.Settings;
using Microsoft.Extensions.Options;
using System;
using System.Linq;

namespace Advance_Control.Services.EndPointProvider
{
    public class ApiEndpointProvider : IApiEndpointProvider
    {
        private readonly ExternalApiOptions _options;
        private readonly string _baseUrlNormalized;
        private readonly bool _isProductionMode;

        public ApiEndpointProvider(IOptions<ExternalApiOptions> options, IOptions<DevelopmentModeOptions> devModeOptions)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            var devMode = devModeOptions?.Value ?? new DevelopmentModeOptions();

            // Producción = DevelopmentMode.Enabled es false (valor por defecto)
            _isProductionMode = !devMode.Enabled;

            // Seleccionar la URL según el modo
            string selectedUrl;
            if (_isProductionMode && !string.IsNullOrWhiteSpace(_options.ProductionUrl))
                selectedUrl = _options.ProductionUrl;
            else if (!string.IsNullOrWhiteSpace(_options.BaseUrl))
                selectedUrl = _options.BaseUrl;
            else
                throw new ArgumentException("ExternalApi:BaseUrl o ExternalApi:ProductionUrl debe estar configurado en appsettings.json");

            // Normalize base URL (remove trailing slash)
            _baseUrlNormalized = selectedUrl.Trim().TrimEnd('/');
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