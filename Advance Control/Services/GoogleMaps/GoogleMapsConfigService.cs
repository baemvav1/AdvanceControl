using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.EndPointProvider;
using Advance_Control.Services.Logging;

namespace Advance_Control.Services.GoogleMaps
{
    /// <summary>
    /// Implementación del servicio de configuración de Google Maps
    /// </summary>
    public class GoogleMapsConfigService : IGoogleMapsConfigService
    {
        private readonly HttpClient _http;
        private readonly IApiEndpointProvider _endpoints;
        private readonly ILoggingService _logger;

        public GoogleMapsConfigService(HttpClient http, IApiEndpointProvider endpoints, ILoggingService logger)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Obtiene la clave de API de Google Maps
        /// </summary>
        public async Task<string?> GetApiKeyAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var url = _endpoints.GetEndpoint("api", "GoogleMapsConfig", "api-key");
                await _logger.LogInformationAsync($"Obteniendo API key de Google Maps desde: {url}", "GoogleMapsConfigService", "GetApiKeyAsync");

                var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al obtener API key. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "GoogleMapsConfigService",
                        "GetApiKeyAsync");
                    return null;
                }

                var result = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken).ConfigureAwait(false);
                
                if (result.TryGetProperty("apiKey", out var apiKeyElement))
                {
                    var apiKey = apiKeyElement.GetString();
                    await _logger.LogInformationAsync("API key de Google Maps obtenida exitosamente", "GoogleMapsConfigService", "GetApiKeyAsync");
                    return apiKey;
                }

                return null;
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al obtener API key de Google Maps", ex, "GoogleMapsConfigService", "GetApiKeyAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al obtener API key de Google Maps", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al obtener API key de Google Maps", ex, "GoogleMapsConfigService", "GetApiKeyAsync");
                throw;
            }
        }

        /// <summary>
        /// Obtiene toda la configuración de Google Maps
        /// </summary>
        public async Task<GoogleMapsConfigDto?> GetConfigAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var url = _endpoints.GetEndpoint("api", "GoogleMapsConfig");
                await _logger.LogInformationAsync($"Obteniendo configuración de Google Maps desde: {url}", "GoogleMapsConfigService", "GetConfigAsync");

                var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al obtener configuración. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "GoogleMapsConfigService",
                        "GetConfigAsync");
                    return null;
                }

                var config = await response.Content.ReadFromJsonAsync<GoogleMapsConfigDto>(cancellationToken: cancellationToken).ConfigureAwait(false);

                await _logger.LogInformationAsync("Configuración de Google Maps obtenida exitosamente", "GoogleMapsConfigService", "GetConfigAsync");

                return config;
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al obtener configuración de Google Maps", ex, "GoogleMapsConfigService", "GetConfigAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al obtener configuración de Google Maps", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al obtener configuración de Google Maps", ex, "GoogleMapsConfigService", "GetConfigAsync");
                throw;
            }
        }
    }
}
