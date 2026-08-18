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

namespace Advance_Control.Services.VisorMundial
{
    public class VisorMundialService : IVisorMundialService
    {
        private readonly HttpClient _http;
        private readonly IApiEndpointProvider _endpoints;
        private readonly ILoggingService _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public VisorMundialService(HttpClient http, IApiEndpointProvider endpoints, ILoggingService logger)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<List<VisorMundialUbicacionDto>> ObtenerUbicacionesAsync(CancellationToken cancellationToken = default)
        {
            var url = _endpoints.GetEndpoint("api", "visor-mundial", "ubicaciones");

            try
            {
                await _logger.LogInformationAsync($"Consultando ubicaciones del Visor Mundial en: {url}", "VisorMundialService", "ObtenerUbicacionesAsync");
                var result = await _http.GetFromJsonAsync<List<VisorMundialUbicacionDto>>(url, _jsonOptions, cancellationToken).ConfigureAwait(false);
                return result ?? new List<VisorMundialUbicacionDto>();
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al consultar ubicaciones del Visor Mundial", ex, "VisorMundialService", "ObtenerUbicacionesAsync");
                throw new InvalidOperationException("Error de comunicacion con el servidor al consultar ubicaciones.", ex);
            }
        }

        public async Task<List<VisorMundialEquipoDto>> ObtenerEquiposPorUbicacionAsync(int idUbicacion, CancellationToken cancellationToken = default)
        {
            var url = _endpoints.GetEndpoint("api", "visor-mundial", "ubicaciones", idUbicacion.ToString(), "equipos");

            try
            {
                await _logger.LogInformationAsync($"Consultando equipos de la ubicacion en: {url}", "VisorMundialService", "ObtenerEquiposPorUbicacionAsync");
                var result = await _http.GetFromJsonAsync<List<VisorMundialEquipoDto>>(url, _jsonOptions, cancellationToken).ConfigureAwait(false);
                return result ?? new List<VisorMundialEquipoDto>();
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al consultar equipos de la ubicacion", ex, "VisorMundialService", "ObtenerEquiposPorUbicacionAsync");
                throw new InvalidOperationException("Error de comunicacion con el servidor al consultar equipos de la ubicacion.", ex);
            }
        }

        public async Task<List<VisorMundialEquipoFacturaDto>> ObtenerFacturasPorEquipoAsync(int idEquipo, CancellationToken cancellationToken = default)
        {
            var url = _endpoints.GetEndpoint("api", "visor-mundial", "equipos", idEquipo.ToString(), "facturas");

            try
            {
                await _logger.LogInformationAsync($"Consultando facturas del equipo en: {url}", "VisorMundialService", "ObtenerFacturasPorEquipoAsync");
                var result = await _http.GetFromJsonAsync<List<VisorMundialEquipoFacturaDto>>(url, _jsonOptions, cancellationToken).ConfigureAwait(false);
                return result ?? new List<VisorMundialEquipoFacturaDto>();
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al consultar facturas del equipo", ex, "VisorMundialService", "ObtenerFacturasPorEquipoAsync");
                throw new InvalidOperationException("Error de comunicacion con el servidor al consultar facturas del equipo.", ex);
            }
        }
    }
}
