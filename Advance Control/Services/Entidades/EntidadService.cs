using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.EndPointProvider;
using Advance_Control.Services.Logging;

namespace Advance_Control.Services.Entidades
{
    /// <summary>
    /// Implementación del servicio de entidades que se comunica con la API
    /// </summary>
    public class EntidadService : IEntidadService
    {
        private readonly HttpClient _http;
        private readonly IApiEndpointProvider _endpoints;
        private readonly ILoggingService _logger;

        public EntidadService(HttpClient http, IApiEndpointProvider endpoints, ILoggingService logger)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Obtiene una lista de entidades según los criterios de búsqueda proporcionados
        /// </summary>
        public async Task<List<EntidadDto>> GetEntidadesAsync(EntidadQueryDto? query = null, CancellationToken cancellationToken = default)
        {
            try
            {
                // Construir la URL base
                var url = _endpoints.GetEndpoint("api", "entidad");

                // Agregar parámetros de consulta si existen
                if (query != null)
                {
                    var queryParams = new List<string>();

                    if (!string.IsNullOrWhiteSpace(query.NombreComercial))
                        queryParams.Add($"nombreComercial={Uri.EscapeDataString(query.NombreComercial)}");

                    if (!string.IsNullOrWhiteSpace(query.RazonSocial))
                        queryParams.Add($"razonSocial={Uri.EscapeDataString(query.RazonSocial)}");

                    if (!string.IsNullOrWhiteSpace(query.RFC))
                        queryParams.Add($"rfc={Uri.EscapeDataString(query.RFC)}");

                    if (!string.IsNullOrWhiteSpace(query.CP))
                        queryParams.Add($"cp={Uri.EscapeDataString(query.CP)}");

                    if (!string.IsNullOrWhiteSpace(query.Estado))
                        queryParams.Add($"estado={Uri.EscapeDataString(query.Estado)}");

                    if (!string.IsNullOrWhiteSpace(query.Ciudad))
                        queryParams.Add($"ciudad={Uri.EscapeDataString(query.Ciudad)}");

                    if (query.Estatus.HasValue)
                        queryParams.Add($"estatus={query.Estatus.Value.ToString().ToLowerInvariant()}");

                    if (queryParams.Count > 0)
                    {
                        url = $"{url}?{string.Join("&", queryParams)}";
                    }
                }

                await _logger.LogInformationAsync($"Obteniendo entidades desde: {url}", "EntidadService", "GetEntidadesAsync");

                // Realizar la petición GET
                var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);

                // Verificar si la respuesta fue exitosa
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al obtener entidades. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "EntidadService",
                        "GetEntidadesAsync");
                    return new List<EntidadDto>();
                }

                // Deserializar la respuesta
                var entidades = await response.Content.ReadFromJsonAsync<List<EntidadDto>>(cancellationToken: cancellationToken).ConfigureAwait(false);

                await _logger.LogInformationAsync($"Se obtuvieron {entidades?.Count ?? 0} entidades", "EntidadService", "GetEntidadesAsync");

