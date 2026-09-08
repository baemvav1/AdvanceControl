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

namespace Advance_Control.Services.RelacionesInmueble
{
    /// <summary>
    /// Implementación del servicio de relaciones inmueble-cliente que se comunica con la API
    /// </summary>
    public class RelacionInmuebleService : IRelacionInmuebleService
    {
        private readonly HttpClient _http;
        private readonly IApiEndpointProvider _endpoints;
        private readonly ILoggingService _logger;

        public RelacionInmuebleService(HttpClient http, IApiEndpointProvider endpoints, ILoggingService logger)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Obtiene una lista de relaciones cliente para un identificador de inmueble
        /// </summary>
        public async Task<List<RelacionClienteDto>> GetRelacionesAsync(string identificador, int idCliente = 0, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = _endpoints.GetEndpoint("api", "RelacionesInmueble");

                url = new ApiQueryBuilder()
                    .Add("identificador", identificador)
                    .AddRequired("idCliente", idCliente)
                    .Build(url);

                await _logger.LogInformationAsync($"Obteniendo relaciones desde: {url}", "RelacionInmuebleService", "GetRelacionesAsync");

                var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al obtener relaciones. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "RelacionInmuebleService",
                        "GetRelacionesAsync");
                    return new List<RelacionClienteDto>();
                }

                var relaciones = await response.Content.ReadFromJsonAsync<List<RelacionClienteDto>>(cancellationToken: cancellationToken).ConfigureAwait(false);

                await _logger.LogInformationAsync($"Se obtuvieron {relaciones?.Count ?? 0} relaciones", "RelacionInmuebleService", "GetRelacionesAsync");

                return relaciones ?? new List<RelacionClienteDto>();
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al obtener relaciones", ex, "RelacionInmuebleService", "GetRelacionesAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al obtener relaciones", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al obtener relaciones", ex, "RelacionInmuebleService", "GetRelacionesAsync");
                throw;
            }
        }

        /// <summary>
        /// Elimina (soft delete) una relación inmueble-cliente
        /// </summary>
        public async Task<bool> DeleteRelacionAsync(string identificador, int idCliente, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(identificador))
                {
                    await _logger.LogWarningAsync("El identificador es requerido para eliminar relación", "RelacionInmuebleService", "DeleteRelacionAsync");
                    return false;
                }

                if (idCliente <= 0)
                {
                    await _logger.LogWarningAsync("El idCliente debe ser mayor que 0 para eliminar relación", "RelacionInmuebleService", "DeleteRelacionAsync");
                    return false;
                }

                var url = _endpoints.GetEndpoint("api", "RelacionesInmueble");
                url = $"{url}?identificador={Uri.EscapeDataString(identificador)}&idCliente={idCliente}";

                await _logger.LogInformationAsync($"Eliminando relación desde: {url}", "RelacionInmuebleService", "DeleteRelacionAsync");

                using var response = await _http.DeleteAsync(url, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al eliminar relación. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "RelacionInmuebleService",
                        "DeleteRelacionAsync");
                    return false;
                }

                await _logger.LogInformationAsync($"Relación eliminada exitosamente: identificador={identificador}, idCliente={idCliente}", "RelacionInmuebleService", "DeleteRelacionAsync");
                return true;
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al eliminar relación", ex, "RelacionInmuebleService", "DeleteRelacionAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al eliminar relación", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al eliminar relación", ex, "RelacionInmuebleService", "DeleteRelacionAsync");
                throw;
            }
        }

        /// <summary>
        /// Actualiza la nota de una relación inmueble-cliente
        /// </summary>
        public async Task<bool> UpdateNotaAsync(string identificador, int idCliente, string? nota, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(identificador))
                {
                    await _logger.LogWarningAsync("El identificador es requerido para actualizar la nota", "RelacionInmuebleService", "UpdateNotaAsync");
                    return false;
                }

                if (idCliente <= 0)
                {
                    await _logger.LogWarningAsync("El idCliente debe ser mayor que 0 para actualizar la nota", "RelacionInmuebleService", "UpdateNotaAsync");
                    return false;
                }

                var url = _endpoints.GetEndpoint("api", "RelacionesInmueble/nota");
                url = $"{url}?identificador={Uri.EscapeDataString(identificador)}&idCliente={idCliente}";

                if (!string.IsNullOrWhiteSpace(nota))
                {
                    url = $"{url}&nota={Uri.EscapeDataString(nota)}";
                }

                await _logger.LogInformationAsync($"Actualizando nota de relación: {url}", "RelacionInmuebleService", "UpdateNotaAsync");

                using var response = await _http.PutAsync(url, null, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al actualizar nota de relación. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "RelacionInmuebleService",
                        "UpdateNotaAsync");
                    return false;
                }

                await _logger.LogInformationAsync($"Nota actualizada exitosamente: identificador={identificador}, idCliente={idCliente}", "RelacionInmuebleService", "UpdateNotaAsync");
                return true;
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al actualizar nota de relación", ex, "RelacionInmuebleService", "UpdateNotaAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al actualizar nota de relación", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al actualizar nota de relación", ex, "RelacionInmuebleService", "UpdateNotaAsync");
                throw;
            }
        }

        /// <summary>
        /// Crea una nueva relación inmueble-cliente
        /// </summary>
        public async Task<bool> CreateRelacionAsync(string identificador, int idCliente, string? nota, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(identificador))
                {
                    await _logger.LogWarningAsync("El identificador es requerido para crear relación", "RelacionInmuebleService", "CreateRelacionAsync");
                    return false;
                }

                if (idCliente <= 0)
                {
                    await _logger.LogWarningAsync("El idCliente debe ser mayor que 0 para crear relación", "RelacionInmuebleService", "CreateRelacionAsync");
                    return false;
                }

                var url = _endpoints.GetEndpoint("api", "RelacionesInmueble");
                url = $"{url}?identificador={Uri.EscapeDataString(identificador)}&idCliente={idCliente}";

                if (!string.IsNullOrWhiteSpace(nota))
                {
                    url = $"{url}&nota={Uri.EscapeDataString(nota)}";
                }

                await _logger.LogInformationAsync($"Creando relación: {url}", "RelacionInmuebleService", "CreateRelacionAsync");

                using var response = await _http.PostAsync(url, null, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al crear relación. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "RelacionInmuebleService",
                        "CreateRelacionAsync");
                    return false;
                }

                try
                {
                    var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse>(cancellationToken: cancellationToken).ConfigureAwait(false);
                    if (apiResponse != null && !apiResponse.Success)
                    {
                        await _logger.LogWarningAsync(
                            $"La API retornó success=false: {apiResponse.Message}",
                            "RelacionInmuebleService",
                            "CreateRelacionAsync");
                        return false;
                    }
                }
                catch (System.Text.Json.JsonException ex)
                {
                    await _logger.LogWarningAsync(
                        $"No se pudo parsear la respuesta JSON: {ex.Message}",
                        "RelacionInmuebleService",
                        "CreateRelacionAsync");
                }

                await _logger.LogInformationAsync($"Relación creada exitosamente: identificador={identificador}, idCliente={idCliente}", "RelacionInmuebleService", "CreateRelacionAsync");
                return true;
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al crear relación", ex, "RelacionInmuebleService", "CreateRelacionAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al crear relación", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al crear relación", ex, "RelacionInmuebleService", "CreateRelacionAsync");
                throw;
            }
        }
    }
}
