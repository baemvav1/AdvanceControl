using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.EndPointProvider;
using Advance_Control.Services.Logging;

namespace Advance_Control.Services.EstadoCuenta
{
    /// <summary>
    /// Implementación del servicio de estados de cuenta que se comunica con la API
    /// </summary>
    public class EstadoCuentaService : IEstadoCuentaService
    {
        private readonly HttpClient _http;
        private readonly IApiEndpointProvider _endpoints;
        private readonly ILoggingService _logger;

        public EstadoCuentaService(HttpClient http, IApiEndpointProvider endpoints, ILoggingService logger)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Obtiene la lista completa de estados de cuenta
        /// </summary>
        public async Task<List<EstadoCuentaDto>> GetEstadosCuentaAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var url = _endpoints.GetEndpoint("api", "estadocuenta");

                await _logger.LogInformationAsync($"Obteniendo estados de cuenta desde: {url}", "EstadoCuentaService", "GetEstadosCuentaAsync");

                var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al obtener estados de cuenta. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "EstadoCuentaService",
                        "GetEstadosCuentaAsync");
                    return new List<EstadoCuentaDto>();
                }

                var estados = await response.Content.ReadFromJsonAsync<List<EstadoCuentaDto>>(cancellationToken: cancellationToken).ConfigureAwait(false);

                await _logger.LogInformationAsync($"Se obtuvieron {estados?.Count ?? 0} estados de cuenta", "EstadoCuentaService", "GetEstadosCuentaAsync");

                return estados ?? new List<EstadoCuentaDto>();
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al obtener estados de cuenta", ex, "EstadoCuentaService", "GetEstadosCuentaAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al obtener estados de cuenta", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al obtener estados de cuenta", ex, "EstadoCuentaService", "GetEstadosCuentaAsync");
                throw;
            }
        }

        /// <summary>
        /// Crea un nuevo estado de cuenta con los datos del periodo
        /// </summary>
        public async Task<EstadoCuentaOperationResponse> CreateEstadoCuentaAsync(
            DateTime fechaCorte,
            DateTime periodoDesde,
            DateTime periodoHasta,
            decimal saldoInicial,
            decimal saldoCorte,
            decimal totalDepositos,
            decimal totalRetiros,
            decimal? comisiones = null,
            string? nombreArchivo = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var baseUrl = _endpoints.GetEndpoint("api", "estadocuenta");

                // Build query parameters
                var queryParams = new List<string>
                {
                    $"fechaCorte={Uri.EscapeDataString(fechaCorte.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))}",
                    $"periodoDesde={Uri.EscapeDataString(periodoDesde.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))}",
                    $"periodoHasta={Uri.EscapeDataString(periodoHasta.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))}",
                    $"saldoInicial={saldoInicial.ToString(CultureInfo.InvariantCulture)}",
                    $"saldoCorte={saldoCorte.ToString(CultureInfo.InvariantCulture)}",
                    $"totalDepositos={totalDepositos.ToString(CultureInfo.InvariantCulture)}",
                    $"totalRetiros={totalRetiros.ToString(CultureInfo.InvariantCulture)}"
                };

                if (comisiones.HasValue)
                {
                    queryParams.Add($"comisiones={comisiones.Value.ToString(CultureInfo.InvariantCulture)}");
                }

                if (!string.IsNullOrWhiteSpace(nombreArchivo))
                {
                    queryParams.Add($"nombreArchivo={Uri.EscapeDataString(nombreArchivo)}");
                }

                var url = $"{baseUrl}?{string.Join("&", queryParams)}";

                await _logger.LogInformationAsync($"Creando estado de cuenta en: {url}", "EstadoCuentaService", "CreateEstadoCuentaAsync");

                var response = await _http.PostAsync(url, null, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al crear estado de cuenta. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "EstadoCuentaService",
                        "CreateEstadoCuentaAsync");
                    return new EstadoCuentaOperationResponse { Success = false, Message = errorContent };
                }

                var result = await response.Content.ReadFromJsonAsync<EstadoCuentaOperationResponse>(cancellationToken: cancellationToken).ConfigureAwait(false);

                await _logger.LogInformationAsync($"Estado de cuenta creado exitosamente con ID: {result?.Id}", "EstadoCuentaService", "CreateEstadoCuentaAsync");

                if (result != null)
                {
                    result.Success = true;
                }

                return result ?? new EstadoCuentaOperationResponse { Success = true, Message = "Estado de cuenta creado exitosamente" };
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al crear estado de cuenta", ex, "EstadoCuentaService", "CreateEstadoCuentaAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al crear estado de cuenta", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al crear estado de cuenta", ex, "EstadoCuentaService", "CreateEstadoCuentaAsync");
                throw;
            }
        }

        /// <summary>
        /// Obtiene todos los depósitos asociados a un estado de cuenta específico
        /// </summary>
        public async Task<List<DepositoDto>> GetDepositosAsync(int estadoCuentaId, CancellationToken cancellationToken = default)
        {
            if (estadoCuentaId <= 0)
            {
                throw new ArgumentException("El ID del estado de cuenta debe ser mayor que 0", nameof(estadoCuentaId));
            }

            try
            {
                var url = _endpoints.GetEndpoint("api", "estadocuenta", estadoCuentaId.ToString(CultureInfo.InvariantCulture), "depositos");

                await _logger.LogInformationAsync($"Obteniendo depósitos desde: {url}", "EstadoCuentaService", "GetDepositosAsync");

                var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al obtener depósitos. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "EstadoCuentaService",
                        "GetDepositosAsync");
                    return new List<DepositoDto>();
                }

                var depositos = await response.Content.ReadFromJsonAsync<List<DepositoDto>>(cancellationToken: cancellationToken).ConfigureAwait(false);

                await _logger.LogInformationAsync($"Se obtuvieron {depositos?.Count ?? 0} depósitos", "EstadoCuentaService", "GetDepositosAsync");

                return depositos ?? new List<DepositoDto>();
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al obtener depósitos", ex, "EstadoCuentaService", "GetDepositosAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al obtener depósitos", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al obtener depósitos", ex, "EstadoCuentaService", "GetDepositosAsync");
                throw;
            }
        }

        /// <summary>
        /// Agrega un nuevo depósito a un estado de cuenta existente
        /// </summary>
        public async Task<EstadoCuentaOperationResponse> AddDepositoAsync(
            int estadoCuentaId,
            DateTime fechaDeposito,
            string descripcionDeposito,
            decimal montoDeposito,
            string tipoDeposito,
            CancellationToken cancellationToken = default)
        {
            if (estadoCuentaId <= 0)
            {
                throw new ArgumentException("El ID del estado de cuenta debe ser mayor que 0", nameof(estadoCuentaId));
            }

            if (string.IsNullOrWhiteSpace(descripcionDeposito))
            {
                throw new ArgumentException("La descripción del depósito es obligatoria", nameof(descripcionDeposito));
            }

            if (montoDeposito <= 0)
            {
                throw new ArgumentException("El monto del depósito debe ser mayor que 0", nameof(montoDeposito));
            }

            if (string.IsNullOrWhiteSpace(tipoDeposito))
            {
                throw new ArgumentException("El tipo de depósito es obligatorio", nameof(tipoDeposito));
            }

            try
            {
                var baseUrl = _endpoints.GetEndpoint("api", "estadocuenta", estadoCuentaId.ToString(CultureInfo.InvariantCulture), "depositos");

                var queryParams = new List<string>
                {
                    $"fechaDeposito={Uri.EscapeDataString(fechaDeposito.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))}",
                    $"descripcionDeposito={Uri.EscapeDataString(descripcionDeposito)}",
                    $"montoDeposito={montoDeposito.ToString(CultureInfo.InvariantCulture)}",
                    $"tipoDeposito={Uri.EscapeDataString(tipoDeposito)}"
                };

                var url = $"{baseUrl}?{string.Join("&", queryParams)}";

                await _logger.LogInformationAsync($"Agregando depósito en: {url}", "EstadoCuentaService", "AddDepositoAsync");

                var response = await _http.PostAsync(url, null, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al agregar depósito. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "EstadoCuentaService",
                        "AddDepositoAsync");
                    return new EstadoCuentaOperationResponse { Success = false, Message = errorContent };
                }

                var result = await response.Content.ReadFromJsonAsync<EstadoCuentaOperationResponse>(cancellationToken: cancellationToken).ConfigureAwait(false);

                await _logger.LogInformationAsync($"Depósito agregado exitosamente con ID: {result?.Id}", "EstadoCuentaService", "AddDepositoAsync");

                if (result != null)
                {
                    result.Success = true;
                }

                return result ?? new EstadoCuentaOperationResponse { Success = true, Message = "Depósito agregado exitosamente" };
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al agregar depósito", ex, "EstadoCuentaService", "AddDepositoAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al agregar depósito", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al agregar depósito", ex, "EstadoCuentaService", "AddDepositoAsync");
                throw;
            }
        }

        /// <summary>
        /// Obtiene un resumen de los depósitos agrupados por tipo para un estado de cuenta
        /// </summary>
        public async Task<List<ResumenDepositoDto>> GetResumenDepositosAsync(int estadoCuentaId, CancellationToken cancellationToken = default)
        {
            if (estadoCuentaId <= 0)
            {
                throw new ArgumentException("El ID del estado de cuenta debe ser mayor que 0", nameof(estadoCuentaId));
            }

            try
            {
                var url = _endpoints.GetEndpoint("api", "estadocuenta", estadoCuentaId.ToString(CultureInfo.InvariantCulture), "resumen");

                await _logger.LogInformationAsync($"Obteniendo resumen de depósitos desde: {url}", "EstadoCuentaService", "GetResumenDepositosAsync");

                var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al obtener resumen de depósitos. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "EstadoCuentaService",
                        "GetResumenDepositosAsync");
                    return new List<ResumenDepositoDto>();
                }

                var resumen = await response.Content.ReadFromJsonAsync<List<ResumenDepositoDto>>(cancellationToken: cancellationToken).ConfigureAwait(false);

                await _logger.LogInformationAsync($"Se obtuvieron {resumen?.Count ?? 0} tipos de depósito en el resumen", "EstadoCuentaService", "GetResumenDepositosAsync");

                return resumen ?? new List<ResumenDepositoDto>();
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al obtener resumen de depósitos", ex, "EstadoCuentaService", "GetResumenDepositosAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al obtener resumen de depósitos", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al obtener resumen de depósitos", ex, "EstadoCuentaService", "GetResumenDepositosAsync");
                throw;
            }
        }

        /// <summary>
        /// Verifica si un depósito específico existe en el estado de cuenta
        /// </summary>
        public async Task<DepositoVerificacionDto> VerificarDepositoAsync(
            int estadoCuentaId,
            DateTime fechaDeposito,
            string descripcionDeposito,
            decimal montoDeposito,
            CancellationToken cancellationToken = default)
        {
            if (estadoCuentaId <= 0)
            {
                throw new ArgumentException("El ID del estado de cuenta debe ser mayor que 0", nameof(estadoCuentaId));
            }

            if (string.IsNullOrWhiteSpace(descripcionDeposito))
            {
                throw new ArgumentException("La descripción del depósito es obligatoria", nameof(descripcionDeposito));
            }

            try
            {
                var baseUrl = _endpoints.GetEndpoint("api", "estadocuenta", estadoCuentaId.ToString(CultureInfo.InvariantCulture), "verificar-deposito");

                var queryParams = new List<string>
                {
                    $"fechaDeposito={Uri.EscapeDataString(fechaDeposito.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))}",
                    $"descripcionDeposito={Uri.EscapeDataString(descripcionDeposito)}",
                    $"montoDeposito={montoDeposito.ToString(CultureInfo.InvariantCulture)}"
                };

                var url = $"{baseUrl}?{string.Join("&", queryParams)}";

                await _logger.LogInformationAsync($"Verificando depósito en: {url}", "EstadoCuentaService", "VerificarDepositoAsync");

                var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al verificar depósito. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "EstadoCuentaService",
                        "VerificarDepositoAsync");
                    return new DepositoVerificacionDto { Existe = false, Mensaje = errorContent };
                }

                var result = await response.Content.ReadFromJsonAsync<DepositoVerificacionDto>(cancellationToken: cancellationToken).ConfigureAwait(false);

                await _logger.LogInformationAsync($"Verificación de depósito completada. Existe: {result?.Existe}", "EstadoCuentaService", "VerificarDepositoAsync");

                return result ?? new DepositoVerificacionDto { Existe = false, Mensaje = "Error al verificar depósito" };
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al verificar depósito", ex, "EstadoCuentaService", "VerificarDepositoAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al verificar depósito", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al verificar depósito", ex, "EstadoCuentaService", "VerificarDepositoAsync");
                throw;
            }
        }
    }
}
