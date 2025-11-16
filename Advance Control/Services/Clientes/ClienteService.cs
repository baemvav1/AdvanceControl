using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.EndPointProvider;
using Advance_Control.Services.Logging;

namespace Advance_Control.Services.Clientes
{
    /// <summary>
    /// Implementación del servicio de clientes que se comunica con la API
    /// </summary>
    public class ClienteService : IClienteService
    {
        private readonly HttpClient _http;
        private readonly IApiEndpointProvider _endpoints;
        private readonly ILoggingService _logger;

        public ClienteService(HttpClient http, IApiEndpointProvider endpoints, ILoggingService logger)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Obtiene una lista de clientes según los criterios de búsqueda proporcionados
        /// </summary>
        public async Task<List<CustomerDto>> GetClientesAsync(ClienteQueryDto? query = null, CancellationToken cancellationToken = default)
        {
            try
            {
                // Construir la URL base
                var url = _endpoints.GetEndpoint("api", "Clientes");

                // Agregar parámetros de consulta si existen
                if (query != null)
                {
                    var queryParams = new List<string>();

                    if (!string.IsNullOrWhiteSpace(query.Search))
                        queryParams.Add($"search={Uri.EscapeDataString(query.Search)}");

                    if (!string.IsNullOrWhiteSpace(query.Rfc))
                        queryParams.Add($"rfc={Uri.EscapeDataString(query.Rfc)}");

                    if (!string.IsNullOrWhiteSpace(query.Curp))
                        queryParams.Add($"curp={Uri.EscapeDataString(query.Curp)}");

                    if (!string.IsNullOrWhiteSpace(query.Notas))
                        queryParams.Add($"notas={Uri.EscapeDataString(query.Notas)}");

                    if (query.Prioridad.HasValue)
                        queryParams.Add($"prioridad={query.Prioridad.Value}");

                    if (queryParams.Count > 0)
                    {
                        url = $"{url}?{string.Join("&", queryParams)}";
                    }
                }

                await _logger.LogInformationAsync($"Obteniendo clientes desde: {url}", "ClienteService", "GetClientesAsync");

                // Realizar la petición GET
                var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);

                // Verificar si la respuesta fue exitosa
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    var errorMessage = $"Error al obtener clientes. Status: {response.StatusCode}, Content: {errorContent}";
                    await _logger.LogErrorAsync(errorMessage, null, "ClienteService", "GetClientesAsync");
                    
                    // Lanzar excepción específica según el código de estado para permitir manejo diferenciado
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        throw new UnauthorizedAccessException("No autorizado para obtener clientes. Por favor, inicie sesión nuevamente.");
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                    {
                        throw new UnauthorizedAccessException("No tiene permisos para obtener la lista de clientes.");
                    }
                    else if ((int)response.StatusCode >= 500)
                    {
                        throw new InvalidOperationException($"Error del servidor al obtener clientes: {response.StatusCode}");
                    }
                    else
                    {
                        throw new InvalidOperationException($"Error al obtener clientes: {response.StatusCode}");
                    }
                }

                // Deserializar la respuesta
                var clientes = await response.Content.ReadFromJsonAsync<List<CustomerDto>>(cancellationToken: cancellationToken).ConfigureAwait(false);

                await _logger.LogInformationAsync($"Se obtuvieron {clientes?.Count ?? 0} clientes", "ClienteService", "GetClientesAsync");

                return clientes ?? new List<CustomerDto>();
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al obtener clientes", ex, "ClienteService", "GetClientesAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al obtener clientes", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al obtener clientes", ex, "ClienteService", "GetClientesAsync");
                throw;
            }
        }
    }
}
