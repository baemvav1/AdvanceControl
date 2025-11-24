using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.EndPointProvider;
using Advance_Control.Services.Logging;

namespace Advance_Control.Services.UserInfo
{
    /// <summary>
    /// Implementación del servicio de información de usuario que se comunica con la API
    /// </summary>
    public class UserInfoService : IUserInfoService
    {
        private readonly HttpClient _http;
        private readonly IApiEndpointProvider _endpoints;
        private readonly ILoggingService _logger;

        public UserInfoService(HttpClient http, IApiEndpointProvider endpoints, ILoggingService logger)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Obtiene la información del usuario autenticado actual
        /// </summary>
        public async Task<UserInfoDto?> GetUserInfoAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // Construir la URL del endpoint
                var url = _endpoints.GetEndpoint("api", "UserInfo", "infoUsuario");

                await _logger.LogInformationAsync($"Obteniendo información del usuario desde: {url}", "UserInfoService", "GetUserInfoAsync");

                // Realizar la petición GET
                var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);

                // Verificar si la respuesta fue exitosa
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al obtener información del usuario. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "UserInfoService",
                        "GetUserInfoAsync");
                    return null;
                }

                // Deserializar la respuesta
                var userInfo = await response.Content.ReadFromJsonAsync<UserInfoDto>(cancellationToken: cancellationToken).ConfigureAwait(false);


                return userInfo;
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al obtener información del usuario", ex, "UserInfoService", "GetUserInfoAsync");
                return null;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al obtener información del usuario", ex, "UserInfoService", "GetUserInfoAsync");
                return null;
            }
        }
    }
}
