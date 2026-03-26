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
using Advance_Control.Utilities;

namespace Advance_Control.Services.Facturas
{
    public class FacturaService : IFacturaService
    {
        private readonly HttpClient _http;
        private readonly IApiEndpointProvider _endpoints;
        private readonly ILoggingService _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public FacturaService(HttpClient http, IApiEndpointProvider endpoints, ILoggingService logger)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<GuardarFacturaResponseDto> GuardarFacturaAsync(GuardarFacturaRequestDto request, CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var url = _endpoints.GetEndpoint("api", "factura", "guardar");

            try
            {
                await _logger.LogInformationAsync($"Guardando factura en: {url}", "FacturaService", "GuardarFacturaAsync");

                using var response = await _http.PostAsJsonAsync(url, request, cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al guardar factura. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "FacturaService",
                        "GuardarFacturaAsync");

                    return new GuardarFacturaResponseDto
                    {
                        Success = false,
                        Message = errorContent
                    };
                }

                var result = await response.Content.ReadFromJsonAsync<GuardarFacturaResponseDto>(_jsonOptions, cancellationToken).ConfigureAwait(false);
                if (result != null)
                {
                    return result;
                }

                await _logger.LogErrorAsync("La API devolvio una respuesta vacia al guardar la factura", null, "FacturaService", "GuardarFacturaAsync");
                return new GuardarFacturaResponseDto
                {
                    Success = false,
                    Message = "La API devolvio una respuesta vacia al guardar la factura."
                };
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al guardar factura", ex, "FacturaService", "GuardarFacturaAsync");
                throw new InvalidOperationException("Error de comunicacion con el servidor al guardar la factura.", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al guardar factura", ex, "FacturaService", "GuardarFacturaAsync");
                throw;
            }
        }

