using System;
using System.Net;
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
                using var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);

                // Verificar si la respuesta fue exitosa
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
                    {
                        await _logger.LogWarningAsync(
                            $"La sesión ya no es válida al obtener información del usuario. Status: {response.StatusCode}, Content: {errorContent}",
                            "UserInfoService",
                            "GetUserInfoAsync");
                        throw new UnauthorizedAccessException("La sesión ya no es válida. Inicia sesión nuevamente.");
                    }

                    await _logger.LogErrorAsync(
                        $"Error al obtener información del usuario. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "UserInfoService",
                        "GetUserInfoAsync");
                    throw new InvalidOperationException("No se pudo obtener la información del usuario porque el servidor no respondió correctamente.");
                }

                // Deserializar la respuesta
                var userInfo = await response.Content.ReadFromJsonAsync<UserInfoDto>(cancellationToken: cancellationToken).ConfigureAwait(false);


                return userInfo;
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al obtener información del usuario", ex, "UserInfoService", "GetUserInfoAsync");
                throw new InvalidOperationException("No se pudo obtener la información del usuario porque el servidor no está disponible.", ex);
            }
            catch (OperationCanceledException ex)
            {
                await _logger.LogErrorAsync("La solicitud de información del usuario expiró o fue cancelada", ex, "UserInfoService", "GetUserInfoAsync");
                throw new InvalidOperationException("El servidor tardó demasiado en responder al cargar la sesión del usuario.", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al obtener información del usuario", ex, "UserInfoService", "GetUserInfoAsync");
                throw new InvalidOperationException("Ocurrió un error inesperado al obtener la información del usuario.", ex);
            }
        }
    }
}
