using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.EndPointProvider;
using Advance_Control.Services.Logging;

namespace Advance_Control.Services.Relaciones
{
    /// <summary>
    /// Implementación del servicio de relaciones equipo-cliente que se comunica con la API
    /// </summary>
    public class RelacionService : IRelacionService
    {
        private readonly HttpClient _http;
        private readonly IApiEndpointProvider _endpoints;
        private readonly ILoggingService _logger;

        public RelacionService(HttpClient http, IApiEndpointProvider endpoints, ILoggingService logger)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Obtiene una lista de relaciones cliente para un identificador de equipo
        /// </summary>
        public async Task<List<RelacionClienteDto>> GetRelacionesAsync(string identificador, int idCliente = 0, CancellationToken cancellationToken = default)
        {
            try
            {
                // Construir la URL base usando el endpoint correcto
                var url = _endpoints.GetEndpoint("api", "Relaciones");

                // Agregar parámetros de consulta
                var queryParams = new List<string>();

                if (!string.IsNullOrWhiteSpace(identificador))
                    queryParams.Add($"identificador={Uri.EscapeDataString(identificador)}");

                queryParams.Add($"idCliente={idCliente}");

                if (queryParams.Count > 0)
                {
                    url = $"{url}?{string.Join("&", queryParams)}";
                }

                await _logger.LogInformationAsync($"Obteniendo relaciones desde: {url}", "RelacionService", "GetRelacionesAsync");

                // Realizar la petición GET
                var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);

                // Verificar si la respuesta fue exitosa
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al obtener relaciones. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "RelacionService",
                        "GetRelacionesAsync");
                    return new List<RelacionClienteDto>();
                }

                // Deserializar la respuesta
                var relaciones = await response.Content.ReadFromJsonAsync<List<RelacionClienteDto>>(cancellationToken: cancellationToken).ConfigureAwait(false);

                await _logger.LogInformationAsync($"Se obtuvieron {relaciones?.Count ?? 0} relaciones", "RelacionService", "GetRelacionesAsync");

                return relaciones ?? new List<RelacionClienteDto>();
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al obtener relaciones", ex, "RelacionService", "GetRelacionesAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al obtener relaciones", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al obtener relaciones", ex, "RelacionService", "GetRelacionesAsync");
                throw;
            }
        }

        /// <summary>
        /// Elimina una relación entre un equipo y un cliente
        /// </summary>
        public async Task<bool> DeleteRelacionAsync(string identificador, int idCliente, CancellationToken cancellationToken = default)
        {
            try
            {
                // Construir la URL usando el endpoint correcto
                var url = _endpoints.GetEndpoint("api", "Relaciones");

                // Agregar parámetros de consulta
                url = $"{url}?identificador={Uri.EscapeDataString(identificador)}&idCliente={idCliente}";

                await _logger.LogInformationAsync($"Eliminando relación desde: {url}", "RelacionService", "DeleteRelacionAsync");

                // Realizar la petición DELETE
                var response = await _http.DeleteAsync(url, cancellationToken).ConfigureAwait(false);

                // Verificar si la respuesta fue exitosa
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al eliminar relación. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "RelacionService",
                        "DeleteRelacionAsync");
                    return false;
                }

                await _logger.LogInformationAsync($"Relación eliminada exitosamente", "RelacionService", "DeleteRelacionAsync");
                return true;
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al eliminar relación", ex, "RelacionService", "DeleteRelacionAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al eliminar relación", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al eliminar relación", ex, "RelacionService", "DeleteRelacionAsync");
                throw;
            }
        }

        /// <summary>
        /// Actualiza la nota de una relación entre un equipo y un cliente
        /// </summary>
        public async Task<bool> UpdateRelacionNotaAsync(string identificador, int idCliente, string nota, CancellationToken cancellationToken = default)
        {
            try
            {
                // Construir la URL usando el endpoint correcto
                var url = _endpoints.GetEndpoint("api", "Relaciones", "nota");

                // Agregar parámetros de consulta
                url = $"{url}?identificador={Uri.EscapeDataString(identificador)}&idCliente={idCliente}&nota={Uri.EscapeDataString(nota ?? string.Empty)}";

                await _logger.LogInformationAsync($"Actualizando nota de relación desde: {url}", "RelacionService", "UpdateRelacionNotaAsync");

                // Realizar la petición PUT (sin body ya que todo va en query string)
                var response = await _http.PutAsync(url, null, cancellationToken).ConfigureAwait(false);

                // Verificar si la respuesta fue exitosa
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al actualizar nota de relación. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "RelacionService",
                        "UpdateRelacionNotaAsync");
                    return false;
                }

                await _logger.LogInformationAsync($"Nota de relación actualizada exitosamente", "RelacionService", "UpdateRelacionNotaAsync");
                return true;
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al actualizar nota de relación", ex, "RelacionService", "UpdateRelacionNotaAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al actualizar nota de relación", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al actualizar nota de relación", ex, "RelacionService", "UpdateRelacionNotaAsync");
                throw;
            }
        }
    }
}
