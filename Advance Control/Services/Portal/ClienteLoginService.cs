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

namespace Advance_Control.Services.Portal
{
    public class ClienteLoginService : IClienteLoginService
    {
        private readonly HttpClient _http;
        private readonly IApiEndpointProvider _endpoints;
        private readonly ILoggingService _logger;

        public ClienteLoginService(HttpClient http, IApiEndpointProvider endpoints, ILoggingService logger)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private string Url(int idCliente, string? sufijo = null) =>
            $"{_endpoints.GetEndpoint("api", "Clientes")}/{idCliente}/logins{sufijo}";

        public async Task<List<ClienteLoginDto>> ObtenerAsync(int idCliente, CancellationToken cancellationToken = default)
        {
            using var response = await EnviarAsync(() => _http.GetAsync(Url(idCliente), cancellationToken), "ObtenerAsync").ConfigureAwait(false);
            return await response.Content.ReadFromJsonAsync<List<ClienteLoginDto>>(cancellationToken: cancellationToken).ConfigureAwait(false)
                ?? new List<ClienteLoginDto>();
        }

        public async Task<ClienteLoginPasswordDto> CrearAsync(int idCliente, long contactoId, string usuario, CancellationToken cancellationToken = default)
        {
            using var response = await EnviarAsync(
                () => _http.PostAsJsonAsync(Url(idCliente), new { contactoId, usuario }, cancellationToken), "CrearAsync").ConfigureAwait(false);
            return await LeerAsync<ClienteLoginPasswordDto>(response, cancellationToken).ConfigureAwait(false);
        }

        public async Task<ClienteLoginPasswordDto> RestablecerAsync(int idCliente, long loginId, CancellationToken cancellationToken = default)
        {
            using var response = await EnviarAsync(
                () => _http.PostAsync(Url(idCliente, $"/{loginId}/restablecer"), null, cancellationToken), "RestablecerAsync").ConfigureAwait(false);
            return await LeerAsync<ClienteLoginPasswordDto>(response, cancellationToken).ConfigureAwait(false);
        }

        public async Task<ClienteLoginDto> MarcarDatosEnviadosAsync(int idCliente, long loginId, CancellationToken cancellationToken = default)
        {
            using var response = await EnviarAsync(
                () => _http.PostAsync(Url(idCliente, $"/{loginId}/datos-enviados"), null, cancellationToken), "MarcarDatosEnviadosAsync").ConfigureAwait(false);
            return await LeerAsync<ClienteLoginDto>(response, cancellationToken).ConfigureAwait(false);
        }

        public async Task EliminarAsync(int idCliente, long loginId, CancellationToken cancellationToken = default)
        {
            using var _ = await EnviarAsync(
                () => _http.DeleteAsync(Url(idCliente, $"/{loginId}"), cancellationToken), "EliminarAsync").ConfigureAwait(false);
        }

        /// <summary>Devuelve la respuesta si fue exitosa; si no, lanza InvalidOperationException con el mensaje de la API.</summary>
        private async Task<HttpResponseMessage> EnviarAsync(Func<Task<HttpResponseMessage>> enviar, string operacion)
        {
            HttpResponseMessage response;
            try
            {
                response = await enviar().ConfigureAwait(false);
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red en logins del portal", ex, nameof(ClienteLoginService), operacion);
                throw new InvalidOperationException("No hay conexión con el servidor. Intenta de nuevo.", ex);
            }

            if (response.IsSuccessStatusCode)
                return response;

            var contenido = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            response.Dispose();
            await _logger.LogErrorAsync($"Error en logins del portal. Status: {response.StatusCode}. Content: {contenido}", null, nameof(ClienteLoginService), operacion);
            throw new InvalidOperationException(ExtraerMensaje(contenido) ?? $"El servidor respondió {(int)response.StatusCode}.");
        }

        private static async Task<T> LeerAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
            => await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken).ConfigureAwait(false)
               ?? throw new InvalidOperationException("Respuesta vacía del servidor.");

        private static string? ExtraerMensaje(string contenido)
        {
            try
            {
                using var doc = JsonDocument.Parse(contenido);
                return doc.RootElement.ValueKind == JsonValueKind.Object
                       && doc.RootElement.TryGetProperty("message", out var message)
                       && message.ValueKind == JsonValueKind.String
                    ? message.GetString()
                    : null;
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}
