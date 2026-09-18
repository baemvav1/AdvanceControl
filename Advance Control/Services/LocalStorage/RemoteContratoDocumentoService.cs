using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.Logging;

namespace Advance_Control.Services.LocalStorage
{
    /// <summary>
    /// Implementación de <see cref="IContratoDocumentoService"/> que sube los
    /// PDFs de contratos de suscripción al VPS vía /api/uploads/contratos.
    /// </summary>
    public sealed class RemoteContratoDocumentoService : IContratoDocumentoService
    {
        private readonly HttpClient _http;
        private readonly ILoggingService _logger;

        public RemoteContratoDocumentoService(HttpClient http, ILoggingService logger)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task<string?> SubirGeneradoAsync(int idContrato, Stream pdfStream, string contentType, CancellationToken ct = default)
            => SubirAsync(idContrato, pdfStream, contentType, "generado", ct);

        public Task<string?> SubirFirmadoAsync(int idContrato, Stream pdfStream, string contentType, CancellationToken ct = default)
            => SubirAsync(idContrato, pdfStream, contentType, "firmado", ct);

        private async Task<string?> SubirAsync(int idContrato, Stream pdfStream, string contentType, string tipo, CancellationToken ct)
        {
            try
            {
                using var content = new MultipartFormDataContent();
                var sc = new StreamContent(pdfStream);
                sc.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
                content.Add(sc, "file", $"{idContrato}_{tipo}.pdf");

                var response = await _http.PostAsync($"api/uploads/contratos/{idContrato}?tipo={tipo}", content, ct);
                if (!response.IsSuccessStatusCode)
                {
                    await _logger.LogWarningAsync(
                        $"API devolvió {(int)response.StatusCode} al subir documento {tipo} del contrato {idContrato}",
                        nameof(RemoteContratoDocumentoService), nameof(SubirAsync));
                    return null;
                }

                var dto = await response.Content.ReadFromJsonAsync<UploadFileResponseDto>(cancellationToken: ct);
                if (dto == null || string.IsNullOrWhiteSpace(dto.Url)) return null;

                if (Uri.TryCreate(dto.Url, UriKind.Absolute, out _)) return dto.Url;

                var baseUri = _http.BaseAddress?.ToString().TrimEnd('/') ?? string.Empty;
                return $"{baseUri}{dto.Url}";
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error al subir documento {tipo} del contrato {idContrato}", ex,
                    nameof(RemoteContratoDocumentoService), nameof(SubirAsync));
                return null;
            }
        }
    }
}
