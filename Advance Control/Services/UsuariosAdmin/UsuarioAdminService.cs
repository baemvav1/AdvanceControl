using System;
using System.Collections.Generic;
using System.Net.Http;
using Advance_Control.Models;
using Advance_Control.Services.EndPointProvider;
using Advance_Control.Services.Logging;
using Advance_Control.Utilities;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Advance_Control.Services.UsuariosAdmin
{
    public class UsuarioAdminService : IUsuarioAdminService
    {
        private readonly HttpClient _http;
        private readonly IApiEndpointProvider _endpoints;
        private readonly ILoggingService _logger;

        public UsuarioAdminService(HttpClient http, IApiEndpointProvider endpoints, ILoggingService logger)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<UsuarioAdminDto>> GetUsuariosAsync(UsuarioAdminQueryDto? query = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = _endpoints.GetEndpoint("api", "UsuariosAdmin");
                if (query != null)
                {
                    url = new ApiQueryBuilder()
                        .Add("credencialId", query.CredencialId == 0 ? null : query.CredencialId)
                        .Add("usuario", query.Usuario)
                        .Add("estaActiva", query.EstaActiva)
                        .Add("nivel", query.Nivel == 0 ? null : query.Nivel)
                        .Build(url);
                }

                var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync($"Error al obtener usuarios. Status: {response.StatusCode}. Content: {content}", null, "UsuarioAdminService", "GetUsuariosAsync");
                    return new List<UsuarioAdminDto>();
                }

                return await response.Content.ReadFromJsonAsync<List<UsuarioAdminDto>>(cancellationToken: cancellationToken).ConfigureAwait(false)
                    ?? new List<UsuarioAdminDto>();
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error al obtener usuarios administrativos", ex, "UsuarioAdminService", "GetUsuariosAsync");
                throw;
            }
        }

        public async Task<UsuarioAdminDto?> GetUsuarioAsync(long credencialId, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"{_endpoints.GetEndpoint("api", "UsuariosAdmin")}/{credencialId}";
                var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                    return null;

                return await response.Content.ReadFromJsonAsync<UsuarioAdminDto>(cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error al obtener detalle de usuario administrativo", ex, "UsuarioAdminService", "GetUsuarioAsync");
                throw;
            }
        }

        public async Task<UsuarioAdminOperationResponse> CreateUsuarioAsync(UsuarioAdminEditDto request, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = _endpoints.GetEndpoint("api", "UsuariosAdmin");
                using var response = await _http.PostAsJsonAsync(url, request, cancellationToken).ConfigureAwait(false);
                return await ReadOperationResponseAsync(response, cancellationToken, "CreateUsuarioAsync").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error al crear usuario administrativo", ex, "UsuarioAdminService", "CreateUsuarioAsync");
                throw;
            }
        }

        public async Task<UsuarioAdminOperationResponse> UpdateUsuarioAsync(long credencialId, UsuarioAdminEditDto request, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"{_endpoints.GetEndpoint("api", "UsuariosAdmin")}/{credencialId}";
                using var response = await _http.PutAsJsonAsync(url, request, cancellationToken).ConfigureAwait(false);
                return await ReadOperationResponseAsync(response, cancellationToken, "UpdateUsuarioAsync").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error al actualizar usuario administrativo", ex, "UsuarioAdminService", "UpdateUsuarioAsync");
                throw;
            }
        }

        public async Task<UsuarioAdminOperationResponse> DeleteUsuarioAsync(long credencialId, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"{_endpoints.GetEndpoint("api", "UsuariosAdmin")}/{credencialId}";
                using var response = await _http.DeleteAsync(url, cancellationToken).ConfigureAwait(false);
                return await ReadOperationResponseAsync(response, cancellationToken, "DeleteUsuarioAsync").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error al eliminar usuario administrativo", ex, "UsuarioAdminService", "DeleteUsuarioAsync");
                throw;
            }
        }

        private async Task<UsuarioAdminOperationResponse> ReadOperationResponseAsync(HttpResponseMessage response, CancellationToken cancellationToken, string method)
        {
            if (!response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                await _logger.LogErrorAsync($"Error en {method}. Status: {response.StatusCode}. Content: {content}", null, "UsuarioAdminService", method);
                return new UsuarioAdminOperationResponse { Success = false, Message = ExtractMessage(content) };
            }

            return await response.Content.ReadFromJsonAsync<UsuarioAdminOperationResponse>(cancellationToken: cancellationToken).ConfigureAwait(false)
                ?? new UsuarioAdminOperationResponse { Success = false, Message = "No se recibió una respuesta válida." };
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
                    var message = messageElement.GetString();
                    if (!string.IsNullOrWhiteSpace(message))
                        return message;
                }
            }
            catch (JsonException)
            {
            }

            return content;
        }
    }
}
