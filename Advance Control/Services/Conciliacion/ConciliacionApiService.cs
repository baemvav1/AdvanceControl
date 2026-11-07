using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.EndPointProvider;
using Advance_Control.Services.Logging;

namespace Advance_Control.Services.Conciliacion
{
    public interface IConciliacionApiService
    {
        Task<IReadOnlyList<ConciliacionUnoAUnoPropuestaDto>> ProponerUnoAUnoAsync(ConciliacionUnoAUnoPropuestasRequestDto request, CancellationToken ct = default);
        Task<ConciliacionUnoAUnoValidarResponseDto> ValidarUnoAUnoAsync(ConciliacionUnoAUnoValidarRequestDto request, CancellationToken ct = default);
        Task<ConciliacionUnoAUnoAplicarResponseDto> AplicarUnoAUnoAsync(ConciliacionUnoAUnoAplicarRequestDto request, CancellationToken ct = default);
    }

    /// <summary>Cliente del motor de conciliación del servidor (api/conciliacion).</summary>
    public sealed class ConciliacionApiService : IConciliacionApiService
    {
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

        private readonly HttpClient _http;
        private readonly IApiEndpointProvider _endpoints;
        private readonly ILoggingService _logger;

        public ConciliacionApiService(HttpClient http, IApiEndpointProvider endpoints, ILoggingService logger)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IReadOnlyList<ConciliacionUnoAUnoPropuestaDto>> ProponerUnoAUnoAsync(ConciliacionUnoAUnoPropuestasRequestDto request, CancellationToken ct = default)
            => await PostAsync<List<ConciliacionUnoAUnoPropuestaDto>>("propuestas", request, ct) ?? new List<ConciliacionUnoAUnoPropuestaDto>();

        public async Task<ConciliacionUnoAUnoValidarResponseDto> ValidarUnoAUnoAsync(ConciliacionUnoAUnoValidarRequestDto request, CancellationToken ct = default)
            => await PostAsync<ConciliacionUnoAUnoValidarResponseDto>("validar", request, ct)
                ?? new ConciliacionUnoAUnoValidarResponseDto { Motivo = "El servidor no devolvió respuesta." };

        public async Task<ConciliacionUnoAUnoAplicarResponseDto> AplicarUnoAUnoAsync(ConciliacionUnoAUnoAplicarRequestDto request, CancellationToken ct = default)
            => await PostAsync<ConciliacionUnoAUnoAplicarResponseDto>("aplicar", request, ct)
                ?? throw new InvalidOperationException("El servidor no devolvió el resultado de la conciliación.");

        private async Task<T?> PostAsync<T>(string accion, object request, CancellationToken ct)
        {
            var url = _endpoints.GetEndpoint("api", "conciliacion", "uno-a-uno", accion);
            try
            {
                using var response = await _http.PostAsJsonAsync(url, request, ct).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    var contenido = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                    await _logger.LogErrorAsync($"Error en {url}. Status: {response.StatusCode}, Content: {contenido}", null, nameof(ConciliacionApiService), accion);
                    throw new InvalidOperationException($"El servidor rechazó la solicitud de conciliación ({(int)response.StatusCode}): {contenido}");
                }

                return await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct).ConfigureAwait(false);
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync($"Error de red en {url}", ex, nameof(ConciliacionApiService), accion);
                throw new InvalidOperationException("Error de comunicación con el servidor de conciliación.", ex);
            }
        }
    }
}
