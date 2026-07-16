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
                        Message = ExtraerMensajeError(errorContent)
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

        public async Task<FacturaResumenDto?> BuscarFacturaPorFolioAsync(string folio, CancellationToken cancellationToken = default)
        {
            var url = new ApiQueryBuilder()
                .Add("folio", folio)
                .Build(_endpoints.GetEndpoint("api", "factura"));

            try
            {
                await _logger.LogInformationAsync($"Buscando factura por folio en: {url}", "FacturaService", "BuscarFacturaPorFolioAsync");
                var result = await _http.GetFromJsonAsync<List<FacturaResumenDto>>(url, _jsonOptions, cancellationToken).ConfigureAwait(false);
                return result?.FirstOrDefault();
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al buscar factura por folio", ex, "FacturaService", "BuscarFacturaPorFolioAsync");
                throw new InvalidOperationException("Error de comunicacion con el servidor al buscar la factura.", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al buscar factura por folio", ex, "FacturaService", "BuscarFacturaPorFolioAsync");
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

        public async Task<List<OperacionSinFacturaDto>> ObtenerOperacionesSinFacturaAsync(CancellationToken cancellationToken = default)
        {
            var url = _endpoints.GetEndpoint("api", "factura", "operaciones-sin-factura");

            try
            {
                await _logger.LogInformationAsync($"Consultando operaciones sin factura en: {url}", "FacturaService", "ObtenerOperacionesSinFacturaAsync");
                var result = await _http.GetFromJsonAsync<List<OperacionSinFacturaDto>>(url, _jsonOptions, cancellationToken).ConfigureAwait(false);
                return result ?? new List<OperacionSinFacturaDto>();
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al consultar operaciones sin factura", ex, "FacturaService", "ObtenerOperacionesSinFacturaAsync");
                throw new InvalidOperationException("Error de comunicacion con el servidor al consultar operaciones sin factura.", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al consultar operaciones sin factura", ex, "FacturaService", "ObtenerOperacionesSinFacturaAsync");
                throw;
            }
        }

        public async Task<List<OperacionFacturadaDto>> ObtenerOperacionesFacturadasAsync(CancellationToken cancellationToken = default)
        {
            var url = _endpoints.GetEndpoint("api", "factura", "operaciones-facturadas");

            try
            {
                await _logger.LogInformationAsync($"Consultando operaciones facturadas en: {url}", "FacturaService", "ObtenerOperacionesFacturadasAsync");
                var result = await _http.GetFromJsonAsync<List<OperacionFacturadaDto>>(url, _jsonOptions, cancellationToken).ConfigureAwait(false);
                return result ?? new List<OperacionFacturadaDto>();
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al consultar operaciones facturadas", ex, "FacturaService", "ObtenerOperacionesFacturadasAsync");
                throw new InvalidOperationException("Error de comunicacion con el servidor al consultar operaciones facturadas.", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al consultar operaciones facturadas", ex, "FacturaService", "ObtenerOperacionesFacturadasAsync");
                throw;
            }
        }

        public async Task<CancelarFacturaOperacionResponseDto> CancelarFacturaOperacionAsync(int idOperacion, CancellationToken cancellationToken = default)
        {
            var url = _endpoints.GetEndpoint("api", "factura", "operacion", idOperacion.ToString());

            try
            {
                await _logger.LogInformationAsync($"Cancelando factura de operacion en: {url}", "FacturaService", "CancelarFacturaOperacionAsync");
                using var response = await _http.DeleteAsync(url, cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    var mensaje = ExtraerMensajeError(errorContent);
                    await _logger.LogErrorAsync(
                        $"Error al cancelar factura de operacion. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "FacturaService",
                        "CancelarFacturaOperacionAsync");
                    throw new InvalidOperationException(mensaje);
                }

                var result = await response.Content.ReadFromJsonAsync<CancelarFacturaOperacionResponseDto>(_jsonOptions, cancellationToken).ConfigureAwait(false);
                return result ?? new CancelarFacturaOperacionResponseDto { IdOperacion = idOperacion };
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al cancelar factura de operacion", ex, "FacturaService", "CancelarFacturaOperacionAsync");
                throw new InvalidOperationException("Error de comunicacion con el servidor al cancelar la factura de la operacion.", ex);
            }
        }

        private static string ExtraerMensajeError(string errorContent)
        {
            try
            {
                using var doc = JsonDocument.Parse(errorContent);
                if (doc.RootElement.TryGetProperty("message", out var messageProp))
                {
                    return messageProp.GetString() ?? errorContent;
                }
            }
            catch (JsonException)
            {
                // errorContent no es JSON valido; se devuelve tal cual.
            }

            return errorContent;
        }
    }
}
