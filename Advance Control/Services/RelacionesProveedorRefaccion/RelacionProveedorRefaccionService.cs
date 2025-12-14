using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.EndPointProvider;
using Advance_Control.Services.Logging;

namespace Advance_Control.Services.RelacionesProveedorRefaccion
{
    /// <summary>
    /// Implementación del servicio de relaciones proveedor-refacción que se comunica con la API
    /// </summary>
    public class RelacionProveedorRefaccionService : IRelacionProveedorRefaccionService
    {
        private readonly HttpClient _http;
        private readonly IApiEndpointProvider _endpoints;
        private readonly ILoggingService _logger;

        public RelacionProveedorRefaccionService(HttpClient http, IApiEndpointProvider endpoints, ILoggingService logger)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Obtiene una lista de relaciones refacción para un ID de proveedor
        /// </summary>
        public async Task<List<RelacionProveedorRefaccionDto>> GetRelacionesAsync(int idProveedor, int idRefaccion = 0, CancellationToken cancellationToken = default)
        {
            try
            {
                // Construir la URL base usando el endpoint correcto
                var url = _endpoints.GetEndpoint("api", "RelacionProveedorRefaccion");

                // Agregar parámetros de consulta
                var queryParams = new List<string>();

                if (idProveedor > 0)
                    queryParams.Add($"idProveedor={idProveedor}");

                if (idRefaccion > 0)
                    queryParams.Add($"idRefaccion={idRefaccion}");

                if (queryParams.Count > 0)
                {
                    url = $"{url}?{string.Join("&", queryParams)}";
                }

                await _logger.LogInformationAsync($"Obteniendo relaciones proveedor-refacción desde: {url}", "RelacionProveedorRefaccionService", "GetRelacionesAsync");

                // Realizar la petición GET
                var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);

                // Verificar si la respuesta fue exitosa
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al obtener relaciones proveedor-refacción. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "RelacionProveedorRefaccionService",
                        "GetRelacionesAsync");
                    return new List<RelacionProveedorRefaccionDto>();
                }

                // Deserializar la respuesta
                var relaciones = await response.Content.ReadFromJsonAsync<List<RelacionProveedorRefaccionDto>>(cancellationToken: cancellationToken).ConfigureAwait(false);

                await _logger.LogInformationAsync($"Se obtuvieron {relaciones?.Count ?? 0} relaciones proveedor-refacción", "RelacionProveedorRefaccionService", "GetRelacionesAsync");

