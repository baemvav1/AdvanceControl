using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Advance_Control.Services.EndPointProvider
{
    public class ApiEndpointProvider : IApiEndpointProvider
    {
        private readonly ExternalApiOptions _options;

        public ApiEndpointProvider(IOptions<ExternalApiOptions> options)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            if (string.IsNullOrWhiteSpace(_options.BaseUrl))
                throw new ArgumentException("ExternalApi:BaseUrl must be configured in appsettings.json");
        }

        public string GetEndpoint(string routeRelative)
        {
            if (string.IsNullOrWhiteSpace(routeRelative))
                throw new ArgumentException(nameof(routeRelative));

            return Combine(_options.BaseUrl, routeRelative);
        }

        public string GetEndpoint(params string[] routeParts)
        {
            if (routeParts == null || routeParts.Length == 0)
                throw new ArgumentException(nameof(routeParts));

            var joined = string.Join("/", routeParts.Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p.Trim('/')));
            return Combine(_options.BaseUrl, joined);
        }

        private static string Combine(string baseUrl, string relative)
        {
            // Asegura que baseUrl termina con /
            baseUrl = baseUrl.Trim();
            if (!baseUrl.EndsWith("/")) baseUrl += "/";

            // Normaliza relativa sin /
            relative = relative.Trim();

            // Evita doble slash en la concatenación
            if (relative.StartsWith("/")) relative = relative.TrimStart('/');

            return new Uri(new Uri(baseUrl, UriKind.Absolute), relative).ToString();
        }
    }
}
