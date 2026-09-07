using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.EndPointProvider;
using Advance_Control.Services.Logging;
using Advance_Control.Utilities;

namespace Advance_Control.Services.Inmuebles
{
    /// <summary>
    /// Implementación del servicio de inmuebles que se comunica con la API
    /// </summary>
    public class InmuebleService : IInmuebleService
    {
        private readonly HttpClient _http;
        private readonly IApiEndpointProvider _endpoints;
        private readonly ILoggingService _logger;

        public InmuebleService(HttpClient http, IApiEndpointProvider endpoints, ILoggingService logger)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Obtiene una lista de inmuebles según los criterios de búsqueda proporcionados
        /// </summary>
        public async Task<List<InmuebleDto>> GetInmueblesAsync(InmuebleQueryDto? query = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = _endpoints.GetEndpoint("api", "inmueble_crud");

                if (query != null)
                {
                    url = new ApiQueryBuilder()
                        .Add("descripcion", query.Descripcion)
                        .Add("identificador", query.Identificador)
                        .Add("idTipoInmueble", query.IdTipoInmueble)
                        .Add("codigoPostal", query.CodigoPostal)
                        .Add("idUbicacion", query.IdUbicacion)
                        .Build(url);
                }

                await _logger.LogInformationAsync($"Obteniendo inmuebles desde: {url}", "InmuebleService", "GetInmueblesAsync");

                var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al obtener inmuebles. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "InmuebleService",
                        "GetInmueblesAsync");

                    try
                    {
                        var errorResponse = await response.Content.ReadFromJsonAsync<ApiResponse>(cancellationToken: cancellationToken).ConfigureAwait(false);
                        if (errorResponse?.Message != null)
                        {
                            throw new InvalidOperationException(errorResponse.Message);
                        }
                    }
                    catch (System.Text.Json.JsonException jsonEx)
                    {
                        await _logger.LogWarningAsync($"No se pudo parsear respuesta de error de la API: {jsonEx.Message}", "InmuebleService", "GetInmueblesAsync");
                        throw new InvalidOperationException("Error al obtener inmuebles del servidor.");
                    }

                    return new List<InmuebleDto>();
                }

                var inmuebles = await response.Content.ReadFromJsonAsync<List<InmuebleDto>>(cancellationToken: cancellationToken).ConfigureAwait(false);

                await _logger.LogInformationAsync($"Se obtuvieron {inmuebles?.Count ?? 0} inmuebles", "InmuebleService", "GetInmueblesAsync");

