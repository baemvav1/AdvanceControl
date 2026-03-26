using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Services.EndPointProvider;
using Advance_Control.Services.Logging;
using Advance_Control.Services.Session;

namespace Advance_Control.Services.Levantamiento
{
    public class LevantamientoApiService : ILevantamientoApiService
    {
        private readonly HttpClient _http;
        private readonly IApiEndpointProvider _endpoints;
        private readonly ILoggingService _logger;
        private readonly Lazy<IUserSessionService> _session;

        public LevantamientoApiService(
            HttpClient http,
            IApiEndpointProvider endpoints,
            ILoggingService logger,
            Lazy<IUserSessionService> session)
        {
            _http      = http      ?? throw new ArgumentNullException(nameof(http));
            _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
            _logger    = logger    ?? throw new ArgumentNullException(nameof(logger));
            _session   = session   ?? throw new ArgumentNullException(nameof(session));
        }

        public async Task<LevantamientoResultResponse?> CrearLevantamientoAsync(
            LevantamientoCreateRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var sess = _session.Value;
                var credencialId = sess.IsLoaded ? sess.CredencialId : 0;

                var url = _endpoints.GetEndpoint("api", "Levantamiento")
                          + $"?credencialId={credencialId}";

                var response = await _http
                    .PostAsJsonAsync(url, request, cancellationToken)
                    .ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content
                        .ReadFromJsonAsync<LevantamientoResultResponse>(cancellationToken: cancellationToken)
                        .ConfigureAwait(false);

                    return result;
                }

                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                await _logger.LogErrorAsync(
                    $"Error al crear levantamiento: {response.StatusCode} - {errorBody}",
                    null, "LevantamientoApiService", "CrearLevantamientoAsync");

                return new LevantamientoResultResponse
                {
                    Success = false,
                    Message = $"Error del servidor: {response.StatusCode}"
                };
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync(
                    $"Error de conexion al crear levantamiento: {ex.Message}",
                    ex, "LevantamientoApiService", "CrearLevantamientoAsync");

                return new LevantamientoResultResponse
                {
                    Success = false,
                    Message = "No se pudo conectar con el servidor."
                };
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync(
                    $"Error inesperado al crear levantamiento: {ex.Message}",
                    ex, "LevantamientoApiService", "CrearLevantamientoAsync");
                throw;
            }
        }
        public async Task<LevantamientoResultResponse?> ActualizarLevantamientoAsync(
            LevantamientoUpdateRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var url = _endpoints.GetEndpoint("api", "Levantamiento");

                var response = await _http
                    .PutAsJsonAsync(url, request, cancellationToken)
                    .ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content
                        .ReadFromJsonAsync<LevantamientoResultResponse>(cancellationToken: cancellationToken)
                        .ConfigureAwait(false);

                    return result;
                }

                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                await _logger.LogErrorAsync(
                    $"Error al actualizar levantamiento: {response.StatusCode} - {errorBody}",
                    null, "LevantamientoApiService", "ActualizarLevantamientoAsync");

                return new LevantamientoResultResponse
                {
                    Success = false,
                    Message = $"Error del servidor: {response.StatusCode}"
                };
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync(
                    $"Error de conexion al actualizar levantamiento: {ex.Message}",
                    ex, "LevantamientoApiService", "ActualizarLevantamientoAsync");

                return new LevantamientoResultResponse
                {
                    Success = false,
                    Message = "No se pudo conectar con el servidor."
                };
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync(
                    $"Error inesperado al actualizar levantamiento: {ex.Message}",
                    ex, "LevantamientoApiService", "ActualizarLevantamientoAsync");
                throw;
            }
        }

        public async Task<LevantamientoDetailResponse?> ObtenerLevantamientoAsync(
            int idLevantamiento, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = _endpoints.GetEndpoint("api", "Levantamiento") + $"/{idLevantamiento}";
                var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content
                        .ReadFromJsonAsync<LevantamientoDetailResponse>(cancellationToken: cancellationToken)
                        .ConfigureAwait(false);
                }

                await _logger.LogErrorAsync(
                    $"Error al obtener levantamiento {idLevantamiento}: {response.StatusCode}",
                    null, "LevantamientoApiService", "ObtenerLevantamientoAsync");
                return null;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync(
                    $"Error al obtener levantamiento {idLevantamiento}: {ex.Message}",
                    ex, "LevantamientoApiService", "ObtenerLevantamientoAsync");
                return null;
            }
        }

        public async Task<List<LevantamientoListItemResponse>> ListarLevantamientosAsync(
            CancellationToken cancellationToken = default)
        {
            try
            {
                var url = _endpoints.GetEndpoint("api", "Levantamiento");
                var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    var list = await response.Content
                        .ReadFromJsonAsync<List<LevantamientoListItemResponse>>(cancellationToken: cancellationToken)
                        .ConfigureAwait(false);
                    return list ?? new List<LevantamientoListItemResponse>();
                }

                await _logger.LogErrorAsync(
                    $"Error al listar levantamientos: {response.StatusCode}",
                    null, "LevantamientoApiService", "ListarLevantamientosAsync");
                return new List<LevantamientoListItemResponse>();
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync(
                    $"Error al listar levantamientos: {ex.Message}",
                    ex, "LevantamientoApiService", "ListarLevantamientosAsync");
                return new List<LevantamientoListItemResponse>();
            }
        }

        public async Task<bool> EliminarLevantamientoAsync(
            int idLevantamiento, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = _endpoints.GetEndpoint("api", "Levantamiento") + $"/{idLevantamiento}";
                using var response = await _http.DeleteAsync(url, cancellationToken).ConfigureAwait(false);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync(
                    $"Error al eliminar levantamiento {idLevantamiento}: {ex.Message}",
                    ex, "LevantamientoApiService", "EliminarLevantamientoAsync");
                return false;
            }
        }
    }
}