        public async Task<List<FacturaResumenDto>> ObtenerFacturasAsync(CancellationToken cancellationToken = default)
        {
            var url = _endpoints.GetEndpoint("api", "factura");

            try
            {
                await _logger.LogInformationAsync($"Consultando facturas en: {url}", "FacturaService", "ObtenerFacturasAsync");
                var result = await _http.GetFromJsonAsync<List<FacturaResumenDto>>(url, _jsonOptions, cancellationToken).ConfigureAwait(false);
                return result ?? new List<FacturaResumenDto>();
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al consultar facturas", ex, "FacturaService", "ObtenerFacturasAsync");
                throw new InvalidOperationException("Error de comunicacion con el servidor al consultar las facturas.", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al consultar facturas", ex, "FacturaService", "ObtenerFacturasAsync");
                throw;
            }
        }

        public async Task<FacturaDetalleDto?> ObtenerDetalleFacturaAsync(int idFactura, CancellationToken cancellationToken = default)
        {
            var url = new ApiQueryBuilder()
                .AddRequired("idFactura", idFactura)
                .Build(_endpoints.GetEndpoint("api", "factura", "consulta"));

            try
            {
                await _logger.LogInformationAsync($"Consultando detalle de factura en: {url}", "FacturaService", "ObtenerDetalleFacturaAsync");
                return await _http.GetFromJsonAsync<FacturaDetalleDto>(url, _jsonOptions, cancellationToken).ConfigureAwait(false);
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al consultar detalle de factura", ex, "FacturaService", "ObtenerDetalleFacturaAsync");
                throw new InvalidOperationException("Error de comunicacion con el servidor al consultar el detalle de la factura.", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al consultar detalle de factura", ex, "FacturaService", "ObtenerDetalleFacturaAsync");
                throw;
            }
        }

        public async Task<RegistrarAbonoFacturaResponseDto> RegistrarAbonoAsync(RegistrarAbonoFacturaRequestDto request, CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var url = _endpoints.GetEndpoint("api", "factura", "abono");

            try
            {
                await _logger.LogInformationAsync($"Registrando abono de factura en: {url}", "FacturaService", "RegistrarAbonoAsync");
                using var response = await _http.PostAsJsonAsync(url, request, cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al registrar abono. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "FacturaService",
                        "RegistrarAbonoAsync");

                    return new RegistrarAbonoFacturaResponseDto
                    {
                        Success = false,
                        Message = errorContent
                    };
                }

                var result = await response.Content.ReadFromJsonAsync<RegistrarAbonoFacturaResponseDto>(_jsonOptions, cancellationToken).ConfigureAwait(false);
                if (result != null)
                {
                    return result;
                }

                await _logger.LogErrorAsync("La API devolvio una respuesta vacia al registrar el abono", null, "FacturaService", "RegistrarAbonoAsync");
                return new RegistrarAbonoFacturaResponseDto
                {
                    Success = false,
                    Message = "La API devolvio una respuesta vacia al registrar el abono."
                };
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al registrar abono de factura", ex, "FacturaService", "RegistrarAbonoAsync");
                throw new InvalidOperationException("Error de comunicacion con el servidor al registrar el abono.", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al registrar abono de factura", ex, "FacturaService", "RegistrarAbonoAsync");
                throw;
            }
        }

        public Task<BitacoraConciliacionResponseDto> InicializarBitacoraConciliacionAsync(CancellationToken cancellationToken = default)
        {
            var url = _endpoints.GetEndpoint("api", "factura", "conciliacion", "inicializar-bitacora");
            return PostBitacoraConciliacionAsync(url, "InicializarBitacoraConciliacionAsync", "inicializar la bitacora de conciliacion", cancellationToken);
        }

        public Task<BitacoraConciliacionResponseDto> DeshacerUltimaOperacionConciliacionAsync(CancellationToken cancellationToken = default)
        {
            var url = _endpoints.GetEndpoint("api", "factura", "conciliacion", "deshacer-ultimo");
            return PostBitacoraConciliacionAsync(url, "DeshacerUltimaOperacionConciliacionAsync", "deshacer la ultima operacion de conciliacion", cancellationToken);
        }

        public Task<BitacoraConciliacionResponseDto> DeshacerTodasOperacionesConciliacionAsync(CancellationToken cancellationToken = default)
        {
            var url = _endpoints.GetEndpoint("api", "factura", "conciliacion", "deshacer-todo");
            return PostBitacoraConciliacionAsync(url, "DeshacerTodasOperacionesConciliacionAsync", "deshacer todas las operaciones de conciliacion", cancellationToken);
        }

        private async Task<BitacoraConciliacionResponseDto> PostBitacoraConciliacionAsync(
            string url,
            string metodo,
            string descripcionOperacion,
            CancellationToken cancellationToken)
        {
            try
            {
                await _logger.LogInformationAsync($"Ejecutando POST sin cuerpo en: {url}", "FacturaService", metodo);
                using var response = await _http.PostAsync(url, content: null, cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al {descripcionOperacion}. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "FacturaService",
                        metodo);

                    return ConstruirRespuestaError(errorContent);
                }

                var result = await response.Content.ReadFromJsonAsync<BitacoraConciliacionResponseDto>(_jsonOptions, cancellationToken).ConfigureAwait(false);
                if (result != null)
                {
                    return result;
                }

                await _logger.LogErrorAsync($"La API devolvio una respuesta vacia al {descripcionOperacion}", null, "FacturaService", metodo);
                return ConstruirRespuestaError($"La API devolvio una respuesta vacia al {descripcionOperacion}.");
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync($"Error de red al {descripcionOperacion}", ex, "FacturaService", metodo);
                throw new InvalidOperationException($"Error de comunicacion con el servidor al {descripcionOperacion}.", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error inesperado al {descripcionOperacion}", ex, "FacturaService", metodo);
                throw;
            }
        }

        private static BitacoraConciliacionResponseDto ConstruirRespuestaError(string mensaje)
        {
            return new BitacoraConciliacionResponseDto
            {
                Success = false,
                Message = mensaje
            };
        }
    }
}