                return inmuebles ?? new List<InmuebleDto>();
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al obtener inmuebles", ex, "InmuebleService", "GetInmueblesAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al obtener inmuebles", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al obtener inmuebles", ex, "InmuebleService", "GetInmueblesAsync");
                throw;
            }
        }

        /// <summary>
        /// Elimina (soft delete) un inmueble por su ID
        /// </summary>
        public async Task<bool> DeleteInmuebleAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = _endpoints.GetEndpoint("api", "inmueble_crud", id.ToString());

                await _logger.LogInformationAsync($"Eliminando inmueble {id} en: {url}", "InmuebleService", "DeleteInmuebleAsync");

                using var response = await _http.DeleteAsync(url, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al eliminar inmueble. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "InmuebleService",
                        "DeleteInmuebleAsync");

                    try
                    {
                        var errorResponse = await response.Content.ReadFromJsonAsync<ApiResponse>(cancellationToken: cancellationToken).ConfigureAwait(false);
                        if (errorResponse?.Message != null)
                        {
                            throw new InvalidOperationException(errorResponse.Message);
                        }
                    }
                    catch (System.Text.Json.JsonException jsonEx)
                    {
                        await _logger.LogWarningAsync($"No se pudo parsear respuesta de error de la API: {jsonEx.Message}", "InmuebleService", "DeleteInmuebleAsync");
                        throw new InvalidOperationException("Error al eliminar inmueble del servidor.");
                    }

                    return false;
                }

                await _logger.LogInformationAsync($"Inmueble {id} eliminado correctamente", "InmuebleService", "DeleteInmuebleAsync");
                return true;
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al eliminar inmueble", ex, "InmuebleService", "DeleteInmuebleAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al eliminar inmueble", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al eliminar inmueble", ex, "InmuebleService", "DeleteInmuebleAsync");
                throw;
            }
        }

        /// <summary>
        /// Actualiza un inmueble existente
        /// </summary>
        public async Task<bool> UpdateInmuebleAsync(int id, InmuebleQueryDto query, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = _endpoints.GetEndpoint("api", "inmueble_crud", id.ToString());

                url = new ApiQueryBuilder()
                    .Add("descripcion", query.Descripcion)
                    .Add("identificador", query.Identificador)
                    .Add("idTipoInmueble", query.IdTipoInmueble)
                    .Add("superficieM2", query.SuperficieM2)
                    .Add("codigoPostal", query.CodigoPostal)
                    .Add("idUbicacion", query.IdUbicacion)
                    .Build(url);

                await _logger.LogInformationAsync($"Actualizando inmueble {id} en: {url}", "InmuebleService", "UpdateInmuebleAsync");

                using var response = await _http.PutAsync(url, null, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al actualizar inmueble. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "InmuebleService",
                        "UpdateInmuebleAsync");

                    try
                    {
                        var errorResponse = await response.Content.ReadFromJsonAsync<ApiResponse>(cancellationToken: cancellationToken).ConfigureAwait(false);
                        if (errorResponse?.Message != null)
                        {
                            throw new InvalidOperationException(errorResponse.Message);
                        }
                    }
                    catch (System.Text.Json.JsonException jsonEx)
                    {
                        await _logger.LogWarningAsync($"No se pudo parsear respuesta de error de la API: {jsonEx.Message}", "InmuebleService", "UpdateInmuebleAsync");
                        throw new InvalidOperationException("Error al actualizar inmueble en el servidor.");
                    }

                    return false;
                }

                await _logger.LogInformationAsync($"Inmueble {id} actualizado correctamente", "InmuebleService", "UpdateInmuebleAsync");
                return true;
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al actualizar inmueble", ex, "InmuebleService", "UpdateInmuebleAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al actualizar inmueble", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al actualizar inmueble", ex, "InmuebleService", "UpdateInmuebleAsync");
                throw;
            }
        }

        /// <summary>
        /// Crea un nuevo inmueble
        /// </summary>
        public async Task<bool> CreateInmuebleAsync(string? descripcion = null, string identificador = "", int? idTipoInmueble = null, double? superficieM2 = null, string? codigoPostal = null, bool estatus = true, int? idUbicacion = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = _endpoints.GetEndpoint("api", "inmueble_crud");

                url = new ApiQueryBuilder()
                    .Add("descripcion", descripcion)
                    .Add("identificador", identificador)
                    .Add("idTipoInmueble", idTipoInmueble)
                    .Add("superficieM2", superficieM2)
                    .Add("codigoPostal", codigoPostal)
                    .AddRequired("estatus", estatus)
                    .Add("idUbicacion", idUbicacion)
                    .Build(url);

                await _logger.LogInformationAsync($"Creando inmueble en: {url}", "InmuebleService", "CreateInmuebleAsync");

                using var response = await _http.PostAsync(url, null, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al crear inmueble. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "InmuebleService",
                        "CreateInmuebleAsync");

                    try
                    {
                        var errorResponse = await response.Content.ReadFromJsonAsync<ApiResponse>(cancellationToken: cancellationToken).ConfigureAwait(false);
                        if (errorResponse?.Message != null)
                        {
                            throw new InvalidOperationException(errorResponse.Message);
                        }
                    }
                    catch (System.Text.Json.JsonException jsonEx)
                    {
                        await _logger.LogWarningAsync($"No se pudo parsear respuesta de error de la API: {jsonEx.Message}", "InmuebleService", "CreateInmuebleAsync");
                        throw new InvalidOperationException("Error al crear inmueble en el servidor.");
                    }

                    return false;
                }

                await _logger.LogInformationAsync("Inmueble creado correctamente", "InmuebleService", "CreateInmuebleAsync");
                return true;
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al crear inmueble", ex, "InmuebleService", "CreateInmuebleAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al crear inmueble", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al crear inmueble", ex, "InmuebleService", "CreateInmuebleAsync");
                throw;
            }
        }

        /// <summary>
        /// Sugiere el siguiente identificador numérico disponible
        /// </summary>
        public async Task<string> GetSiguienteIdentificadorAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"{_endpoints.GetEndpoint("api", "inmueble_crud")}/siguiente-identificador";

                var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al obtener siguiente identificador. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "InmuebleService",
                        "GetSiguienteIdentificadorAsync");
                    throw new InvalidOperationException("No se pudo calcular el siguiente identificador.");
                }

                var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var resultado = await response.Content.ReadFromJsonAsync<SiguienteIdentificadorResponse>(options, cancellationToken).ConfigureAwait(false);
                return resultado?.Identificador ?? string.Empty;
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al obtener siguiente identificador", ex, "InmuebleService", "GetSiguienteIdentificadorAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al obtener el siguiente identificador", ex);
            }
        }

        private class SiguienteIdentificadorResponse
        {
            public string? Identificador { get; set; }
        }
    }
}
