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

namespace Advance_Control.Services.Mantenimiento
{
    /// <summary>
    /// Implementación del servicio de mantenimientos que se comunica con la API
    /// </summary>
    public class MantenimientoService : IMantenimientoService
    {
        private readonly HttpClient _http;
        private readonly IApiEndpointProvider _endpoints;
        private readonly ILoggingService _logger;

        public MantenimientoService(HttpClient http, IApiEndpointProvider endpoints, ILoggingService logger)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Obtiene una lista de mantenimientos según los criterios de búsqueda proporcionados
        /// </summary>
        public async Task<List<MantenimientoDto>> GetMantenimientosAsync(MantenimientoQueryDto? query = null, CancellationToken cancellationToken = default)
        {
            try
            {
                // Construir la URL base usando el endpoint correcto
                var url = _endpoints.GetEndpoint("api", "Mantenimiento");

                // Agregar parámetros de consulta si existen
                if (query != null)
                {
                    url = new ApiQueryBuilder()
                        .Add("identificador", query.Identificador)
                        .AddPositive("idCliente", query.IdCliente)
                        .Build(url);
                }

                await _logger.LogInformationAsync($"Obteniendo mantenimientos desde: {url}", "MantenimientoService", "GetMantenimientosAsync");

                // Realizar la petición GET
                var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);

                // Verificar si la respuesta fue exitosa
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al obtener mantenimientos. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "MantenimientoService",
                        "GetMantenimientosAsync");
                    return new List<MantenimientoDto>();
                }

                // Deserializar la respuesta
                List<MantenimientoDto>? mantenimientos;
                try
                {
                    mantenimientos = await response.Content.ReadFromJsonAsync<List<MantenimientoDto>>(cancellationToken: cancellationToken).ConfigureAwait(false);
                }
                catch (System.Text.Json.JsonException ex)
                {
                    await _logger.LogErrorAsync("Error al deserializar respuesta de mantenimientos", ex, "MantenimientoService", "GetMantenimientosAsync");
                    return new List<MantenimientoDto>();
                }

                await _logger.LogInformationAsync($"Se obtuvieron {mantenimientos?.Count ?? 0} mantenimientos", "MantenimientoService", "GetMantenimientosAsync");

