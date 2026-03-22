using Advance_Control.Models;
using Advance_Control.Services.EndPointProvider;
using Advance_Control.Services.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Advance_Control.Services.TipoUsuario
{
    public class TipoUsuarioService : ITipoUsuarioService
    {
        private readonly HttpClient _http;
        private readonly IApiEndpointProvider _endpoints;
        private readonly ILoggingService _logger;

        public TipoUsuarioService(HttpClient http, IApiEndpointProvider endpoints, ILoggingService logger)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<TipoUsuarioDto>> GetTiposUsuarioAsync(int idTipoUsuario = 0, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"{_endpoints.GetEndpoint("api", "TipoUsuario")}?idTipoUsuario={idTipoUsuario}";
                var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    await _logger.LogErrorAsync(
                        $"Error al obtener tipos de usuario. Status: {response.StatusCode}",
                        null,
                        "TipoUsuarioService",
                        "GetTiposUsuarioAsync");
                    return new List<TipoUsuarioDto>();
                }

                return await response.Content.ReadFromJsonAsync<List<TipoUsuarioDto>>(cancellationToken: cancellationToken).ConfigureAwait(false)
                    ?? new List<TipoUsuarioDto>();
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al obtener tipos de usuario", ex, "TipoUsuarioService", "GetTiposUsuarioAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al obtener tipos de usuario.", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al obtener tipos de usuario", ex, "TipoUsuarioService", "GetTiposUsuarioAsync");
                throw;
            }
        }
    }
}
