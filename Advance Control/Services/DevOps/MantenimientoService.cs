using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Services.EndPointProvider;

namespace Advance_Control.Services.DevOps
{
    /// <summary>Estado del modo mantenimiento y entorno de la API (api/Sistema/mantenimiento).</summary>
    public class MantenimientoEstado
    {
        public bool Activo { get; set; }
        public DateTime? ActivadoEn { get; set; }
        public string? ActivadoPor { get; set; }

        /// <summary>"Produccion" o "Pruebas".</summary>
        public string? Entorno { get; set; }
    }

    /// <summary>
    /// Modo mantenimiento: mientras está activo solo trabajan usuarios nivel 1, el portal queda
    /// cerrado y no se timbra (todo lo impone la API). Lo activa un nivel 1 desde DevOps.
    /// </summary>
    public interface IMantenimientoService
    {
        /// <summary>Último estado conocido (lo actualiza <see cref="ObtenerEstadoAsync"/>).</summary>
        MantenimientoEstado Estado { get; }

        /// <summary>Se dispara cuando cambia el estado activo/inactivo.</summary>
        event EventHandler? EstadoCambiado;

        Task<MantenimientoEstado> ObtenerEstadoAsync(CancellationToken ct = default);
        Task ActivarAsync(CancellationToken ct = default);
        Task DesactivarAsync(CancellationToken ct = default);
    }

    public class MantenimientoService : IMantenimientoService
    {
        private readonly HttpClient _http;
        private readonly IApiEndpointProvider _endpoints;

        public MantenimientoService(HttpClient http, IApiEndpointProvider endpoints)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
        }

        public MantenimientoEstado Estado { get; private set; } = new();

        public event EventHandler? EstadoCambiado;

        public async Task<MantenimientoEstado> ObtenerEstadoAsync(CancellationToken ct = default)
        {
            var estado = await _http.GetFromJsonAsync<MantenimientoEstado>(_endpoints.GetEndpoint("api", "Sistema", "mantenimiento"), ct).ConfigureAwait(false)
                         ?? new MantenimientoEstado();
            var cambio = estado.Activo != Estado.Activo;
            Estado = estado;
            if (cambio)
                EstadoCambiado?.Invoke(this, EventArgs.Empty);
            return estado;
        }

        public Task ActivarAsync(CancellationToken ct = default) => CambiarAsync("mantenimiento/activar", ct);

        public Task DesactivarAsync(CancellationToken ct = default) => CambiarAsync("mantenimiento/desactivar", ct);

        private async Task CambiarAsync(string ruta, CancellationToken ct)
        {
            using var response = await _http.PostAsync(_endpoints.GetEndpoint("api", "DevOps", ruta), null, ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                var cuerpo = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                throw new InvalidOperationException(Mensaje(cuerpo) ?? $"El servidor respondió {(int)response.StatusCode}.");
            }

            await ObtenerEstadoAsync(ct).ConfigureAwait(false);
        }

        private static string? Mensaje(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                return doc.RootElement.ValueKind == JsonValueKind.Object
                       && doc.RootElement.TryGetProperty("message", out var m) && m.ValueKind == JsonValueKind.String
                    ? m.GetString() : null;
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}
