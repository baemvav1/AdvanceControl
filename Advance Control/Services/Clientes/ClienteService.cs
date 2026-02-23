using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.EndPointProvider;
using Advance_Control.Services.Logging;

namespace Advance_Control.Services.Clientes
{
    /// <summary>
    /// Implementación del servicio de clientes que se comunica con la API
    /// </summary>
    public class ClienteService : IClienteService
    {
        private readonly HttpClient _http;
        private readonly IApiEndpointProvider _endpoints;
        private readonly ILoggingService _logger;

        public ClienteService(HttpClient http, IApiEndpointProvider endpoints, ILoggingService logger)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Obtiene una lista de clientes según los criterios de búsqueda proporcionados
        /// </summary>
        public async Task<List<CustomerDto>> GetClientesAsync(ClienteQueryDto? query = null, CancellationToken cancellationToken = default)
        {
            try
            {
                // Construir la URL base
                var url = _endpoints.GetEndpoint("api", "Clientes");

                // Agregar parámetros de consulta si existen
                if (query != null)
                {
                    var queryParams = new List<string>();

                    if (!string.IsNullOrWhiteSpace(query.Rfc))
                        queryParams.Add($"rfc={Uri.EscapeDataString(query.Rfc)}");

                    if (!string.IsNullOrWhiteSpace(query.Notas))
                        queryParams.Add($"notas={Uri.EscapeDataString(query.Notas)}");

                    if (query.Prioridad.HasValue)
                        queryParams.Add($"prioridad={query.Prioridad.Value}");

                    if (queryParams.Count > 0)
                    {
                        url = $"{url}?{string.Join("&", queryParams)}";
                    }
                }

                await _logger.LogInformationAsync($"Obteniendo clientes desde: {url}", "ClienteService", "GetClientesAsync");

                // Realizar la petición GET
                var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);

                // Verificar si la respuesta fue exitosa
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al obtener clientes. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "ClienteService",
                        "GetClientesAsync");
                    return new List<CustomerDto>();
                }

                // Deserializar la respuesta
                var clientes = await response.Content.ReadFromJsonAsync<List<CustomerDto>>(cancellationToken: cancellationToken).ConfigureAwait(false);

                await _logger.LogInformationAsync($"Se obtuvieron {clientes?.Count ?? 0} clientes", "ClienteService", "GetClientesAsync");

