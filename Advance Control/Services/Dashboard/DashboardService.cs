using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.EndPointProvider;
using Advance_Control.Services.Logging;

namespace Advance_Control.Services.Dashboard
{
    /// <summary>
    /// Implementación del servicio de conteos del dashboard.
    /// Consume GET /api/Dashboard/conteos.
    /// </summary>
    public class DashboardService : IDashboardService
    {
        private readonly HttpClient _http;
        private readonly IApiEndpointProvider _endpoints;
        private readonly ILoggingService _logger;

        public DashboardService(
            HttpClient http,
            IApiEndpointProvider endpoints,
            ILoggingService logger)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<DashboardConteoDto?> GetConteosAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var url = _endpoints.GetEndpoint("api", "Dashboard") + "/conteos";

                var result = await _http
                    .GetFromJsonAsync<DashboardConteoDto>(url, cancellationToken)
                    .ConfigureAwait(false);

                return result;
            }
            catch (HttpRequestException)
            {
                await _logger.LogWarningAsync(
                    "No se pudo obtener los conteos del dashboard",
                    "DashboardService", "GetConteosAsync");
                return null;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync(
                    "Error inesperado al obtener conteos del dashboard",
                    ex, "DashboardService", "GetConteosAsync");
                return null;
            }
        }
    }
}
