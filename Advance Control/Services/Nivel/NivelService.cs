using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.EndPointProvider;
using Advance_Control.Services.Logging;

namespace Advance_Control.Services.Nivel
{
    public class NivelService : INivelService
    {
        private readonly HttpClient _http;
        private readonly IApiEndpointProvider _endpoints;
        private readonly ILoggingService _logger;

        public NivelService(HttpClient http, IApiEndpointProvider endpoints, ILoggingService logger)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<NivelDto>> GetNivelesAsync(int idNivel = 0, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"{_endpoints.GetEndpoint("api", "Nivel")}?idNivel={idNivel}";
                var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    await _logger.LogErrorAsync(
                        $"Error al obtener niveles. Status: {response.StatusCode}",
                        null, "NivelService", "GetNivelesAsync");
                    return new List<NivelDto>();
                }

                var niveles = await response.Content
                    .ReadFromJsonAsync<List<NivelDto>>(cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                return niveles ?? new List<NivelDto>();
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al obtener niveles", ex, "NivelService", "GetNivelesAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al obtener niveles", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al obtener niveles", ex, "NivelService", "GetNivelesAsync");
                throw;
            }
        }
    }
}
