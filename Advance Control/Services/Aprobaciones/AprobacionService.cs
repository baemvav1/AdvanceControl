using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.EndPointProvider;
using Advance_Control.Services.Logging;

namespace Advance_Control.Services.Aprobaciones
{
    /// <summary>
    /// Contacto dirigido, cerrar/reabrir cotización, aprobación registrada por el
    /// técnico y marca de notificación (AprobacionesController de la API). Los
    /// errores de negocio salen como InvalidOperationException con el mensaje de la API.
    /// </summary>
    public interface IAprobacionService
    {
        Task<OperacionAprobacionDto> ObtenerOperacionAsync(int idOperacion, CancellationToken cancellationToken = default);
        Task<OperacionAprobacionDto> SetDirigidoAsync(int idOperacion, long idContacto, CancellationToken cancellationToken = default);
        Task<OperacionAprobacionDto> CerrarCotizacionAsync(int idOperacion, CancellationToken cancellationToken = default);
        Task<OperacionAprobacionDto> ReabrirCotizacionAsync(int idOperacion, CancellationToken cancellationToken = default);
        Task<OperacionAprobacionDto> AprobarCotizacionAsync(int idOperacion, string? comentario, string? archivo = null, CancellationToken cancellationToken = default);
        Task MarcarCotizacionNotificadaAsync(int idOperacion, string destinatarios, CancellationToken cancellationToken = default);

        Task<int> ObtenerClienteDeOrdenAsync(int idOrdenServicio, CancellationToken cancellationToken = default);
        Task<OperacionAprobacionDto> SetDirigidoDeOrdenAsync(int idOrdenServicio, long idContacto, CancellationToken cancellationToken = default);

        Task<HojaAprobacionDto> ObtenerHojaAsync(long idHoja, CancellationToken cancellationToken = default);
        Task<HojaAprobacionDto> AprobarHojaAsync(long idHoja, string? comentario, string? archivo = null, CancellationToken cancellationToken = default);
        Task MarcarHojaNotificadaAsync(long idHoja, string destinatarios, CancellationToken cancellationToken = default);
    }

    public class AprobacionService : IAprobacionService
    {
        private readonly HttpClient _http;
        private readonly IApiEndpointProvider _endpoints;
        private readonly ILoggingService _logger;

        public AprobacionService(HttpClient http, IApiEndpointProvider endpoints, ILoggingService logger)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private string Operacion(int idOperacion, string sufijo) => $"{_endpoints.GetEndpoint("api", "Operaciones")}/{idOperacion}/{sufijo}";
        private string Hoja(long idHoja, string sufijo) => $"{_endpoints.GetEndpoint("api", "MantenimientoPreventivo")}/{idHoja}/{sufijo}";

        public Task<OperacionAprobacionDto> ObtenerOperacionAsync(int idOperacion, CancellationToken cancellationToken = default)
            => LeerAsync<OperacionAprobacionDto>(() => _http.GetAsync(Operacion(idOperacion, "aprobacion"), cancellationToken), "ObtenerOperacionAsync", cancellationToken);

        public Task<OperacionAprobacionDto> SetDirigidoAsync(int idOperacion, long idContacto, CancellationToken cancellationToken = default)
            => LeerAsync<OperacionAprobacionDto>(() => _http.PutAsJsonAsync(Operacion(idOperacion, "dirigido"), new { idContacto }, cancellationToken), "SetDirigidoAsync", cancellationToken);

        public Task<OperacionAprobacionDto> CerrarCotizacionAsync(int idOperacion, CancellationToken cancellationToken = default)
            => LeerAsync<OperacionAprobacionDto>(() => _http.PostAsync(Operacion(idOperacion, "cotizacion/cerrar"), null, cancellationToken), "CerrarCotizacionAsync", cancellationToken);

        public Task<OperacionAprobacionDto> ReabrirCotizacionAsync(int idOperacion, CancellationToken cancellationToken = default)
            => LeerAsync<OperacionAprobacionDto>(() => _http.PostAsync(Operacion(idOperacion, "cotizacion/reabrir"), null, cancellationToken), "ReabrirCotizacionAsync", cancellationToken);

        public Task<OperacionAprobacionDto> AprobarCotizacionAsync(int idOperacion, string? comentario, string? archivo = null, CancellationToken cancellationToken = default)
            => LeerAsync<OperacionAprobacionDto>(() => _http.PostAsJsonAsync(Operacion(idOperacion, "cotizacion/aprobar"), new { comentario, archivo }, cancellationToken), "AprobarCotizacionAsync", cancellationToken);

