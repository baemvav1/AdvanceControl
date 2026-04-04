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

namespace Advance_Control.Services.OrdenServicio
{
    /// <summary>
    /// Implementación del servicio de órdenes de servicio que se comunica con la API
    /// </summary>
    public class OrdenServicioService : IOrdenServicioService
    {
        private readonly HttpClient _http;
        private readonly IApiEndpointProvider _endpoints;
        private readonly ILoggingService _logger;

        public OrdenServicioService(HttpClient http, IApiEndpointProvider endpoints, ILoggingService logger)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Obtiene una lista de órdenes de servicio según los criterios de búsqueda proporcionados
        /// </summary>
        public async Task<List<OrdenServicioDto>> GetOrdenesServicioAsync(OrdenServicioQueryDto? query = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = _endpoints.GetEndpoint("api", "OrdenServicio");

                if (query != null)
                {
                    url = new ApiQueryBuilder()
                        .Add("identificador", query.Identificador)
                        .AddPositive("idCliente", query.IdCliente)
                        .Build(url);
                }

                await _logger.LogInformationAsync($"Obteniendo órdenes de servicio desde: {url}", "OrdenServicioService", "GetOrdenesServicioAsync");

                var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al obtener órdenes de servicio. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "OrdenServicioService",
                        "GetOrdenesServicioAsync");
                    return new List<OrdenServicioDto>();
                }

                List<OrdenServicioDto>? ordenes;
                try
                {
                    ordenes = await response.Content.ReadFromJsonAsync<List<OrdenServicioDto>>(cancellationToken: cancellationToken).ConfigureAwait(false);
                }
                catch (System.Text.Json.JsonException ex)
                {
                    await _logger.LogErrorAsync("Error al deserializar respuesta de órdenes de servicio", ex, "OrdenServicioService", "GetOrdenesServicioAsync");
                    return new List<OrdenServicioDto>();
                }

                await _logger.LogInformationAsync($"Se obtuvieron {ordenes?.Count ?? 0} órdenes de servicio", "OrdenServicioService", "GetOrdenesServicioAsync");

