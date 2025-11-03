using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Advance_Control.Services.EndPointProvider
{
    /// <summary>
    /// Proveedor de endpoints que construye URLs absolutas combinando la URL base
    /// configurada con rutas relativas.
    /// </summary>
    public class ApiEndpointProvider : IApiEndpointProvider
    {
        private readonly ExternalApiOptions _options;

        /// <summary>
        /// Inicializa una nueva instancia de ApiEndpointProvider.
        /// </summary>
        /// <param name="options">Opciones de configuración de la API externa.</param>
        /// <exception cref="ArgumentNullException">Si options es null.</exception>
        /// <exception cref="ArgumentException">Si ExternalApi:BaseUrl no está configurado.</exception>
        public ApiEndpointProvider(IOptions<ExternalApiOptions> options)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            if (string.IsNullOrWhiteSpace(_options.BaseUrl))
                throw new ArgumentException("ExternalApi:BaseUrl must be configured in appsettings.json");
        }

        /// <summary>
        /// Construye una URL absoluta combinando la BaseUrl con la ruta relativa.
        /// </summary>
        /// <param name="routeRelative">Ruta relativa al recurso (ej: "Online", "auth/login", "customers/123").</param>
        /// <returns>URL completa como string.</returns>
        /// <exception cref="ArgumentException">Si routeRelative es null o vacío.</exception>
        /// <example>
        /// <code>
        /// // BaseUrl configurado como "https://api.example.com/"
        /// var url = provider.GetEndpoint("customers/123");
        /// // Resultado: "https://api.example.com/customers/123"
        /// </code>
        /// </example>
        public string GetEndpoint(string routeRelative)
        {
            if (string.IsNullOrWhiteSpace(routeRelative))
                throw new ArgumentException(nameof(routeRelative));

            return Combine(_options.BaseUrl, routeRelative);
        }

        /// <summary>
        /// Construye una URL absoluta combinando la BaseUrl con múltiples partes de ruta.
        /// </summary>
        /// <param name="routeParts">Array de partes de la ruta que serán unidas.</param>
        /// <returns>URL completa como string.</returns>
        /// <exception cref="ArgumentException">Si routeParts es null o vacío.</exception>
        /// <example>
        /// <code>
        /// // BaseUrl configurado como "https://api.example.com/"
        /// var url = provider.GetEndpoint("customers", "123", "orders");
        /// // Resultado: "https://api.example.com/customers/123/orders"
        /// </code>
        /// </example>
        public string GetEndpoint(params string[] routeParts)
        {
            if (routeParts == null || routeParts.Length == 0)
                throw new ArgumentException(nameof(routeParts));

            var joined = string.Join("/", routeParts.Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p.Trim('/')));
            return Combine(_options.BaseUrl, joined);
        }

        /// <summary>
        /// Combina la URL base con una ruta relativa normalizando barras.
        /// </summary>
        /// <param name="baseUrl">URL base de la API.</param>
        /// <param name="relative">Ruta relativa al recurso.</param>
        /// <returns>URL completa normalizada.</returns>
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
