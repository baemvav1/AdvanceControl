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

namespace Advance_Control.Services.PermisosUi
{
    public class PermisoUiService : IPermisoUiService
    {
        private readonly HttpClient _http;
        private readonly IApiEndpointProvider _endpoints;
        private readonly ILoggingService _logger;

        public PermisoUiService(HttpClient http, IApiEndpointProvider endpoints, ILoggingService logger)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<PermisoModuloDto>> GetCatalogoAsync(bool soloActivos = true, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"{_endpoints.GetEndpoint("api", "PermisosUi")}/catalogo?soloActivos={soloActivos.ToString().ToLowerInvariant()}";
                var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync($"Error al obtener catálogo de permisos. Status: {response.StatusCode}. Content: {content}", null, "PermisoUiService", "GetCatalogoAsync");
                    return new List<PermisoModuloDto>();
                }

                return await response.Content.ReadFromJsonAsync<List<PermisoModuloDto>>(cancellationToken: cancellationToken).ConfigureAwait(false)
                    ?? new List<PermisoModuloDto>();
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error al obtener catálogo de permisos UI", ex, "PermisoUiService", "GetCatalogoAsync");
                throw;
            }
        }

        public async Task<PermisoUiSyncResultDto> SyncCatalogoAsync(PermisoUiSyncRequestDto request, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"{_endpoints.GetEndpoint("api", "PermisosUi")}/sincronizar";
                using var response = await _http.PostAsJsonAsync(url, request, cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync($"Error al sincronizar catálogo de permisos. Status: {response.StatusCode}. Content: {content}", null, "PermisoUiService", "SyncCatalogoAsync");
                    throw new InvalidOperationException(ExtractMessage(content));
                }

                return await response.Content.ReadFromJsonAsync<PermisoUiSyncResultDto>(cancellationToken: cancellationToken).ConfigureAwait(false)
                    ?? new PermisoUiSyncResultDto();
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error al sincronizar catálogo de permisos UI", ex, "PermisoUiService", "SyncCatalogoAsync");
                throw;
            }
        }

        public async Task<PermisoModuloDto?> UpdateNivelModuloAsync(PermisoModuloNivelUpdateDto request, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"{_endpoints.GetEndpoint("api", "PermisosUi")}/modulos/nivel";
                using var response = await _http.PutAsJsonAsync(url, request, cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync($"Error al actualizar nivel de módulo. Status: {response.StatusCode}. Content: {content}", null, "PermisoUiService", "UpdateNivelModuloAsync");
                    throw new InvalidOperationException(ExtractMessage(content));
                }

                return await response.Content.ReadFromJsonAsync<PermisoModuloDto>(cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error al actualizar nivel del módulo", ex, "PermisoUiService", "UpdateNivelModuloAsync");
                throw;
            }
        }

        public async Task<PermisoAccionModuloDto?> UpdateNivelAccionAsync(PermisoAccionNivelUpdateDto request, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"{_endpoints.GetEndpoint("api", "PermisosUi")}/acciones/nivel";
                using var response = await _http.PutAsJsonAsync(url, request, cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync($"Error al actualizar nivel de acción. Status: {response.StatusCode}. Content: {content}", null, "PermisoUiService", "UpdateNivelAccionAsync");
                    throw new InvalidOperationException(ExtractMessage(content));
                }

                return await response.Content.ReadFromJsonAsync<PermisoAccionModuloDto>(cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error al actualizar nivel de la acción", ex, "PermisoUiService", "UpdateNivelAccionAsync");
                throw;
            }
        }

        private static string ExtractMessage(string? content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return "No se recibió detalle del error.";

            try
            {
                using var document = JsonDocument.Parse(content);
                if (document.RootElement.ValueKind == JsonValueKind.Object
                    && document.RootElement.TryGetProperty("message", out var messageElement)
                    && messageElement.ValueKind == JsonValueKind.String)
                {
                    var message = messageElement.GetString();
                    if (!string.IsNullOrWhiteSpace(message))
                        return message;
                }
            }
            catch (JsonException)
            {
            }

            return content;
        }
    }
}