                return ordenes ?? new List<OrdenServicioDto>();
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al obtener órdenes de servicio", ex, "OrdenServicioService", "GetOrdenesServicioAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al obtener órdenes de servicio", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al obtener órdenes de servicio", ex, "OrdenServicioService", "GetOrdenesServicioAsync");
                throw;
            }
        }

        /// <summary>
        /// Elimina (soft delete) una orden de servicio por su ID
        /// </summary>
        public async Task<bool> DeleteOrdenServicioAsync(int idOrdenServicio, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"{_endpoints.GetEndpoint("api", "OrdenServicio")}?idOrdenServicio={idOrdenServicio}";

                await _logger.LogInformationAsync($"Eliminando orden de servicio {idOrdenServicio} en: {url}", "OrdenServicioService", "DeleteOrdenServicioAsync");

                using var response = await _http.DeleteAsync(url, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al eliminar orden de servicio. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "OrdenServicioService",
                        "DeleteOrdenServicioAsync");
                    return false;
                }

                await _logger.LogInformationAsync($"Orden de servicio {idOrdenServicio} eliminada correctamente", "OrdenServicioService", "DeleteOrdenServicioAsync");
                return true;
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al eliminar orden de servicio", ex, "OrdenServicioService", "DeleteOrdenServicioAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al eliminar orden de servicio", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al eliminar orden de servicio", ex, "OrdenServicioService", "DeleteOrdenServicioAsync");
                throw;
            }
        }

        /// <summary>
        /// Crea una nueva orden de servicio
        /// </summary>
        public async Task<bool> CreateOrdenServicioAsync(int idTipoMantenimiento, int idCliente, int idEquipo, string? nota = null, int credencialId = 0, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = _endpoints.GetEndpoint("api", "OrdenServicio");

                url = new ApiQueryBuilder()
                    .AddRequired("idTipoMantenimiento", idTipoMantenimiento)
                    .AddRequired("idCliente", idCliente)
                    .AddRequired("idEquipo", idEquipo)
                    .AddRequired("credencialId", credencialId)
                    .Add("nota", nota)
                    .Build(url);

                await _logger.LogInformationAsync($"Creando orden de servicio en: {url}", "OrdenServicioService", "CreateOrdenServicioAsync");

                using var response = await _http.PostAsync(url, null, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al crear orden de servicio. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "OrdenServicioService",
                        "CreateOrdenServicioAsync");
                    return false;
                }

                await _logger.LogInformationAsync("Orden de servicio creada correctamente", "OrdenServicioService", "CreateOrdenServicioAsync");
                return true;
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al crear orden de servicio", ex, "OrdenServicioService", "CreateOrdenServicioAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al crear orden de servicio", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al crear orden de servicio", ex, "OrdenServicioService", "CreateOrdenServicioAsync");
                throw;
            }
        }

        /// <summary>
        /// Actualiza el estado de atendido de una orden de servicio
        /// </summary>
        public async Task<bool> UpdateAtendidoAsync(int idOrdenServicio, int idAtendio, CancellationToken cancellationToken = default)
        {
            try
            {
                var baseUrl = _endpoints.GetEndpoint("api", "OrdenServicio");
                var url = $"{baseUrl}/atendido?idOrdenServicio={Uri.EscapeDataString(idOrdenServicio.ToString())}&idAtendio={Uri.EscapeDataString(idAtendio.ToString())}";

                await _logger.LogInformationAsync($"Actualizando estado atendido de orden de servicio {idOrdenServicio} en: {url}", "OrdenServicioService", "UpdateAtendidoAsync");

                var response = await _http.PatchAsync(url, null, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al actualizar estado atendido. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "OrdenServicioService",
                        "UpdateAtendidoAsync");
                    return false;
                }

                await _logger.LogInformationAsync($"Estado atendido de orden de servicio {idOrdenServicio} actualizado correctamente", "OrdenServicioService", "UpdateAtendidoAsync");
                return true;
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al actualizar estado atendido", ex, "OrdenServicioService", "UpdateAtendidoAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al actualizar estado atendido", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al actualizar estado atendido", ex, "OrdenServicioService", "UpdateAtendidoAsync");
                throw;
            }
        }

        /// <summary>
        /// Obtiene los técnicos disponibles para atender una orden de servicio,
        /// filtrados por el área del equipo asociado.
        /// </summary>
        public async Task<List<TecnicoDisponibleDto>> GetTecnicosDisponiblesAsync(string identificador, CancellationToken cancellationToken = default)
        {
            try
            {
                var baseUrl = _endpoints.GetEndpoint("api", "OrdenServicio");
                var url = $"{baseUrl}/tecnicos?identificador={Uri.EscapeDataString(identificador)}";

                await _logger.LogInformationAsync(
                    $"Obteniendo técnicos disponibles para equipo {identificador} en: {url}",
                    "OrdenServicioService",
                    "GetTecnicosDisponiblesAsync");

                var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al obtener técnicos disponibles. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "OrdenServicioService",
                        "GetTecnicosDisponiblesAsync");
                    return new List<TecnicoDisponibleDto>();
                }

                var tecnicos = await response.Content.ReadFromJsonAsync<List<TecnicoDisponibleDto>>(cancellationToken: cancellationToken).ConfigureAwait(false);

                await _logger.LogInformationAsync(
                    $"Se obtuvieron {tecnicos?.Count ?? 0} técnicos disponibles para equipo {identificador}",
                    "OrdenServicioService",
                    "GetTecnicosDisponiblesAsync");

                return tecnicos ?? new List<TecnicoDisponibleDto>();
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al obtener técnicos disponibles", ex, "OrdenServicioService", "GetTecnicosDisponiblesAsync");
                return new List<TecnicoDisponibleDto>();
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al obtener técnicos disponibles", ex, "OrdenServicioService", "GetTecnicosDisponiblesAsync");
                return new List<TecnicoDisponibleDto>();
            }
        }
    }
}
