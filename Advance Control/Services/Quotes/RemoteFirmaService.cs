using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.Logging;
using global::Windows.Storage;

namespace Advance_Control.Services.Quotes
{
    /// <summary>
    /// Implementación remota de <see cref="IFirmaService"/>.
    /// Guarda las firmas en el VPS y las cachea localmente en la misma carpeta
    /// que <see cref="FirmaService"/> para que QuoteService reciba rutas locales.
    /// </summary>
    public sealed class RemoteFirmaService : IFirmaService
    {
        private readonly HttpClient _http;
        private readonly ILoggingService _logger;

        public RemoteFirmaService(HttpClient http, ILoggingService logger)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // ── Rutas locales (igual que FirmaService) ────────────────────────────

        public string GetFirmasFolder()
        {
            var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            return Path.Combine(docs, "Advance Control", "Firmas");
        }

        public string GetFirmaDireccionPath()
            => Path.Combine(GetFirmasFolder(), "FirmaDireccion.png");

        public string? GetFirmaOperadorPath(int idAtiende)
        {
            var folder = GetFirmasFolder();
            if (!Directory.Exists(folder))
                EnsureFirmasFolder();

            // Buscar primero en caché local
            if (Directory.Exists(folder))
            {
                foreach (var file in Directory.EnumerateFiles(folder, "*.png"))
                {
                    var nombre = Path.GetFileNameWithoutExtension(file);
                    var partes = nombre.Split('_');
                    if (partes.Length >= 2 && int.TryParse(partes[0], out int fileId) && fileId == idAtiende)
                        return file;
                }
            }

            // Si no hay caché local, intentar descargar del VPS de forma síncrona
            // (la interfaz no expone async, se hace best-effort)
            try
            {
                var apiFiles = _http.GetFromJsonAsync<System.Collections.Generic.List<UploadFileResponseDto>>(
                    "api/uploads/firmas").GetAwaiter().GetResult();

                if (apiFiles == null) return null;

                var match = apiFiles.FirstOrDefault(f =>
                {
                    var name = Path.GetFileNameWithoutExtension(f.FileName);
                    var parts = name.Split('_');
                    return parts.Length >= 2 && int.TryParse(parts[0], out var id) && id == idAtiende;
                });

                if (match == null) return null;

                EnsureFirmasFolder();
                var localPath = Path.Combine(GetFirmasFolder(), match.FileName);
                DownloadToLocalSync(match.Url, localPath);
                return localPath;
            }
            catch
            {
                return null;
            }
        }

        // ── Exists ────────────────────────────────────────────────────────────

        public bool ExisteFirmaDireccion()
        {
            if (File.Exists(GetFirmaDireccionPath())) return true;

            // Intentar descargar del VPS
            try
            {
                var apiFiles = _http.GetFromJsonAsync<System.Collections.Generic.List<UploadFileResponseDto>>(
                    "api/uploads/firmas").GetAwaiter().GetResult();

                var match = apiFiles?.FirstOrDefault(f =>
                    f.FileName.Equals("FirmaDireccion.png", StringComparison.OrdinalIgnoreCase));

                if (match == null) return false;

                EnsureFirmasFolder();
                var localPath = GetFirmaDireccionPath();
                DownloadToLocalSync(match.Url, localPath);
                return File.Exists(localPath);
            }
            catch
            {
                return false;
            }
        }

        public bool ExisteFirmaOperador(int idAtiende)
            => GetFirmaOperadorPath(idAtiende) != null;

        // ── Guardar ───────────────────────────────────────────────────────────

        public async Task GuardarFirmaDireccionAsync(StorageFile archivo)
        {
            EnsureFirmasFolder();
            var destino = GetFirmaDireccionPath();
            File.Copy(archivo.Path, destino, overwrite: true);

            // Subir al VPS
            await UploadFirmaAsync(archivo.Path, "direccion", 0, string.Empty, CancellationToken.None);
        }

        public async Task GuardarFirmaOperadorAsync(int idAtiende, string nombre, StorageFile archivo)
        {
            EnsureFirmasFolder();
            var folder = GetFirmasFolder();

            // Eliminar firmas previas locales del mismo operador
            foreach (var old in Directory.EnumerateFiles(folder, $"{idAtiende}_*.png").ToList())
            {
                try { File.Delete(old); } catch { /* ignorar */ }
            }

            var nombreLimpio = string.Concat(nombre.Split(Path.GetInvalidFileNameChars()));
            var destino = Path.Combine(folder, $"{idAtiende}_{nombreLimpio}.png");
            File.Copy(archivo.Path, destino, overwrite: true);

            // Subir al VPS
            await UploadFirmaAsync(archivo.Path, "operador", idAtiende, nombre, CancellationToken.None);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private async Task UploadFirmaAsync(string localFilePath, string tipo, int idAtiende, string nombre, CancellationToken ct)
        {
            try
            {
                await using var fs = new FileStream(localFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var form = new MultipartFormDataContent();
                var sc = new StreamContent(fs);
                sc.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
                form.Add(sc, "file", Path.GetFileName(localFilePath));

                var query = tipo == "operador"
                    ? $"api/uploads/firmas?tipo=operador&idAtiende={idAtiende}&nombre={Uri.EscapeDataString(nombre)}"
                    : "api/uploads/firmas?tipo=direccion";

                var response = await _http.PostAsync(query, form, ct);
                if (!response.IsSuccessStatusCode)
                {
                    await _logger.LogWarningAsync(
                        $"API devolvió {(int)response.StatusCode} al subir firma {tipo}",
                        nameof(RemoteFirmaService), nameof(UploadFirmaAsync));
                }
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error al subir firma {tipo} al VPS", ex,
                    nameof(RemoteFirmaService), nameof(UploadFirmaAsync));
            }
        }

        private void EnsureFirmasFolder()
            => Directory.CreateDirectory(GetFirmasFolder());

        private void DownloadToLocalSync(string relativeUrl, string localPath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);
            using var response = _http.GetAsync(relativeUrl).GetAwaiter().GetResult();
            if (!response.IsSuccessStatusCode) return;
            using var fs = new FileStream(localPath, FileMode.Create, FileAccess.Write, FileShare.None);
            response.Content.CopyToAsync(fs).GetAwaiter().GetResult();
        }
    }
}
