using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.EndPointProvider;
using Advance_Control.Services.Logging;
using Advance_Control.Utilities;

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
                url = new ApiQueryBuilder()
                    .AddPositive("idArea", idArea ?? 0)
                    .Add("nombre", nombre)
                    .Add("activo", activo)
                    .Add("tipoGeometria", tipoGeometria)
                    .Build(url);

                await _logger.LogInformationAsync($"Obteniendo áreas desde: {url}", "AreasService", "GetAreasAsync");

                using var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);

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
                url = new ApiQueryBuilder()
                    .AddPositive("idArea", idArea ?? 0)
                    .Add("activo", activo)
                    .Build(url);

                await _logger.LogInformationAsync($"Obteniendo áreas para Google Maps desde: {url}", "AreasService", "GetAreasForGoogleMapsAsync");

                using var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);

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
                // Format coordinates using invariant culture to avoid numeric conversion errors
                url = new ApiQueryBuilder()
                    .AddRequired("latitud", latitud)
                    .AddRequired("longitud", longitud)
                    .AddPositive("idArea", idArea ?? 0)
                    .Build(url);

                await _logger.LogInformationAsync($"Validando punto ({latitud}, {longitud}) en áreas", "AreasService", "ValidatePointAsync");

                using var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);

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

                // Build query parameters (sin metadataJSON — va en el body para evitar URI Too Long)
                url = BuildAreaQueryParams(area, url, isCreate: true);

                await _logger.LogInformationAsync($"Creando área: {area.Nombre}", "AreasService", "CreateAreaAsync");
                await LogDataFlowInfoAsync(area, 0, "CreateAreaAsync");

                // Enviar metadataJSON en el body JSON para no superar el límite de URI
                var body = new { metadataJSON = area.MetadataJSON, coordenadas = (string?)null };
                using var response = await _http.PostAsJsonAsync(url, body, cancellationToken).ConfigureAwait(false);

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

                // Build query parameters (sin metadataJSON — va en el body para evitar URI Too Long)
                url = BuildAreaQueryParams(area, url, isCreate: false);

                await _logger.LogInformationAsync($"Actualizando área ID: {idArea}", "AreasService", "UpdateAreaAsync");
                await LogDataFlowInfoAsync(area, 0, "UpdateAreaAsync");

                // Enviar metadataJSON en el body JSON para no superar el límite de URI
                var body = new { metadataJSON = area.MetadataJSON, coordenadas = (string?)null };
                using var response = await _http.PutAsJsonAsync(url, body, cancellationToken).ConfigureAwait(false);

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

                using var response = await _http.DeleteAsync(url, cancellationToken).ConfigureAwait(false);

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
        /// Obtiene identificadores de equipos cuya ubicacion pertenece al area indicada
        /// </summary>
        public async Task<List<string>> GetIdentificadoresEnAreaAsync(int idArea, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = _endpoints.GetEndpoint("api", "Areas", "equipos-en-area");
                url = new ApiQueryBuilder()
                    .AddRequired("idArea", idArea)
                    .Build(url);

                await _logger.LogInformationAsync($"Obteniendo equipos en area {idArea}", "AreasService", "GetIdentificadoresEnAreaAsync");

                using var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    await _logger.LogErrorAsync(
                        $"Error al obtener equipos en area. Status: {response.StatusCode}",
                        null, "AreasService", "GetIdentificadoresEnAreaAsync");
                    return new List<string>();
                }

                var result = await response.Content.ReadFromJsonAsync<List<string>>(cancellationToken: cancellationToken).ConfigureAwait(false);
                return result ?? new List<string>();
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error al obtener equipos en area", ex, "AreasService", "GetIdentificadoresEnAreaAsync");
                return new List<string>();
            }
        }

        /// <summary>
        /// Logs data flow information for create/update operations
        /// </summary>
        private async Task LogDataFlowInfoAsync(AreaDto area, int queryParamsCount, string methodName)
        {
            var metadataLen = area.MetadataJSON?.Length ?? 0;
            await _logger.LogInformationAsync(
                $"[DATA_FLOW] Step 3 - MetadataJSON received ({metadataLen} chars), Query params count: {queryParamsCount}",
                "AreasService",
                methodName);
        }

        /// <summary>
        /// Builds query parameters from an AreaDto to match the API controller's [FromQuery] parameters
        /// </summary>
        private string BuildAreaQueryParams(AreaDto area, string baseUrl, bool isCreate)
        {
            return new ApiQueryBuilder()
                .Add("nombre", area.Nombre)
                .Add("descripcion", area.Descripcion)
                .Add("colorMapa", area.ColorMapa)
                .Add("opacidad", area.Opacidad)
                .Add("colorBorde", area.ColorBorde)
                .Add("anchoBorde", area.AnchoBorde)
                .Add("activo", area.Activo)
                .Add("tipoGeometria", area.TipoGeometria)
                .Add("centroLatitud", area.CentroLatitud)
                .Add("centroLongitud", area.CentroLongitud)
                .Add("radio", area.Radio)
                .Add("boundingBoxNE_Lat", area.BoundingBoxNE_Lat)
                .Add("boundingBoxNE_Lng", area.BoundingBoxNE_Lng)
                .Add("boundingBoxSW_Lat", area.BoundingBoxSW_Lat)
                .Add("boundingBoxSW_Lng", area.BoundingBoxSW_Lng)
                .Add("etiquetaMostrar", area.EtiquetaMostrar)
                .Add("etiquetaTexto", area.EtiquetaTexto)
                .Add("nivelZoom", area.NivelZoom)
                // metadataJSON va en el body (PostAsJsonAsync/PutAsJsonAsync) — no en la URI
                .Add(isCreate ? "usuarioCreacion" : "usuarioModificacion",
                     isCreate ? area.UsuarioCreacion : area.UsuarioModificacion)
                .Build(baseUrl);
        }
    }
}