                return relaciones ?? new List<RelacionProveedorRefaccionDto>();
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al obtener relaciones proveedor-refacción", ex, "RelacionProveedorRefaccionService", "GetRelacionesAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al obtener relaciones proveedor-refacción", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al obtener relaciones proveedor-refacción", ex, "RelacionProveedorRefaccionService", "GetRelacionesAsync");
                throw;
            }
        }

        /// <summary>
        /// Elimina (soft delete) una relación proveedor-refacción
        /// </summary>
        public async Task<bool> DeleteRelacionAsync(int idRelacionProveedor, CancellationToken cancellationToken = default)
        {
            try
            {
                if (idRelacionProveedor <= 0)
                {
                    await _logger.LogWarningAsync("El idRelacionProveedor debe ser mayor que 0 para eliminar relación", "RelacionProveedorRefaccionService", "DeleteRelacionAsync");
                    return false;
                }

                // Construir la URL usando el endpoint correcto
                var url = _endpoints.GetEndpoint("api", "RelacionProveedorRefaccion");
                url = $"{url}?idRelacionProveedor={idRelacionProveedor}";

                await _logger.LogInformationAsync($"Eliminando relación proveedor-refacción desde: {url}", "RelacionProveedorRefaccionService", "DeleteRelacionAsync");

                // Realizar la petición DELETE
                var response = await _http.DeleteAsync(url, cancellationToken).ConfigureAwait(false);

                // Verificar si la respuesta fue exitosa
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al eliminar relación proveedor-refacción. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "RelacionProveedorRefaccionService",
                        "DeleteRelacionAsync");
                    return false;
                }

                await _logger.LogInformationAsync($"Relación proveedor-refacción eliminada exitosamente: idRelacionProveedor={idRelacionProveedor}", "RelacionProveedorRefaccionService", "DeleteRelacionAsync");
                return true;
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al eliminar relación proveedor-refacción", ex, "RelacionProveedorRefaccionService", "DeleteRelacionAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al eliminar relación proveedor-refacción", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al eliminar relación proveedor-refacción", ex, "RelacionProveedorRefaccionService", "DeleteRelacionAsync");
                throw;
            }
        }

        /// <summary>
        /// Actualiza la nota de una relación proveedor-refacción
        /// </summary>
        public async Task<bool> UpdateNotaAsync(int idRelacionProveedor, string? nota, CancellationToken cancellationToken = default)
        {
            try
            {
                if (idRelacionProveedor <= 0)
                {
                    await _logger.LogWarningAsync("El idRelacionProveedor debe ser mayor que 0 para actualizar la nota", "RelacionProveedorRefaccionService", "UpdateNotaAsync");
                    return false;
                }

                // Construir la URL usando el endpoint correcto para actualizar nota
                var url = _endpoints.GetEndpoint("api", "RelacionProveedorRefaccion/nota");
                url = $"{url}?idRelacionProveedor={idRelacionProveedor}";
                
                // Agregar el parámetro nota solo si no está vacío
                if (!string.IsNullOrWhiteSpace(nota))
                {
                    url = $"{url}&nota={Uri.EscapeDataString(nota)}";
                }

                await _logger.LogInformationAsync($"Actualizando nota de relación proveedor-refacción: {url}", "RelacionProveedorRefaccionService", "UpdateNotaAsync");

                // Realizar la petición PUT
                var response = await _http.PutAsync(url, null, cancellationToken).ConfigureAwait(false);

                // Verificar si la respuesta fue exitosa
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al actualizar nota de relación proveedor-refacción. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "RelacionProveedorRefaccionService",
                        "UpdateNotaAsync");
                    return false;
                }

                await _logger.LogInformationAsync($"Nota actualizada exitosamente: idRelacionProveedor={idRelacionProveedor}", "RelacionProveedorRefaccionService", "UpdateNotaAsync");
                return true;
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al actualizar nota de relación proveedor-refacción", ex, "RelacionProveedorRefaccionService", "UpdateNotaAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al actualizar nota de relación proveedor-refacción", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al actualizar nota de relación proveedor-refacción", ex, "RelacionProveedorRefaccionService", "UpdateNotaAsync");
                throw;
            }
        }

        /// <summary>
        /// Actualiza el precio de una relación proveedor-refacción
        /// </summary>
        public async Task<bool> UpdatePrecioAsync(int idRelacionProveedor, double precio, CancellationToken cancellationToken = default)
        {
            try
            {
                if (idRelacionProveedor <= 0)
                {
                    await _logger.LogWarningAsync("El idRelacionProveedor debe ser mayor que 0 para actualizar el precio", "RelacionProveedorRefaccionService", "UpdatePrecioAsync");
                    return false;
                }

                if (precio <= 0)
                {
                    await _logger.LogWarningAsync("El precio debe ser mayor que 0 para actualizar", "RelacionProveedorRefaccionService", "UpdatePrecioAsync");
                    return false;
                }

                // Construir la URL usando el endpoint correcto para actualizar precio
                var url = _endpoints.GetEndpoint("api", "RelacionProveedorRefaccion/precio");
                url = $"{url}?idRelacionProveedor={idRelacionProveedor}&precio={precio}";

                await _logger.LogInformationAsync($"Actualizando precio de relación proveedor-refacción: {url}", "RelacionProveedorRefaccionService", "UpdatePrecioAsync");

                // Realizar la petición PUT
                var response = await _http.PutAsync(url, null, cancellationToken).ConfigureAwait(false);

                // Verificar si la respuesta fue exitosa
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al actualizar precio de relación proveedor-refacción. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "RelacionProveedorRefaccionService",
                        "UpdatePrecioAsync");
                    return false;
                }

                await _logger.LogInformationAsync($"Precio actualizado exitosamente: idRelacionProveedor={idRelacionProveedor}, precio={precio}", "RelacionProveedorRefaccionService", "UpdatePrecioAsync");
                return true;
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al actualizar precio de relación proveedor-refacción", ex, "RelacionProveedorRefaccionService", "UpdatePrecioAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al actualizar precio de relación proveedor-refacción", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al actualizar precio de relación proveedor-refacción", ex, "RelacionProveedorRefaccionService", "UpdatePrecioAsync");
                throw;
            }
        }

        /// <summary>
        /// Crea una nueva relación proveedor-refacción
        /// </summary>
        public async Task<bool> CreateRelacionAsync(int idProveedor, int idRefaccion, double precio, string? nota, CancellationToken cancellationToken = default)
        {
            try
            {
                if (idProveedor <= 0)
                {
                    await _logger.LogWarningAsync("El idProveedor debe ser mayor que 0 para crear relación", "RelacionProveedorRefaccionService", "CreateRelacionAsync");
                    return false;
                }

                if (idRefaccion <= 0)
                {
                    await _logger.LogWarningAsync("El idRefaccion debe ser mayor que 0 para crear relación", "RelacionProveedorRefaccionService", "CreateRelacionAsync");
                    return false;
                }

                if (precio <= 0)
                {
                    await _logger.LogWarningAsync("El precio debe ser mayor que 0 para crear relación", "RelacionProveedorRefaccionService", "CreateRelacionAsync");
                    return false;
                }

                // Construir la URL usando el endpoint correcto
                var url = _endpoints.GetEndpoint("api", "RelacionProveedorRefaccion");
                url = $"{url}?idProveedor={idProveedor}&idRefaccion={idRefaccion}&precio={precio}";
                
                // Agregar el parámetro nota solo si no está vacío
                if (!string.IsNullOrWhiteSpace(nota))
                {
                    url = $"{url}&nota={Uri.EscapeDataString(nota)}";
                }

                await _logger.LogInformationAsync($"Creando relación proveedor-refacción: {url}", "RelacionProveedorRefaccionService", "CreateRelacionAsync");

                // Realizar la petición POST
                var response = await _http.PostAsync(url, null, cancellationToken).ConfigureAwait(false);

                // Verificar si la respuesta fue exitosa a nivel HTTP
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al crear relación proveedor-refacción. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "RelacionProveedorRefaccionService",
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
                            "RelacionProveedorRefaccionService",
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
                        "RelacionProveedorRefaccionService",
                        "CreateRelacionAsync");
                }

                await _logger.LogInformationAsync($"Relación proveedor-refacción creada exitosamente: idProveedor={idProveedor}, idRefaccion={idRefaccion}, precio={precio}", "RelacionProveedorRefaccionService", "CreateRelacionAsync");
                return true;
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al crear relación proveedor-refacción", ex, "RelacionProveedorRefaccionService", "CreateRelacionAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al crear relación proveedor-refacción", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al crear relación proveedor-refacción", ex, "RelacionProveedorRefaccionService", "CreateRelacionAsync");
                throw;
            }
        }
    }
}