        public async Task MarcarCotizacionNotificadaAsync(int idOperacion, string destinatarios, CancellationToken cancellationToken = default)
        {
            using var _ = await EnviarAsync(() => _http.PostAsJsonAsync(Operacion(idOperacion, "cotizacion/notificada"), new { destinatarios }, cancellationToken), "MarcarCotizacionNotificadaAsync").ConfigureAwait(false);
        }

        private string Orden(int idOrden, string sufijo) => $"{_endpoints.GetEndpoint("api", "OrdenServicio")}/{idOrden}/{sufijo}";

        public async Task<int> ObtenerClienteDeOrdenAsync(int idOrdenServicio, CancellationToken cancellationToken = default)
            => (await LeerAsync<ClienteDeOrden>(() => _http.GetAsync(Orden(idOrdenServicio, "cliente"), cancellationToken), "ObtenerClienteDeOrdenAsync", cancellationToken).ConfigureAwait(false)).IdCliente;

        public Task<OperacionAprobacionDto> SetDirigidoDeOrdenAsync(int idOrdenServicio, long idContacto, CancellationToken cancellationToken = default)
            => LeerAsync<OperacionAprobacionDto>(() => _http.PutAsJsonAsync(Orden(idOrdenServicio, "dirigido"), new { idContacto }, cancellationToken), "SetDirigidoDeOrdenAsync", cancellationToken);

        private sealed class ClienteDeOrden { public int IdCliente { get; set; } }

        public Task<HojaAprobacionDto> ObtenerHojaAsync(long idHoja, CancellationToken cancellationToken = default)
            => LeerAsync<HojaAprobacionDto>(() => _http.GetAsync(Hoja(idHoja, "aprobacion"), cancellationToken), "ObtenerHojaAsync", cancellationToken);

        public Task<HojaAprobacionDto> AprobarHojaAsync(long idHoja, string? comentario, string? archivo = null, CancellationToken cancellationToken = default)
            => LeerAsync<HojaAprobacionDto>(() => _http.PostAsJsonAsync(Hoja(idHoja, "aprobar"), new { comentario, archivo }, cancellationToken), "AprobarHojaAsync", cancellationToken);

        public async Task MarcarHojaNotificadaAsync(long idHoja, string destinatarios, CancellationToken cancellationToken = default)
        {
            using var _ = await EnviarAsync(() => _http.PostAsJsonAsync(Hoja(idHoja, "notificada"), new { destinatarios }, cancellationToken), "MarcarHojaNotificadaAsync").ConfigureAwait(false);
        }

        private async Task<T> LeerAsync<T>(Func<Task<HttpResponseMessage>> enviar, string operacion, CancellationToken cancellationToken)
        {
            using var response = await EnviarAsync(enviar, operacion).ConfigureAwait(false);
            return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken).ConfigureAwait(false)
                   ?? throw new InvalidOperationException("Respuesta vacía del servidor.");
        }

        /// <summary>Devuelve la respuesta si fue exitosa; si no, lanza InvalidOperationException con el mensaje de la API.</summary>
        private async Task<HttpResponseMessage> EnviarAsync(Func<Task<HttpResponseMessage>> enviar, string operacion)
        {
            HttpResponseMessage response;
            try
            {
                response = await enviar().ConfigureAwait(false);
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red en aprobaciones", ex, nameof(AprobacionService), operacion);
                throw new InvalidOperationException("No hay conexión con el servidor. Intenta de nuevo.", ex);
            }

            if (response.IsSuccessStatusCode)
                return response;

            var contenido = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            response.Dispose();
            await _logger.LogErrorAsync($"Error en aprobaciones. Status: {response.StatusCode}. Content: {contenido}", null, nameof(AprobacionService), operacion);
            throw new InvalidOperationException(ExtraerMensaje(contenido) ?? $"El servidor respondió {(int)response.StatusCode}.");
        }

        private static string? ExtraerMensaje(string contenido)
        {
            try
            {
                using var doc = JsonDocument.Parse(contenido);
                return doc.RootElement.ValueKind == JsonValueKind.Object
                       && doc.RootElement.TryGetProperty("message", out var message)
                       && message.ValueKind == JsonValueKind.String
                    ? message.GetString()
                    : null;
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}
