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

namespace Advance_Control.Services.PersonalCliente
{
    /// <summary>Personal asignado a clientes: Técnicos, Supervisores y Agentes (api/personal_cliente).</summary>
    public interface IPersonalClienteService
    {
        Task<List<PersonalClienteDto>> GetPersonalAsync(int idCliente, CancellationToken cancellationToken = default);

        /// <summary>Usuarios que pueden ocupar el rol (tecnico, supervisor, agente).</summary>
        Task<List<PersonalClienteDto>> GetCandidatosAsync(string rol, CancellationToken cancellationToken = default);

        /// <summary>Devuelve null si salió bien, o el mensaje de error.</summary>
        Task<string?> AsignarAsync(int idCliente, long credencialId, string rol, CancellationToken cancellationToken = default);

        /// <summary>Devuelve null si salió bien, o el mensaje de error.</summary>
        Task<string?> QuitarAsync(int idCliente, long credencialId, string rol, CancellationToken cancellationToken = default);
    }

    public class PersonalClienteService : IPersonalClienteService
    {
        private readonly HttpClient _http;
        private readonly IApiEndpointProvider _endpoints;
        private readonly ILoggingService _logger;

        public PersonalClienteService(HttpClient http, IApiEndpointProvider endpoints, ILoggingService logger)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task<List<PersonalClienteDto>> GetPersonalAsync(int idCliente, CancellationToken cancellationToken = default)
            => GetListaAsync(_endpoints.GetEndpoint("api", "personal_cliente", idCliente.ToString()), "No se pudo cargar el personal asignado.", cancellationToken);

        public Task<List<PersonalClienteDto>> GetCandidatosAsync(string rol, CancellationToken cancellationToken = default)
            => GetListaAsync(_endpoints.GetEndpoint("api", "personal_cliente", "candidatos") + "?rol=" + Uri.EscapeDataString(rol),
                "No se pudo cargar la lista de usuarios.", cancellationToken);

        public async Task<string?> AsignarAsync(int idCliente, long credencialId, string rol, CancellationToken cancellationToken = default)
        {
            var url = _endpoints.GetEndpoint("api", "personal_cliente", idCliente.ToString());
            using var response = await _http.PostAsJsonAsync(url, new Dictionary<string, object> { ["credencial_id"] = credencialId, ["rol"] = rol }, cancellationToken).ConfigureAwait(false);
            return await ResultadoAsync(response, "No se pudo asignar el usuario.", cancellationToken).ConfigureAwait(false);
        }

        public async Task<string?> QuitarAsync(int idCliente, long credencialId, string rol, CancellationToken cancellationToken = default)
        {
            var url = _endpoints.GetEndpoint("api", "personal_cliente", idCliente.ToString(), credencialId.ToString()) + "?rol=" + Uri.EscapeDataString(rol);
            using var response = await _http.DeleteAsync(url, cancellationToken).ConfigureAwait(false);
            return await ResultadoAsync(response, "No se pudo quitar el usuario.", cancellationToken).ConfigureAwait(false);
        }

        private async Task<List<PersonalClienteDto>> GetListaAsync(string url, string mensajeError, CancellationToken cancellationToken)
        {
            using var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                await _logger.LogErrorAsync($"{mensajeError} Status: {response.StatusCode}, Content: {error}", null, nameof(PersonalClienteService), nameof(GetListaAsync));
                throw new InvalidOperationException(mensajeError);
            }
            return await response.Content.ReadFromJsonAsync<List<PersonalClienteDto>>(cancellationToken: cancellationToken).ConfigureAwait(false) ?? new();
        }

        private static async Task<string?> ResultadoAsync(HttpResponseMessage response, string mensajeError, CancellationToken cancellationToken)
        {
            if (response.IsSuccessStatusCode) return null;
            try
            {
                var contenido = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                using var doc = JsonDocument.Parse(contenido);
                if (doc.RootElement.TryGetProperty("message", out var m) && m.GetString() is { Length: > 0 } texto)
                    return texto;
            }
            catch (JsonException) { }
            return mensajeError;
        }
    }
}
