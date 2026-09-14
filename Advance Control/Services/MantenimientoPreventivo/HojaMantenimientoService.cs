using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.EndPointProvider;
using Advance_Control.Services.Logging;

namespace Advance_Control.Services.MantenimientoPreventivo
{
    public class HojaMantenimientoService : IHojaMantenimientoService
    {
        private readonly HttpClient _http;
        private readonly IApiEndpointProvider _endpoints;
        private readonly ILoggingService _logger;

        public HojaMantenimientoService(HttpClient http, IApiEndpointProvider endpoints, ILoggingService logger)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<MantenimientoPreventivoHojaDto>?> ObtenerPorOperacionAsync(int idOperacion, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"{_endpoints.GetEndpoint("api", "MantenimientoPreventivo")}/operacion/{idOperacion}";
                var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync($"Error al obtener hojas de mantenimiento. Status: {response.StatusCode}. Content: {content}", null, "HojaMantenimientoService", "ObtenerPorOperacionAsync");
                    return null;
                }

                return await response.Content.ReadFromJsonAsync<List<MantenimientoPreventivoHojaDto>>(cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error al obtener hojas de mantenimiento", ex, "HojaMantenimientoService", "ObtenerPorOperacionAsync");
                return null;
            }
        }

        public async Task<MantenimientoPreventivoHojaDto?> CrearAsync(MantenimientoPreventivoGuardarRequestDto request, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = _endpoints.GetEndpoint("api", "MantenimientoPreventivo");
                using var response = await _http.PostAsJsonAsync(url, request, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync($"Error al crear hoja de mantenimiento. Status: {response.StatusCode}. Content: {content}", null, "HojaMantenimientoService", "CrearAsync");
                    return null;
                }

                return await response.Content.ReadFromJsonAsync<MantenimientoPreventivoHojaDto>(cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error al crear hoja de mantenimiento", ex, "HojaMantenimientoService", "CrearAsync");
                return null;
            }
        }

        public async Task<MantenimientoPreventivoHojaDto?> ActualizarAsync(long id, MantenimientoPreventivoGuardarRequestDto request, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"{_endpoints.GetEndpoint("api", "MantenimientoPreventivo")}/{id}";
                using var response = await _http.PutAsJsonAsync(url, request, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync($"Error al actualizar hoja de mantenimiento. Status: {response.StatusCode}. Content: {content}", null, "HojaMantenimientoService", "ActualizarAsync");
                    return null;
                }

                return await response.Content.ReadFromJsonAsync<MantenimientoPreventivoHojaDto>(cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error al actualizar hoja de mantenimiento", ex, "HojaMantenimientoService", "ActualizarAsync");
                return null;
            }
        }
    }
}
