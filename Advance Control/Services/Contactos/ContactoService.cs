using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.EndPointProvider;
using Advance_Control.Services.Logging;

namespace Advance_Control.Services.Contactos
{
    /// <summary>
    /// Implementación del servicio de contactos que se comunica con la API
    /// </summary>
    public class ContactoService : IContactoService
    {
        private readonly HttpClient _http;
        private readonly IApiEndpointProvider _endpoints;
        private readonly ILoggingService _logger;

        public ContactoService(HttpClient http, IApiEndpointProvider endpoints, ILoggingService logger)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Obtiene una lista de contactos según los criterios de búsqueda proporcionados
        /// </summary>
        public async Task<List<ContactoDto>> GetContactosAsync(ContactoQueryDto? query = null, CancellationToken cancellationToken = default)
        {
            try
            {
                // Construir la URL base
                var url = _endpoints.GetEndpoint("api", "Contacto");

                // Agregar parámetros de consulta si existen
                if (query != null)
                {
                    var queryParams = new List<string>();

                    if (query.ContactoId.HasValue)
                        queryParams.Add($"contactoId={query.ContactoId.Value}");

                    if (query.CredencialId.HasValue)
                        queryParams.Add($"credencialId={query.CredencialId.Value}");

                    if (!string.IsNullOrWhiteSpace(query.Nombre))
                        queryParams.Add($"nombre={Uri.EscapeDataString(query.Nombre)}");

                    if (!string.IsNullOrWhiteSpace(query.Apellido))
                        queryParams.Add($"apellido={Uri.EscapeDataString(query.Apellido)}");

                    if (!string.IsNullOrWhiteSpace(query.Correo))
                        queryParams.Add($"correo={Uri.EscapeDataString(query.Correo)}");

                    if (!string.IsNullOrWhiteSpace(query.Telefono))
                        queryParams.Add($"telefono={Uri.EscapeDataString(query.Telefono)}");

                    if (!string.IsNullOrWhiteSpace(query.Departamento))
                        queryParams.Add($"departamento={Uri.EscapeDataString(query.Departamento)}");

                    if (!string.IsNullOrWhiteSpace(query.CodigoInterno))
                        queryParams.Add($"codigoInterno={Uri.EscapeDataString(query.CodigoInterno)}");

                    if (query.IdProveedor.HasValue)
                        queryParams.Add($"idProveedor={query.IdProveedor.Value}");

                    if (!string.IsNullOrWhiteSpace(query.Cargo))
                        queryParams.Add($"cargo={Uri.EscapeDataString(query.Cargo)}");

                    if (query.IdCliente.HasValue)
                        queryParams.Add($"idCliente={query.IdCliente.Value}");

                    if (queryParams.Count > 0)
                    {
                        url = $"{url}?{string.Join("&", queryParams)}";
                    }
                }

                await _logger.LogInformationAsync($"Obteniendo contactos desde: {url}", "ContactoService", "GetContactosAsync");

                // Realizar la petición GET
                var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);

                // Verificar si la respuesta fue exitosa
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al obtener contactos. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "ContactoService",
                        "GetContactosAsync");
                    return new List<ContactoDto>();
                }

                // Deserializar la respuesta
                var contactos = await response.Content.ReadFromJsonAsync<List<ContactoDto>>(cancellationToken: cancellationToken).ConfigureAwait(false);

                await _logger.LogInformationAsync($"Se obtuvieron {contactos?.Count ?? 0} contactos", "ContactoService", "GetContactosAsync");

