using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.EndPointProvider;
using Advance_Control.Services.Logging;

namespace Advance_Control.Services.RelacionesRefaccionEquipo
{
    /// <summary>
    /// Implementación del servicio de relaciones refacción-equipo que se comunica con la API
    /// </summary>
    public class RelacionRefaccionEquipoService : IRelacionRefaccionEquipoService
    {
        private readonly HttpClient _http;
        private readonly IApiEndpointProvider _endpoints;
        private readonly ILoggingService _logger;

        public RelacionRefaccionEquipoService(HttpClient http, IApiEndpointProvider endpoints, ILoggingService logger)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Obtiene una lista de relaciones equipo para un ID de refacción
        /// </summary>
        public async Task<List<RelacionEquipoDto>> GetRelacionesAsync(int idRefaccion, int idEquipo = 0, CancellationToken cancellationToken = default)
        {
            try
            {
                // Construir la URL base usando el endpoint correcto con la ruta /equipos
                var url = _endpoints.GetEndpoint("api", "RelacionesRefaccionEquipo/equipos");

                // Agregar parámetros de consulta
                var queryParams = new List<string>();

                if (idRefaccion > 0)
                    queryParams.Add($"idRefaccion={idRefaccion}");

                if (idEquipo > 0)
                    queryParams.Add($"idEquipo={idEquipo}");

                if (queryParams.Count > 0)
                {
                    url = $"{url}?{string.Join("&", queryParams)}";
                }

                await _logger.LogInformationAsync($"Obteniendo relaciones refacción-equipo desde: {url}", "RelacionRefaccionEquipoService", "GetRelacionesAsync");

                // Realizar la petición GET
                var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);

                // Verificar si la respuesta fue exitosa
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al obtener relaciones refacción-equipo. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "RelacionRefaccionEquipoService",
                        "GetRelacionesAsync");
                    return new List<RelacionEquipoDto>();
                }

                // Deserializar la respuesta
                var relaciones = await response.Content.ReadFromJsonAsync<List<RelacionEquipoDto>>(cancellationToken: cancellationToken).ConfigureAwait(false);

                await _logger.LogInformationAsync($"Se obtuvieron {relaciones?.Count ?? 0} relaciones refacción-equipo", "RelacionRefaccionEquipoService", "GetRelacionesAsync");

