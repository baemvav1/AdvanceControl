using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Services.EndPointProvider;
using Advance_Control.Services.Logging;

namespace Advance_Control.Services.DevOps
{
    /// <summary>
    /// Implementación del servicio DevOps que se comunica con la API.
    /// </summary>
    public class DevOpsService : IDevOpsService
    {
        private readonly HttpClient _http;
        private readonly IApiEndpointProvider _endpoints;
        private readonly ILoggingService _logger;

        public DevOpsService(HttpClient http, IApiEndpointProvider endpoints, ILoggingService logger)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<DevOpsWipeResult>> LimpiarFinancieroAsync(CancellationToken ct = default)
            => await EjecutarLimpiezaAsync("limpiar/financiero", ct);

        public async Task<List<DevOpsWipeResult>> LimpiarOperacionesAsync(CancellationToken ct = default)
            => await EjecutarLimpiezaAsync("limpiar/operaciones", ct);

        public async Task<List<DevOpsWipeResult>> LimpiarMantenimientoAsync(CancellationToken ct = default)
            => await EjecutarLimpiezaAsync("limpiar/mantenimiento", ct);

        public async Task<List<DevOpsWipeResult>> LimpiarLevantamientosAsync(CancellationToken ct = default)
            => await EjecutarLimpiezaAsync("limpiar/levantamientos", ct);

        public async Task<List<DevOpsWipeResult>> LimpiarServiciosAsync(CancellationToken ct = default)
            => await EjecutarLimpiezaAsync("limpiar/servicios", ct);

        public async Task<List<DevOpsWipeResult>> LimpiarLogsAsync(CancellationToken ct = default)
            => await EjecutarLimpiezaAsync("limpiar/logs", ct);

        public async Task<List<DevOpsWipeResult>> LimpiarUbicacionesAsync(CancellationToken ct = default)
            => await EjecutarLimpiezaAsync("limpiar/ubicaciones", ct);

        public async Task<List<DevOpsWipeResult>> LimpiarConciliacionPorRangoAsync(
            DateTime fechaInicio, DateTime fechaFin, CancellationToken ct = default)
        {
            try
            {
                var url = _endpoints.GetEndpoint("api", "DevOps", "limpiar/conciliacion-rango");
                await _logger.LogWarningAsync(
                    $"Ejecutando limpieza de conciliación por rango: {fechaInicio:yyyy-MM-dd} – {fechaFin:yyyy-MM-dd}",
                    "DevOpsService", "LimpiarConciliacionPorRangoAsync");

                var payload = new { FechaInicio = fechaInicio, FechaFin = fechaFin };
                using var response = await _http.PostAsJsonAsync(url, payload, ct).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                    throw new Exception($"Error al ejecutar limpieza de conciliación: {response.StatusCode} - {error}");
                }

                return await response.Content.ReadFromJsonAsync<List<DevOpsWipeResult>>(cancellationToken: ct)
                    .ConfigureAwait(false) ?? new List<DevOpsWipeResult>();
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync(
                    $"Error al limpiar conciliación por rango: {ex.Message}", ex,
                    "DevOpsService", "LimpiarConciliacionPorRangoAsync");
                throw;
            }
        }

        public async Task<List<DevOpsStatsResult>> ObtenerEstadisticasAsync(CancellationToken ct = default)
        {
            try
            {
                var url = _endpoints.GetEndpoint("api", "DevOps", "estadisticas");
                await _logger.LogInformationAsync("Obteniendo estadísticas DevOps", "DevOpsService", "ObtenerEstadisticasAsync");

                var response = await _http.GetAsync(url, ct).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                    throw new Exception($"Error al obtener estadísticas: {response.StatusCode} - {error}");
                }

                return await response.Content.ReadFromJsonAsync<List<DevOpsStatsResult>>(cancellationToken: ct).ConfigureAwait(false)
                    ?? new List<DevOpsStatsResult>();
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error al obtener estadísticas DevOps: {ex.Message}", ex, "DevOpsService", "ObtenerEstadisticasAsync");
                throw;
            }
        }

        private async Task<List<DevOpsWipeResult>> EjecutarLimpiezaAsync(string ruta, CancellationToken ct)
        {
            try
            {
                var url = _endpoints.GetEndpoint("api", "DevOps", ruta);
                await _logger.LogWarningAsync($"Ejecutando limpieza DevOps: {ruta}", "DevOpsService", "EjecutarLimpiezaAsync");

                using var response = await _http.PostAsync(url, null, ct).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                    throw new Exception($"Error al ejecutar limpieza ({ruta}): {response.StatusCode} - {error}");
                }

                return await response.Content.ReadFromJsonAsync<List<DevOpsWipeResult>>(cancellationToken: ct).ConfigureAwait(false)
                    ?? new List<DevOpsWipeResult>();
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error al ejecutar limpieza DevOps ({ruta}): {ex.Message}", ex, "DevOpsService", "EjecutarLimpiezaAsync");
                throw;
            }
        }
    }
}
