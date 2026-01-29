using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.EndPointProvider;
using Advance_Control.Services.Logging;

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
            
            // Configurar opciones de JSON para ser case-insensitive
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
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
                    var queryParams = new List<string>();

                    if (query.IdCargo > 0)
                        queryParams.Add($"idCargo={query.IdCargo}");

                    if (query.IdTipoCargo.HasValue)
                        queryParams.Add($"idTipoCargo={query.IdTipoCargo.Value}");

                    if (query.IdOperacion.HasValue)
                        queryParams.Add($"idOperacion={query.IdOperacion.Value}");

                    if (query.IdRelacionCargo.HasValue)
                        queryParams.Add($"idRelacionCargo={query.IdRelacionCargo.Value}");

                    if (query.Monto.HasValue)
                        queryParams.Add($"monto={query.Monto.Value}");

                    if (!string.IsNullOrWhiteSpace(query.Nota))
                        queryParams.Add($"nota={Uri.EscapeDataString(query.Nota)}");

                    if (query.IdProveedor.HasValue)
                        queryParams.Add($"idProveedor={query.IdProveedor.Value}");

                    if (queryParams.Count > 0)
                    {
                        url = $"{url}?{string.Join("&", queryParams)}";
                    }
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
                var cargos = await response.Content.ReadFromJsonAsync<List<CargoDto>>(_jsonOptions, cancellationToken: cancellationToken).ConfigureAwait(false);

                await _logger.LogInformationAsync($"Se obtuvieron {cargos?.Count ?? 0} cargos", "CargoService", "GetCargosAsync");

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
                var url = _endpoints.GetEndpoint("api", "Cargos");
                var queryParams = new List<string>
                {
                    $"idTipoCargo={query.IdTipoCargo.Value}",
                    $"idOperacion={query.IdOperacion.Value}",
                    $"idRelacionCargo={query.IdRelacionCargo.Value}",
                    $"monto={query.Monto.Value}"
                };

                // Parámetro opcional
                if (!string.IsNullOrWhiteSpace(query.Nota))
                    queryParams.Add($"nota={Uri.EscapeDataString(query.Nota)}");

                if (query.IdProveedor.HasValue)
                    queryParams.Add($"idProveedor={query.IdProveedor.Value}");

                url = $"{url}?{string.Join("&", queryParams)}";

                await _logger.LogInformationAsync($"Creando cargo en: {url}", "CargoService", "CreateCargoAsync");

                var response = await _http.PostAsync(url, null, cancellationToken).ConfigureAwait(false);

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
                var responseObj = JsonSerializer.Deserialize<JsonElement>(responseText);

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
                            Nota = query.Nota
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
                var url = $"{_endpoints.GetEndpoint("api", "Cargos")}/{query.IdCargo}";
                var queryParams = new List<string>();

                if (query.IdTipoCargo.HasValue)
                    queryParams.Add($"idTipoCargo={query.IdTipoCargo.Value}");

                if (query.IdRelacionCargo.HasValue)
                    queryParams.Add($"idRelacionCargo={query.IdRelacionCargo.Value}");

                if (query.Monto.HasValue)
                    queryParams.Add($"monto={query.Monto.Value}");

                if (!string.IsNullOrWhiteSpace(query.Nota))
                    queryParams.Add($"nota={Uri.EscapeDataString(query.Nota)}");

                if (query.IdProveedor.HasValue)
                    queryParams.Add($"idProveedor={query.IdProveedor.Value}");

                if (queryParams.Count > 0)
                {
                    url = $"{url}?{string.Join("&", queryParams)}";
                }

                await _logger.LogInformationAsync($"Actualizando cargo {query.IdCargo} en: {url}", "CargoService", "UpdateCargoAsync");

                var response = await _http.PutAsync(url, null, cancellationToken).ConfigureAwait(false);

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

                var response = await _http.DeleteAsync(url, cancellationToken).ConfigureAwait(false);

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