                return relaciones ?? new List<RelacionEquipoDto>();
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al obtener relaciones refacción-equipo", ex, "RelacionRefaccionEquipoService", "GetRelacionesAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al obtener relaciones refacción-equipo", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al obtener relaciones refacción-equipo", ex, "RelacionRefaccionEquipoService", "GetRelacionesAsync");
                throw;
            }
        }

        /// <summary>
        /// Elimina (soft delete) una relación refacción-equipo
        /// </summary>
        public async Task<bool> DeleteRelacionAsync(int idRelacionRefaccion, CancellationToken cancellationToken = default)
        {
            try
            {
                if (idRelacionRefaccion <= 0)
                {
                    await _logger.LogWarningAsync("El idRelacionRefaccion debe ser mayor que 0 para eliminar relación", "RelacionRefaccionEquipoService", "DeleteRelacionAsync");
                    return false;
                }

                // Construir la URL usando el endpoint correcto
                var url = _endpoints.GetEndpoint("api", "RelacionesRefaccionEquipo");
                url = $"{url}?idRelacionRefaccion={idRelacionRefaccion}";

                await _logger.LogInformationAsync($"Eliminando relación refacción-equipo desde: {url}", "RelacionRefaccionEquipoService", "DeleteRelacionAsync");

                // Realizar la petición DELETE
                var response = await _http.DeleteAsync(url, cancellationToken).ConfigureAwait(false);

                // Verificar si la respuesta fue exitosa
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al eliminar relación refacción-equipo. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "RelacionRefaccionEquipoService",
                        "DeleteRelacionAsync");
                    return false;
                }

                await _logger.LogInformationAsync($"Relación refacción-equipo eliminada exitosamente: idRelacionRefaccion={idRelacionRefaccion}", "RelacionRefaccionEquipoService", "DeleteRelacionAsync");
                return true;
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al eliminar relación refacción-equipo", ex, "RelacionRefaccionEquipoService", "DeleteRelacionAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al eliminar relación refacción-equipo", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al eliminar relación refacción-equipo", ex, "RelacionRefaccionEquipoService", "DeleteRelacionAsync");
                throw;
            }
        }

        /// <summary>
        /// Actualiza la nota de una relación refacción-equipo
        /// </summary>
        public async Task<bool> UpdateNotaAsync(int idRelacionRefaccion, string? nota, CancellationToken cancellationToken = default)
        {
            try
            {
                if (idRelacionRefaccion <= 0)
                {
                    await _logger.LogWarningAsync("El idRelacionRefaccion debe ser mayor que 0 para actualizar la nota", "RelacionRefaccionEquipoService", "UpdateNotaAsync");
                    return false;
                }

                // Construir la URL usando el endpoint correcto para actualizar nota
                var url = _endpoints.GetEndpoint("api", "RelacionesRefaccionEquipo/nota");
                url = $"{url}?idRelacionRefaccion={idRelacionRefaccion}";
                
                // Agregar el parámetro nota solo si no está vacío
                if (!string.IsNullOrWhiteSpace(nota))
                {
                    url = $"{url}&nota={Uri.EscapeDataString(nota)}";
                }

                await _logger.LogInformationAsync($"Actualizando nota de relación refacción-equipo: {url}", "RelacionRefaccionEquipoService", "UpdateNotaAsync");

                // Realizar la petición PUT
                var response = await _http.PutAsync(url, null, cancellationToken).ConfigureAwait(false);

                // Verificar si la respuesta fue exitosa
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al actualizar nota de relación refacción-equipo. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "RelacionRefaccionEquipoService",
                        "UpdateNotaAsync");
                    return false;
                }

                await _logger.LogInformationAsync($"Nota actualizada exitosamente: idRelacionRefaccion={idRelacionRefaccion}", "RelacionRefaccionEquipoService", "UpdateNotaAsync");
                return true;
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al actualizar nota de relación refacción-equipo", ex, "RelacionRefaccionEquipoService", "UpdateNotaAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al actualizar nota de relación refacción-equipo", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al actualizar nota de relación refacción-equipo", ex, "RelacionRefaccionEquipoService", "UpdateNotaAsync");
                throw;
            }
        }

        /// <summary>
        /// Crea una nueva relación refacción-equipo
        /// </summary>
        public async Task<bool> CreateRelacionAsync(int idRefaccion, int idEquipo, string? nota, CancellationToken cancellationToken = default)
        {
            try
            {
                if (idRefaccion <= 0)
                {
                    await _logger.LogWarningAsync("El idRefaccion debe ser mayor que 0 para crear relación", "RelacionRefaccionEquipoService", "CreateRelacionAsync");
                    return false;
                }

                if (idEquipo <= 0)
                {
                    await _logger.LogWarningAsync("El idEquipo debe ser mayor que 0 para crear relación", "RelacionRefaccionEquipoService", "CreateRelacionAsync");
                    return false;
                }

                // Construir la URL usando el endpoint correcto
                var url = _endpoints.GetEndpoint("api", "RelacionesRefaccionEquipo");
                url = $"{url}?idRefaccion={idRefaccion}&idEquipo={idEquipo}";
                
                // Agregar el parámetro nota solo si no está vacío
                if (!string.IsNullOrWhiteSpace(nota))
                {
                    url = $"{url}&nota={Uri.EscapeDataString(nota)}";
                }

                await _logger.LogInformationAsync($"Creando relación refacción-equipo: {url}", "RelacionRefaccionEquipoService", "CreateRelacionAsync");

                // Realizar la petición POST
                var response = await _http.PostAsync(url, null, cancellationToken).ConfigureAwait(false);

                // Verificar si la respuesta fue exitosa a nivel HTTP
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al crear relación refacción-equipo. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "RelacionRefaccionEquipoService",
                        "CreateRelacionAsync");
                    return false;
                }

                // Verificar el campo success en el cuerpo de la respuesta
                // La API puede retornar HTTP 200 pero con success=false si la relación ya existe
                try
                {
                    var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse>(cancellationToken: cancellationToken).ConfigureAwait(false);
                    if (apiResponse != null && !apiResponse.Success)
                    {
                        await _logger.LogWarningAsync(
                            $"La API retornó success=false: {apiResponse.Message}",
                            "RelacionRefaccionEquipoService",
                            "CreateRelacionAsync");
                        return false;
                    }
                }
                catch (System.Text.Json.JsonException ex)
                {
                    // Si la respuesta no es JSON válido o no tiene el formato esperado,
                    // asumimos que la operación fue exitosa (HTTP 200 OK)
                    await _logger.LogWarningAsync(
                        $"No se pudo parsear la respuesta JSON: {ex.Message}",
                        "RelacionRefaccionEquipoService",
                        "CreateRelacionAsync");
                }

                await _logger.LogInformationAsync($"Relación refacción-equipo creada exitosamente: idRefaccion={idRefaccion}, idEquipo={idEquipo}", "RelacionRefaccionEquipoService", "CreateRelacionAsync");
                return true;
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al crear relación refacción-equipo", ex, "RelacionRefaccionEquipoService", "CreateRelacionAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al crear relación refacción-equipo", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al crear relación refacción-equipo", ex, "RelacionRefaccionEquipoService", "CreateRelacionAsync");
                throw;
            }
        }
    }
}
