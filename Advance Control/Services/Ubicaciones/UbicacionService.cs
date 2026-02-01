using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.EndPointProvider;
using Advance_Control.Services.Logging;

namespace Advance_Control.Services.Ubicaciones
{
    /// <summary>
    /// Implementación del servicio de ubicaciones de Google Maps
    /// </summary>
    public class UbicacionService : IUbicacionService
    {
        private readonly HttpClient _http;
        private readonly IApiEndpointProvider _endpoints;
        private readonly ILoggingService _logger;

        public UbicacionService(HttpClient http, IApiEndpointProvider endpoints, ILoggingService logger)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Obtiene todas las ubicaciones activas
        /// </summary>
        public async Task<List<UbicacionDto>> GetUbicacionesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var url = _endpoints.GetEndpoint("api", "Ubicacion");

                await _logger.LogInformationAsync($"Obteniendo ubicaciones desde: {url}", "UbicacionService", "GetUbicacionesAsync");

                var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al obtener ubicaciones. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "UbicacionService",
                        "GetUbicacionesAsync");
                    return new List<UbicacionDto>();
                }

                var ubicaciones = await response.Content.ReadFromJsonAsync<List<UbicacionDto>>(cancellationToken: cancellationToken).ConfigureAwait(false);

                await _logger.LogInformationAsync($"Se obtuvieron {ubicaciones?.Count ?? 0} ubicaciones", "UbicacionService", "GetUbicacionesAsync");

                return ubicaciones ?? new List<UbicacionDto>();
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al obtener ubicaciones", ex, "UbicacionService", "GetUbicacionesAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al obtener ubicaciones", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al obtener ubicaciones", ex, "UbicacionService", "GetUbicacionesAsync");
                throw;
            }
        }

        /// <summary>
        /// Obtiene una ubicación específica por su ID
        /// </summary>
        public async Task<UbicacionDto?> GetUbicacionByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                if (id <= 0)
                {
                    await _logger.LogWarningAsync($"ID de ubicación inválido: {id}", "UbicacionService", "GetUbicacionByIdAsync");
                    return null;
                }

                var url = _endpoints.GetEndpoint("api", "Ubicacion", id.ToString());

                await _logger.LogInformationAsync($"Obteniendo ubicación con ID {id} desde: {url}", "UbicacionService", "GetUbicacionByIdAsync");

                var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    await _logger.LogWarningAsync($"Ubicación con ID {id} no encontrada", "UbicacionService", "GetUbicacionByIdAsync");
                    return null;
                }

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al obtener ubicación. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "UbicacionService",
                        "GetUbicacionByIdAsync");
                    return null;
                }

                var ubicacion = await response.Content.ReadFromJsonAsync<UbicacionDto>(cancellationToken: cancellationToken).ConfigureAwait(false);

                await _logger.LogInformationAsync($"Ubicación obtenida: {ubicacion?.Nombre}", "UbicacionService", "GetUbicacionByIdAsync");

                return ubicacion;
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al obtener ubicación por ID", ex, "UbicacionService", "GetUbicacionByIdAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al obtener ubicación", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al obtener ubicación por ID", ex, "UbicacionService", "GetUbicacionByIdAsync");
                throw;
            }
        }

        /// <summary>
        /// Busca una ubicación por nombre exacto
        /// </summary>
        public async Task<UbicacionDto?> GetUbicacionByNombreAsync(string nombre, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(nombre))
                {
                    await _logger.LogWarningAsync("Nombre de ubicación vacío o nulo", "UbicacionService", "GetUbicacionByNombreAsync");
                    return null;
                }

                var url = _endpoints.GetEndpoint("api", "Ubicacion", "buscar", Uri.EscapeDataString(nombre));

                await _logger.LogInformationAsync($"Buscando ubicación con nombre '{nombre}' desde: {url}", "UbicacionService", "GetUbicacionByNombreAsync");

                var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    await _logger.LogWarningAsync($"Ubicación con nombre '{nombre}' no encontrada", "UbicacionService", "GetUbicacionByNombreAsync");
                    return null;
                }

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al buscar ubicación. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "UbicacionService",
                        "GetUbicacionByNombreAsync");
                    return null;
                }

                var ubicacion = await response.Content.ReadFromJsonAsync<UbicacionDto>(cancellationToken: cancellationToken).ConfigureAwait(false);

                await _logger.LogInformationAsync($"Ubicación encontrada: {ubicacion?.Nombre}", "UbicacionService", "GetUbicacionByNombreAsync");

                return ubicacion;
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al buscar ubicación por nombre", ex, "UbicacionService", "GetUbicacionByNombreAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al buscar ubicación", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al buscar ubicación por nombre", ex, "UbicacionService", "GetUbicacionByNombreAsync");
                throw;
            }
        }

        /// <summary>
        /// Crea una nueva ubicación
        /// </summary>
        public async Task<ApiResponse> CreateUbicacionAsync(UbicacionDto ubicacion, CancellationToken cancellationToken = default)
        {
            try
            {
                if (ubicacion == null)
                {
                    await _logger.LogWarningAsync("Ubicación nula al intentar crear", "UbicacionService", "CreateUbicacionAsync");
                    return new ApiResponse { Success = false, Message = "Ubicación no puede ser nula" };
                }

                // Validaciones requeridas según el endpoint
                if (string.IsNullOrWhiteSpace(ubicacion.Nombre))
                {
                    return new ApiResponse { Success = false, Message = "El nombre es requerido" };
                }

                if (!ubicacion.Latitud.HasValue)
                {
                    return new ApiResponse { Success = false, Message = "La latitud es requerida" };
                }

                if (!ubicacion.Longitud.HasValue)
                {
                    return new ApiResponse { Success = false, Message = "La longitud es requerida" };
                }

                var url = _endpoints.GetEndpoint("api", "Ubicacion");

                await _logger.LogInformationAsync($"Creando ubicación '{ubicacion.Nombre}' en: {url}", "UbicacionService", "CreateUbicacionAsync");

                var json = JsonSerializer.Serialize(ubicacion);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _http.PostAsync(url, content, cancellationToken).ConfigureAwait(false);

                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    await _logger.LogErrorAsync(
                        $"Error al crear ubicación. Status: {response.StatusCode}, Content: {responseContent}",
                        null,
                        "UbicacionService",
                        "CreateUbicacionAsync");
                    return new ApiResponse { Success = false, Message = $"Error al crear ubicación: {response.StatusCode}" };
                }

                await _logger.LogInformationAsync($"Ubicación '{ubicacion.Nombre}' creada exitosamente", "UbicacionService", "CreateUbicacionAsync");

                // Try to parse the response as ApiResponse
                try
                {
                    var apiResponse = JsonSerializer.Deserialize<ApiResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return apiResponse ?? new ApiResponse { Success = true, Message = "Ubicación creada exitosamente" };
                }
                catch
                {
                    return new ApiResponse { Success = true, Message = "Ubicación creada exitosamente" };
                }
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al crear ubicación", ex, "UbicacionService", "CreateUbicacionAsync");
                return new ApiResponse { Success = false, Message = "Error de comunicación con el servidor" };
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al crear ubicación", ex, "UbicacionService", "CreateUbicacionAsync");
                return new ApiResponse { Success = false, Message = "Error inesperado al crear ubicación" };
            }
        }

        /// <summary>
        /// Actualiza una ubicación existente
        /// </summary>
        public async Task<ApiResponse> UpdateUbicacionAsync(int id, UbicacionDto ubicacion, CancellationToken cancellationToken = default)
        {
            try
            {
                if (id <= 0)
                {
                    await _logger.LogWarningAsync($"ID de ubicación inválido: {id}", "UbicacionService", "UpdateUbicacionAsync");
                    return new ApiResponse { Success = false, Message = "ID de ubicación inválido" };
                }

                if (ubicacion == null)
                {
                    await _logger.LogWarningAsync("Ubicación nula al intentar actualizar", "UbicacionService", "UpdateUbicacionAsync");
                    return new ApiResponse { Success = false, Message = "Ubicación no puede ser nula" };
                }

                // El ID del parámetro de ruta se asigna al objeto ubicación
                ubicacion.IdUbicacion = id;

                var url = _endpoints.GetEndpoint("api", "Ubicacion", id.ToString());

                await _logger.LogInformationAsync($"Actualizando ubicación con ID {id} en: {url}", "UbicacionService", "UpdateUbicacionAsync");

                var json = JsonSerializer.Serialize(ubicacion);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _http.PutAsync(url, content, cancellationToken).ConfigureAwait(false);

                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    await _logger.LogErrorAsync(
                        $"Error al actualizar ubicación. Status: {response.StatusCode}, Content: {responseContent}",
                        null,
                        "UbicacionService",
                        "UpdateUbicacionAsync");
                    return new ApiResponse { Success = false, Message = $"Error al actualizar ubicación: {response.StatusCode}" };
                }

                await _logger.LogInformationAsync($"Ubicación con ID {id} actualizada exitosamente", "UbicacionService", "UpdateUbicacionAsync");

                // Try to parse the response as ApiResponse
                try
                {
                    var apiResponse = JsonSerializer.Deserialize<ApiResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return apiResponse ?? new ApiResponse { Success = true, Message = "Ubicación actualizada exitosamente" };
                }
                catch
                {
                    return new ApiResponse { Success = true, Message = "Ubicación actualizada exitosamente" };
                }
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al actualizar ubicación", ex, "UbicacionService", "UpdateUbicacionAsync");
                return new ApiResponse { Success = false, Message = "Error de comunicación con el servidor" };
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al actualizar ubicación", ex, "UbicacionService", "UpdateUbicacionAsync");
                return new ApiResponse { Success = false, Message = "Error inesperado al actualizar ubicación" };
            }
        }

        /// <summary>
        /// Elimina una ubicación
        /// </summary>
        public async Task<ApiResponse> DeleteUbicacionAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                if (id <= 0)
                {
                    await _logger.LogWarningAsync($"ID de ubicación inválido: {id}", "UbicacionService", "DeleteUbicacionAsync");
                    return new ApiResponse { Success = false, Message = "ID de ubicación inválido" };
                }

                var url = _endpoints.GetEndpoint("api", "Ubicacion", id.ToString());

                await _logger.LogInformationAsync($"Eliminando ubicación con ID {id} en: {url}", "UbicacionService", "DeleteUbicacionAsync");

                var response = await _http.DeleteAsync(url, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al eliminar ubicación. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "UbicacionService",
                        "DeleteUbicacionAsync");
                    return new ApiResponse { Success = false, Message = $"Error al eliminar ubicación: {response.StatusCode}" };
                }

                await _logger.LogInformationAsync($"Ubicación con ID {id} eliminada exitosamente", "UbicacionService", "DeleteUbicacionAsync");

                return new ApiResponse { Success = true, Message = "Ubicación eliminada exitosamente" };
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al eliminar ubicación", ex, "UbicacionService", "DeleteUbicacionAsync");
                return new ApiResponse { Success = false, Message = "Error de comunicación con el servidor" };
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al eliminar ubicación", ex, "UbicacionService", "DeleteUbicacionAsync");
                return new ApiResponse { Success = false, Message = "Error inesperado al eliminar ubicación" };
            }
        }
    }
}
