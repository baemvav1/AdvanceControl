using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.EndPointProvider;
using Advance_Control.Services.Logging;
using Advance_Control.Services.Session;

namespace Advance_Control.Services.Activity
{
    /// <summary>
    /// Implementación del servicio de actividad de usuario.
    /// Consume GET /api/Actividad y POST /api/Actividad.
    /// </summary>
    public class ActivityService : IActivityService
    {
        private readonly HttpClient _http;
        private readonly IApiEndpointProvider _endpoints;
        private readonly ILoggingService _logger;
        private readonly Lazy<IUserSessionService> _session;

        public ActivityService(
            HttpClient http,
            IApiEndpointProvider endpoints,
            ILoggingService logger,
            Lazy<IUserSessionService> session)
        {
            _http      = http      ?? throw new ArgumentNullException(nameof(http));
            _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
            _logger    = logger    ?? throw new ArgumentNullException(nameof(logger));
            _session   = session   ?? throw new ArgumentNullException(nameof(session));
        }

        public async Task<IReadOnlyList<ActivityItem>> GetActividadAsync(
            int credencialId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var url = _endpoints.GetEndpoint("api", "Actividad") + $"?credencialId={credencialId}";

                var items = await _http
                    .GetFromJsonAsync<List<ActivityItem>>(url, cancellationToken)
                    .ConfigureAwait(false);

                return items?.AsReadOnly() ?? (IReadOnlyList<ActivityItem>)Array.Empty<ActivityItem>();
            }
            catch (HttpRequestException)
            {
                await _logger.LogWarningAsync(
                    "No se pudo obtener la actividad del usuario",
                    "ActivityService", "GetActividadAsync");
                return Array.Empty<ActivityItem>();
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync(
                    "Error inesperado al obtener la actividad",
                    ex, "ActivityService", "GetActividadAsync");
                return Array.Empty<ActivityItem>();
            }
        }

        public async Task CrearActividadAsync(
            string origen,
            string titulo,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var sess = _session.Value;
                if (!sess.IsLoaded || sess.CredencialId <= 0) return;

                var url = _endpoints.GetEndpoint("api", "Actividad");

                var payload = new
                {
                    credencialId = sess.CredencialId,
                    origen       = origen ?? "Sistema",
                    titulo       = titulo ?? string.Empty
                };

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(5));
                _ = await _http.PostAsJsonAsync(url, payload, cts.Token).ConfigureAwait(false);
            }
            catch
            {
                // El registro de actividad nunca debe interrumpir el flujo principal
            }
        }
    }
}
