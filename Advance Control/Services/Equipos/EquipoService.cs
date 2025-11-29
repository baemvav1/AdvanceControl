using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.EndPointProvider;
using Advance_Control.Services.Logging;

namespace Advance_Control.Services.Equipos
{
    /// <summary>
    /// Implementación del servicio de equipos que se comunica con la API
    /// </summary>
    public class EquipoService : IEquipoService
    {
        private readonly HttpClient _http;
        private readonly IApiEndpointProvider _endpoints;
        private readonly ILoggingService _logger;

        public EquipoService(HttpClient http, IApiEndpointProvider endpoints, ILoggingService logger)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Obtiene una lista de equipos según los criterios de búsqueda proporcionados
        /// </summary>
        public async Task<List<EquipoDto>> GetEquiposAsync(EquipoQueryDto? query = null, CancellationToken cancellationToken = default)
        {
            try
            {
                // Construir la URL base usando el endpoint correcto
                var url = _endpoints.GetEndpoint("api", "equipo_crud");

                // Agregar parámetros de consulta si existen
                if (query != null)
                {
                    var queryParams = new List<string>();

                    if (!string.IsNullOrWhiteSpace(query.Marca))
                        queryParams.Add($"marca={Uri.EscapeDataString(query.Marca)}");

                    if (query.Creado.HasValue)
                        queryParams.Add($"creado={query.Creado.Value}");

                    if (!string.IsNullOrWhiteSpace(query.Descripcion))
                        queryParams.Add($"descripcion={Uri.EscapeDataString(query.Descripcion)}");

                    if (!string.IsNullOrWhiteSpace(query.Identificador))
                        queryParams.Add($"identificador={Uri.EscapeDataString(query.Identificador)}");

                    if (queryParams.Count > 0)
                    {
                        url = $"{url}?{string.Join("&", queryParams)}";
                    }
                }

                await _logger.LogInformationAsync($"Obteniendo equipos desde: {url}", "EquipoService", "GetEquiposAsync");

                // Realizar la petición GET
                var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);

                // Verificar si la respuesta fue exitosa
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al obtener equipos. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "EquipoService",
                        "GetEquiposAsync");
                    return new List<EquipoDto>();
                }

                // Deserializar la respuesta
                var equipos = await response.Content.ReadFromJsonAsync<List<EquipoDto>>(cancellationToken: cancellationToken).ConfigureAwait(false);

                await _logger.LogInformationAsync($"Se obtuvieron {equipos?.Count ?? 0} equipos", "EquipoService", "GetEquiposAsync");

                return equipos ?? new List<EquipoDto>();
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al obtener equipos", ex, "EquipoService", "GetEquiposAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al obtener equipos", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al obtener equipos", ex, "EquipoService", "GetEquiposAsync");
                throw;
            }
        }

        /// <summary>
        /// Elimina (soft delete) un equipo por su ID
        /// </summary>
        public async Task<bool> DeleteEquipoAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = _endpoints.GetEndpoint("api", "equipo_crud", id.ToString());

                await _logger.LogInformationAsync($"Eliminando equipo {id} en: {url}", "EquipoService", "DeleteEquipoAsync");

                var response = await _http.DeleteAsync(url, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al eliminar equipo. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "EquipoService",
                        "DeleteEquipoAsync");
                    return false;
                }

                await _logger.LogInformationAsync($"Equipo {id} eliminado correctamente", "EquipoService", "DeleteEquipoAsync");
                return true;
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al eliminar equipo", ex, "EquipoService", "DeleteEquipoAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al eliminar equipo", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al eliminar equipo", ex, "EquipoService", "DeleteEquipoAsync");
                throw;
            }
        }

        /// <summary>
        /// Actualiza un equipo existente
        /// </summary>
        public async Task<bool> UpdateEquipoAsync(int id, EquipoQueryDto query, CancellationToken cancellationToken = default)
        {
            try
            {
                // Construir la URL con parámetros de consulta
                var url = _endpoints.GetEndpoint("api", "equipo_crud", id.ToString());

                var queryParams = new List<string>();

                if (!string.IsNullOrWhiteSpace(query.Marca))
                    queryParams.Add($"marca={Uri.EscapeDataString(query.Marca)}");

                if (query.Creado.HasValue)
                    queryParams.Add($"creado={query.Creado.Value}");

                if (!string.IsNullOrWhiteSpace(query.Descripcion))
                    queryParams.Add($"descripcion={Uri.EscapeDataString(query.Descripcion)}");

                if (!string.IsNullOrWhiteSpace(query.Identificador))
                    queryParams.Add($"identificador={Uri.EscapeDataString(query.Identificador)}");

                if (queryParams.Count > 0)
                {
                    url = $"{url}?{string.Join("&", queryParams)}";
                }

                await _logger.LogInformationAsync($"Actualizando equipo {id} en: {url}", "EquipoService", "UpdateEquipoAsync");

                var response = await _http.PutAsync(url, null, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al actualizar equipo. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "EquipoService",
                        "UpdateEquipoAsync");
                    return false;
                }

                await _logger.LogInformationAsync($"Equipo {id} actualizado correctamente", "EquipoService", "UpdateEquipoAsync");
                return true;
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al actualizar equipo", ex, "EquipoService", "UpdateEquipoAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al actualizar equipo", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al actualizar equipo", ex, "EquipoService", "UpdateEquipoAsync");
                throw;
            }
        }

        /// <summary>
        /// Crea un nuevo equipo
        /// </summary>
        public async Task<bool> CreateEquipoAsync(string marca, int creado, string? descripcion, string identificador, bool estatus = true, CancellationToken cancellationToken = default)
        {
            try
            {
                // Construir la URL base usando el endpoint correcto
                var url = _endpoints.GetEndpoint("api", "equipo_crud");

                // Agregar parámetros de consulta
                var queryParams = new List<string>
                {
                    $"marca={Uri.EscapeDataString(marca)}",
                    $"creado={creado}",
                    $"identificador={Uri.EscapeDataString(identificador)}",
                    $"estatus={estatus.ToString().ToLower()}"
                };

                if (!string.IsNullOrWhiteSpace(descripcion))
                    queryParams.Add($"descripcion={Uri.EscapeDataString(descripcion)}");

                url = $"{url}?{string.Join("&", queryParams)}";

                await _logger.LogInformationAsync($"Creando equipo en: {url}", "EquipoService", "CreateEquipoAsync");

                // Realizar la petición POST
                var response = await _http.PostAsync(url, null, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al crear equipo. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "EquipoService",
                        "CreateEquipoAsync");
                    return false;
                }

                await _logger.LogInformationAsync("Equipo creado correctamente", "EquipoService", "CreateEquipoAsync");
                return true;
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al crear equipo", ex, "EquipoService", "CreateEquipoAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al crear equipo", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al crear equipo", ex, "EquipoService", "CreateEquipoAsync");
                throw;
            }
        }
    }
}
