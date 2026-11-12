using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Services.EndPointProvider;
using Advance_Control.Services.Logging;

namespace Advance_Control.Services.Http
{
    /// <summary>
    /// Archivos de /storage de la API (chat, contratos…). Desde que la API exige token para
    /// servirlos, ya no se pueden abrir con su URL directa (BitmapImage, navegador): se bajan
    /// con el HttpClient autenticado a una caché local y se usan desde ahí.
    /// </summary>
    public interface IArchivoProtegidoService
    {
        /// <summary>URL absoluta de un archivo de storage ("/storage/..." o "/pruebas/storage/...").</summary>
        string? ResolverUrl(string? url);

        /// <summary>Ruta local del archivo (lo baja si no está en caché); null si no se pudo.</summary>
        Task<string?> DescargarAsync(string? url, CancellationToken ct = default);

        /// <summary>Lo baja y lo abre con la aplicación predeterminada de Windows.</summary>
        Task<bool> AbrirAsync(string? url, CancellationToken ct = default);
    }

    public sealed class ArchivoProtegidoService : IArchivoProtegidoService
    {
        private readonly HttpClient _http;
        private readonly IApiEndpointProvider _endpoints;
        private readonly ILoggingService _logger;
        private readonly ConcurrentDictionary<string, Task<string?>> _enCurso = new();

        private static readonly string Carpeta = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AdvanceControl", "ArchivosStorage");

        public ArchivoProtegidoService(HttpClient http, IApiEndpointProvider endpoints, ILoggingService logger)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string? ResolverUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;
            if (Uri.TryCreate(url, UriKind.Absolute, out var absoluta) && absoluta.Scheme.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                return absoluta.ToString();
            if (!url.StartsWith('/')) return null;

            if (!Uri.TryCreate(_endpoints.GetApiBaseUrl().TrimEnd('/') + "/", UriKind.Absolute, out var baseUri)) return null;

            // La API ya manda el PathBase del entorno ("/pruebas/storage/..."): va contra la raíz del
            // host. Las ligas guardadas antes de eso ("/storage/...") van debajo de la base configurada.
            var prefijo = baseUri.AbsolutePath.TrimEnd('/');
            var conPrefijo = prefijo.Length == 0 || url.StartsWith(prefijo + "/", StringComparison.OrdinalIgnoreCase);
            return new Uri(baseUri, conPrefijo ? url : prefijo + url).ToString();
        }

        public Task<string?> DescargarAsync(string? url, CancellationToken ct = default)
        {
            var absoluta = ResolverUrl(url);
            if (absoluta == null) return Task.FromResult<string?>(null);

            var local = RutaLocal(absoluta);
            if (File.Exists(local)) return Task.FromResult<string?>(local);

            // Varias burbujas con la misma imagen comparten una sola descarga.
            return _enCurso.GetOrAdd(local, _ => BajarAsync(absoluta, local, ct)
                .ContinueWith(t => { _enCurso.TryRemove(local, out var _); return t.Result; }, TaskScheduler.Default));
        }

        public async Task<bool> AbrirAsync(string? url, CancellationToken ct = default)
        {
            var local = await DescargarAsync(url, ct);
            if (local == null) return false;
            try
            {
                var archivo = await global::Windows.Storage.StorageFile.GetFileFromPathAsync(local);
                return await global::Windows.System.Launcher.LaunchFileAsync(archivo);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"No se pudo abrir {local}", ex, nameof(ArchivoProtegidoService), nameof(AbrirAsync));
                return false;
            }
        }

        private async Task<string?> BajarAsync(string absoluta, string local, CancellationToken ct)
        {
            try
            {
                using var respuesta = await _http.GetAsync(absoluta, ct);
                if (!respuesta.IsSuccessStatusCode)
                {
                    await _logger.LogWarningAsync($"Storage respondió {(int)respuesta.StatusCode} para {absoluta}",
                        nameof(ArchivoProtegidoService), nameof(BajarAsync));
                    return null;
                }

                Directory.CreateDirectory(Carpeta);
                var temporal = local + ".tmp";
                await using (var destino = File.Create(temporal))
                    await respuesta.Content.CopyToAsync(destino, ct);
                File.Move(temporal, local, overwrite: true);
                return local;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"No se pudo descargar {absoluta}", ex, nameof(ArchivoProtegidoService), nameof(BajarAsync));
                return null;
            }
        }

        /// <summary>Nombre estable por URL (sin el ?ref=), conservando la extensión para abrirlo con Windows.</summary>
        private static string RutaLocal(string absoluta)
        {
            var uri = new Uri(absoluta);
            var clave = uri.GetLeftPart(UriPartial.Path);
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(clave.ToLowerInvariant())))[..24];
            var nombre = Path.GetFileName(Uri.UnescapeDataString(uri.AbsolutePath));
            foreach (var c in Path.GetInvalidFileNameChars()) nombre = nombre.Replace(c, '_');
            return Path.Combine(Carpeta, $"{hash}_{nombre}");
        }
    }
}
