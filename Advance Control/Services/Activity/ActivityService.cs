using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.EndPointProvider;
using Advance_Control.Services.Logging;

namespace Advance_Control.Services.Activity
{
    /// <summary>
    /// Implementación del servicio de actividad reciente.
    /// Consume el endpoint GET /api/Logging/actividad del servidor.
    /// </summary>
    public class ActivityService : IActivityService
    {
        private readonly HttpClient _http;
        private readonly IApiEndpointProvider _endpoints;
        private readonly ILoggingService _logger;

        public ActivityService(HttpClient http, IApiEndpointProvider endpoints, ILoggingService logger)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IReadOnlyList<ActivityItem>> GetActividadRecienteAsync(
            int? credencialId = null,
            string? categoria = null,
            bool soloErrores = false,
            int top = 30,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Construir query string
                var qs = $"?nivelMinimo=2&soloErrores={soloErrores.ToString().ToLower()}&top={top}";
                if (credencialId.HasValue) qs += $"&credencialId={credencialId.Value}";
                if (!string.IsNullOrEmpty(categoria)) qs += $"&categoria={Uri.EscapeDataString(categoria)}";

                var url = _endpoints.GetEndpoint("api", "Logging", "actividad") + qs;

                var items = await _http
                    .GetFromJsonAsync<List<ActivityItem>>(url, cancellationToken)
                    .ConfigureAwait(false);

                return items as IReadOnlyList<ActivityItem> ?? Array.Empty<ActivityItem>();
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogWarningAsync(
                    "No se pudo obtener la actividad reciente del servidor",
                    "ActivityService", "GetActividadRecienteAsync");
                _ = ex;
                return Array.Empty<ActivityItem>();
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync(
                    "Error inesperado al obtener actividad reciente",
                    ex, "ActivityService", "GetActividadRecienteAsync");
                return Array.Empty<ActivityItem>();
            }
        }
    }
}