                return clientes ?? new List<CustomerDto>();
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al obtener clientes", ex, "ClienteService", "GetClientesAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al obtener clientes", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al obtener clientes", ex, "ClienteService", "GetClientesAsync");
                throw;
            }
        }


        /// <summary>
        /// Obtiene una lista de clientes según los criterios de búsqueda proporcionados
        /// </summary>
        public async Task<List<CustomerDto>> GetClienteByIdAsync(int idCliente, CancellationToken cancellationToken = default)
        {
            try
            {
                // Construir la URL base
                var url = $"{_endpoints.GetEndpoint("api", "Clientes")}/{idCliente}";

                // Agregar parámetros de consulta si existen
                                

                await _logger.LogInformationAsync($"Obteniendo clientes desde: {url}", "ClienteService", "GetClienteByIdAsync");

                // Realizar la petición GET
                var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);

                // Verificar si la respuesta fue exitosa
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al obtener clientes. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "ClienteService",
                        "GetClienteByIdAsync");
                    return new List<CustomerDto>();
                }

                // Deserializar la respuesta (el API retorna un único objeto CustomerDto)
                var cliente = await response.Content.ReadFromJsonAsync<CustomerDto>(cancellationToken: cancellationToken).ConfigureAwait(false);

                await _logger.LogInformationAsync($"Se obtuvo {(cliente != null ? 1 : 0)} cliente", "ClienteService", "GetClienteByIdAsync");

                return cliente != null ? new List<CustomerDto> { cliente } : new List<CustomerDto>();
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al obtener clientes", ex, "ClienteService", "GetClienteByIdAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al obtener clientes", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al obtener clientes", ex, "ClienteService", "GetClienteByIdAsync");
                throw;
            }
        }

        /// <summary>
        /// Crea un nuevo cliente usando el procedimiento almacenado sp_cliente_edit
        /// </summary>
        public async Task<ClienteOperationResponse> CreateClienteAsync(ClienteEditDto query, CancellationToken cancellationToken = default)
        {
            if (query == null)
                throw new ArgumentNullException(nameof(query));

            try
            {
                var url = _endpoints.GetEndpoint("api", "Clientes");

                // Construir parámetros de consulta según lo que el API espera [FromQuery]
                var queryParams = new List<string>();
                if (!string.IsNullOrWhiteSpace(query.Rfc))
                    queryParams.Add($"rfc={Uri.EscapeDataString(query.Rfc)}");
                if (!string.IsNullOrWhiteSpace(query.RazonSocial))
                    queryParams.Add($"razonSocial={Uri.EscapeDataString(query.RazonSocial)}");
                if (!string.IsNullOrWhiteSpace(query.NombreComercial))
                    queryParams.Add($"nombreComercial={Uri.EscapeDataString(query.NombreComercial)}");
                if (!string.IsNullOrWhiteSpace(query.RegimenFiscal))
                    queryParams.Add($"regimenFiscal={Uri.EscapeDataString(query.RegimenFiscal)}");
                if (!string.IsNullOrWhiteSpace(query.UsoCfdi))
                    queryParams.Add($"usoCfdi={Uri.EscapeDataString(query.UsoCfdi)}");
                if (query.DiasCredito.HasValue)
                    queryParams.Add($"diasCredito={query.DiasCredito.Value}");
                if (query.LimiteCredito.HasValue)
                    queryParams.Add($"limiteCredito={query.LimiteCredito.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
                if (query.Prioridad.HasValue)
                    queryParams.Add($"prioridad={query.Prioridad.Value}");
                if (query.CredencialId.HasValue)
                    queryParams.Add($"credencialId={query.CredencialId.Value}");
                if (!string.IsNullOrWhiteSpace(query.Notas))
                    queryParams.Add($"notas={Uri.EscapeDataString(query.Notas)}");
                if (query.IdUsuario.HasValue)
                    queryParams.Add($"idUsuario={query.IdUsuario.Value}");
                if (queryParams.Count > 0)
                    url = $"{url}?{string.Join("&", queryParams)}";

                await _logger.LogInformationAsync($"Creando cliente en: {url}", "ClienteService", "CreateClienteAsync");

                // Realizar la petición POST
                var response = await _http.PostAsync(url, null, cancellationToken).ConfigureAwait(false);

                // Verificar si la respuesta fue exitosa
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al crear cliente. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "ClienteService",
                        "CreateClienteAsync");
                    return new ClienteOperationResponse { Success = false, Message = errorContent };
                }

                // Deserializar la respuesta
                var result = await response.Content.ReadFromJsonAsync<ClienteOperationResponse>(cancellationToken: cancellationToken).ConfigureAwait(false);

                await _logger.LogInformationAsync("Cliente creado exitosamente", "ClienteService", "CreateClienteAsync");

                return result ?? new ClienteOperationResponse { Success = true, Message = "Cliente creado correctamente" };
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al crear cliente", ex, "ClienteService", "CreateClienteAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al crear cliente", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al crear cliente", ex, "ClienteService", "CreateClienteAsync");
                throw;
            }
        }

        /// <summary>
        /// Actualiza un cliente por su ID
        /// </summary>
        public async Task<ClienteOperationResponse> UpdateClienteAsync(ClienteEditDto query, CancellationToken cancellationToken = default)
        {
            if (query == null)
                throw new ArgumentNullException(nameof(query));

            try
            {
                var url = $"{_endpoints.GetEndpoint("api", "Clientes")}/{query.IdCliente}";

                // Construir parámetros de consulta según lo que el API espera [FromQuery]
                var queryParams = new List<string>();
                if (!string.IsNullOrWhiteSpace(query.Rfc))
                    queryParams.Add($"rfc={Uri.EscapeDataString(query.Rfc)}");
                if (!string.IsNullOrWhiteSpace(query.RazonSocial))
                    queryParams.Add($"razonSocial={Uri.EscapeDataString(query.RazonSocial)}");
                if (!string.IsNullOrWhiteSpace(query.NombreComercial))
                    queryParams.Add($"nombreComercial={Uri.EscapeDataString(query.NombreComercial)}");
                if (!string.IsNullOrWhiteSpace(query.RegimenFiscal))
                    queryParams.Add($"regimenFiscal={Uri.EscapeDataString(query.RegimenFiscal)}");
                if (!string.IsNullOrWhiteSpace(query.UsoCfdi))
                    queryParams.Add($"usoCfdi={Uri.EscapeDataString(query.UsoCfdi)}");
                if (query.DiasCredito.HasValue)
                    queryParams.Add($"diasCredito={query.DiasCredito.Value}");
                if (query.LimiteCredito.HasValue)
                    queryParams.Add($"limiteCredito={query.LimiteCredito.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
                if (query.Prioridad.HasValue)
                    queryParams.Add($"prioridad={query.Prioridad.Value}");
                if (query.CredencialId.HasValue)
                    queryParams.Add($"credencialId={query.CredencialId.Value}");
                if (!string.IsNullOrWhiteSpace(query.Notas))
                    queryParams.Add($"notas={Uri.EscapeDataString(query.Notas)}");
                if (query.IdUsuario.HasValue)
                    queryParams.Add($"idUsuario={query.IdUsuario.Value}");
                if (queryParams.Count > 0)
                    url = $"{url}?{string.Join("&", queryParams)}";

                await _logger.LogInformationAsync($"Actualizando cliente en: {url}", "ClienteService", "UpdateClienteAsync");

                // Realizar la petición PUT
                var response = await _http.PutAsync(url, null, cancellationToken).ConfigureAwait(false);

                // Verificar si la respuesta fue exitosa
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al actualizar cliente. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "ClienteService",
                        "UpdateClienteAsync");
                    return new ClienteOperationResponse { Success = false, Message = errorContent };
                }

                // Deserializar la respuesta
                var result = await response.Content.ReadFromJsonAsync<ClienteOperationResponse>(cancellationToken: cancellationToken).ConfigureAwait(false);

                await _logger.LogInformationAsync($"Cliente {query.IdCliente} actualizado exitosamente", "ClienteService", "UpdateClienteAsync");

                return result ?? new ClienteOperationResponse { Success = true, Message = "Cliente actualizado exitosamente" };
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al actualizar cliente", ex, "ClienteService", "UpdateClienteAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al actualizar cliente", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al actualizar cliente", ex, "ClienteService", "UpdateClienteAsync");
                throw;
            }
        }

        /// <summary>
        /// Elimina (soft delete) un cliente por su ID
        /// </summary>
        public async Task<ClienteOperationResponse> DeleteClienteAsync(int idCliente, int? idUsuario, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"{_endpoints.GetEndpoint("api", "Clientes")}/{idCliente}";

                if (idUsuario.HasValue)
                {
                    url = $"{url}?idUsuario={idUsuario.Value}";
                }

                await _logger.LogInformationAsync($"Eliminando cliente en: {url}", "ClienteService", "DeleteClienteAsync");

                // Realizar la petición DELETE
                var response = await _http.DeleteAsync(url, cancellationToken).ConfigureAwait(false);

                // Verificar si la respuesta fue exitosa
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al eliminar cliente. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "ClienteService",
                        "DeleteClienteAsync");
                    return new ClienteOperationResponse { Success = false, Message = errorContent };
                }

                // Deserializar la respuesta
                var result = await response.Content.ReadFromJsonAsync<ClienteOperationResponse>(cancellationToken: cancellationToken).ConfigureAwait(false);

                await _logger.LogInformationAsync($"Cliente {idCliente} eliminado exitosamente", "ClienteService", "DeleteClienteAsync");

                return result ?? new ClienteOperationResponse { Success = true, Message = "Cliente eliminado exitosamente" };
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al eliminar cliente", ex, "ClienteService", "DeleteClienteAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al eliminar cliente", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al eliminar cliente", ex, "ClienteService", "DeleteClienteAsync");
                throw;
            }
        }
    }
}
