using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.EndPointProvider;
using Advance_Control.Services.Logging;

namespace Advance_Control.Services.ConfiguracionEmisor
{
    public class ConfiguracionEmisorService : IConfiguracionEmisorService
    {
        private readonly HttpClient _http;
        private readonly IApiEndpointProvider _endpoints;
        private readonly ILoggingService _logger;

        public ConfiguracionEmisorService(HttpClient http, IApiEndpointProvider endpoints, ILoggingService logger)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ConfiguracionEmisorDto?> ObtenerConfiguracionAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var url = _endpoints.GetEndpoint("api", "configuracion-emisor");
                var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return null;
                }

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al obtener configuración del emisor. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "ConfiguracionEmisorService",
                        "ObtenerConfiguracionAsync");
                    return null;
                }

                return await response.Content.ReadFromJsonAsync<ConfiguracionEmisorDto>(cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al obtener configuración del emisor", ex, "ConfiguracionEmisorService", "ObtenerConfiguracionAsync");
                return null;
            }
        }

        public async Task<CsdEmisorEstadoDto> ObtenerEstadoCsdAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var url = _endpoints.GetEndpoint("api", "configuracion-emisor", "csd");
                var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al obtener estado del CSD. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "ConfiguracionEmisorService",
                        "ObtenerEstadoCsdAsync");
                    return new CsdEmisorEstadoDto { Cargado = false, Vigente = false };
                }

                var estado = await response.Content.ReadFromJsonAsync<CsdEmisorEstadoDto>(cancellationToken: cancellationToken).ConfigureAwait(false);
                return estado ?? new CsdEmisorEstadoDto { Cargado = false, Vigente = false };
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al obtener estado del CSD", ex, "ConfiguracionEmisorService", "ObtenerEstadoCsdAsync");
                return new CsdEmisorEstadoDto { Cargado = false, Vigente = false };
            }
        }

        public async Task<CsdUploadResultDto> GuardarCsdAsync(byte[] certificado, byte[] llavePrivada, string password, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = _endpoints.GetEndpoint("api", "configuracion-emisor", "csd");

                using var form = new MultipartFormDataContent();
                var certContent = new ByteArrayContent(certificado);
                certContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-x509-ca-cert");
                form.Add(certContent, "certificado", "csd.cer");

                var llaveContent = new ByteArrayContent(llavePrivada);
                llaveContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
                form.Add(llaveContent, "llavePrivada", "csd.key");

                form.Add(new StringContent(password), "password");

                var response = await _http.PostAsync(url, form, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogWarningAsync(
                        $"API rechazó la carga del CSD. Status: {response.StatusCode}, Content: {errorContent}",
                        "ConfiguracionEmisorService", "GuardarCsdAsync");

                    string mensaje = "No se pudo cargar el CSD.";
                    try
                    {
                        var errorDoc = System.Text.Json.JsonDocument.Parse(errorContent);
                        if (errorDoc.RootElement.TryGetProperty("message", out var msgProp))
                        {
                            mensaje = msgProp.GetString() ?? mensaje;
                        }
                    }
                    catch (System.Text.Json.JsonException) { /* dejar el mensaje genérico */ }

                    return new CsdUploadResultDto { Success = false, Message = mensaje };
                }

                var estado = await response.Content.ReadFromJsonAsync<CsdEmisorEstadoDto>(cancellationToken: cancellationToken).ConfigureAwait(false);
                return new CsdUploadResultDto { Success = true, Estado = estado };
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al cargar el CSD", ex, "ConfiguracionEmisorService", "GuardarCsdAsync");
                return new CsdUploadResultDto { Success = false, Message = "Error de comunicación con el servidor." };
            }
        }
    }
}
