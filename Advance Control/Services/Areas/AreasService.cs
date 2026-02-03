using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.EndPointProvider;
using Advance_Control.Services.Logging;

namespace Advance_Control.Services.Areas
{
    /// <summary>
    /// Implementación del servicio de áreas geográficas
    /// </summary>
    public class AreasService : IAreasService
    {
        private readonly HttpClient _http;
        private readonly IApiEndpointProvider _endpoints;
        private readonly ILoggingService _logger;

        public AreasService(HttpClient http, IApiEndpointProvider endpoints, ILoggingService logger)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Obtiene áreas con filtros opcionales
        /// </summary>
        public async Task<List<AreaDto>> GetAreasAsync(
            int? idArea = null,
            string? nombre = null,
            bool? activo = null,
            string? tipoGeometria = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var url = _endpoints.GetEndpoint("api", "Areas");
                var queryParams = new List<string>();

                if (idArea.HasValue && idArea.Value > 0)
                    queryParams.Add($"idArea={idArea.Value}");

                if (!string.IsNullOrWhiteSpace(nombre))
                    queryParams.Add($"nombre={Uri.EscapeDataString(nombre)}");

                if (activo.HasValue)
                    queryParams.Add($"activo={activo.Value.ToString().ToLower()}");

                if (!string.IsNullOrWhiteSpace(tipoGeometria))
                    queryParams.Add($"tipoGeometria={Uri.EscapeDataString(tipoGeometria)}");

                if (queryParams.Count > 0)
                {
                    url = $"{url}?{string.Join("&", queryParams)}";
                }

                await _logger.LogInformationAsync($"Obteniendo áreas desde: {url}", "AreasService", "GetAreasAsync");

                var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al obtener áreas. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "AreasService",
                        "GetAreasAsync");
                    return new List<AreaDto>();
                }

                var areas = await response.Content.ReadFromJsonAsync<List<AreaDto>>(cancellationToken: cancellationToken).ConfigureAwait(false);

                await _logger.LogInformationAsync($"Se obtuvieron {areas?.Count ?? 0} áreas", "AreasService", "GetAreasAsync");

