using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.EndPointProvider;
using Advance_Control.Services.Logging;

namespace Advance_Control.Services.RelacionesOperacionProveedorRefaccion
{
    /// <summary>
    /// Implementación del servicio de relaciones operación-proveedor-refacción que se comunica con la API
    /// </summary>
    public class RelacionOperacionProveedorRefaccionService : IRelacionOperacionProveedorRefaccionService
    {
        private readonly HttpClient _http;
        private readonly IApiEndpointProvider _endpoints;
        private readonly ILoggingService _logger;

        public RelacionOperacionProveedorRefaccionService(HttpClient http, IApiEndpointProvider endpoints, ILoggingService logger)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Obtiene una lista de relaciones refacción para un ID de operación
        /// </summary>
        public async Task<List<RelacionOperacionProveedorRefaccionDto>> GetRelacionesAsync(int idOperacion, CancellationToken cancellationToken = default)
        {
            try
            {
                // Construir la URL base usando el endpoint correcto
                var url = _endpoints.GetEndpoint("api", "RelacionOperacionProveedorRefaccion");

                // Agregar parámetros de consulta
                if (idOperacion > 0)
                {
                    url = $"{url}?idOperacion={idOperacion}";
                }

                await _logger.LogInformationAsync($"Obteniendo relaciones operación-proveedor-refacción desde: {url}", "RelacionOperacionProveedorRefaccionService", "GetRelacionesAsync");

                // Realizar la petición GET
                var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);

                // Verificar si la respuesta fue exitosa
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al obtener relaciones operación-proveedor-refacción. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "RelacionOperacionProveedorRefaccionService",
                        "GetRelacionesAsync");
                    return new List<RelacionOperacionProveedorRefaccionDto>();
                }

                // Deserializar la respuesta
                var relaciones = await response.Content.ReadFromJsonAsync<List<RelacionOperacionProveedorRefaccionDto>>(cancellationToken: cancellationToken).ConfigureAwait(false);

                await _logger.LogInformationAsync($"Se obtuvieron {relaciones?.Count ?? 0} relaciones operación-proveedor-refacción", "RelacionOperacionProveedorRefaccionService", "GetRelacionesAsync");

