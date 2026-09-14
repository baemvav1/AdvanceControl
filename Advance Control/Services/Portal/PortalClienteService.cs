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

namespace Advance_Control.Services.Portal
{
    public class PortalClienteService : IPortalClienteService
    {
        private readonly HttpClient _http;
        private readonly IApiEndpointProvider _endpoints;
        private readonly ILoggingService _logger;

        public PortalClienteService(HttpClient http, IApiEndpointProvider endpoints, ILoggingService logger)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<OperacionClientePortalDto>> ObtenerOperacionesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"{_endpoints.GetEndpoint("api", "PortalCliente")}/operaciones";
                var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync($"Error al obtener operaciones del portal. Status: {response.StatusCode}. Content: {content}", null, "PortalClienteService", "ObtenerOperacionesAsync");
                    return new List<OperacionClientePortalDto>();
                }

                return await response.Content.ReadFromJsonAsync<List<OperacionClientePortalDto>>(cancellationToken: cancellationToken).ConfigureAwait(false)
                    ?? new List<OperacionClientePortalDto>();
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error al obtener operaciones del portal", ex, "PortalClienteService", "ObtenerOperacionesAsync");
                return new List<OperacionClientePortalDto>();
            }
        }

        public async Task<List<MantenimientoPreventivoHojaDto>?> ObtenerHojasMantenimientoAsync(int idOperacion, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"{_endpoints.GetEndpoint("api", "PortalCliente")}/operaciones/{idOperacion}/hojas-mantenimiento";
                var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync($"Error al obtener hojas de mantenimiento del portal. Status: {response.StatusCode}. Content: {content}", null, "PortalClienteService", "ObtenerHojasMantenimientoAsync");
                    return null;
                }

                return await response.Content.ReadFromJsonAsync<List<MantenimientoPreventivoHojaDto>>(cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error al obtener hojas de mantenimiento del portal", ex, "PortalClienteService", "ObtenerHojasMantenimientoAsync");
                return null;
            }
        }

        public async Task<MantenimientoPreventivoHojaDto?> ObtenerHojaMantenimientoAsync(long idHoja, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"{_endpoints.GetEndpoint("api", "PortalCliente")}/hojas-mantenimiento/{idHoja}";
                var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync($"Error al obtener hoja de mantenimiento del portal. Status: {response.StatusCode}. Content: {content}", null, "PortalClienteService", "ObtenerHojaMantenimientoAsync");
                    return null;
                }

                return await response.Content.ReadFromJsonAsync<MantenimientoPreventivoHojaDto>(cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error al obtener hoja de mantenimiento del portal", ex, "PortalClienteService", "ObtenerHojaMantenimientoAsync");
                return null;
            }
        }

        public async Task<MantenimientoPreventivoFirmarResponseDto?> FirmarHojaMantenimientoAsync(long idHoja, CancellationToken cancellationToken = default)
        {
            var url = $"{_endpoints.GetEndpoint("api", "PortalCliente")}/hojas-mantenimiento/{idHoja}/firmar";
            using var response = await _http.PostAsync(url, null, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                await _logger.LogErrorAsync($"Error al firmar hoja de mantenimiento. Status: {response.StatusCode}. Content: {content}", null, "PortalClienteService", "FirmarHojaMantenimientoAsync");
                throw new InvalidOperationException(ExtractMessage(content));
            }

            return await response.Content.ReadFromJsonAsync<MantenimientoPreventivoFirmarResponseDto>(cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        private static string ExtractMessage(string? content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return "No se recibió detalle del error.";

            try
            {
                using var document = JsonDocument.Parse(content);
                if (document.RootElement.ValueKind == JsonValueKind.Object
                    && document.RootElement.TryGetProperty("message", out var messageElement)
                    && messageElement.ValueKind == JsonValueKind.String)
                {
                    var message = messageElement.GetString();
                    if (!string.IsNullOrWhiteSpace(message))
                        return message;
                }
            }
            catch (JsonException)
            {
            }

            return content;
        }
    }
}