                return areas ?? new List<AreaDto>();
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al obtener áreas", ex, "AreasService", "GetAreasAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al obtener áreas", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al obtener áreas", ex, "AreasService", "GetAreasAsync");
                throw;
            }
        }

        /// <summary>
        /// Obtiene áreas en formato optimizado para Google Maps JavaScript API
        /// </summary>
        public async Task<List<GoogleMapsAreaDto>> GetAreasForGoogleMapsAsync(
            int? idArea = null,
            bool? activo = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var url = _endpoints.GetEndpoint("api", "Areas", "googlemaps");
                var queryParams = new List<string>();

                if (idArea.HasValue && idArea.Value > 0)
                    queryParams.Add($"idArea={idArea.Value}");

                if (activo.HasValue)
                    queryParams.Add($"activo={activo.Value.ToString().ToLower()}");

                if (queryParams.Count > 0)
                {
                    url = $"{url}?{string.Join("&", queryParams)}";
                }

                await _logger.LogInformationAsync($"Obteniendo áreas para Google Maps desde: {url}", "AreasService", "GetAreasForGoogleMapsAsync");

                var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al obtener áreas para Google Maps. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "AreasService",
                        "GetAreasForGoogleMapsAsync");
                    return new List<GoogleMapsAreaDto>();
                }

                var areas = await response.Content.ReadFromJsonAsync<List<GoogleMapsAreaDto>>(cancellationToken: cancellationToken).ConfigureAwait(false);

                await _logger.LogInformationAsync($"Se obtuvieron {areas?.Count ?? 0} áreas para Google Maps", "AreasService", "GetAreasForGoogleMapsAsync");

                return areas ?? new List<GoogleMapsAreaDto>();
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al obtener áreas para Google Maps", ex, "AreasService", "GetAreasForGoogleMapsAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al obtener áreas para Google Maps", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al obtener áreas para Google Maps", ex, "AreasService", "GetAreasForGoogleMapsAsync");
                throw;
            }
        }

        /// <summary>
        /// Valida si un punto está dentro de un área
        /// </summary>
        public async Task<List<AreaValidationResultDto>> ValidatePointAsync(
            decimal latitud,
            decimal longitud,
            int? idArea = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var url = _endpoints.GetEndpoint("api", "Areas", "validate-point");
                var queryParams = new List<string>
                {
                    $"latitud={latitud}",
                    $"longitud={longitud}"
                };

                if (idArea.HasValue && idArea.Value > 0)
                    queryParams.Add($"idArea={idArea.Value}");

                url = $"{url}?{string.Join("&", queryParams)}";

                await _logger.LogInformationAsync($"Validando punto ({latitud}, {longitud}) en áreas", "AreasService", "ValidatePointAsync");

                var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al validar punto. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "AreasService",
                        "ValidatePointAsync");
                    return new List<AreaValidationResultDto>();
                }

                var results = await response.Content.ReadFromJsonAsync<List<AreaValidationResultDto>>(cancellationToken: cancellationToken).ConfigureAwait(false);

                await _logger.LogInformationAsync($"Validación completada. {results?.Count ?? 0} áreas evaluadas", "AreasService", "ValidatePointAsync");

                return results ?? new List<AreaValidationResultDto>();
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al validar punto", ex, "AreasService", "ValidatePointAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al validar punto", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al validar punto", ex, "AreasService", "ValidatePointAsync");
                throw;
            }
        }

        /// <summary>
        /// Crea una nueva área geográfica
        /// </summary>
        public async Task<ApiResponse> CreateAreaAsync(
            AreaDto area,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (area == null)
                {
                    return new ApiResponse { Success = false, Message = "El área no puede ser nula" };
                }

                // Validate required field 'nombre' before making API call
                if (string.IsNullOrWhiteSpace(area.Nombre))
                {
                    return new ApiResponse { Success = false, Message = "El nombre del área es requerido" };
                }

                var url = _endpoints.GetEndpoint("api", "Areas");

                // Build query parameters to match the API controller's [FromQuery] parameters
                var queryParams = BuildAreaQueryParams(area, isCreate: true);
                url = $"{url}?{string.Join("&", queryParams)}";

                await _logger.LogInformationAsync($"Creando área: {area.Nombre}", "AreasService", "CreateAreaAsync");
                await _logger.LogInformationAsync($"[DATA_FLOW] Step 3 - MetadataJSON received by service: {area.MetadataJSON ?? "NULL"}", "AreasService", "CreateAreaAsync");
                await _logger.LogInformationAsync($"[DATA_FLOW] Step 4 - Full URL being sent to API: {url}", "AreasService", "CreateAreaAsync");

                var response = await _http.PostAsync(url, null, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al crear área. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "AreasService",
                        "CreateAreaAsync");
                    return new ApiResponse { Success = false, Message = $"Error del servidor: {response.StatusCode}" };
                }

                var result = await response.Content.ReadFromJsonAsync<ApiResponse>(cancellationToken: cancellationToken).ConfigureAwait(false);

                if (result?.Success == true)
                {
                    await _logger.LogInformationAsync($"Área creada exitosamente: {area.Nombre}", "AreasService", "CreateAreaAsync");
                }
                else
                {
                    await _logger.LogWarningAsync($"No se pudo crear el área. Mensaje: {result?.Message}", "AreasService", "CreateAreaAsync");
                }

                return result ?? new ApiResponse { Success = false, Message = "Error desconocido al crear área" };
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al crear área", ex, "AreasService", "CreateAreaAsync");
                return new ApiResponse { Success = false, Message = "Error de comunicación con el servidor" };
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al crear área", ex, "AreasService", "CreateAreaAsync");
                return new ApiResponse { Success = false, Message = "Error inesperado al crear área" };
            }
        }

        /// <summary>
        /// Actualiza un área geográfica existente
        /// </summary>
        public async Task<ApiResponse> UpdateAreaAsync(
            int idArea,
            AreaDto area,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (area == null)
                {
                    return new ApiResponse { Success = false, Message = "El área no puede ser nula" };
                }

                if (idArea <= 0)
                {
                    return new ApiResponse { Success = false, Message = "ID de área inválido" };
                }

                var url = _endpoints.GetEndpoint("api", "Areas", idArea.ToString());

                // Build query parameters to match the API controller's [FromQuery] parameters
                var queryParams = BuildAreaQueryParams(area, isCreate: false);

                if (queryParams.Count > 0)
                {
                    url = $"{url}?{string.Join("&", queryParams)}";
                }

                await _logger.LogInformationAsync($"Actualizando área ID: {idArea}", "AreasService", "UpdateAreaAsync");
                await _logger.LogInformationAsync($"[DATA_FLOW] Step 3 - MetadataJSON received by service (update): {area.MetadataJSON ?? "NULL"}", "AreasService", "UpdateAreaAsync");
                await _logger.LogInformationAsync($"[DATA_FLOW] Step 4 - Full URL being sent to API (update): {url}", "AreasService", "UpdateAreaAsync");

                var response = await _http.PutAsync(url, null, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al actualizar área. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "AreasService",
                        "UpdateAreaAsync");
                    return new ApiResponse { Success = false, Message = $"Error del servidor: {response.StatusCode}" };
                }

                var result = await response.Content.ReadFromJsonAsync<ApiResponse>(cancellationToken: cancellationToken).ConfigureAwait(false);

                if (result?.Success == true)
                {
                    await _logger.LogInformationAsync($"Área actualizada exitosamente: {idArea}", "AreasService", "UpdateAreaAsync");
                }
                else
                {
                    await _logger.LogWarningAsync($"No se pudo actualizar el área. Mensaje: {result?.Message}", "AreasService", "UpdateAreaAsync");
                }

                return result ?? new ApiResponse { Success = false, Message = "Error desconocido al actualizar área" };
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al actualizar área", ex, "AreasService", "UpdateAreaAsync");
                return new ApiResponse { Success = false, Message = "Error de comunicación con el servidor" };
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al actualizar área", ex, "AreasService", "UpdateAreaAsync");
                return new ApiResponse { Success = false, Message = "Error inesperado al actualizar área" };
            }
        }

        /// <summary>
        /// Elimina un área geográfica
        /// </summary>
        public async Task<ApiResponse> DeleteAreaAsync(
            int idArea,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (idArea <= 0)
                {
                    return new ApiResponse { Success = false, Message = "ID de área inválido" };
                }

                var url = _endpoints.GetEndpoint("api", "Areas", idArea.ToString());

                await _logger.LogInformationAsync($"Eliminando área ID: {idArea}", "AreasService", "DeleteAreaAsync");

                var response = await _http.DeleteAsync(url, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al eliminar área. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "AreasService",
                        "DeleteAreaAsync");
                    return new ApiResponse { Success = false, Message = $"Error del servidor: {response.StatusCode}" };
                }

                var result = await response.Content.ReadFromJsonAsync<ApiResponse>(cancellationToken: cancellationToken).ConfigureAwait(false);

                if (result?.Success == true)
                {
                    await _logger.LogInformationAsync($"Área eliminada exitosamente: {idArea}", "AreasService", "DeleteAreaAsync");
                }
                else
                {
                    await _logger.LogWarningAsync($"No se pudo eliminar el área. Mensaje: {result?.Message}", "AreasService", "DeleteAreaAsync");
                }

                return result ?? new ApiResponse { Success = false, Message = "Error desconocido al eliminar área" };
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al eliminar área", ex, "AreasService", "DeleteAreaAsync");
                return new ApiResponse { Success = false, Message = "Error de comunicación con el servidor" };
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al eliminar área", ex, "AreasService", "DeleteAreaAsync");
                return new ApiResponse { Success = false, Message = "Error inesperado al eliminar área" };
            }
        }

        /// <summary>
        /// Builds query parameters from an AreaDto to match the API controller's [FromQuery] parameters
        /// </summary>
        private List<string> BuildAreaQueryParams(AreaDto area, bool isCreate)
        {
            var queryParams = new List<string>();

            // nombre - should always be present since it's validated before calling this method for create
            if (!string.IsNullOrWhiteSpace(area.Nombre))
                queryParams.Add($"nombre={Uri.EscapeDataString(area.Nombre)}");

            // Optional parameters
            if (!string.IsNullOrWhiteSpace(area.Descripcion))
                queryParams.Add($"descripcion={Uri.EscapeDataString(area.Descripcion)}");

            if (!string.IsNullOrWhiteSpace(area.ColorMapa))
                queryParams.Add($"colorMapa={Uri.EscapeDataString(area.ColorMapa)}");

            if (area.Opacidad.HasValue)
                queryParams.Add($"opacidad={area.Opacidad.Value}");

            if (!string.IsNullOrWhiteSpace(area.ColorBorde))
                queryParams.Add($"colorBorde={Uri.EscapeDataString(area.ColorBorde)}");

            if (area.AnchoBorde.HasValue)
                queryParams.Add($"anchoBorde={area.AnchoBorde.Value}");

            if (area.Activo.HasValue)
                queryParams.Add($"activo={(area.Activo.Value ? "true" : "false")}");

            if (!string.IsNullOrWhiteSpace(area.TipoGeometria))
                queryParams.Add($"tipoGeometria={Uri.EscapeDataString(area.TipoGeometria)}");

            if (area.CentroLatitud.HasValue)
                queryParams.Add($"centroLatitud={area.CentroLatitud.Value}");

            if (area.CentroLongitud.HasValue)
                queryParams.Add($"centroLongitud={area.CentroLongitud.Value}");

            if (area.Radio.HasValue)
                queryParams.Add($"radio={area.Radio.Value}");

            if (area.EtiquetaMostrar.HasValue)
                queryParams.Add($"etiquetaMostrar={(area.EtiquetaMostrar.Value ? "true" : "false")}");

            if (!string.IsNullOrWhiteSpace(area.EtiquetaTexto))
                queryParams.Add($"etiquetaTexto={Uri.EscapeDataString(area.EtiquetaTexto)}");

            if (area.NivelZoom.HasValue)
                queryParams.Add($"nivelZoom={area.NivelZoom.Value}");

            if (!string.IsNullOrWhiteSpace(area.MetadataJSON))
                queryParams.Add($"metadataJSON={Uri.EscapeDataString(area.MetadataJSON)}");

            // User tracking parameters
            if (isCreate && !string.IsNullOrWhiteSpace(area.UsuarioCreacion))
                queryParams.Add($"usuarioCreacion={Uri.EscapeDataString(area.UsuarioCreacion)}");

            if (!isCreate && !string.IsNullOrWhiteSpace(area.UsuarioModificacion))
                queryParams.Add($"usuarioModificacion={Uri.EscapeDataString(area.UsuarioModificacion)}");

            // The API also supports: coordenadas, autoCalcularCentro, validarPoligonoLargo
            // These are not in the current AreaDto, but MetadataJSON can contain coordinate data

            return queryParams;
        }
    }
}
