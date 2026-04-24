using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.EndPointProvider;
using Advance_Control.Services.Logging;
using Advance_Control.Utilities;

namespace Advance_Control.Services.Operaciones
{
    /// <summary>
    /// Implementación del servicio de operaciones que se comunica con la API
    /// </summary>
    public class OperacionService : IOperacionService
    {
        private readonly HttpClient _http;
        private readonly IApiEndpointProvider _endpoints;
        private readonly ILoggingService _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public OperacionService(HttpClient http, IApiEndpointProvider endpoints, ILoggingService logger)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Configurar opciones de JSON para ser case-insensitive
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        /// <summary>
        /// Obtiene una lista de operaciones según los criterios de búsqueda proporcionados
        /// </summary>
        public async Task<List<OperacionDto>> GetOperacionesAsync(OperacionQueryDto? query = null, CancellationToken cancellationToken = default)
        {
            var (items, _) = await GetOperacionesPaginadasInternalAsync(query, 0, 0, cancellationToken).ConfigureAwait(false);
            return items;
        }

        public Task<(List<OperacionDto> Items, long Total)> GetOperacionesPaginadasAsync(OperacionQueryDto? query, int skip, int take, CancellationToken cancellationToken = default)
        {
            return GetOperacionesPaginadasInternalAsync(query, skip, take, cancellationToken);
        }

