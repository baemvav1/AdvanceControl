using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.EndPointProvider;
using Advance_Control.Services.Logging;
using Advance_Control.Utilities;

namespace Advance_Control.Services.Cargos
{
    /// <summary>
    /// Implementación del servicio de cargos que se comunica con la API
    /// </summary>
    public class CargoService : ICargoService
    {
        private readonly HttpClient _http;
        private readonly IApiEndpointProvider _endpoints;
        private readonly ILoggingService _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public CargoService(HttpClient http, IApiEndpointProvider endpoints, ILoggingService logger)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Configurar opciones de JSON para ser case-insensitive y usar el convertidor personalizado
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new Converters.CargoDtoJsonConverter() }
            };
        }

        /// <summary>
        /// Obtiene una lista de cargos según los criterios de búsqueda proporcionados
        /// </summary>
        public async Task<List<CargoDto>> GetCargosAsync(CargoEditDto query, CancellationToken cancellationToken = default)
        {
            try
            {
                // Construir la URL base usando el endpoint correcto
                var url = _endpoints.GetEndpoint("api", "Cargos");

                // Agregar parámetros de consulta si existen
                if (query != null)
                {
                    url = new ApiQueryBuilder()
                        .AddPositive("idCargo", query.IdCargo)
                        .Add("idTipoCargo", query.IdTipoCargo)
                        .Add("idOperacion", query.IdOperacion)
                        .Add("idRelacionCargo", query.IdRelacionCargo)
                        .Add("monto", query.Monto)
                        .Add("nota", query.Nota)
                        .Add("idProveedor", query.IdProveedor)
                        .Add("cantidad", query.Cantidad)
                        .Add("unitario", query.Unitario)
                        .Build(url);
                }

                await _logger.LogInformationAsync($"Obteniendo cargos desde: {url}", "CargoService", "GetCargosAsync");

                // Realizar la petición GET
                var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);

                // Verificar si la respuesta fue exitosa
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al obtener cargos. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "CargoService",
                        "GetCargosAsync");
                    return new List<CargoDto>();
                }

                // Deserializar la respuesta
                List<CargoDto>? cargos;
                try
                {
                    // Get raw response for debugging
                    var responseText = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogInformationAsync($"Respuesta de API de cargos: {responseText.Substring(0, Math.Min(500, responseText.Length))}", "CargoService", "GetCargosAsync");
                    
                    cargos = JsonSerializer.Deserialize<List<CargoDto>>(responseText, _jsonOptions);
                }
                catch (JsonException ex)
                {
                    await _logger.LogErrorAsync(
                        "Error al deserializar respuesta de cargos",
                        ex,
                        "CargoService",
                        "GetCargosAsync");
                    return new List<CargoDto>();
                }

                await _logger.LogInformationAsync($"Se obtuvieron {cargos?.Count ?? 0} cargos", "CargoService", "GetCargosAsync");
                
                // Log details about IdRelacionCargo for debugging
                if (cargos != null && cargos.Count > 0)
                {
                    var cargosWithNullIdRelacion = cargos.Where(c => !c.IdRelacionCargo.HasValue).ToList();
                    if (cargosWithNullIdRelacion.Any())
                    {
                        await _logger.LogWarningAsync($"{cargosWithNullIdRelacion.Count} cargos sin IdRelacionCargo", "CargoService", "GetCargosAsync");
                    }
                }

                return cargos ?? new List<CargoDto>();
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al obtener cargos", ex, "CargoService", "GetCargosAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al obtener cargos", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al obtener cargos", ex, "CargoService", "GetCargosAsync");
                throw;
            }
        }

        /// <summary>
        /// Crea un nuevo cargo
        /// </summary>
        public async Task<CargoDto?> CreateCargoAsync(CargoEditDto query, CancellationToken cancellationToken = default)
        {
            // Validar parámetros requeridos
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            if (!query.IdTipoCargo.HasValue)
            {
                throw new ArgumentException("IdTipoCargo es requerido", nameof(query));
            }

            if (!query.IdOperacion.HasValue)
            {
                throw new ArgumentException("IdOperacion es requerido", nameof(query));
            }

            if (!query.IdRelacionCargo.HasValue)
            {
                throw new ArgumentException("IdRelacionCargo es requerido", nameof(query));
            }

            if (!query.Monto.HasValue)
            {
                throw new ArgumentException("Monto es requerido", nameof(query));
            }

            try
            {
                // Construir la URL con parámetros de consulta
                var url = new ApiQueryBuilder()
                    .AddRequired("idTipoCargo", query.IdTipoCargo.Value)
                    .AddRequired("idOperacion", query.IdOperacion.Value)
                    .AddRequired("idRelacionCargo", query.IdRelacionCargo.Value)
                    .AddRequired("monto", query.Monto.Value)
                    .Add("nota", query.Nota)
                    .Add("idProveedor", query.IdProveedor)
                    .Add("cantidad", query.Cantidad)
                    .Add("unitario", query.Unitario)
                    .Build(_endpoints.GetEndpoint("api", "Cargos"));

                await _logger.LogInformationAsync($"Creando cargo en: {url}", "CargoService", "CreateCargoAsync");

                using var response = await _http.PostAsync(url, null, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al crear cargo. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "CargoService",
                        "CreateCargoAsync");
                    return null;
                }

                // Deserializar la respuesta
                var responseText = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                
                JsonElement responseObj;
                try
                {
                    responseObj = JsonSerializer.Deserialize<JsonElement>(responseText);
                }
                catch (JsonException ex)
                {
                    await _logger.LogErrorAsync(
                        $"Error al deserializar respuesta de crear cargo. Respuesta: {responseText}",
                        ex,
                        "CargoService",
                        "CreateCargoAsync");
                    return null;
                }

                // Verificar si la operación fue exitosa
                if (responseObj.TryGetProperty("success", out var successProp) && successProp.GetBoolean())
                {
                    // Obtener el ID del cargo creado
                    if (responseObj.TryGetProperty("idCargo", out var idCargoProp))
                    {
                        var idCargo = idCargoProp.GetInt32();
                        await _logger.LogInformationAsync($"Cargo creado con ID: {idCargo}", "CargoService", "CreateCargoAsync");
                        
                        return new CargoDto
                        {
                            IdCargo = idCargo,
                            IdTipoCargo = query.IdTipoCargo,
                            IdOperacion = query.IdOperacion,
                            IdRelacionCargo = query.IdRelacionCargo,
                            Monto = query.Monto,
                            Nota = query.Nota,
                            Cantidad = query.Cantidad,
                            Unitario = query.Unitario
                        };
                    }
                    else
                    {
                        await _logger.LogWarningAsync("Cargo creado pero no se devolvió el ID", "CargoService", "CreateCargoAsync");
                        return null;
                    }
                }

                await _logger.LogWarningAsync("Respuesta de crear cargo no indica éxito", "CargoService", "CreateCargoAsync");
                return null;
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al crear cargo", ex, "CargoService", "CreateCargoAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al crear cargo", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al crear cargo", ex, "CargoService", "CreateCargoAsync");
                throw;
            }
        }

        /// <summary>
        /// Actualiza un cargo existente
        /// </summary>
        public async Task<bool> UpdateCargoAsync(CargoEditDto query, CancellationToken cancellationToken = default)
        {
            try
            {
                // Construir la URL con parámetros de consulta
                var url = new ApiQueryBuilder()
                    .Add("idTipoCargo", query.IdTipoCargo)
                    .Add("idRelacionCargo", query.IdRelacionCargo)
                    .Add("monto", query.Monto)
                    .Add("nota", query.Nota)
                    .Add("idProveedor", query.IdProveedor)
                    .Add("cantidad", query.Cantidad)
                    .Add("unitario", query.Unitario)
                    .Build($"{_endpoints.GetEndpoint("api", "Cargos")}/{query.IdCargo}");

                await _logger.LogInformationAsync($"Actualizando cargo {query.IdCargo} en: {url}", "CargoService", "UpdateCargoAsync");

                using var response = await _http.PutAsync(url, null, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al actualizar cargo. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "CargoService",
                        "UpdateCargoAsync");
                    return false;
                }

                await _logger.LogInformationAsync($"Cargo {query.IdCargo} actualizado correctamente", "CargoService", "UpdateCargoAsync");
                return true;
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al actualizar cargo", ex, "CargoService", "UpdateCargoAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al actualizar cargo", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al actualizar cargo", ex, "CargoService", "UpdateCargoAsync");
                throw;
            }
        }

        /// <summary>
        /// Elimina un cargo por su ID
        /// </summary>
        public async Task<bool> DeleteCargoAsync(int idCargo, CancellationToken cancellationToken = default)
        {
            try
            {
                // Construir la URL
                var url = $"{_endpoints.GetEndpoint("api", "Cargos")}/{idCargo}";

                await _logger.LogInformationAsync($"Eliminando cargo {idCargo} en: {url}", "CargoService", "DeleteCargoAsync");

                using var response = await _http.DeleteAsync(url, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al eliminar cargo. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "CargoService",
                        "DeleteCargoAsync");
                    return false;
                }

                await _logger.LogInformationAsync($"Cargo {idCargo} eliminado correctamente", "CargoService", "DeleteCargoAsync");
                return true;
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al eliminar cargo", ex, "CargoService", "DeleteCargoAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al eliminar cargo", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al eliminar cargo", ex, "CargoService", "DeleteCargoAsync");
                throw;
            }
        }
    }
}
