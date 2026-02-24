using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.EndPointProvider;
using Advance_Control.Services.Logging;

namespace Advance_Control.Services.Proveedores
{
    /// <summary>
    /// Implementación del servicio de proveedores que se comunica con la API
    /// </summary>
    public class ProveedorService : IProveedorService
    {
        private readonly HttpClient _http;
        private readonly IApiEndpointProvider _endpoints;
        private readonly ILoggingService _logger;

        public ProveedorService(HttpClient http, IApiEndpointProvider endpoints, ILoggingService logger)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Crea un nuevo proveedor
        /// </summary>
        public async Task<bool> CreateProveedorAsync(string rfc, string? razonSocial, string? nombreComercial, string? nota, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = _endpoints.GetEndpoint("api", "proveedores");
                var queryParams = new List<string> { $"rfc={Uri.EscapeDataString(rfc)}" };

                if (!string.IsNullOrWhiteSpace(razonSocial))
                    queryParams.Add($"razonSocial={Uri.EscapeDataString(razonSocial)}");
                if (!string.IsNullOrWhiteSpace(nombreComercial))
                    queryParams.Add($"nombreComercial={Uri.EscapeDataString(nombreComercial)}");
                if (!string.IsNullOrWhiteSpace(nota))
                    queryParams.Add($"nota={Uri.EscapeDataString(nota)}");

                url = $"{url}?{string.Join("&", queryParams)}";

                await _logger.LogInformationAsync($"Creando proveedor en: {url}", "ProveedorService", "CreateProveedorAsync");

                var response = await _http.PostAsync(url, null, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync($"Error al crear proveedor. Status: {response.StatusCode}, Content: {errorContent}", null, "ProveedorService", "CreateProveedorAsync");
                    return false;
                }

                await _logger.LogInformationAsync("Proveedor creado correctamente", "ProveedorService", "CreateProveedorAsync");
                return true;
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al crear proveedor", ex, "ProveedorService", "CreateProveedorAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al crear proveedor", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al crear proveedor", ex, "ProveedorService", "CreateProveedorAsync");
                throw;
            }
        }

        /// <summary>
        /// Actualiza un proveedor existente
        /// </summary>
        public async Task<bool> UpdateProveedorAsync(int id, string? rfc, string? razonSocial, string? nombreComercial, string? nota, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = _endpoints.GetEndpoint("api", "proveedores");
                url = $"{url}/{id}";

                var queryParams = new List<string>();
                if (!string.IsNullOrWhiteSpace(rfc))
                    queryParams.Add($"rfc={Uri.EscapeDataString(rfc)}");
                if (!string.IsNullOrWhiteSpace(razonSocial))
                    queryParams.Add($"razonSocial={Uri.EscapeDataString(razonSocial)}");
                if (!string.IsNullOrWhiteSpace(nombreComercial))
                    queryParams.Add($"nombreComercial={Uri.EscapeDataString(nombreComercial)}");
                if (!string.IsNullOrWhiteSpace(nota))
                    queryParams.Add($"nota={Uri.EscapeDataString(nota)}");

                if (queryParams.Count > 0)
                    url = $"{url}?{string.Join("&", queryParams)}";

                await _logger.LogInformationAsync($"Actualizando proveedor {id} en: {url}", "ProveedorService", "UpdateProveedorAsync");

                var response = await _http.PutAsync(url, null, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync($"Error al actualizar proveedor. Status: {response.StatusCode}, Content: {errorContent}", null, "ProveedorService", "UpdateProveedorAsync");
                    return false;
                }

                await _logger.LogInformationAsync($"Proveedor {id} actualizado correctamente", "ProveedorService", "UpdateProveedorAsync");
                return true;
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al actualizar proveedor", ex, "ProveedorService", "UpdateProveedorAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al actualizar proveedor", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al actualizar proveedor", ex, "ProveedorService", "UpdateProveedorAsync");
                throw;
            }
        }

        /// <summary>
        /// Elimina (soft delete) un proveedor por su ID
        /// </summary>
        public async Task<bool> DeleteProveedorAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = _endpoints.GetEndpoint("api", "proveedores");
                url = $"{url}/{id}";

                await _logger.LogInformationAsync($"Eliminando proveedor {id} en: {url}", "ProveedorService", "DeleteProveedorAsync");

                var response = await _http.DeleteAsync(url, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync($"Error al eliminar proveedor. Status: {response.StatusCode}, Content: {errorContent}", null, "ProveedorService", "DeleteProveedorAsync");
                    return false;
                }

                await _logger.LogInformationAsync($"Proveedor {id} eliminado correctamente", "ProveedorService", "DeleteProveedorAsync");
                return true;
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al eliminar proveedor", ex, "ProveedorService", "DeleteProveedorAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al eliminar proveedor", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al eliminar proveedor", ex, "ProveedorService", "DeleteProveedorAsync");
                throw;
            }
        }

        /// <summary>
        /// Obtiene una lista de proveedores según los criterios de búsqueda proporcionados
        /// </summary>
        public async Task<List<ProveedorDto>> GetProveedoresAsync(ProveedorQueryDto? query = null, CancellationToken cancellationToken = default)
        {
            try
            {
                // Construir la URL base
                var url = _endpoints.GetEndpoint("api", "proveedores");

                // Agregar parámetros de consulta si existen
                if (query != null)
                {
                    var queryParams = new List<string>();

                    if (!string.IsNullOrWhiteSpace(query.Rfc))
                        queryParams.Add($"rfc={Uri.EscapeDataString(query.Rfc)}");

                    if (!string.IsNullOrWhiteSpace(query.RazonSocial))
                        queryParams.Add($"razonSocial={Uri.EscapeDataString(query.RazonSocial)}");

                    if (!string.IsNullOrWhiteSpace(query.NombreComercial))
                        queryParams.Add($"nombreComercial={Uri.EscapeDataString(query.NombreComercial)}");

                    if (!string.IsNullOrWhiteSpace(query.Nota))
                        queryParams.Add($"nota={Uri.EscapeDataString(query.Nota)}");

                    if (queryParams.Count > 0)
                    {
                        url = $"{url}?{string.Join("&", queryParams)}";
                    }
                }

                await _logger.LogInformationAsync($"Obteniendo proveedores desde: {url}", "ProveedorService", "GetProveedoresAsync");

                // Realizar la petición GET
                var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);

                // Verificar si la respuesta fue exitosa
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al obtener proveedores. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "ProveedorService",
                        "GetProveedoresAsync");
                    return new List<ProveedorDto>();
                }

                // Deserializar la respuesta
                var proveedores = await response.Content.ReadFromJsonAsync<List<ProveedorDto>>(cancellationToken: cancellationToken).ConfigureAwait(false);

                await _logger.LogInformationAsync($"Se obtuvieron {proveedores?.Count ?? 0} proveedores", "ProveedorService", "GetProveedoresAsync");

                return proveedores ?? new List<ProveedorDto>();
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al obtener proveedores", ex, "ProveedorService", "GetProveedoresAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al obtener proveedores", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al obtener proveedores", ex, "ProveedorService", "GetProveedoresAsync");
                throw;
            }
        }
    }
}