        private async Task<(List<OperacionDto> Items, long Total)> GetOperacionesPaginadasInternalAsync(OperacionQueryDto? query, int skip, int take, CancellationToken cancellationToken)
        {
            try
            {
                // Construir la URL base usando el endpoint correcto
                var url = _endpoints.GetEndpoint("api", "Operaciones");

                var builder = new ApiQueryBuilder();
                if (query != null)
                {
                    builder
                        .AddPositive("idTipo", query.IdTipo)
                        .AddPositive("idCliente", query.IdCliente)
                        .AddPositive("idEquipo", query.IdEquipo)
                        .AddPositive("idAtiende", query.IdAtiende)
                        .Add("nota", query.Nota)
                        .Add("fechainicial", query.FechaInicial?.ToString("yyyy-MM-dd"))
                        .Add("fechaFinalFiltro", query.FechaFinalFiltro?.ToString("yyyy-MM-dd"));
                }
                if (skip > 0) builder.Add("skip", skip.ToString());
                if (take > 0) builder.Add("take", take.ToString());
                url = builder.Build(url);

                await _logger.LogInformationAsync($"Obteniendo operaciones desde: {url}", "OperacionService", "GetOperacionesAsync");

                var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
                    {
                        await _logger.LogWarningAsync(
                            $"La sesión fue rechazada al obtener operaciones. Status: {response.StatusCode}, Content: {errorContent}",
                            "OperacionService",
                            "GetOperacionesAsync");
                        throw new UnauthorizedAccessException("La sesión ya no es válida para cargar operaciones. Inicia sesión nuevamente.");
                    }

                    await _logger.LogErrorAsync(
                        $"Error al obtener operaciones. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "OperacionService",
                        "GetOperacionesAsync");
                    throw new InvalidOperationException($"No se pudieron cargar las operaciones porque el servidor respondió con el estado {(int)response.StatusCode} ({response.StatusCode}).");
                }

                long total = 0;
                if (response.Headers.TryGetValues("X-Total-Count", out var totalValues))
                {
                    long.TryParse(System.Linq.Enumerable.FirstOrDefault(totalValues), out total);
                }

                List<OperacionDto>? operaciones;
                try
                {
                    operaciones = await response.Content.ReadFromJsonAsync<List<OperacionDto>>(_jsonOptions, cancellationToken: cancellationToken).ConfigureAwait(false);
                }
                catch (JsonException ex)
                {
                    await _logger.LogErrorAsync(
                        "Error al deserializar respuesta de operaciones",
                        ex,
                        "OperacionService",
                        "GetOperacionesAsync");
                    throw new InvalidOperationException("La respuesta del servidor para operaciones no tiene un formato válido.", ex);
                }

                var items = operaciones ?? new List<OperacionDto>();
                if (total == 0) total = items.Count;

                await _logger.LogInformationAsync($"Se obtuvieron {items.Count} operaciones (total: {total})", "OperacionService", "GetOperacionesAsync");

                return (items, total);
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al obtener operaciones", ex, "OperacionService", "GetOperacionesAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al obtener operaciones", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al obtener operaciones", ex, "OperacionService", "GetOperacionesAsync");
                throw;
            }
        }

        public async Task<OperacionVisorAccessDto?> GetOperacionVisorAsync(int idOperacion, long? mensajeReferenciaId = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = _endpoints.GetEndpoint("api", "Operaciones", $"visor/{idOperacion}");
                url = new ApiQueryBuilder()
                    .Add("mensajeReferenciaId", mensajeReferenciaId)
                    .Build(url);

                using var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);
                if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                    throw new UnauthorizedAccessException("No tienes acceso a esta operación o la referencia de chat ya no está disponible.");

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al resolver operación para visor. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "OperacionService",
                        "GetOperacionVisorAsync");
                    return null;
                }

                return await response.Content.ReadFromJsonAsync<OperacionVisorAccessDto>(_jsonOptions, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al resolver operación para visor", ex, "OperacionService", "GetOperacionVisorAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al resolver la operación", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al resolver operación para visor", ex, "OperacionService", "GetOperacionVisorAsync");
                throw;
            }
        }

        /// <summary>
        /// Elimina (soft delete) una operación por su ID
        /// </summary>
        public async Task<bool> DeleteOperacionAsync(int idOperacion, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"{_endpoints.GetEndpoint("api", "Operaciones")}?idOperacion={idOperacion}";

                await _logger.LogInformationAsync($"Eliminando operación {idOperacion} en: {url}", "OperacionService", "DeleteOperacionAsync");

                using var response = await _http.DeleteAsync(url, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al eliminar operación. Status: {response.StatusCode}, Content: {errorContent}",
                        null, "OperacionService", "DeleteOperacionAsync");
                    return false;
                }

                await _logger.LogInformationAsync($"Operación {idOperacion} eliminada correctamente", "OperacionService", "DeleteOperacionAsync");
                return true;
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al eliminar operación", ex, "OperacionService", "DeleteOperacionAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al eliminar operación", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al eliminar operación", ex, "OperacionService", "DeleteOperacionAsync");
                throw;
            }
        }

        /// <summary>
        /// Actualiza una operación (monto, fechaFinal, etc.)
        /// </summary>
        public async Task<bool> UpdateOperacionAsync(int idOperacion, int idTipo = 0, int idCliente = 0, int idEquipo = 0, int idAtiende = 0, decimal monto = 0, string? nota = null, DateTime? fechaFinal = null, CancellationToken cancellationToken = default)
        {
            try
            {
                // IDs no sensibles van en query string
                var url = new ApiQueryBuilder()
                    .AddRequired("idOperacion", idOperacion)
                    .AddRequired("idTipo", idTipo)
                    .AddRequired("idCliente", idCliente)
                    .AddRequired("idEquipo", idEquipo)
                    .AddRequired("idAtiende", idAtiende)
                    .Build(_endpoints.GetEndpoint("api", "Operaciones"));

                // Datos financieros van en el body (no en query string/logs)
                var body = new { monto, nota, fechaFinal = fechaFinal?.ToString("yyyy-MM-dd") };

                await _logger.LogInformationAsync($"Actualizando operación {idOperacion}", "OperacionService", "UpdateOperacionAsync");

                using var response = await _http.PutAsJsonAsync(url, body, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al actualizar operación. Status: {response.StatusCode}, Content: {errorContent}",
                        null, "OperacionService", "UpdateOperacionAsync");
                    return false;
                }

                await _logger.LogInformationAsync($"Operación {idOperacion} actualizada correctamente", "OperacionService", "UpdateOperacionAsync");
                return true;
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al actualizar operación", ex, "OperacionService", "UpdateOperacionAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al actualizar operación", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al actualizar operación", ex, "OperacionService", "UpdateOperacionAsync");
                throw;
            }
        }

        /// <summary>
        /// Reabre una operación limpiando su fechaFinal
        /// </summary>
        public async Task<bool> ReopenOperacionAsync(int idOperacion, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"{_endpoints.GetEndpoint("api", "Operaciones")}/reabrir?idOperacion={idOperacion}";

                await _logger.LogInformationAsync($"Reabriendo operación {idOperacion} en: {url}", "OperacionService", "ReopenOperacionAsync");

                using var response = await _http.PutAsync(url, null, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al reabrir operación. Status: {response.StatusCode}, Content: {errorContent}",
                        null, "OperacionService", "ReopenOperacionAsync");
                    return false;
                }

                await _logger.LogInformationAsync($"Operación {idOperacion} reabierta correctamente", "OperacionService", "ReopenOperacionAsync");
                return true;
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al reabrir operación", ex, "OperacionService", "ReopenOperacionAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al reabrir operación", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al reabrir operación", ex, "OperacionService", "ReopenOperacionAsync");
                throw;
            }
        }

        /// <summary>
        /// Marca el trabajo técnico de una operación como finalizado
        /// </summary>
        public async Task<bool> FinalizarTrabajoAsync(int idOperacion, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"{_endpoints.GetEndpoint("api", "Operaciones")}/finalizar-trabajo?idOperacion={idOperacion}";

                await _logger.LogInformationAsync($"Finalizando trabajo de operación {idOperacion} en: {url}", "OperacionService", "FinalizarTrabajoAsync");

                using var response = await _http.PutAsync(url, null, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al finalizar trabajo. Status: {response.StatusCode}, Content: {errorContent}",
                        null, "OperacionService", "FinalizarTrabajoAsync");
                    return false;
                }

                await _logger.LogInformationAsync($"Trabajo de operación {idOperacion} finalizado correctamente", "OperacionService", "FinalizarTrabajoAsync");
                return true;
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al finalizar trabajo", ex, "OperacionService", "FinalizarTrabajoAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al finalizar trabajo", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al finalizar trabajo", ex, "OperacionService", "FinalizarTrabajoAsync");
                throw;
            }
        }

        /// <summary>
        /// Desmarca el trabajo técnico de una operación como finalizado
        /// </summary>
        public async Task<bool> DesfinalizarTrabajoAsync(int idOperacion, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"{_endpoints.GetEndpoint("api", "Operaciones")}/desfinalizar-trabajo?idOperacion={idOperacion}";

                await _logger.LogInformationAsync($"Desfinalizando trabajo de operación {idOperacion} en: {url}", "OperacionService", "DesfinalizarTrabajoAsync");

                using var response = await _http.PutAsync(url, null, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al desfinalizar trabajo. Status: {response.StatusCode}, Content: {errorContent}",
                        null, "OperacionService", "DesfinalizarTrabajoAsync");
                    return false;
                }

                await _logger.LogInformationAsync($"Trabajo de operación {idOperacion} desfinalizado correctamente", "OperacionService", "DesfinalizarTrabajoAsync");
                return true;
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al desfinalizar trabajo", ex, "OperacionService", "DesfinalizarTrabajoAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al desfinalizar trabajo", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al desfinalizar trabajo", ex, "OperacionService", "DesfinalizarTrabajoAsync");
                throw;
            }
        }
    }
}
