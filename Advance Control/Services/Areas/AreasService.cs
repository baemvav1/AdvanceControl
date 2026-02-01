using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.EndPointProvider;
using Advance_Control.Services.Logging;

namespace Advance_Control.Services.Areas
{
    /// <summary>
    /// Implementación del servicio de áreas geográficas
    /// </summary>
    public class AreasService : IAreasService
    {
        private readonly HttpClient _http;
        private readonly IApiEndpointProvider _endpoints;
        private readonly ILoggingService _logger;

        public AreasService(HttpClient http, IApiEndpointProvider endpoints, ILoggingService logger)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Obtiene áreas con filtros opcionales
        /// </summary>
        public async Task<List<AreaDto>> GetAreasAsync(
            int? idArea = null,
            string? nombre = null,
            bool? activo = null,
            string? tipoGeometria = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var url = _endpoints.GetEndpoint("api", "Areas");
                var queryParams = new List<string>();

                if (idArea.HasValue && idArea.Value > 0)
                    queryParams.Add($"idArea={idArea.Value}");

                if (!string.IsNullOrWhiteSpace(nombre))
                    queryParams.Add($"nombre={Uri.EscapeDataString(nombre)}");

                if (activo.HasValue)
                    queryParams.Add($"activo={activo.Value.ToString().ToLower()}");

                if (!string.IsNullOrWhiteSpace(tipoGeometria))
                    queryParams.Add($"tipoGeometria={Uri.EscapeDataString(tipoGeometria)}");

                if (queryParams.Count > 0)
                {
                    url = $"{url}?{string.Join("&", queryParams)}";
                }

                await _logger.LogInformationAsync($"Obteniendo áreas desde: {url}", "AreasService", "GetAreasAsync");

                var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al obtener áreas. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "AreasService",
                        "GetAreasAsync");
                    return new List<AreaDto>();
                }

                var areas = await response.Content.ReadFromJsonAsync<List<AreaDto>>(cancellationToken: cancellationToken).ConfigureAwait(false);

                await _logger.LogInformationAsync($"Se obtuvieron {areas?.Count ?? 0} áreas", "AreasService", "GetAreasAsync");

                return areas ?? new List<AreaDto>();
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al obtener áreas", ex, "AreasService", "GetAreasAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al obtener áreas", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al obtener áreas", ex, "AreasService", "GetAreasAsync");
                throw;
            }
        }

        /// <summary>
        /// Obtiene áreas en formato optimizado para Google Maps JavaScript API
        /// </summary>
        public async Task<List<GoogleMapsAreaDto>> GetAreasForGoogleMapsAsync(
            int? idArea = null,
            bool? activo = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var url = _endpoints.GetEndpoint("api", "Areas", "googlemaps");
                var queryParams = new List<string>();

                if (idArea.HasValue && idArea.Value > 0)
                    queryParams.Add($"idArea={idArea.Value}");

                if (activo.HasValue)
                    queryParams.Add($"activo={activo.Value.ToString().ToLower()}");

                if (queryParams.Count > 0)
                {
                    url = $"{url}?{string.Join("&", queryParams)}";
                }

                await _logger.LogInformationAsync($"Obteniendo áreas para Google Maps desde: {url}", "AreasService", "GetAreasForGoogleMapsAsync");

                var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al obtener áreas para Google Maps. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "AreasService",
                        "GetAreasForGoogleMapsAsync");
                    return new List<GoogleMapsAreaDto>();
                }

                var areas = await response.Content.ReadFromJsonAsync<List<GoogleMapsAreaDto>>(cancellationToken: cancellationToken).ConfigureAwait(false);

                await _logger.LogInformationAsync($"Se obtuvieron {areas?.Count ?? 0} áreas para Google Maps", "AreasService", "GetAreasForGoogleMapsAsync");

                return areas ?? new List<GoogleMapsAreaDto>();
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al obtener áreas para Google Maps", ex, "AreasService", "GetAreasForGoogleMapsAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al obtener áreas para Google Maps", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al obtener áreas para Google Maps", ex, "AreasService", "GetAreasForGoogleMapsAsync");
                throw;
            }
        }

        /// <summary>
        /// Valida si un punto está dentro de un área
        /// </summary>
        public async Task<List<AreaValidationResultDto>> ValidatePointAsync(
            decimal latitud,
            decimal longitud,
            int? idArea = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var url = _endpoints.GetEndpoint("api", "Areas", "validate-point");
                var queryParams = new List<string>
                {
                    $"latitud={latitud}",
                    $"longitud={longitud}"
                };

                if (idArea.HasValue && idArea.Value > 0)
                    queryParams.Add($"idArea={idArea.Value}");

                url = $"{url}?{string.Join("&", queryParams)}";

                await _logger.LogInformationAsync($"Validando punto ({latitud}, {longitud}) en áreas", "AreasService", "ValidatePointAsync");

                var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al validar punto. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "AreasService",
                        "ValidatePointAsync");
                    return new List<AreaValidationResultDto>();
                }

                var results = await response.Content.ReadFromJsonAsync<List<AreaValidationResultDto>>(cancellationToken: cancellationToken).ConfigureAwait(false);

                await _logger.LogInformationAsync($"Validación completada. {results?.Count ?? 0} áreas evaluadas", "AreasService", "ValidatePointAsync");

                return results ?? new List<AreaValidationResultDto>();
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al validar punto", ex, "AreasService", "ValidatePointAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al validar punto", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al validar punto", ex, "AreasService", "ValidatePointAsync");
                throw;
            }
        }
    }
}