                return relaciones ?? new List<RelacionOperacionProveedorRefaccionDto>();
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al obtener relaciones operación-proveedor-refacción", ex, "RelacionOperacionProveedorRefaccionService", "GetRelacionesAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al obtener relaciones operación-proveedor-refacción", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al obtener relaciones operación-proveedor-refacción", ex, "RelacionOperacionProveedorRefaccionService", "GetRelacionesAsync");
                throw;
            }
        }

        /// <summary>
        /// Elimina (soft delete) una relación operación-proveedor-refacción
        /// </summary>
        public async Task<bool> DeleteRelacionAsync(int idRelacionOperacionProveedorRefaccion, CancellationToken cancellationToken = default)
        {
            try
            {
                if (idRelacionOperacionProveedorRefaccion <= 0)
                {
                    await _logger.LogWarningAsync("El idRelacionOperacionProveedorRefaccion debe ser mayor que 0 para eliminar relación", "RelacionOperacionProveedorRefaccionService", "DeleteRelacionAsync");
                    return false;
                }

                // Construir la URL usando el endpoint correcto
                var url = _endpoints.GetEndpoint("api", "RelacionOperacionProveedorRefaccion");
                url = $"{url}?idRelacionOperacionProveedorRefaccion={idRelacionOperacionProveedorRefaccion}";

                await _logger.LogInformationAsync($"Eliminando relación operación-proveedor-refacción desde: {url}", "RelacionOperacionProveedorRefaccionService", "DeleteRelacionAsync");

                // Realizar la petición DELETE
                var response = await _http.DeleteAsync(url, cancellationToken).ConfigureAwait(false);

                // Verificar si la respuesta fue exitosa
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al eliminar relación operación-proveedor-refacción. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "RelacionOperacionProveedorRefaccionService",
                        "DeleteRelacionAsync");
                    return false;
                }

                await _logger.LogInformationAsync($"Relación operación-proveedor-refacción eliminada exitosamente: idRelacionOperacionProveedorRefaccion={idRelacionOperacionProveedorRefaccion}", "RelacionOperacionProveedorRefaccionService", "DeleteRelacionAsync");
                return true;
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al eliminar relación operación-proveedor-refacción", ex, "RelacionOperacionProveedorRefaccionService", "DeleteRelacionAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al eliminar relación operación-proveedor-refacción", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al eliminar relación operación-proveedor-refacción", ex, "RelacionOperacionProveedorRefaccionService", "DeleteRelacionAsync");
                throw;
            }
        }

        /// <summary>
        /// Actualiza la nota de una relación operación-proveedor-refacción
        /// </summary>
        public async Task<bool> UpdateNotaAsync(int idRelacionOperacionProveedorRefaccion, string? nota, CancellationToken cancellationToken = default)
        {
            try
            {
                if (idRelacionOperacionProveedorRefaccion <= 0)
                {
                    await _logger.LogWarningAsync("El idRelacionOperacionProveedorRefaccion debe ser mayor que 0 para actualizar la nota", "RelacionOperacionProveedorRefaccionService", "UpdateNotaAsync");
                    return false;
                }

                // Construir la URL usando el endpoint correcto para actualizar nota
                var url = _endpoints.GetEndpoint("api", "RelacionOperacionProveedorRefaccion/nota");
                url = $"{url}?idRelacionOperacionProveedorRefaccion={idRelacionOperacionProveedorRefaccion}";
                
                // Agregar el parámetro nota solo si no está vacío
                if (!string.IsNullOrWhiteSpace(nota))
                {
                    url = $"{url}&nota={Uri.EscapeDataString(nota)}";
                }

                await _logger.LogInformationAsync($"Actualizando nota de relación operación-proveedor-refacción: {url}", "RelacionOperacionProveedorRefaccionService", "UpdateNotaAsync");

                // Realizar la petición PUT
                var response = await _http.PutAsync(url, null, cancellationToken).ConfigureAwait(false);

                // Verificar si la respuesta fue exitosa
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al actualizar nota de relación operación-proveedor-refacción. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "RelacionOperacionProveedorRefaccionService",
                        "UpdateNotaAsync");
                    return false;
                }

                await _logger.LogInformationAsync($"Nota actualizada exitosamente: idRelacionOperacionProveedorRefaccion={idRelacionOperacionProveedorRefaccion}", "RelacionOperacionProveedorRefaccionService", "UpdateNotaAsync");
                return true;
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al actualizar nota de relación operación-proveedor-refacción", ex, "RelacionOperacionProveedorRefaccionService", "UpdateNotaAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al actualizar nota de relación operación-proveedor-refacción", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al actualizar nota de relación operación-proveedor-refacción", ex, "RelacionOperacionProveedorRefaccionService", "UpdateNotaAsync");
                throw;
            }
        }

        /// <summary>
        /// Crea una nueva relación operación-proveedor-refacción
        /// </summary>
        public async Task<bool> CreateRelacionAsync(int idOperacion, int idProveedorRefaccion, double precio, string? nota, CancellationToken cancellationToken = default)
        {
            try
            {
                if (idOperacion <= 0)
                {
                    await _logger.LogWarningAsync("El idOperacion debe ser mayor que 0 para crear relación", "RelacionOperacionProveedorRefaccionService", "CreateRelacionAsync");
                    return false;
                }

                if (idProveedorRefaccion <= 0)
                {
                    await _logger.LogWarningAsync("El idProveedorRefaccion debe ser mayor que 0 para crear relación", "RelacionOperacionProveedorRefaccionService", "CreateRelacionAsync");
                    return false;
                }

                if (precio <= 0)
                {
                    await _logger.LogWarningAsync("El precio debe ser mayor que 0 para crear relación", "RelacionOperacionProveedorRefaccionService", "CreateRelacionAsync");
                    return false;
                }

                // Construir la URL usando el endpoint correcto
                var url = _endpoints.GetEndpoint("api", "RelacionOperacionProveedorRefaccion");
                url = $"{url}?idOperacion={idOperacion}&idProveedorRefaccion={idProveedorRefaccion}&precio={precio}";
                
                // Agregar el parámetro nota solo si no está vacío
                if (!string.IsNullOrWhiteSpace(nota))
                {
                    url = $"{url}&nota={Uri.EscapeDataString(nota)}";
                }

                await _logger.LogInformationAsync($"Creando relación operación-proveedor-refacción: {url}", "RelacionOperacionProveedorRefaccionService", "CreateRelacionAsync");

                // Realizar la petición POST
                var response = await _http.PostAsync(url, null, cancellationToken).ConfigureAwait(false);

                // Verificar si la respuesta fue exitosa a nivel HTTP
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al crear relación operación-proveedor-refacción. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "RelacionOperacionProveedorRefaccionService",
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
                            "RelacionOperacionProveedorRefaccionService",
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
                        "RelacionOperacionProveedorRefaccionService",
                        "CreateRelacionAsync");
                }

                await _logger.LogInformationAsync($"Relación operación-proveedor-refacción creada exitosamente: idOperacion={idOperacion}, idProveedorRefaccion={idProveedorRefaccion}, precio={precio}", "RelacionOperacionProveedorRefaccionService", "CreateRelacionAsync");
                return true;
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al crear relación operación-proveedor-refacción", ex, "RelacionOperacionProveedorRefaccionService", "CreateRelacionAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al crear relación operación-proveedor-refacción", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al crear relación operación-proveedor-refacción", ex, "RelacionOperacionProveedorRefaccionService", "CreateRelacionAsync");
                throw;
            }
        }
    }
}
