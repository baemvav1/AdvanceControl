using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.EndPointProvider;
using Advance_Control.Services.Logging;

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
                    var queryParams = new List<string>();

                    if (!string.IsNullOrWhiteSpace(query.Identificador))
                        queryParams.Add($"identificador={Uri.EscapeDataString(query.Identificador)}");

                    if (query.IdCliente > 0)
                        queryParams.Add($"idCliente={query.IdCliente}");

                    if (queryParams.Count > 0)
                    {
                        url = $"{url}?{string.Join("&", queryParams)}";
                    }
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
                var mantenimientos = await response.Content.ReadFromJsonAsync<List<MantenimientoDto>>(cancellationToken: cancellationToken).ConfigureAwait(false);

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

                var response = await _http.DeleteAsync(url, cancellationToken).ConfigureAwait(false);

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
        public async Task<bool> CreateMantenimientoAsync(int idTipoMantenimiento, int idCliente, int idEquipo, double costo, string? nota = null, CancellationToken cancellationToken = default)
        {
            try
            {
                // Construir la URL base usando el endpoint correcto
                var url = _endpoints.GetEndpoint("api", "Mantenimiento");

                // Agregar parámetros de consulta
                var queryParams = new List<string>
                {
                    $"idTipoMantenimiento={idTipoMantenimiento}",
                    $"idCliente={idCliente}",
                    $"idEquipo={idEquipo}",
                    $"costo={costo}"
                };

                if (!string.IsNullOrWhiteSpace(nota))
                    queryParams.Add($"nota={Uri.EscapeDataString(nota)}");

                url = $"{url}?{string.Join("&", queryParams)}";

                await _logger.LogInformationAsync($"Creando mantenimiento en: {url}", "MantenimientoService", "CreateMantenimientoAsync");

                // Realizar la petición POST
                var response = await _http.PostAsync(url, null, cancellationToken).ConfigureAwait(false);

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
    }
}
