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

namespace Advance_Control.Services.Operaciones
{
    /// <summary>
    /// Implementación del servicio de operaciones que se comunica con la API
    /// </summary>
    public class OperacionService : IOperacionService
    {
        private readonly HttpClient _http;
        private readonly IApiEndpointProvider _endpoints;
        private readonly ILoggingService _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public OperacionService(HttpClient http, IApiEndpointProvider endpoints, ILoggingService logger)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Configurar opciones de JSON para ser case-insensitive
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        /// <summary>
        /// Obtiene una lista de operaciones según los criterios de búsqueda proporcionados
        /// </summary>
        public async Task<List<OperacionDto>> GetOperacionesAsync(OperacionQueryDto? query = null, CancellationToken cancellationToken = default)
        {
            try
            {
                // Construir la URL base usando el endpoint correcto
                var url = _endpoints.GetEndpoint("api", "Operaciones");

                // Agregar parámetros de consulta si existen
                if (query != null)
                {
                    var queryParams = new List<string>();

                    if (query.IdTipo > 0)
                        queryParams.Add($"idTipo={query.IdTipo}");

                    if (query.IdCliente > 0)
                        queryParams.Add($"idCliente={query.IdCliente}");

                    if (query.IdEquipo > 0)
                        queryParams.Add($"idEquipo={query.IdEquipo}");

                    if (query.IdAtiende > 0)
                        queryParams.Add($"idAtiende={query.IdAtiende}");

                    if (!string.IsNullOrWhiteSpace(query.Nota))
                        queryParams.Add($"nota={Uri.EscapeDataString(query.Nota)}");

                    if (queryParams.Count > 0)
                    {
                        url = $"{url}?{string.Join("&", queryParams)}";
                    }
                }

                await _logger.LogInformationAsync($"Obteniendo operaciones desde: {url}", "OperacionService", "GetOperacionesAsync");

                // Realizar la petición GET
                var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);

                // Verificar si la respuesta fue exitosa
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al obtener operaciones. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "OperacionService",
                        "GetOperacionesAsync");
                    return new List<OperacionDto>();
                }

                // Deserializar la respuesta
                List<OperacionDto>? operaciones;
                try
                {
                    operaciones = await response.Content.ReadFromJsonAsync<List<OperacionDto>>(_jsonOptions, cancellationToken: cancellationToken).ConfigureAwait(false);
                }
                catch (JsonException ex)
                {
                    await _logger.LogErrorAsync(
                        "Error al deserializar respuesta de operaciones",
                        ex,
                        "OperacionService",
                        "GetOperacionesAsync");
                    return new List<OperacionDto>();
                }

                await _logger.LogInformationAsync($"Se obtuvieron {operaciones?.Count ?? 0} operaciones", "OperacionService", "GetOperacionesAsync");

                return operaciones ?? new List<OperacionDto>();
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al obtener operaciones", ex, "OperacionService", "GetOperacionesAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al obtener operaciones", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al obtener operaciones", ex, "OperacionService", "GetOperacionesAsync");
                throw;
            }
        }

        /// <summary>
        /// Elimina (soft delete) una operación por su ID
        /// </summary>
        public async Task<bool> DeleteOperacionAsync(int idOperacion, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"{_endpoints.GetEndpoint("api", "Operaciones")}?idOperacion={idOperacion}";

                await _logger.LogInformationAsync($"Eliminando operación {idOperacion} en: {url}", "OperacionService", "DeleteOperacionAsync");

                var response = await _http.DeleteAsync(url, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al eliminar operación. Status: {response.StatusCode}, Content: {errorContent}",
                        null, "OperacionService", "DeleteOperacionAsync");
                    return false;
                }

                await _logger.LogInformationAsync($"Operación {idOperacion} eliminada correctamente", "OperacionService", "DeleteOperacionAsync");
                return true;
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al eliminar operación", ex, "OperacionService", "DeleteOperacionAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al eliminar operación", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al eliminar operación", ex, "OperacionService", "DeleteOperacionAsync");
                throw;
            }
        }

        /// <summary>
        /// Actualiza una operación (monto, fechaFinal, etc.)
        /// </summary>
        public async Task<bool> UpdateOperacionAsync(int idOperacion, int idTipo = 0, int idCliente = 0, int idEquipo = 0, int idAtiende = 0, double monto = 0, string? nota = null, DateTime? fechaFinal = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var queryParams = new System.Collections.Generic.List<string>
                {
                    $"idOperacion={idOperacion}",
                    $"idTipo={idTipo}",
                    $"idCliente={idCliente}",
                    $"idEquipo={idEquipo}",
                    $"idAtiende={idAtiende}",
                    $"monto={monto}"
                };

                if (!string.IsNullOrWhiteSpace(nota))
                    queryParams.Add($"nota={Uri.EscapeDataString(nota)}");

                if (fechaFinal.HasValue)
                    queryParams.Add($"fechaFinal={fechaFinal.Value:yyyy-MM-dd}");

                var url = $"{_endpoints.GetEndpoint("api", "Operaciones")}?{string.Join("&", queryParams)}";

                await _logger.LogInformationAsync($"Actualizando operación {idOperacion} en: {url}", "OperacionService", "UpdateOperacionAsync");

                var response = await _http.PutAsync(url, null, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al actualizar operación. Status: {response.StatusCode}, Content: {errorContent}",
                        null, "OperacionService", "UpdateOperacionAsync");
                    return false;
                }

                await _logger.LogInformationAsync($"Operación {idOperacion} actualizada correctamente", "OperacionService", "UpdateOperacionAsync");
                return true;
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al actualizar operación", ex, "OperacionService", "UpdateOperacionAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al actualizar operación", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al actualizar operación", ex, "OperacionService", "UpdateOperacionAsync");
                throw;
            }
        }

        /// <summary>
        /// Reabre una operación limpiando su fechaFinal
        /// </summary>
        public async Task<bool> ReopenOperacionAsync(int idOperacion, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"{_endpoints.GetEndpoint("api", "Operaciones")}/reabrir?idOperacion={idOperacion}";

                await _logger.LogInformationAsync($"Reabriendo operación {idOperacion} en: {url}", "OperacionService", "ReopenOperacionAsync");

                var response = await _http.PutAsync(url, null, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al reabrir operación. Status: {response.StatusCode}, Content: {errorContent}",
                        null, "OperacionService", "ReopenOperacionAsync");
                    return false;
                }

                await _logger.LogInformationAsync($"Operación {idOperacion} reabierta correctamente", "OperacionService", "ReopenOperacionAsync");
                return true;
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al reabrir operación", ex, "OperacionService", "ReopenOperacionAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al reabrir operación", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al reabrir operación", ex, "OperacionService", "ReopenOperacionAsync");
                throw;
            }
        }
    }
}
