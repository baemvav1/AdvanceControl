using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.EndPointProvider;
using Advance_Control.Services.Logging;
using Advance_Control.Utilities;

namespace Advance_Control.Services.Reportes
{
    public class ReporteFinancieroFacturacionService : IReporteFinancieroFacturacionService
    {
        private readonly HttpClient _http;
        private readonly IApiEndpointProvider _endpoints;
        private readonly ILoggingService _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public ReporteFinancieroFacturacionService(HttpClient http, IApiEndpointProvider endpoints, ILoggingService logger)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<ReporteFinancieroFacturacionResponseDto> ObtenerReporteAsync(
            string? receptorRfc,
            bool? finiquito,
            string? referencia,
            DateTimeOffset? fechaInicio,
            DateTimeOffset? fechaFin,
            CancellationToken cancellationToken = default)
        {
            var url = new ApiQueryBuilder()
                .Add("receptorRfc", receptorRfc)
                .Add("finiquito", finiquito)
                .Add("referencia", referencia)
                .Add("fechaInicio", fechaInicio?.Date.ToString("O", CultureInfo.InvariantCulture))
                .Add("fechaFin", fechaFin?.Date.AddDays(1).AddTicks(-1).ToString("O", CultureInfo.InvariantCulture))
                .Build(_endpoints.GetEndpoint("api", "reportefinancierofacturacion"));

            try
            {
                await _logger.LogInformationAsync($"Consultando reporte financiero de facturación en: {url}", "ReporteFinancieroFacturacionService", "ObtenerReporteAsync");
                var result = await _http.GetFromJsonAsync<ReporteFinancieroFacturacionResponseDto>(url, _jsonOptions, cancellationToken).ConfigureAwait(false);
                return result ?? new ReporteFinancieroFacturacionResponseDto();
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al consultar el reporte financiero de facturación", ex, "ReporteFinancieroFacturacionService", "ObtenerReporteAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al consultar el reporte financiero de facturación.", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al consultar el reporte financiero de facturación", ex, "ReporteFinancieroFacturacionService", "ObtenerReporteAsync");
                throw;
            }
        }
    }
}
