using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Services.EndPointProvider;

namespace Advance_Control.Services.DevOps
{
    /// <summary>Estado del modo pruebas (api/Sistema/modo-pruebas).</summary>
    public class ModoPruebasEstado
    {
        public bool Activo { get; set; }
        public DateTime? ActivadoEn { get; set; }
        public string? ActivadoPor { get; set; }
    }

    /// <summary>
    /// Modo pruebas: mientras está activo solo trabajan usuarios nivel 1 (lo impone la API),
    /// no se timbra y Advance Control solo envía correos a contactos del cliente de prueba.
    /// </summary>
    public interface IModoPruebasService
    {
        /// <summary>Último estado conocido (lo actualiza <see cref="ObtenerEstadoAsync"/>).</summary>
        ModoPruebasEstado Estado { get; }

        /// <summary>Se dispara cuando cambia el estado activo/inactivo.</summary>
        event EventHandler? EstadoCambiado;

        Task<ModoPruebasEstado> ObtenerEstadoAsync(CancellationToken ct = default);
        Task<List<DevOpsWipeResult>> ActivarAsync(CancellationToken ct = default);
        Task<List<DevOpsWipeResult>> DesactivarAsync(CancellationToken ct = default);

        /// <summary>Lanza InvalidOperationException si el modo pruebas está activo y algún destinatario no es del cliente de prueba.</summary>
        Task ValidarDestinatariosAsync(IEnumerable<string> correos, CancellationToken ct = default);
    }

    public class ModoPruebasService : IModoPruebasService
    {
        private readonly HttpClient _http;
        private readonly IApiEndpointProvider _endpoints;

        public ModoPruebasService(HttpClient http, IApiEndpointProvider endpoints)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
        }

        public ModoPruebasEstado Estado { get; private set; } = new();

        public event EventHandler? EstadoCambiado;

        public async Task<ModoPruebasEstado> ObtenerEstadoAsync(CancellationToken ct = default)
        {
            var estado = await _http.GetFromJsonAsync<ModoPruebasEstado>(_endpoints.GetEndpoint("api", "Sistema", "modo-pruebas"), ct).ConfigureAwait(false)
                         ?? new ModoPruebasEstado();
            var cambio = estado.Activo != Estado.Activo;
            Estado = estado;
            if (cambio)
                EstadoCambiado?.Invoke(this, EventArgs.Empty);
            return estado;
        }

        public Task<List<DevOpsWipeResult>> ActivarAsync(CancellationToken ct = default) => CambiarAsync("modo-pruebas/activar", ct);

        public Task<List<DevOpsWipeResult>> DesactivarAsync(CancellationToken ct = default) => CambiarAsync("modo-pruebas/desactivar", ct);

        public async Task ValidarDestinatariosAsync(IEnumerable<string> correos, CancellationToken ct = default)
        {
            if (!(await ObtenerEstadoAsync(ct).ConfigureAwait(false)).Activo)
                return;

            var permitidos = await _http.GetFromJsonAsync<List<string>>(_endpoints.GetEndpoint("api", "Sistema", "modo-pruebas/correos"), ct).ConfigureAwait(false)
                             ?? new List<string>();
            var rechazados = correos
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(c => c.Trim())
                .Where(c => !permitidos.Contains(c.ToLowerInvariant()))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (rechazados.Count > 0)
                throw new InvalidOperationException(
                    $"Modo pruebas: solo se puede enviar a contactos del cliente de prueba. No permitido: {string.Join(", ", rechazados)}.");
        }

        private async Task<List<DevOpsWipeResult>> CambiarAsync(string ruta, CancellationToken ct)
        {
            using var response = await _http.PostAsync(_endpoints.GetEndpoint("api", "DevOps", ruta), null, ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                var cuerpo = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                throw new InvalidOperationException(Mensaje(cuerpo) ?? $"El servidor respondió {(int)response.StatusCode}.");
            }

            var resultados = await response.Content.ReadFromJsonAsync<List<DevOpsWipeResult>>(cancellationToken: ct).ConfigureAwait(false) ?? new();
            await ObtenerEstadoAsync(ct).ConfigureAwait(false);
            return resultados;
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
