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

namespace Advance_Control.Services.Servicios
{
    /// <summary>
    /// Implementación del servicio de servicios que se comunica con la API
    /// </summary>
    public class ServicioService : IServicioService
    {
        private readonly HttpClient _http;
        private readonly IApiEndpointProvider _endpoints;
        private readonly ILoggingService _logger;

        public ServicioService(HttpClient http, IApiEndpointProvider endpoints, ILoggingService logger)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Obtiene una lista de servicios según los criterios de búsqueda proporcionados
        /// </summary>
        public async Task<List<ServicioDto>> GetServiciosAsync(ServicioQueryDto? query = null, CancellationToken cancellationToken = default)
        {
            try
            {
                // Construir la URL base usando el endpoint correcto
                var url = _endpoints.GetEndpoint("api", "servicio");

                // Agregar parámetros de consulta si existen
                if (query != null)
                {
                    url = new ApiQueryBuilder()
                        .Add("concepto", query.Concepto)
                        .Add("descripcion", query.Descripcion)
                        .Add("costo", query.Costo)
                        .Build(url);
                }

                await _logger.LogInformationAsync($"Obteniendo servicios desde: {url}", "ServicioService", "GetServiciosAsync");

                // Realizar la petición GET
                var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);

                // Verificar si la respuesta fue exitosa
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al obtener servicios. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "ServicioService",
                        "GetServiciosAsync");
                    return new List<ServicioDto>();
                }

                // Deserializar la respuesta
                var servicios = await response.Content.ReadFromJsonAsync<List<ServicioDto>>(cancellationToken: cancellationToken).ConfigureAwait(false);

                await _logger.LogInformationAsync($"Se obtuvieron {servicios?.Count ?? 0} servicios", "ServicioService", "GetServiciosAsync");

                return servicios ?? new List<ServicioDto>();
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al obtener servicios", ex, "ServicioService", "GetServiciosAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al obtener servicios", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al obtener servicios", ex, "ServicioService", "GetServiciosAsync");
                throw;
            }
        }

        /// <summary>
        /// Elimina (soft delete) un servicio por su ID
        /// </summary>
        public async Task<bool> DeleteServicioAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = _endpoints.GetEndpoint("api", "servicio", id.ToString());

                await _logger.LogInformationAsync($"Eliminando servicio {id} en: {url}", "ServicioService", "DeleteServicioAsync");

                using var response = await _http.DeleteAsync(url, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al eliminar servicio. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "ServicioService",
                        "DeleteServicioAsync");
                    return false;
                }

                await _logger.LogInformationAsync($"Servicio {id} eliminado correctamente", "ServicioService", "DeleteServicioAsync");
                return true;
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al eliminar servicio", ex, "ServicioService", "DeleteServicioAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al eliminar servicio", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al eliminar servicio", ex, "ServicioService", "DeleteServicioAsync");
                throw;
            }
        }

        /// <summary>
        /// Actualiza un servicio existente
        /// </summary>
        public async Task<bool> UpdateServicioAsync(int id, ServicioQueryDto query, CancellationToken cancellationToken = default)
        {
            try
            {
                // Construir la URL con parámetros de consulta
                var url = _endpoints.GetEndpoint("api", "servicio", id.ToString());

                url = new ApiQueryBuilder()
                    .Add("concepto", query.Concepto)
                    .Add("descripcion", query.Descripcion)
                    .Add("costo", query.Costo)
                    .Build(url);

                await _logger.LogInformationAsync($"Actualizando servicio {id} en: {url}", "ServicioService", "UpdateServicioAsync");

                using var response = await _http.PutAsync(url, null, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al actualizar servicio. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "ServicioService",
                        "UpdateServicioAsync");
                    return false;
                }

                await _logger.LogInformationAsync($"Servicio {id} actualizado correctamente", "ServicioService", "UpdateServicioAsync");
                return true;
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al actualizar servicio", ex, "ServicioService", "UpdateServicioAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al actualizar servicio", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al actualizar servicio", ex, "ServicioService", "UpdateServicioAsync");
                throw;
            }
        }

        /// <summary>
        /// Crea un nuevo servicio
        /// </summary>
        public async Task<bool> CreateServicioAsync(string concepto, string descripcion, double costo, bool estatus = true, CancellationToken cancellationToken = default)
        {
            try
            {
                // Construir la URL base usando el endpoint correcto
                var url = _endpoints.GetEndpoint("api", "servicio");

                // Agregar parámetros de consulta
                url = new ApiQueryBuilder()
                    .Add("concepto", concepto)
                    .Add("descripcion", descripcion)
                    .AddRequired("costo", costo)
                    .AddRequired("estatus", estatus)
                    .Build(url);

                await _logger.LogInformationAsync($"Creando servicio en: {url}", "ServicioService", "CreateServicioAsync");

                // Realizar la petición POST
                using var response = await _http.PostAsync(url, null, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al crear servicio. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "ServicioService",
                        "CreateServicioAsync");
                    return false;
                }

                await _logger.LogInformationAsync("Servicio creado correctamente", "ServicioService", "CreateServicioAsync");
                return true;
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al crear servicio", ex, "ServicioService", "CreateServicioAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al crear servicio", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al crear servicio", ex, "ServicioService", "CreateServicioAsync");
                throw;
            }
        }
    }
}
