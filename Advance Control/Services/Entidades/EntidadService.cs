using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.EndPointProvider;
using Advance_Control.Services.Logging;
using Advance_Control.Utilities;

namespace Advance_Control.Services.Entidades
{
    /// <summary>
    /// Implementación del servicio de entidades que se comunica con la API
    /// </summary>
    public class EntidadService : IEntidadService
    {
        private readonly HttpClient _http;
        private readonly IApiEndpointProvider _endpoints;
        private readonly ILoggingService _logger;

        public EntidadService(HttpClient http, IApiEndpointProvider endpoints, ILoggingService logger)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Obtiene una lista de entidades según los criterios de búsqueda proporcionados
        /// </summary>
        public async Task<List<EntidadDto>> GetEntidadesAsync(EntidadQueryDto? query = null, CancellationToken cancellationToken = default)
        {
            try
            {
                // Construir la URL base
                var url = _endpoints.GetEndpoint("api", "entidad");

                // Agregar parámetros de consulta si existen
                if (query != null)
                {
                    url = new ApiQueryBuilder()
                        .Add("nombreComercial", query.NombreComercial)
                        .Add("razonSocial", query.RazonSocial)
                        .Add("rfc", query.RFC)
                        .Add("cp", query.CP)
                        .Add("estado", query.Estado)
                        .Add("ciudad", query.Ciudad)
                        .Add("estatus", query.Estatus)
                        .Build(url);
                }

                await _logger.LogInformationAsync($"Obteniendo entidades desde: {url}", "EntidadService", "GetEntidadesAsync");

                // Realizar la petición GET
                var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);

                // Verificar si la respuesta fue exitosa
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al obtener entidades. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "EntidadService",
                        "GetEntidadesAsync");
                    return new List<EntidadDto>();
                }

                // Deserializar la respuesta
                var entidades = await response.Content.ReadFromJsonAsync<List<EntidadDto>>(cancellationToken: cancellationToken).ConfigureAwait(false);

                await _logger.LogInformationAsync($"Se obtuvieron {entidades?.Count ?? 0} entidades", "EntidadService", "GetEntidadesAsync");

