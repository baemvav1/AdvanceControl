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

        public async Task<FacturaResumenDto?> BuscarFacturaPorFolioAsync(string folio, string? serie = null, CancellationToken cancellationToken = default)
        {
            var url = new ApiQueryBuilder()
                .Add("folio", folio)
                .Build(_endpoints.GetEndpoint("api", "factura"));

            try
            {
                await _logger.LogInformationAsync($"Buscando factura por folio en: {url}", "FacturaService", "BuscarFacturaPorFolioAsync");
                var result = await _http.GetFromJsonAsync<List<FacturaResumenDto>>(url, _jsonOptions, cancellationToken).ConfigureAwait(false);
                if (result == null)
                {
                    return null;
                }

                // El filtro de la API solo compara contra la columna folio (LIKE parcial); si se pide
                // una serie especifica, se acota en memoria porque el mismo folio puede repetirse entre series.
                return string.IsNullOrWhiteSpace(serie)
                    ? result.FirstOrDefault()
                    : result.FirstOrDefault(f => string.Equals(f.Serie, serie, StringComparison.OrdinalIgnoreCase));
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

        public async Task<string?> ObtenerXmlFacturaAsync(int idFactura, CancellationToken cancellationToken = default)
        {
            var url = _endpoints.GetEndpoint("api", "factura", idFactura.ToString(), "xml");

            try
            {
                await _logger.LogInformationAsync($"Consultando XML de factura en: {url}", "FacturaService", "ObtenerXmlFacturaAsync");
                var respuesta = await _http.GetFromJsonAsync<FacturaXmlDto>(url, _jsonOptions, cancellationToken).ConfigureAwait(false);
                return respuesta?.XmlContenido;
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al consultar XML de factura", ex, "FacturaService", "ObtenerXmlFacturaAsync");
                throw new InvalidOperationException("Error de comunicacion con el servidor al consultar el XML de la factura.", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al consultar XML de factura", ex, "FacturaService", "ObtenerXmlFacturaAsync");
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

        public async Task VincularFacturaOperacionAsync(int idFactura, int idOperacion, CancellationToken cancellationToken = default)
        {
            var url = new ApiQueryBuilder()
                .AddRequired("idOperacion", idOperacion)
                .Build(_endpoints.GetEndpoint("api", "factura", idFactura.ToString(), "vincular-operacion"));

            try
            {
                await _logger.LogInformationAsync($"Vinculando factura a operacion en: {url}", "FacturaService", "VincularFacturaOperacionAsync");
                using var response = await _http.PostAsync(url, content: null, cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    var mensaje = ExtraerMensajeError(errorContent);
                    await _logger.LogErrorAsync(
                        $"Error al vincular factura a operacion. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "FacturaService",
                        "VincularFacturaOperacionAsync");
                    throw new InvalidOperationException(mensaje);
                }
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al vincular factura a operacion", ex, "FacturaService", "VincularFacturaOperacionAsync");
                throw new InvalidOperationException("Error de comunicacion con el servidor al vincular la factura a la operacion.", ex);
            }
        }

        public async Task<CancelarFacturaOperacionResponseDto> DesvincularFacturaOperacionAsync(int idOperacion, CancellationToken cancellationToken = default)
        {
            var url = _endpoints.GetEndpoint("api", "factura", "operacion", idOperacion.ToString(), "desvincular");

            try
            {
                await _logger.LogInformationAsync($"Desvinculando factura de operacion en: {url}", "FacturaService", "DesvincularFacturaOperacionAsync");
                using var response = await _http.PostAsync(url, content: null, cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    var mensaje = ExtraerMensajeError(errorContent);
                    await _logger.LogErrorAsync(
                        $"Error al desvincular factura de operacion. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "FacturaService",
                        "DesvincularFacturaOperacionAsync");
                    throw new InvalidOperationException(mensaje);
                }

                var result = await response.Content.ReadFromJsonAsync<CancelarFacturaOperacionResponseDto>(_jsonOptions, cancellationToken).ConfigureAwait(false);
                return result ?? new CancelarFacturaOperacionResponseDto { IdOperacion = idOperacion };
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al desvincular factura de operacion", ex, "FacturaService", "DesvincularFacturaOperacionAsync");
                throw new InvalidOperationException("Error de comunicacion con el servidor al desvincular la factura de la operacion.", ex);
            }
        }

        public async Task<TimbrarResultadoDto> TimbrarOperacionAsync(int idOperacion, CfdiTimbrarRequestDto request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var url = _endpoints.GetEndpoint("api", "factura", "operacion", idOperacion.ToString(), "timbrar");

            try
            {
                await _logger.LogInformationAsync($"Timbrando operacion {idOperacion} en: {url}", "FacturaService", "TimbrarOperacionAsync");

                using var response = await _http.PostAsJsonAsync(url, request, cancellationToken).ConfigureAwait(false);
                var contenido = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    await _logger.LogErrorAsync(
                        $"Error al timbrar operacion {idOperacion}. Status: {response.StatusCode}, Content: {contenido}",
                        null,
                        "FacturaService",
                        "TimbrarOperacionAsync");

                    return ExtraerResultadoError(contenido);
                }

                var result = JsonSerializer.Deserialize<GuardarFacturaResponseDto>(contenido, _jsonOptions);
                if (result != null)
                {
                    return new TimbrarResultadoDto
                    {
                        Success = result.Success,
                        Message = result.Message,
                        IdFactura = result.IdFactura
                    };
                }

                return new TimbrarResultadoDto { Success = false, Message = "La API devolvió una respuesta vacía al timbrar." };
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al timbrar operacion", ex, "FacturaService", "TimbrarOperacionAsync");
                return new TimbrarResultadoDto { Success = false, Message = "Error de comunicación con el servidor al timbrar." };
            }
        }

        public async Task<TimbrarResultadoDto> TimbrarIgualaMensualAsync(int idContrato, string periodo, CfdiTimbrarRequestDto request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var url = new ApiQueryBuilder()
                .Add("periodo", periodo)
                .Build(_endpoints.GetEndpoint("api", "factura", "contrato-suscripcion", idContrato.ToString(), "timbrar-iguala"));

            try
            {
                await _logger.LogInformationAsync($"Timbrando iguala mensual del contrato {idContrato} ({periodo}) en: {url}", "FacturaService", "TimbrarIgualaMensualAsync");

                using var response = await _http.PostAsJsonAsync(url, request, cancellationToken).ConfigureAwait(false);
                var contenido = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    await _logger.LogErrorAsync(
                        $"Error al timbrar iguala del contrato {idContrato}. Status: {response.StatusCode}, Content: {contenido}",
                        null,
                        "FacturaService",
                        "TimbrarIgualaMensualAsync");

                    return ExtraerResultadoError(contenido);
                }

                var result = JsonSerializer.Deserialize<GuardarFacturaResponseDto>(contenido, _jsonOptions);
                if (result != null)
                {
                    return new TimbrarResultadoDto
                    {
                        Success = result.Success,
                        Message = result.Message,
                        IdFactura = result.IdFactura
                    };
                }

                return new TimbrarResultadoDto { Success = false, Message = "La API devolvió una respuesta vacía al timbrar la iguala." };
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al timbrar la iguala mensual", ex, "FacturaService", "TimbrarIgualaMensualAsync");
                return new TimbrarResultadoDto { Success = false, Message = "Error de comunicación con el servidor al timbrar." };
            }
        }

        public async Task<CancelarCfdiResponseDto> CancelarCfdiAsync(int idFactura, CancelarCfdiRequestDto request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var url = _endpoints.GetEndpoint("api", "factura", idFactura.ToString(), "cancelar");

            try
            {
                await _logger.LogInformationAsync($"Cancelando CFDI de la factura {idFactura} en: {url}", "FacturaService", "CancelarCfdiAsync");

                using var response = await _http.PostAsJsonAsync(url, request, cancellationToken).ConfigureAwait(false);
                var contenido = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    await _logger.LogErrorAsync(
                        $"Error al cancelar CFDI de la factura {idFactura}. Status: {response.StatusCode}, Content: {contenido}",
                        null,
                        "FacturaService",
                        "CancelarCfdiAsync");

                    return ExtraerResultadoErrorCancelacion(contenido);
                }

                var result = JsonSerializer.Deserialize<CancelarCfdiResponseDto>(contenido, _jsonOptions);
                if (result != null)
                {
                    result.Success = true;
                    return result;
                }

                return new CancelarCfdiResponseDto { Success = false, Message = "La API devolvió una respuesta vacía al cancelar." };
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al cancelar CFDI", ex, "FacturaService", "CancelarCfdiAsync");
                return new CancelarCfdiResponseDto { Success = false, Message = "Error de comunicación con el servidor al cancelar." };
            }
        }

        public async Task<List<AbonoPendienteComplementoDto>> ObtenerAbonosPendientesComplementoAsync(string receptorRfc, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(receptorRfc))
                throw new ArgumentException("El RFC del receptor es requerido.", nameof(receptorRfc));

            var url = new ApiQueryBuilder()
                .Add("receptorRfc", receptorRfc)
                .Build(_endpoints.GetEndpoint("api", "factura", "abonos-pendientes-complemento"));

            try
            {
                await _logger.LogInformationAsync($"Consultando abonos pendientes de complemento en: {url}", "FacturaService", "ObtenerAbonosPendientesComplementoAsync");
                var result = await _http.GetFromJsonAsync<List<AbonoPendienteComplementoDto>>(url, _jsonOptions, cancellationToken).ConfigureAwait(false);
                return result ?? new List<AbonoPendienteComplementoDto>();
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al consultar abonos pendientes de complemento", ex, "FacturaService", "ObtenerAbonosPendientesComplementoAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al consultar abonos pendientes de complemento.", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al consultar abonos pendientes de complemento", ex, "FacturaService", "ObtenerAbonosPendientesComplementoAsync");
                throw;
            }
        }

        public async Task<TimbrarResultadoDto> GenerarComplementoPagoAsync(GenerarComplementoPagoRequestDto request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var url = _endpoints.GetEndpoint("api", "factura", "complemento-pago");

            try
            {
                await _logger.LogInformationAsync($"Generando complemento de pago en: {url}", "FacturaService", "GenerarComplementoPagoAsync");

                using var response = await _http.PostAsJsonAsync(url, request, cancellationToken).ConfigureAwait(false);
                var contenido = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    await _logger.LogErrorAsync(
                        $"Error al generar complemento de pago. Status: {response.StatusCode}, Content: {contenido}",
                        null,
                        "FacturaService",
                        "GenerarComplementoPagoAsync");

                    return ExtraerResultadoError(contenido);
                }

                var result = JsonSerializer.Deserialize<GenerarComplementoPagoResponseDto>(contenido, _jsonOptions);
                if (result != null)
                {
                    return new TimbrarResultadoDto
                    {
                        Success = result.Success,
                        Message = result.Message,
                        IdFactura = result.IdFacturaComplemento
                    };
                }

                return new TimbrarResultadoDto { Success = false, Message = "La API devolvió una respuesta vacía al generar el complemento de pago." };
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al generar complemento de pago", ex, "FacturaService", "GenerarComplementoPagoAsync");
                return new TimbrarResultadoDto { Success = false, Message = "Error de comunicación con el servidor al generar el complemento de pago." };
            }
        }

        public async Task<List<ComplementoPagoResumenDto>> ObtenerComplementosPagoAsync(bool soloHuerfanos = false, CancellationToken cancellationToken = default)
        {
            var url = new ApiQueryBuilder()
                .Add("soloHuerfanos", soloHuerfanos)
                .Build(_endpoints.GetEndpoint("api", "factura", "complementos-pago"));

            try
            {
                await _logger.LogInformationAsync($"Consultando complementos de pago en: {url}", "FacturaService", "ObtenerComplementosPagoAsync");
                var result = await _http.GetFromJsonAsync<List<ComplementoPagoResumenDto>>(url, _jsonOptions, cancellationToken).ConfigureAwait(false);
                return result ?? new List<ComplementoPagoResumenDto>();
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al consultar complementos de pago", ex, "FacturaService", "ObtenerComplementosPagoAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al consultar complementos de pago.", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al consultar complementos de pago", ex, "FacturaService", "ObtenerComplementosPagoAsync");
                throw;
            }
        }

        public async Task<ComplementoPagoDetalleDto?> ObtenerComplementoPagoDetalleAsync(int idFacturaComplemento, CancellationToken cancellationToken = default)
        {
            var url = _endpoints.GetEndpoint("api", "factura", idFacturaComplemento.ToString(), "complemento-pago-detalle");

            try
            {
                await _logger.LogInformationAsync($"Consultando detalle de complemento de pago en: {url}", "FacturaService", "ObtenerComplementoPagoDetalleAsync");
                return await _http.GetFromJsonAsync<ComplementoPagoDetalleDto>(url, _jsonOptions, cancellationToken).ConfigureAwait(false);
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al consultar detalle de complemento de pago", ex, "FacturaService", "ObtenerComplementoPagoDetalleAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al consultar el complemento de pago.", ex);
            }
        }

        public async Task<List<ComplementoPagoPendienteMovimientoDto>> ObtenerComplementosSinMovimientoAsync(CancellationToken cancellationToken = default)
        {
            var url = _endpoints.GetEndpoint("api", "factura", "complementos-pago", "pendientes-movimiento");

            try
            {
                await _logger.LogInformationAsync($"Consultando complementos de pago sin movimiento en: {url}", "FacturaService", "ObtenerComplementosSinMovimientoAsync");
                var result = await _http.GetFromJsonAsync<List<ComplementoPagoPendienteMovimientoDto>>(url, _jsonOptions, cancellationToken).ConfigureAwait(false);
                return result ?? new List<ComplementoPagoPendienteMovimientoDto>();
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al consultar complementos de pago sin movimiento", ex, "FacturaService", "ObtenerComplementosSinMovimientoAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al consultar complementos de pago sin movimiento.", ex);
            }
        }

        public async Task<RegistrarAbonoFacturaResponseDto> VincularComplementoMovimientoAsync(VincularComplementoMovimientoRequestDto request, CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var url = _endpoints.GetEndpoint("api", "factura", "complemento-pago", "vincular-movimiento");

            try
            {
                await _logger.LogInformationAsync($"Vinculando complemento de pago a movimiento en: {url}", "FacturaService", "VincularComplementoMovimientoAsync");
                using var response = await _http.PostAsJsonAsync(url, request, cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al vincular complemento de pago a movimiento. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "FacturaService",
                        "VincularComplementoMovimientoAsync");

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

                await _logger.LogErrorAsync("La API devolvio una respuesta vacia al vincular el complemento de pago", null, "FacturaService", "VincularComplementoMovimientoAsync");
                return new RegistrarAbonoFacturaResponseDto
                {
                    Success = false,
                    Message = "La API devolvio una respuesta vacia al vincular el complemento de pago."
                };
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al vincular complemento de pago a movimiento", ex, "FacturaService", "VincularComplementoMovimientoAsync");
                throw new InvalidOperationException("Error de comunicacion con el servidor al vincular el complemento de pago.", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al vincular complemento de pago a movimiento", ex, "FacturaService", "VincularComplementoMovimientoAsync");
                throw;
            }
        }

        public async Task<ComplementoPagoConsolidarResultDto> ConsolidarComplementosPagoAsync(CancellationToken cancellationToken = default)
        {
            var url = _endpoints.GetEndpoint("api", "factura", "complementos-pago", "consolidar");

            try
            {
                await _logger.LogInformationAsync($"Consolidando complementos de pago en: {url}", "FacturaService", "ConsolidarComplementosPagoAsync");
                using var response = await _http.PostAsync(url, content: null, cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al consolidar complementos de pago. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "FacturaService",
                        "ConsolidarComplementosPagoAsync");

                    return new ComplementoPagoConsolidarResultDto
                    {
                        Errores = { errorContent }
                    };
                }

                var result = await response.Content.ReadFromJsonAsync<ComplementoPagoConsolidarResultDto>(_jsonOptions, cancellationToken).ConfigureAwait(false);
                return result ?? new ComplementoPagoConsolidarResultDto();
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al consolidar complementos de pago", ex, "FacturaService", "ConsolidarComplementosPagoAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al consolidar complementos de pago.", ex);
            }
        }

        private static CancelarCfdiResponseDto ExtraerResultadoErrorCancelacion(string errorContent)
        {
            var resultado = new CancelarCfdiResponseDto { Success = false, Message = errorContent };
            try
            {
                using var doc = JsonDocument.Parse(errorContent);
                if (doc.RootElement.TryGetProperty("message", out var messageProp))
                    resultado.Message = messageProp.GetString();
                if (doc.RootElement.TryGetProperty("observacion", out var observacionProp))
                    resultado.Observacion = observacionProp.GetString();
                if (doc.RootElement.TryGetProperty("codigoRespuesta", out var codigoProp))
                    resultado.CodigoRespuesta = codigoProp.GetString();
            }
            catch (JsonException)
            {
                // errorContent no es JSON valido; se conserva tal cual en Message.
            }
            return resultado;
        }

        private static TimbrarResultadoDto ExtraerResultadoError(string errorContent)
        {
            var resultado = new TimbrarResultadoDto { Success = false, Message = errorContent };
            try
            {
                using var doc = JsonDocument.Parse(errorContent);
                if (doc.RootElement.TryGetProperty("message", out var messageProp))
                    resultado.Message = messageProp.GetString();
                if (doc.RootElement.TryGetProperty("observacion", out var observacionProp))
                    resultado.Observacion = observacionProp.GetString();
                if (doc.RootElement.TryGetProperty("codigoRespuesta", out var codigoProp))
                    resultado.CodigoRespuesta = codigoProp.GetString();
            }
            catch (JsonException)
            {
                // errorContent no es JSON valido; se conserva tal cual en Message.
            }
            return resultado;
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
