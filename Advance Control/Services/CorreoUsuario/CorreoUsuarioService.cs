using Advance_Control.Models;
using Advance_Control.Services.EndPointProvider;
using Advance_Control.Services.Logging;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Advance_Control.Services.CorreoUsuario
{
    public class CorreoUsuarioService : ICorreoUsuarioService
    {
        private readonly HttpClient _http;
        private readonly IApiEndpointProvider _endpoints;
        private readonly ILoggingService _logger;

        public CorreoUsuarioService(HttpClient http, IApiEndpointProvider endpoints, ILoggingService logger)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task<CorreoUsuarioDto?> GetCorreoActualAsync(CancellationToken cancellationToken = default)
            => GetAsync($"{_endpoints.GetEndpoint("api", "CorreoUsuario")}/actual", "GetCorreoActualAsync", cancellationToken);

        public Task<CorreoUsuarioDto?> GetCorreoUsuarioAsync(long credencialId, CancellationToken cancellationToken = default)
            => GetAsync($"{_endpoints.GetEndpoint("api", "CorreoUsuario")}/{credencialId}", "GetCorreoUsuarioAsync", cancellationToken);

        public async Task<CorreoUsuarioOperationResponse> SaveCorreoUsuarioAsync(long credencialId, CorreoUsuarioEditDto request, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"{_endpoints.GetEndpoint("api", "CorreoUsuario")}/{credencialId}";
                using var response = await _http.PutAsJsonAsync(url, request, cancellationToken).ConfigureAwait(false);
                return await ReadOperationResponseAsync(response, cancellationToken, "SaveCorreoUsuarioAsync").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error al guardar la configuración de correo del usuario", ex, "CorreoUsuarioService", "SaveCorreoUsuarioAsync");
                throw;
            }
        }

        public async Task<CorreoUsuarioOperationResponse> DeleteCorreoUsuarioAsync(long credencialId, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"{_endpoints.GetEndpoint("api", "CorreoUsuario")}/{credencialId}";
                using var response = await _http.DeleteAsync(url, cancellationToken).ConfigureAwait(false);
                return await ReadOperationResponseAsync(response, cancellationToken, "DeleteCorreoUsuarioAsync").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error al eliminar la configuración de correo del usuario", ex, "CorreoUsuarioService", "DeleteCorreoUsuarioAsync");
                throw;
            }
        }

        private async Task<CorreoUsuarioDto?> GetAsync(string url, string method, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);
                if (response.StatusCode == HttpStatusCode.NotFound)
                    return null;

                if (!response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync($"Error en {method}. Status: {response.StatusCode}. Content: {content}", null, "CorreoUsuarioService", method);
                    throw new InvalidOperationException(ExtractMessage(content));
                }

                return await response.Content.ReadFromJsonAsync<CorreoUsuarioDto>(cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not InvalidOperationException)
            {
                await _logger.LogErrorAsync("Error al obtener la configuración de correo", ex, "CorreoUsuarioService", method);
                throw;
            }
        }

        private async Task<CorreoUsuarioOperationResponse> ReadOperationResponseAsync(HttpResponseMessage response, CancellationToken cancellationToken, string method)
        {
            if (!response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                await _logger.LogErrorAsync($"Error en {method}. Status: {response.StatusCode}. Content: {content}", null, "CorreoUsuarioService", method);
                return new CorreoUsuarioOperationResponse { Success = false, Message = ExtractMessage(content) };
            }

            return await response.Content.ReadFromJsonAsync<CorreoUsuarioOperationResponse>(cancellationToken: cancellationToken).ConfigureAwait(false)
                ?? new CorreoUsuarioOperationResponse { Success = false, Message = "No se recibió una respuesta válida." };
        }

        private static string ExtractMessage(string? content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return "No se recibió detalle del error.";

            try
            {
                using var document = JsonDocument.Parse(content);
                if (document.RootElement.ValueKind == JsonValueKind.Object
                    && document.RootElement.TryGetProperty("message", out var messageElement)
                    && messageElement.ValueKind == JsonValueKind.String)
                {
                    return messageElement.GetString() ?? content;
                }
            }
            catch (JsonException)
            {
            }

            return content;
        }
    }
}