                return entidades ?? new List<EntidadDto>();
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al obtener entidades", ex, "EntidadService", "GetEntidadesAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al obtener entidades", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al obtener entidades", ex, "EntidadService", "GetEntidadesAsync");
                throw;
            }
        }

        /// <summary>
        /// Crea una nueva entidad
        /// </summary>
        public async Task<ApiResponse> CreateEntidadAsync(
            string nombreComercial,
            string razonSocial,
            string? rfc = null,
            string? cp = null,
            string? estado = null,
            string? ciudad = null,
            string? pais = null,
            string? calle = null,
            string? numExt = null,
            string? numInt = null,
            string? colonia = null,
            string? apoderado = null,
            string? regimenFiscal = null,
            bool? esPredeterminada = null,
            string? serieFacturas = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var url = _endpoints.GetEndpoint("api", "entidad");

                // Construir parámetros de consulta
                url = new ApiQueryBuilder()
                    .Add("nombreComercial", nombreComercial)
                    .Add("razonSocial", razonSocial)
                    .Add("rfc", rfc)
                    .Add("cp", cp)
                    .Add("estado", estado)
                    .Add("ciudad", ciudad)
                    .Add("pais", pais)
                    .Add("calle", calle)
                    .Add("nomExt", numExt)
                    .Add("numInt", numInt)
                    .Add("colonia", colonia)
                    .Add("apoderado", apoderado)
                    .Add("regimenFiscal", regimenFiscal)
                    .Add("esPredeterminada", esPredeterminada)
                    .Add("serieFacturas", serieFacturas)
                    .Build(url);

                await _logger.LogInformationAsync($"Creando entidad en: {url}", "EntidadService", "CreateEntidadAsync");

                // Realizar la petición POST
                using var response = await _http.PostAsync(url, null, cancellationToken).ConfigureAwait(false);

                // Verificar si la respuesta fue exitosa
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al crear entidad. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "EntidadService",
                        "CreateEntidadAsync");
                    return new ApiResponse { Success = false, Message = errorContent };
                }

                await _logger.LogInformationAsync("Entidad creada exitosamente", "EntidadService", "CreateEntidadAsync");

                return new ApiResponse { Success = true, Message = "Entidad creada correctamente" };
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al crear entidad", ex, "EntidadService", "CreateEntidadAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al crear entidad", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al crear entidad", ex, "EntidadService", "CreateEntidadAsync");
                throw;
            }
        }

        /// <summary>
        /// Actualiza una entidad por su ID
        /// </summary>
        public async Task<ApiResponse> UpdateEntidadAsync(
            int id,
            string? nombreComercial = null,
            string? razonSocial = null,
            string? rfc = null,
            string? cp = null,
            string? estado = null,
            string? ciudad = null,
            string? pais = null,
            string? calle = null,
            string? numExt = null,
            string? numInt = null,
            string? colonia = null,
            string? apoderado = null,
            string? regimenFiscal = null,
            bool? esPredeterminada = null,
            string? serieFacturas = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"{_endpoints.GetEndpoint("api", "entidad")}/{id}";

                // Construir parámetros de consulta
                url = new ApiQueryBuilder()
                    .Add("nombreComercial", nombreComercial)
                    .Add("razonSocial", razonSocial)
                    .Add("rfc", rfc)
                    .Add("cp", cp)
                    .Add("estado", estado)
                    .Add("ciudad", ciudad)
                    .Add("pais", pais)
                    .Add("calle", calle)
                    .Add("nomExt", numExt)
                    .Add("numInt", numInt)
                    .Add("colonia", colonia)
                    .Add("apoderado", apoderado)
                    .Add("regimenFiscal", regimenFiscal)
                    .Add("esPredeterminada", esPredeterminada)
                    .Add("serieFacturas", serieFacturas)
                    .Build(url);

                await _logger.LogInformationAsync($"Actualizando entidad en: {url}", "EntidadService", "UpdateEntidadAsync");

                // Realizar la petición PUT
                using var response = await _http.PutAsync(url, null, cancellationToken).ConfigureAwait(false);

                // Verificar si la respuesta fue exitosa
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al actualizar entidad. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "EntidadService",
                        "UpdateEntidadAsync");
                    return new ApiResponse { Success = false, Message = errorContent };
                }

                await _logger.LogInformationAsync($"Entidad {id} actualizada exitosamente", "EntidadService", "UpdateEntidadAsync");

                return new ApiResponse { Success = true, Message = "Entidad actualizada exitosamente" };
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al actualizar entidad", ex, "EntidadService", "UpdateEntidadAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al actualizar entidad", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al actualizar entidad", ex, "EntidadService", "UpdateEntidadAsync");
                throw;
            }
        }

        /// <summary>
        /// Elimina una entidad por su ID
        /// </summary>
        public async Task<ApiResponse> DeleteEntidadAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"{_endpoints.GetEndpoint("api", "entidad")}/{id}";

                await _logger.LogInformationAsync($"Eliminando entidad en: {url}", "EntidadService", "DeleteEntidadAsync");

                // Realizar la petición DELETE
                using var response = await _http.DeleteAsync(url, cancellationToken).ConfigureAwait(false);

                // Verificar si la respuesta fue exitosa
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al eliminar entidad. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "EntidadService",
                        "DeleteEntidadAsync");
                    return new ApiResponse { Success = false, Message = errorContent };
                }

                await _logger.LogInformationAsync($"Entidad {id} eliminada exitosamente", "EntidadService", "DeleteEntidadAsync");

                return new ApiResponse { Success = true, Message = "Entidad eliminada exitosamente" };
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al eliminar entidad", ex, "EntidadService", "DeleteEntidadAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al eliminar entidad", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al eliminar entidad", ex, "EntidadService", "DeleteEntidadAsync");
                throw;
            }
        }

        /// <summary>
        /// Obtiene la primera entidad activa (estatus = true)
        /// </summary>
        public async Task<EntidadDto?> GetActiveEntidadAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await _logger.LogInformationAsync("Obteniendo entidad activa...", "EntidadService", "GetActiveEntidadAsync");

                var query = new EntidadQueryDto { Estatus = true };
                var entidades = await GetEntidadesAsync(query, cancellationToken);

                // La API regresa la predeterminada primero; se busca explícitamente por si acaso.
                var entidadActiva = entidades?.FirstOrDefault(e => e.EsPredeterminada) ?? entidades?.FirstOrDefault();

                if (entidadActiva != null)
                {
                    await _logger.LogInformationAsync($"Entidad activa encontrada: {entidadActiva.NombreComercial}", "EntidadService", "GetActiveEntidadAsync");
                }
                else
                {
                    await _logger.LogWarningAsync("No se encontró ninguna entidad activa", "EntidadService", "GetActiveEntidadAsync");
                }

                return entidadActiva;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error al obtener entidad activa", ex, "EntidadService", "GetActiveEntidadAsync");
                return null;
            }
        }

        public async Task<EntidadDto?> GetEntidadPorRfcAsync(string? rfc, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(rfc)) return null;
            var limpio = rfc.Trim();
            var entidades = await GetEntidadesAsync(new EntidadQueryDto { RFC = limpio, Estatus = true }, cancellationToken);
            return entidades?.FirstOrDefault(e => string.Equals(e.RFC?.Trim(), limpio, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<List<EntidadCuentaBancariaDto>> GetCuentasBancariasAsync(int? idEntidad = null, CancellationToken cancellationToken = default)
        {
            var url = new ApiQueryBuilder()
                .Add("idEntidad", idEntidad)
                .Build(_endpoints.GetEndpoint("api", "entidad", "cuentas"));
            try
            {
                var lista = await _http.GetFromJsonAsync<List<EntidadCuentaBancariaDto>>(url, cancellationToken).ConfigureAwait(false);
                return lista ?? new List<EntidadCuentaBancariaDto>();
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al consultar cuentas bancarias de entidades", ex, "EntidadService", "GetCuentasBancariasAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al consultar las cuentas bancarias", ex);
            }
        }

        public async Task<ApiResponse> CrearCuentaBancariaAsync(int idEntidad, string numeroCuenta, string? clabe, string? banco, CancellationToken cancellationToken = default)
        {
            var url = $"{_endpoints.GetEndpoint("api", "entidad")}/{idEntidad}/cuentas";
            try
            {
                using var response = await _http.PostAsJsonAsync(url, new { numeroCuenta, clabe, banco }, cancellationToken).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                    return new ApiResponse { Success = true, Message = "Cuenta registrada" };
                return new ApiResponse { Success = false, Message = await LeerMensajeErrorAsync(response, "No se pudo registrar la cuenta.", cancellationToken) };
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al registrar cuenta bancaria", ex, "EntidadService", "CrearCuentaBancariaAsync");
                return new ApiResponse { Success = false, Message = "Error de comunicación con el servidor." };
            }
        }

        public async Task<ApiResponse> EliminarCuentaBancariaAsync(int idCuenta, CancellationToken cancellationToken = default)
        {
            var url = $"{_endpoints.GetEndpoint("api", "entidad", "cuentas")}/{idCuenta}";
            try
            {
                using var response = await _http.DeleteAsync(url, cancellationToken).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                    return new ApiResponse { Success = true, Message = "Cuenta dada de baja" };
                return new ApiResponse { Success = false, Message = await LeerMensajeErrorAsync(response, "No se pudo dar de baja la cuenta.", cancellationToken) };
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al dar de baja cuenta bancaria", ex, "EntidadService", "EliminarCuentaBancariaAsync");
                return new ApiResponse { Success = false, Message = "Error de comunicación con el servidor." };
            }
        }

        public async Task<EntidadFacturacionClienteDto?> GetEntidadFacturacionClienteAsync(int? idCliente = null, int? idOperacion = null, int? idContrato = null, string? rfc = null, CancellationToken cancellationToken = default)
        {
            var url = new ApiQueryBuilder()
                .Add("idCliente", idCliente)
                .Add("idOperacion", idOperacion)
                .Add("idContrato", idContrato)
                .Add("rfc", rfc)
                .Build(_endpoints.GetEndpoint("api", "entidad", "facturacion-cliente"));
            try
            {
                return await _http.GetFromJsonAsync<EntidadFacturacionClienteDto>(url, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is HttpRequestException or System.Text.Json.JsonException)
            {
                // No es crítico: el timbrado propone la predeterminada.
                await _logger.LogWarningAsync($"No se pudo consultar la entidad de facturación del cliente: {ex.Message}", "EntidadService", "GetEntidadFacturacionClienteAsync");
                return null;
            }
        }

        public async Task<List<EntidadFacturacionClienteDto>> GetEntidadesFacturacionClientesAsync(CancellationToken cancellationToken = default)
        {
            var url = _endpoints.GetEndpoint("api", "entidad", "facturacion-clientes");
            try
            {
                var lista = await _http.GetFromJsonAsync<List<EntidadFacturacionClienteDto>>(url, cancellationToken).ConfigureAwait(false);
                return lista ?? new List<EntidadFacturacionClienteDto>();
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al consultar la entidad de facturación de los clientes", ex, "EntidadService", "GetEntidadesFacturacionClientesAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al consultar la entidad de facturación de los clientes", ex);
            }
        }

        public async Task<EntidadDto?> GetEntidadParaClienteAsync(int? idCliente = null, int? idOperacion = null, CancellationToken cancellationToken = default)
        {
            var sugerida = await GetEntidadFacturacionClienteAsync(idCliente, idOperacion, null, null, cancellationToken);
            if (sugerida?.IdEntidad is int idEntidad)
            {
                var entidades = await GetEntidadesAsync(new EntidadQueryDto { Estatus = true }, cancellationToken);
                var entidad = entidades?.FirstOrDefault(e => e.IdEntidad == idEntidad);
                if (entidad != null)
                    return entidad;
            }
            return await GetActiveEntidadAsync(cancellationToken);
        }

        private static async Task<string> LeerMensajeErrorAsync(HttpResponseMessage response, string porDefecto, CancellationToken cancellationToken)
        {
            var contenido = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(contenido);
                if (doc.RootElement.TryGetProperty("message", out var msg) && msg.GetString() is { Length: > 0 } texto)
                    return texto;
            }
            catch (System.Text.Json.JsonException) { }
            return porDefecto;
        }
    }
}
