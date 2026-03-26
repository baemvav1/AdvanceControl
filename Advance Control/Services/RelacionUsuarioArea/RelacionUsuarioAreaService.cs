using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.EndPointProvider;
using Advance_Control.Services.Logging;
using Advance_Control.Utilities;

namespace Advance_Control.Services.RelacionUsuarioArea
{
    public class RelacionUsuarioAreaService : IRelacionUsuarioAreaService
    {
        private readonly HttpClient _http;
        private readonly IApiEndpointProvider _endpoints;
        private readonly ILoggingService _logger;

        public RelacionUsuarioAreaService(HttpClient http, IApiEndpointProvider endpoints, ILoggingService logger)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<RelacionUsuarioAreaDto>> GetRelacionesPorUsuarioAsync(long credencialId, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = _endpoints.GetEndpoint("api", "RelacionUsuarioArea");
                url = new ApiQueryBuilder()
                    .AddRequired("credencialId", credencialId)
                    .Build(url);

                await _logger.LogInformationAsync($"Obteniendo áreas del usuario desde: {url}", "RelacionUsuarioAreaService", "GetRelacionesPorUsuarioAsync");

                using var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al obtener áreas del usuario. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "RelacionUsuarioAreaService",
                        "GetRelacionesPorUsuarioAsync");
                    return new List<RelacionUsuarioAreaDto>();
                }

                var relaciones = await response.Content.ReadFromJsonAsync<List<RelacionUsuarioAreaDto>>(cancellationToken: cancellationToken).ConfigureAwait(false);
                await _logger.LogInformationAsync($"Se obtuvieron {relaciones?.Count ?? 0} áreas del usuario", "RelacionUsuarioAreaService", "GetRelacionesPorUsuarioAsync");

                return relaciones ?? new List<RelacionUsuarioAreaDto>();
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al obtener áreas del usuario", ex, "RelacionUsuarioAreaService", "GetRelacionesPorUsuarioAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al obtener áreas del usuario", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al obtener áreas del usuario", ex, "RelacionUsuarioAreaService", "GetRelacionesPorUsuarioAsync");
                throw;
            }
        }

        public async Task<List<RelacionUsuarioAreaDto>> GetRelacionesPorAreaAsync(int idArea, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = _endpoints.GetEndpoint("api", "RelacionUsuarioArea");
                url = new ApiQueryBuilder()
                    .AddRequired("idArea", idArea)
                    .Build(url);

                await _logger.LogInformationAsync($"Obteniendo usuarios del área desde: {url}", "RelacionUsuarioAreaService", "GetRelacionesPorAreaAsync");

                using var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al obtener usuarios del área. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "RelacionUsuarioAreaService",
                        "GetRelacionesPorAreaAsync");
                    return new List<RelacionUsuarioAreaDto>();
                }

                var relaciones = await response.Content.ReadFromJsonAsync<List<RelacionUsuarioAreaDto>>(cancellationToken: cancellationToken).ConfigureAwait(false);
                await _logger.LogInformationAsync($"Se obtuvieron {relaciones?.Count ?? 0} usuarios del área", "RelacionUsuarioAreaService", "GetRelacionesPorAreaAsync");

                return relaciones ?? new List<RelacionUsuarioAreaDto>();
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al obtener usuarios del área", ex, "RelacionUsuarioAreaService", "GetRelacionesPorAreaAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al obtener usuarios del área", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al obtener usuarios del área", ex, "RelacionUsuarioAreaService", "GetRelacionesPorAreaAsync");
                throw;
            }
        }

        public async Task<RelacionUsuarioAreaDto?> CreateRelacionAsync(long credencialId, int idArea, string? nota = null, CancellationToken cancellationToken = default)
        {
            try
            {
                if (credencialId <= 0 || idArea <= 0)
                {
                    await _logger.LogWarningAsync("credencialId e idArea son requeridos", "RelacionUsuarioAreaService", "CreateRelacionAsync");
                    return null;
                }

                var url = _endpoints.GetEndpoint("api", "RelacionUsuarioArea");
                var body = new { credencialId, idArea, nota };

                await _logger.LogInformationAsync($"Creando relación usuario-área: credencialId={credencialId}, idArea={idArea}", "RelacionUsuarioAreaService", "CreateRelacionAsync");

                using var response = await _http.PostAsJsonAsync(url, body, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al crear relación usuario-área. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "RelacionUsuarioAreaService",
                        "CreateRelacionAsync");
                    return null;
                }

                var result = await response.Content.ReadFromJsonAsync<RelacionUsuarioAreaDto>(cancellationToken: cancellationToken).ConfigureAwait(false);
                await _logger.LogInformationAsync($"Relación usuario-área creada exitosamente: id={result?.Id}", "RelacionUsuarioAreaService", "CreateRelacionAsync");

                return result;
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al crear relación usuario-área", ex, "RelacionUsuarioAreaService", "CreateRelacionAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al crear relación usuario-área", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al crear relación usuario-área", ex, "RelacionUsuarioAreaService", "CreateRelacionAsync");
                throw;
            }
        }

        public async Task<bool> DeleteRelacionAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                if (id <= 0)
                {
                    await _logger.LogWarningAsync("ID inválido para eliminar relación", "RelacionUsuarioAreaService", "DeleteRelacionAsync");
                    return false;
                }

                var url = _endpoints.GetEndpoint("api", "RelacionUsuarioArea", id.ToString());

                await _logger.LogInformationAsync($"Eliminando relación usuario-área: id={id}", "RelacionUsuarioAreaService", "DeleteRelacionAsync");

                using var response = await _http.DeleteAsync(url, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al eliminar relación usuario-área. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "RelacionUsuarioAreaService",
                        "DeleteRelacionAsync");
                    return false;
                }

                await _logger.LogInformationAsync($"Relación usuario-área eliminada exitosamente: id={id}", "RelacionUsuarioAreaService", "DeleteRelacionAsync");
                return true;
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al eliminar relación usuario-área", ex, "RelacionUsuarioAreaService", "DeleteRelacionAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al eliminar relación usuario-área", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al eliminar relación usuario-área", ex, "RelacionUsuarioAreaService", "DeleteRelacionAsync");
                throw;
            }
        }

        public async Task<List<string>> GetEquiposEnAreasAsync(long credencialId, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = _endpoints.GetEndpoint("api", "RelacionUsuarioArea", "equipos");
                url = new ApiQueryBuilder()
                    .AddRequired("credencialId", credencialId)
                    .Build(url);

                await _logger.LogInformationAsync($"Obteniendo equipos en áreas del usuario: {credencialId}", "RelacionUsuarioAreaService", "GetEquiposEnAreasAsync");

                using var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al obtener equipos en áreas. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "RelacionUsuarioAreaService",
                        "GetEquiposEnAreasAsync");
                    return new List<string>();
                }

                var identificadores = await response.Content.ReadFromJsonAsync<List<string>>(cancellationToken: cancellationToken).ConfigureAwait(false);
                await _logger.LogInformationAsync($"Se obtuvieron {identificadores?.Count ?? 0} equipos en áreas del usuario", "RelacionUsuarioAreaService", "GetEquiposEnAreasAsync");

                return identificadores ?? new List<string>();
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al obtener equipos en áreas", ex, "RelacionUsuarioAreaService", "GetEquiposEnAreasAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al obtener equipos en áreas", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al obtener equipos en áreas", ex, "RelacionUsuarioAreaService", "GetEquiposEnAreasAsync");
                throw;
            }
        }
    }
}
