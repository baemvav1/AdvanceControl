using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.EndPointProvider;
using Advance_Control.Services.Logging;

namespace Advance_Control.Services.EstadoCuenta
{
    public class EstadoCuentaXmlService : IEstadoCuentaXmlService
    {
        private readonly HttpClient _http;
        private readonly IApiEndpointProvider _endpoints;
        private readonly ILoggingService _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public EstadoCuentaXmlService(HttpClient http, IApiEndpointProvider endpoints, ILoggingService logger)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<GuardarEstadoCuentaResponseDto> GuardarEstadoCuentaAsync(GuardarEstadoCuentaRequestDto request, CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var url = _endpoints.GetEndpoint("api", "estadocuenta", "guardar");

            try
            {
                await _logger.LogInformationAsync($"Guardando estado de cuenta en: {url}", "EstadoCuentaXmlService", "GuardarEstadoCuentaAsync");

                var response = await _http.PostAsJsonAsync(url, request, cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al guardar estado de cuenta. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "EstadoCuentaXmlService",
                        "GuardarEstadoCuentaAsync");

                    return new GuardarEstadoCuentaResponseDto
                    {
                        Success = false,
                        Message = errorContent
                    };
                }

                var result = await response.Content.ReadFromJsonAsync<GuardarEstadoCuentaResponseDto>(_jsonOptions, cancellationToken).ConfigureAwait(false);
                return result ?? new GuardarEstadoCuentaResponseDto
                {
                    Success = true,
                    Message = "Estado de cuenta guardado correctamente."
                };
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al guardar estado de cuenta", ex, "EstadoCuentaXmlService", "GuardarEstadoCuentaAsync");
                throw new InvalidOperationException("Error de comunicacion con el servidor al guardar el estado de cuenta.", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al guardar estado de cuenta", ex, "EstadoCuentaXmlService", "GuardarEstadoCuentaAsync");
                throw;
            }
        }
    }
}
