using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.EndPointProvider;
using Advance_Control.Services.Logging;
using Advance_Control.Utilities;

namespace Advance_Control.Services.Suscripciones
{
    /// <summary>
    /// Implementación del servicio de contratos de suscripción que se comunica con la API
    /// </summary>
    public class ContratoSuscripcionService : IContratoSuscripcionService
    {
        private readonly HttpClient _http;
        private readonly IApiEndpointProvider _endpoints;
        private readonly ILoggingService _logger;

        public ContratoSuscripcionService(HttpClient http, IApiEndpointProvider endpoints, ILoggingService logger)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<ContratoSuscripcionDto>> GetContratosAsync(int idCliente, CancellationToken ct = default)
        {
            try
            {
                var url = new ApiQueryBuilder()
                    .AddRequired("idCliente", idCliente)
                    .Build(_endpoints.GetEndpoint("api", "contratos-suscripcion"));

                var response = await _http.GetAsync(url, ct).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    await _logger.LogErrorAsync(
                        $"Error al obtener contratos de suscripción. Status: {response.StatusCode}",
                        null, "ContratoSuscripcionService", "GetContratosAsync");
                    return new List<ContratoSuscripcionDto>();
                }

                var contratos = await response.Content.ReadFromJsonAsync<List<ContratoSuscripcionDto>>(cancellationToken: ct).ConfigureAwait(false);
                return contratos ?? new List<ContratoSuscripcionDto>();
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al obtener contratos de suscripción", ex, "ContratoSuscripcionService", "GetContratosAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al obtener contratos de suscripción", ex);
            }
        }

        public async Task<ContratoSuscripcionDto?> GetContratoByIdAsync(int idContrato, CancellationToken ct = default)
        {
            try
            {
                var url = _endpoints.GetEndpoint("api", "contratos-suscripcion", idContrato.ToString());
                var response = await _http.GetAsync(url, ct).ConfigureAwait(false);

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return null;

                if (!response.IsSuccessStatusCode)
                {
                    await _logger.LogErrorAsync(
                        $"Error al obtener contrato de suscripción {idContrato}. Status: {response.StatusCode}",
                        null, "ContratoSuscripcionService", "GetContratoByIdAsync");
                    return null;
                }

                return await response.Content.ReadFromJsonAsync<ContratoSuscripcionDto>(cancellationToken: ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al obtener contrato de suscripción por id", ex, "ContratoSuscripcionService", "GetContratoByIdAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al obtener el contrato de suscripción", ex);
            }
        }

        public async Task<ContratoSuscripcionDto?> CreateContratoAsync(
            int idCliente, string nivel, string? numeroContrato, string? direccionInstalacion,
            decimal montoMensual, int numeroUnidades, DateTime vigenciaInicio, DateTime vigenciaFin,
            string? nombreFirmante, string? telefonoFirmante, DateTime? fechaFirma,
            string? pdfGeneradoUrl, int[] idsEquipos, CancellationToken ct = default)
        {
            try
            {
                var url = new ApiQueryBuilder()
                    .AddRequired("idCliente", idCliente)
                    .Add("nivel", nivel)
                    .Add("numeroContrato", numeroContrato)
                    .Add("direccionInstalacion", direccionInstalacion)
                    .AddRequired("montoMensual", montoMensual)
                    .AddRequired("numeroUnidades", numeroUnidades)
                    .Add("vigenciaInicio", vigenciaInicio.ToString("yyyy-MM-dd"))
                    .Add("vigenciaFin", vigenciaFin.ToString("yyyy-MM-dd"))
                    .Add("nombreFirmante", nombreFirmante)
                    .Add("telefonoFirmante", telefonoFirmante)
                    .Add("fechaFirma", fechaFirma?.ToString("yyyy-MM-dd"))
                    .Add("pdfGeneradoUrl", pdfGeneradoUrl)
                    .Add("idsEquipos", idsEquipos)
                    .Build(_endpoints.GetEndpoint("api", "contratos-suscripcion"));

                using var response = await _http.PostAsync(url, null, ct).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al crear contrato de suscripción. Status: {response.StatusCode}, Content: {errorContent}",
                        null, "ContratoSuscripcionService", "CreateContratoAsync");
                    return null;
                }

                var result = await response.Content.ReadFromJsonAsync<CreateContratoResponse>(cancellationToken: ct).ConfigureAwait(false);
                return result?.Contrato;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al crear contrato de suscripción", ex, "ContratoSuscripcionService", "CreateContratoAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al crear el contrato de suscripción", ex);
            }
        }

        public async Task<bool> ActualizarPdfGeneradoAsync(int idContrato, string pdfGeneradoUrl, CancellationToken ct = default)
        {
            try
            {
                var url = new ApiQueryBuilder()
                    .Add("pdfGeneradoUrl", pdfGeneradoUrl)
                    .Build(_endpoints.GetEndpoint("api", "contratos-suscripcion", idContrato.ToString()));

                using var response = await _http.PutAsync(url, null, ct).ConfigureAwait(false);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al actualizar el PDF generado del contrato", ex, "ContratoSuscripcionService", "ActualizarPdfGeneradoAsync");
                return false;
            }
        }

        public async Task<bool> MarcarFirmadoAsync(int idContrato, string pdfFirmadoUrl, CancellationToken ct = default)
        {
            try
            {
                var url = new ApiQueryBuilder()
                    .Add("pdfFirmadoUrl", pdfFirmadoUrl)
                    .Build(_endpoints.GetEndpoint("api", "contratos-suscripcion", idContrato.ToString(), "marcar-firmado"));

                using var response = await _http.PostAsync(url, null, ct).ConfigureAwait(false);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al marcar el contrato como firmado", ex, "ContratoSuscripcionService", "MarcarFirmadoAsync");
                return false;
            }
        }

        private class CreateContratoResponse
        {
            [JsonPropertyName("success")]
            public bool Success { get; set; }

            [JsonPropertyName("message")]
            public string? Message { get; set; }

            [JsonPropertyName("idContrato")]
            public int IdContrato { get; set; }

            [JsonPropertyName("contrato")]
            public ContratoSuscripcionDto? Contrato { get; set; }
        }
    }
}