                return contactos ?? new List<ContactoDto>();
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al obtener contactos", ex, "ContactoService", "GetContactosAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al obtener contactos", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al obtener contactos", ex, "ContactoService", "GetContactosAsync");
                throw;
            }
        }

        /// <summary>
        /// Crea un nuevo contacto
        /// </summary>
        public async Task<ContactoOperationResponse> CreateContactoAsync(ContactoEditDto query, CancellationToken cancellationToken = default)
        {
            if (query == null)
                throw new ArgumentNullException(nameof(query));

            try
            {
                // Construir la URL con query parameters según el API controller
                var url = _endpoints.GetEndpoint("api", "Contacto");
                var queryParams = new List<string>();

                if (!string.IsNullOrWhiteSpace(query.Nombre))
                    queryParams.Add($"nombre={Uri.EscapeDataString(query.Nombre)}");

                if (query.CredencialId.HasValue)
                    queryParams.Add($"credencialId={query.CredencialId.Value}");

                if (!string.IsNullOrWhiteSpace(query.Apellido))
                    queryParams.Add($"apellido={Uri.EscapeDataString(query.Apellido)}");

                if (!string.IsNullOrWhiteSpace(query.Correo))
                    queryParams.Add($"correo={Uri.EscapeDataString(query.Correo)}");

                if (!string.IsNullOrWhiteSpace(query.Telefono))
                    queryParams.Add($"telefono={Uri.EscapeDataString(query.Telefono)}");

                if (!string.IsNullOrWhiteSpace(query.Departamento))
                    queryParams.Add($"departamento={Uri.EscapeDataString(query.Departamento)}");

                if (!string.IsNullOrWhiteSpace(query.CodigoInterno))
                    queryParams.Add($"codigoInterno={Uri.EscapeDataString(query.CodigoInterno)}");

                if (query.Activo.HasValue)
                    queryParams.Add($"activo={query.Activo.Value.ToString().ToLower()}");

                if (!string.IsNullOrWhiteSpace(query.Notas))
                    queryParams.Add($"notas={Uri.EscapeDataString(query.Notas)}");

                if (query.IdProveedor.HasValue)
                    queryParams.Add($"idProveedor={query.IdProveedor.Value}");

                if (!string.IsNullOrWhiteSpace(query.Cargo))
                    queryParams.Add($"cargo={Uri.EscapeDataString(query.Cargo)}");

                if (query.IdCliente.HasValue)
                    queryParams.Add($"idCliente={query.IdCliente.Value}");

                if (queryParams.Count > 0)
                {
                    url = $"{url}?{string.Join("&", queryParams)}";
                }

                await _logger.LogInformationAsync($"Creando contacto en: {url}", "ContactoService", "CreateContactoAsync");

                // Realizar la petición POST (sin body, los parámetros van en la URL)
                var response = await _http.PostAsync(url, null, cancellationToken).ConfigureAwait(false);

                // Verificar si la respuesta fue exitosa
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al crear contacto. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "ContactoService",
                        "CreateContactoAsync");
                    return new ContactoOperationResponse { Success = false, Message = errorContent };
                }

                // Deserializar la respuesta
                var result = await response.Content.ReadFromJsonAsync<ContactoOperationResponse>(cancellationToken: cancellationToken).ConfigureAwait(false);

                await _logger.LogInformationAsync("Contacto creado exitosamente", "ContactoService", "CreateContactoAsync");

                return result ?? new ContactoOperationResponse { Success = true, Message = "Contacto creado correctamente" };
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al crear contacto", ex, "ContactoService", "CreateContactoAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al crear contacto", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al crear contacto", ex, "ContactoService", "CreateContactoAsync");
                throw;
            }
        }

        /// <summary>
        /// Actualiza un contacto por su ID
        /// </summary>
        public async Task<ContactoOperationResponse> UpdateContactoAsync(ContactoEditDto query, CancellationToken cancellationToken = default)
        {
            if (query == null)
                throw new ArgumentNullException(nameof(query));

            try
            {
                // Construir la URL con query parameters según el API controller
                var url = $"{_endpoints.GetEndpoint("api", "Contacto")}/{query.ContactoId}";
                var queryParams = new List<string>();

                if (query.CredencialId.HasValue)
                    queryParams.Add($"credencialId={query.CredencialId.Value}");

                if (!string.IsNullOrWhiteSpace(query.Nombre))
                    queryParams.Add($"nombre={Uri.EscapeDataString(query.Nombre)}");

                if (!string.IsNullOrWhiteSpace(query.Apellido))
                    queryParams.Add($"apellido={Uri.EscapeDataString(query.Apellido)}");

                if (!string.IsNullOrWhiteSpace(query.Correo))
                    queryParams.Add($"correo={Uri.EscapeDataString(query.Correo)}");

                if (!string.IsNullOrWhiteSpace(query.Telefono))
                    queryParams.Add($"telefono={Uri.EscapeDataString(query.Telefono)}");

                if (!string.IsNullOrWhiteSpace(query.Departamento))
                    queryParams.Add($"departamento={Uri.EscapeDataString(query.Departamento)}");

                if (!string.IsNullOrWhiteSpace(query.CodigoInterno))
                    queryParams.Add($"codigoInterno={Uri.EscapeDataString(query.CodigoInterno)}");

                if (query.Activo.HasValue)
                    queryParams.Add($"activo={query.Activo.Value.ToString().ToLower()}");

                if (!string.IsNullOrWhiteSpace(query.Notas))
                    queryParams.Add($"notas={Uri.EscapeDataString(query.Notas)}");

                if (query.IdProveedor.HasValue)
                    queryParams.Add($"idProveedor={query.IdProveedor.Value}");

                if (!string.IsNullOrWhiteSpace(query.Cargo))
                    queryParams.Add($"cargo={Uri.EscapeDataString(query.Cargo)}");

                if (query.IdCliente.HasValue)
                    queryParams.Add($"idCliente={query.IdCliente.Value}");

                if (queryParams.Count > 0)
                {
                    url = $"{url}?{string.Join("&", queryParams)}";
                }

                await _logger.LogInformationAsync($"Actualizando contacto en: {url}", "ContactoService", "UpdateContactoAsync");

                // Realizar la petición PUT (sin body, los parámetros van en la URL)
                var response = await _http.PutAsync(url, null, cancellationToken).ConfigureAwait(false);

                // Verificar si la respuesta fue exitosa
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al actualizar contacto. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "ContactoService",
                        "UpdateContactoAsync");
                    return new ContactoOperationResponse { Success = false, Message = errorContent };
                }

                // Deserializar la respuesta
                var result = await response.Content.ReadFromJsonAsync<ContactoOperationResponse>(cancellationToken: cancellationToken).ConfigureAwait(false);

                await _logger.LogInformationAsync($"Contacto {query.ContactoId} actualizado exitosamente", "ContactoService", "UpdateContactoAsync");

                return result ?? new ContactoOperationResponse { Success = true, Message = "Contacto actualizado exitosamente" };
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al actualizar contacto", ex, "ContactoService", "UpdateContactoAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al actualizar contacto", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al actualizar contacto", ex, "ContactoService", "UpdateContactoAsync");
                throw;
            }
        }

        /// <summary>
        /// Elimina (soft delete) un contacto por su ID
        /// </summary>
        public async Task<ContactoOperationResponse> DeleteContactoAsync(long contactoId, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"{_endpoints.GetEndpoint("api", "Contacto")}/{contactoId}";

                await _logger.LogInformationAsync($"Eliminando contacto en: {url}", "ContactoService", "DeleteContactoAsync");

                // Realizar la petición DELETE
                var response = await _http.DeleteAsync(url, cancellationToken).ConfigureAwait(false);

                // Verificar si la respuesta fue exitosa
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al eliminar contacto. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "ContactoService",
                        "DeleteContactoAsync");
                    return new ContactoOperationResponse { Success = false, Message = errorContent };
                }

                // Deserializar la respuesta
                var result = await response.Content.ReadFromJsonAsync<ContactoOperationResponse>(cancellationToken: cancellationToken).ConfigureAwait(false);

                await _logger.LogInformationAsync($"Contacto {contactoId} eliminado exitosamente", "ContactoService", "DeleteContactoAsync");

                return result ?? new ContactoOperationResponse { Success = true, Message = "Contacto eliminado exitosamente" };
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al eliminar contacto", ex, "ContactoService", "DeleteContactoAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al eliminar contacto", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al eliminar contacto", ex, "ContactoService", "DeleteContactoAsync");
                throw;
            }
        }
    }
}