                return mantenimientos ?? new List<MantenimientoDto>();
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al obtener mantenimientos", ex, "MantenimientoService", "GetMantenimientosAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al obtener mantenimientos", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al obtener mantenimientos", ex, "MantenimientoService", "GetMantenimientosAsync");
                throw;
            }
        }

        /// <summary>
        /// Elimina (soft delete) un mantenimiento por su ID
        /// </summary>
        public async Task<bool> DeleteMantenimientoAsync(int idMantenimiento, CancellationToken cancellationToken = default)
        {
            try
            {
                // Construir la URL con parámetros de consulta
                var url = $"{_endpoints.GetEndpoint("api", "Mantenimiento")}?idMantenimiento={idMantenimiento}";

                await _logger.LogInformationAsync($"Eliminando mantenimiento {idMantenimiento} en: {url}", "MantenimientoService", "DeleteMantenimientoAsync");

                using var response = await _http.DeleteAsync(url, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al eliminar mantenimiento. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "MantenimientoService",
                        "DeleteMantenimientoAsync");
                    return false;
                }

                await _logger.LogInformationAsync($"Mantenimiento {idMantenimiento} eliminado correctamente", "MantenimientoService", "DeleteMantenimientoAsync");
                return true;
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al eliminar mantenimiento", ex, "MantenimientoService", "DeleteMantenimientoAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al eliminar mantenimiento", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al eliminar mantenimiento", ex, "MantenimientoService", "DeleteMantenimientoAsync");
                throw;
            }
        }

        /// <summary>
        /// Crea un nuevo mantenimiento
        /// </summary>
        public async Task<bool> CreateMantenimientoAsync(int idTipoMantenimiento, int idCliente, int idEquipo, string? nota = null, int credencialId = 0, CancellationToken cancellationToken = default)
        {
            try
            {
                // Construir la URL base usando el endpoint correcto
                var url = _endpoints.GetEndpoint("api", "Mantenimiento");

                // Agregar parámetros de consulta
                url = new ApiQueryBuilder()
                    .AddRequired("idTipoMantenimiento", idTipoMantenimiento)
                    .AddRequired("idCliente", idCliente)
                    .AddRequired("idEquipo", idEquipo)
                    .AddRequired("credencialId", credencialId)
                    .Add("nota", nota)
                    .Build(url);

                await _logger.LogInformationAsync($"Creando mantenimiento en: {url}", "MantenimientoService", "CreateMantenimientoAsync");

                // Realizar la petición POST
                using var response = await _http.PostAsync(url, null, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al crear mantenimiento. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "MantenimientoService",
                        "CreateMantenimientoAsync");
                    return false;
                }

                await _logger.LogInformationAsync("Mantenimiento creado correctamente", "MantenimientoService", "CreateMantenimientoAsync");
                return true;
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al crear mantenimiento", ex, "MantenimientoService", "CreateMantenimientoAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al crear mantenimiento", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al crear mantenimiento", ex, "MantenimientoService", "CreateMantenimientoAsync");
                throw;
            }
        }

        /// <summary>
        /// Actualiza el estado de atendido de un mantenimiento
        /// </summary>
        public async Task<bool> UpdateAtendidoAsync(int idMantenimiento, int idAtendio, CancellationToken cancellationToken = default)
        {
            try
            {
                // Construir la URL con parámetros de consulta escapados
                var baseUrl = _endpoints.GetEndpoint("api", "Mantenimiento");
                var url = $"{baseUrl}/atendido?idMantenimiento={Uri.EscapeDataString(idMantenimiento.ToString())}&idAtendio={Uri.EscapeDataString(idAtendio.ToString())}";

                await _logger.LogInformationAsync($"Actualizando estado atendido del mantenimiento {idMantenimiento} en: {url}", "MantenimientoService", "UpdateAtendidoAsync");

                // Realizar la petición PATCH
                var response = await _http.PatchAsync(url, null, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al actualizar estado atendido. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "MantenimientoService",
                        "UpdateAtendidoAsync");
                    return false;
                }

                await _logger.LogInformationAsync($"Estado atendido del mantenimiento {idMantenimiento} actualizado correctamente", "MantenimientoService", "UpdateAtendidoAsync");
                return true;
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al actualizar estado atendido", ex, "MantenimientoService", "UpdateAtendidoAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al actualizar estado atendido", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al actualizar estado atendido", ex, "MantenimientoService", "UpdateAtendidoAsync");
                throw;
            }
        }

        /// <summary>
        /// Obtiene los técnicos disponibles para atender un mantenimiento,
        /// filtrados por el área del equipo asociado.
        /// </summary>
        public async Task<List<TecnicoDisponibleDto>> GetTecnicosDisponiblesAsync(string identificador, CancellationToken cancellationToken = default)
        {
            try
            {
                var baseUrl = _endpoints.GetEndpoint("api", "Mantenimiento");
                var url = $"{baseUrl}/tecnicos?identificador={Uri.EscapeDataString(identificador)}";

                await _logger.LogInformationAsync(
                    $"Obteniendo técnicos disponibles para equipo {identificador} en: {url}",
                    "MantenimientoService",
                    "GetTecnicosDisponiblesAsync");

                var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al obtener técnicos disponibles. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "MantenimientoService",
                        "GetTecnicosDisponiblesAsync");
                    return new List<TecnicoDisponibleDto>();
                }

                var tecnicos = await response.Content.ReadFromJsonAsync<List<TecnicoDisponibleDto>>(cancellationToken: cancellationToken).ConfigureAwait(false);

                await _logger.LogInformationAsync(
                    $"Se obtuvieron {tecnicos?.Count ?? 0} técnicos disponibles para equipo {identificador}",
                    "MantenimientoService",
                    "GetTecnicosDisponiblesAsync");

                return tecnicos ?? new List<TecnicoDisponibleDto>();
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al obtener técnicos disponibles", ex, "MantenimientoService", "GetTecnicosDisponiblesAsync");
                return new List<TecnicoDisponibleDto>();
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al obtener técnicos disponibles", ex, "MantenimientoService", "GetTecnicosDisponiblesAsync");
                return new List<TecnicoDisponibleDto>();
            }
        }
    }
}