                return entidades ?? new List<EntidadDto>();
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al obtener entidades", ex, "EntidadService", "GetEntidadesAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al obtener entidades", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al obtener entidades", ex, "EntidadService", "GetEntidadesAsync");
                throw;
            }
        }

        /// <summary>
        /// Crea una nueva entidad
        /// </summary>
        public async Task<ApiResponse> CreateEntidadAsync(
            string nombreComercial,
            string razonSocial,
            string? rfc = null,
            string? cp = null,
            string? estado = null,
            string? ciudad = null,
            string? pais = null,
            string? calle = null,
            string? numExt = null,
            string? numInt = null,
            string? colonia = null,
            string? apoderado = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var url = _endpoints.GetEndpoint("api", "entidad");

                // Construir parámetros de consulta
                var queryParams = new List<string>
                {
                    $"nombreComercial={Uri.EscapeDataString(nombreComercial)}",
                    $"razonSocial={Uri.EscapeDataString(razonSocial)}"
                };

                if (!string.IsNullOrWhiteSpace(rfc))
                    queryParams.Add($"rfc={Uri.EscapeDataString(rfc)}");
                if (!string.IsNullOrWhiteSpace(cp))
                    queryParams.Add($"cp={Uri.EscapeDataString(cp)}");
                if (!string.IsNullOrWhiteSpace(estado))
                    queryParams.Add($"estado={Uri.EscapeDataString(estado)}");
                if (!string.IsNullOrWhiteSpace(ciudad))
                    queryParams.Add($"ciudad={Uri.EscapeDataString(ciudad)}");
                if (!string.IsNullOrWhiteSpace(pais))
                    queryParams.Add($"pais={Uri.EscapeDataString(pais)}");
                if (!string.IsNullOrWhiteSpace(calle))
                    queryParams.Add($"calle={Uri.EscapeDataString(calle)}");
                if (!string.IsNullOrWhiteSpace(numExt))
                    queryParams.Add($"nomExt={Uri.EscapeDataString(numExt)}");
                if (!string.IsNullOrWhiteSpace(numInt))
                    queryParams.Add($"numInt={Uri.EscapeDataString(numInt)}");
                if (!string.IsNullOrWhiteSpace(colonia))
                    queryParams.Add($"colonia={Uri.EscapeDataString(colonia)}");
                if (!string.IsNullOrWhiteSpace(apoderado))
                    queryParams.Add($"apoderado={Uri.EscapeDataString(apoderado)}");

                url = $"{url}?{string.Join("&", queryParams)}";

                await _logger.LogInformationAsync($"Creando entidad en: {url}", "EntidadService", "CreateEntidadAsync");

                // Realizar la petición POST
                var response = await _http.PostAsync(url, null, cancellationToken).ConfigureAwait(false);

                // Verificar si la respuesta fue exitosa
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al crear entidad. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "EntidadService",
                        "CreateEntidadAsync");
                    return new ApiResponse { Success = false, Message = errorContent };
                }

                await _logger.LogInformationAsync("Entidad creada exitosamente", "EntidadService", "CreateEntidadAsync");

                return new ApiResponse { Success = true, Message = "Entidad creada correctamente" };
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al crear entidad", ex, "EntidadService", "CreateEntidadAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al crear entidad", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al crear entidad", ex, "EntidadService", "CreateEntidadAsync");
                throw;
            }
        }

        /// <summary>
        /// Actualiza una entidad por su ID
        /// </summary>
        public async Task<ApiResponse> UpdateEntidadAsync(
            int id,
            string? nombreComercial = null,
            string? razonSocial = null,
            string? rfc = null,
            string? cp = null,
            string? estado = null,
            string? ciudad = null,
            string? pais = null,
            string? calle = null,
            string? numExt = null,
            string? numInt = null,
            string? colonia = null,
            string? apoderado = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"{_endpoints.GetEndpoint("api", "entidad")}/{id}";

                // Construir parámetros de consulta
                var queryParams = new List<string>();

                if (!string.IsNullOrWhiteSpace(nombreComercial))
                    queryParams.Add($"nombreComercial={Uri.EscapeDataString(nombreComercial)}");
                if (!string.IsNullOrWhiteSpace(razonSocial))
                    queryParams.Add($"razonSocial={Uri.EscapeDataString(razonSocial)}");
                if (!string.IsNullOrWhiteSpace(rfc))
                    queryParams.Add($"rfc={Uri.EscapeDataString(rfc)}");
                if (!string.IsNullOrWhiteSpace(cp))
                    queryParams.Add($"cp={Uri.EscapeDataString(cp)}");
                if (!string.IsNullOrWhiteSpace(estado))
                    queryParams.Add($"estado={Uri.EscapeDataString(estado)}");
                if (!string.IsNullOrWhiteSpace(ciudad))
                    queryParams.Add($"ciudad={Uri.EscapeDataString(ciudad)}");
                if (!string.IsNullOrWhiteSpace(pais))
                    queryParams.Add($"pais={Uri.EscapeDataString(pais)}");
                if (!string.IsNullOrWhiteSpace(calle))
                    queryParams.Add($"calle={Uri.EscapeDataString(calle)}");
                if (!string.IsNullOrWhiteSpace(numExt))
                    queryParams.Add($"nomExt={Uri.EscapeDataString(numExt)}");
                if (!string.IsNullOrWhiteSpace(numInt))
                    queryParams.Add($"numInt={Uri.EscapeDataString(numInt)}");
                if (!string.IsNullOrWhiteSpace(colonia))
                    queryParams.Add($"colonia={Uri.EscapeDataString(colonia)}");
                if (!string.IsNullOrWhiteSpace(apoderado))
                    queryParams.Add($"apoderado={Uri.EscapeDataString(apoderado)}");

                if (queryParams.Count > 0)
                {
                    url = $"{url}?{string.Join("&", queryParams)}";
                }

                await _logger.LogInformationAsync($"Actualizando entidad en: {url}", "EntidadService", "UpdateEntidadAsync");

                // Realizar la petición PUT
                var response = await _http.PutAsync(url, null, cancellationToken).ConfigureAwait(false);

                // Verificar si la respuesta fue exitosa
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al actualizar entidad. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "EntidadService",
                        "UpdateEntidadAsync");
                    return new ApiResponse { Success = false, Message = errorContent };
                }

                await _logger.LogInformationAsync($"Entidad {id} actualizada exitosamente", "EntidadService", "UpdateEntidadAsync");

                return new ApiResponse { Success = true, Message = "Entidad actualizada exitosamente" };
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al actualizar entidad", ex, "EntidadService", "UpdateEntidadAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al actualizar entidad", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al actualizar entidad", ex, "EntidadService", "UpdateEntidadAsync");
                throw;
            }
        }

        /// <summary>
        /// Elimina una entidad por su ID
        /// </summary>
        public async Task<ApiResponse> DeleteEntidadAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"{_endpoints.GetEndpoint("api", "entidad")}/{id}";

                await _logger.LogInformationAsync($"Eliminando entidad en: {url}", "EntidadService", "DeleteEntidadAsync");

                // Realizar la petición DELETE
                var response = await _http.DeleteAsync(url, cancellationToken).ConfigureAwait(false);

                // Verificar si la respuesta fue exitosa
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al eliminar entidad. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "EntidadService",
                        "DeleteEntidadAsync");
                    return new ApiResponse { Success = false, Message = errorContent };
                }

                await _logger.LogInformationAsync($"Entidad {id} eliminada exitosamente", "EntidadService", "DeleteEntidadAsync");

                return new ApiResponse { Success = true, Message = "Entidad eliminada exitosamente" };
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al eliminar entidad", ex, "EntidadService", "DeleteEntidadAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al eliminar entidad", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al eliminar entidad", ex, "EntidadService", "DeleteEntidadAsync");
                throw;
            }
        }

        /// <summary>
        /// Obtiene la primera entidad activa (estatus = true)
        /// </summary>
        public async Task<EntidadDto?> GetActiveEntidadAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await _logger.LogInformationAsync("Obteniendo entidad activa...", "EntidadService", "GetActiveEntidadAsync");

                var query = new EntidadQueryDto { Estatus = true };
                var entidades = await GetEntidadesAsync(query, cancellationToken);

                var entidadActiva = entidades?.FirstOrDefault();

                if (entidadActiva != null)
                {
                    await _logger.LogInformationAsync($"Entidad activa encontrada: {entidadActiva.NombreComercial}", "EntidadService", "GetActiveEntidadAsync");
                }
                else
                {
                    await _logger.LogWarningAsync("No se encontró ninguna entidad activa", "EntidadService", "GetActiveEntidadAsync");
                }

                return entidadActiva;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error al obtener entidad activa", ex, "EntidadService", "GetActiveEntidadAsync");
                return null;
            }
        }
    }
}
