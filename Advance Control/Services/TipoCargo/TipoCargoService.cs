using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.EndPointProvider;
using Advance_Control.Services.Logging;

namespace Advance_Control.Services.TipoCargo
{
    public class TipoCargoService : ITipoCargoService
    {
        private readonly HttpClient _http;
        private readonly IApiEndpointProvider _endpoints;
        private readonly ILoggingService _logger;

        public TipoCargoService(HttpClient http, IApiEndpointProvider endpoints, ILoggingService logger)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<TipoCargoDefaultDto>> ObtenerDefaultsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"{_endpoints.GetEndpoint("api", "tipo-cargo")}/defaults";
                var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode) return new List<TipoCargoDefaultDto>();

                var resultado = await response.Content.ReadFromJsonAsync<List<TipoCargoDefaultDto>>(cancellationToken: cancellationToken).ConfigureAwait(false);
                return resultado ?? new List<TipoCargoDefaultDto>();
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al obtener defaults de tipo de cargo", ex, "TipoCargoService", "ObtenerDefaultsAsync");
                return new List<TipoCargoDefaultDto>();
            }
        }

        public async Task<bool> GuardarDefaultAsync(int id, string? claveProdServ, string? claveUnidad, decimal? tasaIva, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"{_endpoints.GetEndpoint("api", "tipo-cargo")}/{id}/defaults";
                var body = new
                {
                    claveProdServDefault = claveProdServ,
                    claveUnidadDefault = claveUnidad,
                    tasaIvaDefault = tasaIva
                };
                var json = JsonSerializer.Serialize(body);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");
                using var response = await _http.PutAsync(url, content, cancellationToken).ConfigureAwait(false);
                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al guardar defaults de tipo de cargo", ex, "TipoCargoService", "GuardarDefaultAsync");
                return false;
            }
        }
    }
}
