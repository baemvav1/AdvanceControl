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

        Task<ConciliacionVariasAUnoPropuestasResponseDto> ProponerVariasAUnoAsync(ConciliacionVariasAUnoPropuestasRequestDto request, CancellationToken ct = default);
        Task<ConciliacionVariasAUnoValidarResponseDto> ValidarVariasAUnoAsync(ConciliacionVariasAUnoValidarRequestDto request, CancellationToken ct = default);
        Task<ConciliacionVariasAUnoAplicarResponseDto> AplicarVariasAUnoAsync(ConciliacionVariasAUnoAplicarRequestDto request, CancellationToken ct = default);

        Task<ConciliacionAbonosPropuestasResponseDto> ProponerAbonosAsync(ConciliacionAbonosPropuestasRequestDto request, CancellationToken ct = default);
        Task<ConciliacionAbonosAplicarResponseDto> AplicarAbonosAsync(ConciliacionAbonosAplicarRequestDto request, CancellationToken ct = default);

        /// <summary>Complementos o Ingresos manuales, según <paramref name="modo"/>.</summary>
        Task<IReadOnlyList<ConciliacionVinculoPropuestaDto>> ProponerVinculosAsync(ConciliacionAutomaticaModo modo, ConciliacionVinculosPropuestasRequestDto request, CancellationToken ct = default);
        Task<ConciliacionVinculosAplicarResponseDto> AplicarVinculosAsync(ConciliacionAutomaticaModo modo, ConciliacionVinculosAplicarRequestDto request, CancellationToken ct = default);
    }

    /// <summary>Cliente del motor de conciliación del servidor (api/conciliacion).</summary>
    public sealed class ConciliacionApiService : IConciliacionApiService
    {
        private const string UnoAUno = "uno-a-uno";
        private const string VariasAUno = "varias-a-uno";
        private const string Abonos = "abonos";
        private const string Complementos = "complementos";
        private const string IngresosManuales = "ingresos-manuales";

        /// <summary>
        /// Tope de espera por petición. El servidor corta sus cálculos a los 20 s; si no contesta en
        /// este tiempo es que está caído o reiniciándose, y la ventana avisa en lugar de quedarse esperando.
        /// </summary>
        private static readonly TimeSpan EsperaMaxima = TimeSpan.FromSeconds(75);

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
            => await PostAsync<List<ConciliacionUnoAUnoPropuestaDto>>(UnoAUno, "propuestas", request, ct) ?? new List<ConciliacionUnoAUnoPropuestaDto>();

        public async Task<ConciliacionUnoAUnoValidarResponseDto> ValidarUnoAUnoAsync(ConciliacionUnoAUnoValidarRequestDto request, CancellationToken ct = default)
            => await PostAsync<ConciliacionUnoAUnoValidarResponseDto>(UnoAUno, "validar", request, ct)
                ?? new ConciliacionUnoAUnoValidarResponseDto { Motivo = "El servidor no devolvió respuesta." };

        public async Task<ConciliacionUnoAUnoAplicarResponseDto> AplicarUnoAUnoAsync(ConciliacionUnoAUnoAplicarRequestDto request, CancellationToken ct = default)
            => await PostAsync<ConciliacionUnoAUnoAplicarResponseDto>(UnoAUno, "aplicar", request, ct)
                ?? throw new InvalidOperationException("El servidor no devolvió el resultado de la conciliación.");

        public async Task<ConciliacionVariasAUnoPropuestasResponseDto> ProponerVariasAUnoAsync(ConciliacionVariasAUnoPropuestasRequestDto request, CancellationToken ct = default)
            => await PostAsync<ConciliacionVariasAUnoPropuestasResponseDto>(VariasAUno, "propuestas", request, ct) ?? new ConciliacionVariasAUnoPropuestasResponseDto();

        public async Task<ConciliacionVariasAUnoValidarResponseDto> ValidarVariasAUnoAsync(ConciliacionVariasAUnoValidarRequestDto request, CancellationToken ct = default)
            => await PostAsync<ConciliacionVariasAUnoValidarResponseDto>(VariasAUno, "validar", request, ct)
                ?? new ConciliacionVariasAUnoValidarResponseDto { Motivo = "El servidor no devolvió respuesta." };

        public async Task<ConciliacionVariasAUnoAplicarResponseDto> AplicarVariasAUnoAsync(ConciliacionVariasAUnoAplicarRequestDto request, CancellationToken ct = default)
            => await PostAsync<ConciliacionVariasAUnoAplicarResponseDto>(VariasAUno, "aplicar", request, ct)
                ?? throw new InvalidOperationException("El servidor no devolvió el resultado de la conciliación.");

        public async Task<ConciliacionAbonosPropuestasResponseDto> ProponerAbonosAsync(ConciliacionAbonosPropuestasRequestDto request, CancellationToken ct = default)
            => await PostAsync<ConciliacionAbonosPropuestasResponseDto>(Abonos, "propuestas", request, ct) ?? new ConciliacionAbonosPropuestasResponseDto();

        public async Task<ConciliacionAbonosAplicarResponseDto> AplicarAbonosAsync(ConciliacionAbonosAplicarRequestDto request, CancellationToken ct = default)
            => await PostAsync<ConciliacionAbonosAplicarResponseDto>(Abonos, "aplicar", request, ct)
                ?? throw new InvalidOperationException("El servidor no devolvió el resultado de la conciliación.");

        public async Task<IReadOnlyList<ConciliacionVinculoPropuestaDto>> ProponerVinculosAsync(ConciliacionAutomaticaModo modo, ConciliacionVinculosPropuestasRequestDto request, CancellationToken ct = default)
            => await PostAsync<List<ConciliacionVinculoPropuestaDto>>(RutaVinculo(modo), "propuestas", request, ct) ?? new List<ConciliacionVinculoPropuestaDto>();

        public async Task<ConciliacionVinculosAplicarResponseDto> AplicarVinculosAsync(ConciliacionAutomaticaModo modo, ConciliacionVinculosAplicarRequestDto request, CancellationToken ct = default)
            => await PostAsync<ConciliacionVinculosAplicarResponseDto>(RutaVinculo(modo), "aplicar", request, ct)
                ?? throw new InvalidOperationException("El servidor no devolvió el resultado de la conciliación.");

        private static string RutaVinculo(ConciliacionAutomaticaModo modo) => modo switch
        {
            ConciliacionAutomaticaModo.Complementos => Complementos,
            ConciliacionAutomaticaModo.IngresosManuales => IngresosManuales,
            _ => throw new ArgumentOutOfRangeException(nameof(modo), modo, "Solo Complementos o Ingresos manuales.")
        };

        private async Task<T?> PostAsync<T>(string modo, string accion, object request, CancellationToken ct)
        {
            var url = _endpoints.GetEndpoint("api", "conciliacion", modo, accion);
            using var limite = CancellationTokenSource.CreateLinkedTokenSource(ct);
            limite.CancelAfter(EsperaMaxima);
            try
            {
                using var response = await _http.PostAsJsonAsync(url, request, limite.Token).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    var contenido = await response.Content.ReadAsStringAsync(limite.Token).ConfigureAwait(false);
                    await _logger.LogErrorAsync($"Error en {url}. Status: {response.StatusCode}, Content: {contenido}", null, nameof(ConciliacionApiService), accion);
                    throw new InvalidOperationException((int)response.StatusCode switch
                    {
                        429 => LeerMensaje(contenido) ?? "El servidor está ocupado con otro cálculo de conciliación. Intenta de nuevo en unos segundos.",
                        502 or 503 or 504 => "El servidor no está disponible en este momento (puede estar reiniciándose). Intenta de nuevo en un minuto.",
                        _ => $"El servidor rechazó la solicitud de conciliación ({(int)response.StatusCode}): {LeerMensaje(contenido) ?? contenido}"
                    });
                }

                return await response.Content.ReadFromJsonAsync<T>(JsonOptions, limite.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException ex) when (!ct.IsCancellationRequested)
            {
                // Se venció el tope de espera (no lo canceló el usuario).
                await _logger.LogErrorAsync($"Sin respuesta de {url} en {EsperaMaxima.TotalSeconds:0} s", ex, nameof(ConciliacionApiService), accion);
                throw new InvalidOperationException(
                    $"El servidor no respondió en {EsperaMaxima.TotalSeconds:0} segundos. Puede estar reiniciándose; intenta de nuevo en un minuto.", ex);
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync($"Error de red en {url}", ex, nameof(ConciliacionApiService), accion);
                throw new InvalidOperationException("Error de comunicación con el servidor de conciliación.", ex);
            }
        }

        private static string? LeerMensaje(string contenido)
        {
            try
            {
                using var json = JsonDocument.Parse(contenido);
                return json.RootElement.TryGetProperty("message", out var mensaje) ? mensaje.GetString() : null;
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}
