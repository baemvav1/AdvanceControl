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

namespace Advance_Control.Services.RelacionUsuarioRubro
{
    public class RelacionUsuarioRubroService : IRelacionUsuarioRubroService
    {
        private readonly HttpClient _http;
        private readonly IApiEndpointProvider _endpoints;
        private readonly ILoggingService _logger;

        public RelacionUsuarioRubroService(HttpClient http, IApiEndpointProvider endpoints, ILoggingService logger)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<RubroDto>> GetRubrosAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var url = _endpoints.GetEndpoint("api", "RelacionUsuarioRubro", "rubros");

                await _logger.LogInformationAsync($"Obteniendo catálogo de rubros desde: {url}", "RelacionUsuarioRubroService", "GetRubrosAsync");

                using var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al obtener catálogo de rubros. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "RelacionUsuarioRubroService",
                        "GetRubrosAsync");
                    return new List<RubroDto>();
                }

                var rubros = await response.Content.ReadFromJsonAsync<List<RubroDto>>(cancellationToken: cancellationToken).ConfigureAwait(false);
                return rubros ?? new List<RubroDto>();
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al obtener catálogo de rubros", ex, "RelacionUsuarioRubroService", "GetRubrosAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al obtener catálogo de rubros", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al obtener catálogo de rubros", ex, "RelacionUsuarioRubroService", "GetRubrosAsync");
                throw;
            }
        }

        public async Task<List<RelacionUsuarioRubroDto>> GetRelacionesPorUsuarioAsync(long credencialId, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = _endpoints.GetEndpoint("api", "RelacionUsuarioRubro");
                url = new ApiQueryBuilder()
                    .AddRequired("credencialId", credencialId)
                    .Build(url);

                await _logger.LogInformationAsync($"Obteniendo rubros del usuario desde: {url}", "RelacionUsuarioRubroService", "GetRelacionesPorUsuarioAsync");

                using var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al obtener rubros del usuario. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "RelacionUsuarioRubroService",
                        "GetRelacionesPorUsuarioAsync");
                    return new List<RelacionUsuarioRubroDto>();
                }

                var relaciones = await response.Content.ReadFromJsonAsync<List<RelacionUsuarioRubroDto>>(cancellationToken: cancellationToken).ConfigureAwait(false);
                return relaciones ?? new List<RelacionUsuarioRubroDto>();
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al obtener rubros del usuario", ex, "RelacionUsuarioRubroService", "GetRelacionesPorUsuarioAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al obtener rubros del usuario", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al obtener rubros del usuario", ex, "RelacionUsuarioRubroService", "GetRelacionesPorUsuarioAsync");
                throw;
            }
        }

        public async Task<RelacionUsuarioRubroDto?> CreateRelacionAsync(long credencialId, int idRubro, CancellationToken cancellationToken = default)
        {
            try
            {
                if (credencialId <= 0 || idRubro <= 0)
                {
                    await _logger.LogWarningAsync("credencialId e idRubro son requeridos", "RelacionUsuarioRubroService", "CreateRelacionAsync");
                    return null;
                }

                var url = _endpoints.GetEndpoint("api", "RelacionUsuarioRubro");
                var body = new { credencialId, idRubro };

                await _logger.LogInformationAsync($"Creando relación usuario-rubro: credencialId={credencialId}, idRubro={idRubro}", "RelacionUsuarioRubroService", "CreateRelacionAsync");

                using var response = await _http.PostAsJsonAsync(url, body, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al crear relación usuario-rubro. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "RelacionUsuarioRubroService",
                        "CreateRelacionAsync");
                    return null;
                }

                var result = await response.Content.ReadFromJsonAsync<RelacionUsuarioRubroDto>(cancellationToken: cancellationToken).ConfigureAwait(false);
                return result;
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al crear relación usuario-rubro", ex, "RelacionUsuarioRubroService", "CreateRelacionAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al crear relación usuario-rubro", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al crear relación usuario-rubro", ex, "RelacionUsuarioRubroService", "CreateRelacionAsync");
                throw;
            }
        }

        public async Task<bool> DeleteRelacionAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                if (id <= 0)
                {
                    await _logger.LogWarningAsync("ID inválido para eliminar relación", "RelacionUsuarioRubroService", "DeleteRelacionAsync");
                    return false;
                }

                var url = _endpoints.GetEndpoint("api", "RelacionUsuarioRubro", id.ToString());

                await _logger.LogInformationAsync($"Eliminando relación usuario-rubro: id={id}", "RelacionUsuarioRubroService", "DeleteRelacionAsync");

                using var response = await _http.DeleteAsync(url, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al eliminar relación usuario-rubro. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "RelacionUsuarioRubroService",
                        "DeleteRelacionAsync");
                    return false;
                }

                return true;
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al eliminar relación usuario-rubro", ex, "RelacionUsuarioRubroService", "DeleteRelacionAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al eliminar relación usuario-rubro", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al eliminar relación usuario-rubro", ex, "RelacionUsuarioRubroService", "DeleteRelacionAsync");
                throw;
            }
        }
    }
}
