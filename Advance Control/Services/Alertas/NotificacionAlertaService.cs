using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.EndPointProvider;

namespace Advance_Control.Services.Alertas
{
    public class NotificacionAlertaService : INotificacionAlertaService
    {
        private readonly HttpClient _http;
        private readonly IApiEndpointProvider _endpoints;

        public NotificacionAlertaService(HttpClient http, IApiEndpointProvider endpoints)
        {
            _http      = http      ?? throw new ArgumentNullException(nameof(http));
            _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
        }

        public async Task<IReadOnlyList<NotificacionAlerta>> GenerarYObtenerAsync(int credencialId, CancellationToken cancellationToken = default)
        {
            var url = _endpoints.GetEndpoint("api", "Notificacion", $"generar/{credencialId}");
            var result = await _http.PostAsJsonAsync(url, new { }, cancellationToken);
            result.EnsureSuccessStatusCode();
            return await result.Content.ReadFromJsonAsync<List<NotificacionAlerta>>(cancellationToken: cancellationToken)
                   ?? new List<NotificacionAlerta>();
        }

        public async Task<IReadOnlyList<NotificacionAlerta>> GetAsync(int credencialId, CancellationToken cancellationToken = default)
        {
            var url = _endpoints.GetEndpoint("api", "Notificacion", credencialId.ToString());
            return await _http.GetFromJsonAsync<List<NotificacionAlerta>>(url, cancellationToken)
                   ?? new List<NotificacionAlerta>();
        }

        public async Task MarcarVistasAsync(int credencialId, CancellationToken cancellationToken = default)
        {
            var url = _endpoints.GetEndpoint("api", "Notificacion", $"{credencialId}/vistas");
            await _http.PostAsJsonAsync(url, new { }, cancellationToken);
        }
    }
}
