using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.EndPointProvider;
using Advance_Control.Services.Logging;

namespace Advance_Control.Services.Proveedores
{
    /// <summary>
    /// Implementación del servicio de proveedores que se comunica con la API
    /// </summary>
    public class ProveedorService : IProveedorService
    {
        private readonly HttpClient _http;
        private readonly IApiEndpointProvider _endpoints;
        private readonly ILoggingService _logger;

        public ProveedorService(HttpClient http, IApiEndpointProvider endpoints, ILoggingService logger)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Obtiene una lista de proveedores según los criterios de búsqueda proporcionados
        /// </summary>
        public async Task<List<ProveedorDto>> GetProveedoresAsync(ProveedorQueryDto? query = null, CancellationToken cancellationToken = default)
        {
            try
            {
                // Construir la URL base
                var url = _endpoints.GetEndpoint("api", "proveedores");

                // Agregar parámetros de consulta si existen
                if (query != null)
                {
                    var queryParams = new List<string>();

                    if (!string.IsNullOrWhiteSpace(query.Rfc))
                        queryParams.Add($"rfc={Uri.EscapeDataString(query.Rfc)}");

                    if (!string.IsNullOrWhiteSpace(query.RazonSocial))
                        queryParams.Add($"razonSocial={Uri.EscapeDataString(query.RazonSocial)}");

                    if (!string.IsNullOrWhiteSpace(query.NombreComercial))
                        queryParams.Add($"nombreComercial={Uri.EscapeDataString(query.NombreComercial)}");

                    if (!string.IsNullOrWhiteSpace(query.Nota))
                        queryParams.Add($"nota={Uri.EscapeDataString(query.Nota)}");

                    if (queryParams.Count > 0)
                    {
                        url = $"{url}?{string.Join("&", queryParams)}";
                    }
                }

                await _logger.LogInformationAsync($"Obteniendo proveedores desde: {url}", "ProveedorService", "GetProveedoresAsync");

                // Realizar la petición GET
                var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);

                // Verificar si la respuesta fue exitosa
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al obtener proveedores. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "ProveedorService",
                        "GetProveedoresAsync");
                    return new List<ProveedorDto>();
                }

                // Deserializar la respuesta
                var proveedores = await response.Content.ReadFromJsonAsync<List<ProveedorDto>>(cancellationToken: cancellationToken).ConfigureAwait(false);

                await _logger.LogInformationAsync($"Se obtuvieron {proveedores?.Count ?? 0} proveedores", "ProveedorService", "GetProveedoresAsync");

                return proveedores ?? new List<ProveedorDto>();
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al obtener proveedores", ex, "ProveedorService", "GetProveedoresAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al obtener proveedores", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al obtener proveedores", ex, "ProveedorService", "GetProveedoresAsync");
                throw;
            }
        }
    }
}
