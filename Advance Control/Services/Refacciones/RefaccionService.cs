using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.EndPointProvider;
using Advance_Control.Services.Logging;

namespace Advance_Control.Services.Refacciones
{
    /// <summary>
    /// Implementación del servicio de refacciones que se comunica con la API
    /// </summary>
    public class RefaccionService : IRefaccionService
    {
        private readonly HttpClient _http;
        private readonly IApiEndpointProvider _endpoints;
        private readonly ILoggingService _logger;

        public RefaccionService(HttpClient http, IApiEndpointProvider endpoints, ILoggingService logger)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Obtiene una lista de refacciones según los criterios de búsqueda proporcionados
        /// </summary>
        public async Task<List<RefaccionDto>> GetRefaccionesAsync(RefaccionQueryDto? query = null, CancellationToken cancellationToken = default)
        {
            try
            {
                // Construir la URL base usando el endpoint correcto
                var url = _endpoints.GetEndpoint("api", "refaccion_crud");

                // Agregar parámetros de consulta si existen
                if (query != null)
                {
                    var queryParams = new List<string>();

                    if (!string.IsNullOrWhiteSpace(query.Marca))
                        queryParams.Add($"marca={Uri.EscapeDataString(query.Marca)}");

                    if (!string.IsNullOrWhiteSpace(query.Serie))
                        queryParams.Add($"serie={Uri.EscapeDataString(query.Serie)}");

                    if (!string.IsNullOrWhiteSpace(query.Descripcion))
                        queryParams.Add($"descripcion={Uri.EscapeDataString(query.Descripcion)}");

                    if (queryParams.Count > 0)
                    {
                        url = $"{url}?{string.Join("&", queryParams)}";
                    }
                }

                await _logger.LogInformationAsync($"Obteniendo refacciones desde: {url}", "RefaccionService", "GetRefaccionesAsync");

                // Realizar la petición GET
                var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);

                // Verificar si la respuesta fue exitosa
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al obtener refacciones. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "RefaccionService",
                        "GetRefaccionesAsync");
                    return new List<RefaccionDto>();
                }

                // Deserializar la respuesta
                var refacciones = await response.Content.ReadFromJsonAsync<List<RefaccionDto>>(cancellationToken: cancellationToken).ConfigureAwait(false);

                await _logger.LogInformationAsync($"Se obtuvieron {refacciones?.Count ?? 0} refacciones", "RefaccionService", "GetRefaccionesAsync");

                return refacciones ?? new List<RefaccionDto>();
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al obtener refacciones", ex, "RefaccionService", "GetRefaccionesAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al obtener refacciones", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al obtener refacciones", ex, "RefaccionService", "GetRefaccionesAsync");
                throw;
            }
        }

        /// <summary>
        /// Elimina (soft delete) una refacción por su ID
        /// </summary>
        public async Task<bool> DeleteRefaccionAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = _endpoints.GetEndpoint("api", "refaccion_crud");
                url = $"{url}?idRefaccion={id}";

                await _logger.LogInformationAsync($"Eliminando refacción {id} en: {url}", "RefaccionService", "DeleteRefaccionAsync");

                var response = await _http.DeleteAsync(url, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al eliminar refacción. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "RefaccionService",
                        "DeleteRefaccionAsync");
                    return false;
                }

                await _logger.LogInformationAsync($"Refacción {id} eliminada correctamente", "RefaccionService", "DeleteRefaccionAsync");
                return true;
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al eliminar refacción", ex, "RefaccionService", "DeleteRefaccionAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al eliminar refacción", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al eliminar refacción", ex, "RefaccionService", "DeleteRefaccionAsync");
                throw;
            }
        }

        /// <summary>
        /// Actualiza una refacción existente
        /// </summary>
        public async Task<bool> UpdateRefaccionAsync(int id, RefaccionQueryDto query, CancellationToken cancellationToken = default)
        {
            try
            {
                // Construir la URL con parámetros de consulta
                var url = _endpoints.GetEndpoint("api", "refaccion_crud");

                var queryParams = new List<string>
                {
                    $"idRefaccion={id}"
                };

                if (!string.IsNullOrWhiteSpace(query.Marca))
                    queryParams.Add($"marca={Uri.EscapeDataString(query.Marca)}");

                if (!string.IsNullOrWhiteSpace(query.Serie))
                    queryParams.Add($"serie={Uri.EscapeDataString(query.Serie)}");

                if (query.Costo.HasValue)
                    queryParams.Add($"costo={query.Costo.Value}");

                if (!string.IsNullOrWhiteSpace(query.Descripcion))
                    queryParams.Add($"descripcion={Uri.EscapeDataString(query.Descripcion)}");

                queryParams.Add($"estatus={query.Estatus.ToString().ToLower()}");

                url = $"{url}?{string.Join("&", queryParams)}";

                await _logger.LogInformationAsync($"Actualizando refacción {id} en: {url}", "RefaccionService", "UpdateRefaccionAsync");

                var response = await _http.PutAsync(url, null, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al actualizar refacción. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "RefaccionService",
                        "UpdateRefaccionAsync");
                    return false;
                }

                await _logger.LogInformationAsync($"Refacción {id} actualizada correctamente", "RefaccionService", "UpdateRefaccionAsync");
                return true;
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al actualizar refacción", ex, "RefaccionService", "UpdateRefaccionAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al actualizar refacción", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al actualizar refacción", ex, "RefaccionService", "UpdateRefaccionAsync");
                throw;
            }
        }

        /// <summary>
        /// Crea una nueva refacción
        /// </summary>
        public async Task<bool> CreateRefaccionAsync(string? marca, string? serie, double? costo, string? descripcion, bool estatus = true, CancellationToken cancellationToken = default)
        {
            try
            {
                // Construir la URL base usando el endpoint correcto
                var url = _endpoints.GetEndpoint("api", "refaccion_crud");

                // Agregar parámetros de consulta
                var queryParams = new List<string>
                {
                    $"estatus={estatus.ToString().ToLower()}"
                };

                if (!string.IsNullOrWhiteSpace(marca))
                    queryParams.Add($"marca={Uri.EscapeDataString(marca)}");

                if (!string.IsNullOrWhiteSpace(serie))
                    queryParams.Add($"serie={Uri.EscapeDataString(serie)}");

                if (costo.HasValue)
                    queryParams.Add($"costo={costo.Value}");

                if (!string.IsNullOrWhiteSpace(descripcion))
                    queryParams.Add($"descripcion={Uri.EscapeDataString(descripcion)}");

                url = $"{url}?{string.Join("&", queryParams)}";

                await _logger.LogInformationAsync($"Creando refacción en: {url}", "RefaccionService", "CreateRefaccionAsync");

                // Realizar la petición POST
                var response = await _http.PostAsync(url, null, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al crear refacción. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "RefaccionService",
                        "CreateRefaccionAsync");
                    return false;
                }

                await _logger.LogInformationAsync("Refacción creada correctamente", "RefaccionService", "CreateRefaccionAsync");
                return true;
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al crear refacción", ex, "RefaccionService", "CreateRefaccionAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al crear refacción", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al crear refacción", ex, "RefaccionService", "CreateRefaccionAsync");
                throw;
            }
        }
    }
}
