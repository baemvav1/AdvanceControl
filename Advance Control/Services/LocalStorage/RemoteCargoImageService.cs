using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.Logging;

namespace Advance_Control.Services.LocalStorage
{
    /// <summary>
    /// Implementación remota de <see cref="ICargoImageService"/>.
    /// Almacena las imágenes en el VPS y las cachea localmente en la misma
    /// carpeta que <see cref="LocalCargoImageService"/> (Operacion_{idOp}/).
    /// </summary>
    public sealed class RemoteCargoImageService : ICargoImageService
    {
        private readonly HttpClient _http;
        private readonly ILoggingService _logger;
        private readonly string _basePath;

        public RemoteCargoImageService(HttpClient http, ILoggingService logger)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _basePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "Advance Control");
            Directory.CreateDirectory(_basePath);
        }

        public async Task<CargoImageDto?> UploadImageAsync(int idOperacion, int idCargo,
            Stream imageStream, string contentType, CancellationToken ct = default)
        {
            try
            {
                using var ms = new MemoryStream();
                await imageStream.CopyToAsync(ms, ct);

                ms.Position = 0;
                var ext = GetExtension(contentType);
                using var form = new MultipartFormDataContent();
                var sc = new StreamContent(ms);
                sc.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
                form.Add(sc, "file", $"upload{ext}");

                var response = await _http.PostAsync(
                    $"api/uploads/operaciones/{idOperacion}?tipo=cargo&idCargo={idCargo}", form, ct);

                if (!response.IsSuccessStatusCode)
                {
                    await _logger.LogWarningAsync(
                        $"API devolvió {(int)response.StatusCode} al subir imagen de cargo {idCargo}",
                        nameof(RemoteCargoImageService), nameof(UploadImageAsync));
                    return null;
                }

                var dto = await response.Content.ReadFromJsonAsync<UploadFileResponseDto>(cancellationToken: ct);
                if (dto == null) return null;

                // Guardar caché local
                ms.Position = 0;
                var localPath = Path.Combine(GetOperacionFolder(idOperacion), dto.FileName);
                await SaveLocalAsync(localPath, ms, ct);

                return BuildDto(idOperacion, idCargo, dto);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error al subir imagen de cargo {idCargo}", ex,
                    nameof(RemoteCargoImageService), nameof(UploadImageAsync));
                return null;
            }
        }

        public async Task<List<CargoImageDto>> GetImagesAsync(int idOperacion, int idCargo,
            CancellationToken ct = default)
        {
            var result = new List<CargoImageDto>();
            try
            {
                var apiFiles = await _http.GetFromJsonAsync<List<UploadFileResponseDto>>(
                    $"api/uploads/operaciones/{idOperacion}?tipo=cargo&idCargo={idCargo}", ct);

                if (apiFiles == null) return result;

                // Limpiar archivos locales que ya no existen en el VPS (stale cache).
                // Protege contra operaciones eliminadas/recreadas con el mismo ID y
                // contra archivos subidos desde otra PC que ya fueron eliminados remotamente.
                var folder = GetOperacionFolder(idOperacion);
                var vpsNames = new HashSet<string>(apiFiles.Select(f => f.FileName), StringComparer.OrdinalIgnoreCase);
                foreach (var localFile in Directory.GetFiles(folder, $"{idOperacion}_{idCargo}_*_Cargo.*"))
                {
                    if (!vpsNames.Contains(Path.GetFileName(localFile)))
                        try { File.Delete(localFile); } catch { /* ignorar errores de borrado */ }
                }

                foreach (var file in apiFiles)
                {
                    var localPath = Path.Combine(folder, file.FileName);
                    if (!File.Exists(localPath))
                        await DownloadToLocalAsync(file.Url, localPath, ct);

                    result.Add(BuildDto(idOperacion, idCargo, file));
                }
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error al obtener imágenes del cargo {idCargo} desde el VPS", ex,
                    nameof(RemoteCargoImageService), nameof(GetImagesAsync));
            }
            return result;
        }

        public async Task<bool> DeleteImageAsync(int idOperacion, string fileName, CancellationToken ct = default)
        {
            try
            {
                var response = await _http.DeleteAsync(
                    $"api/uploads/operaciones/{idOperacion}/{Uri.EscapeDataString(fileName)}", ct);

                var localPath = Path.Combine(GetOperacionFolder(idOperacion), fileName);
                if (File.Exists(localPath))
                    File.Delete(localPath);

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error al eliminar imagen de cargo {fileName}", ex,
                    nameof(RemoteCargoImageService), nameof(DeleteImageAsync));
                return false;
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private string GetOperacionFolder(int idOperacion)
        {
            var folder = Path.Combine(_basePath, $"Operacion_{idOperacion}");
            Directory.CreateDirectory(folder);
            return folder;
        }

        private CargoImageDto BuildDto(int idOperacion, int idCargo, UploadFileResponseDto remote)
        {
            var localPath = Path.Combine(GetOperacionFolder(idOperacion), remote.FileName);
            var num = ParseImageNumber(remote.FileName, idOperacion, idCargo);
            return new CargoImageDto
            {
                FileName = remote.FileName,
                Url = localPath,
                IdCargo = idCargo,
                ImageNumber = num
            };
        }

        private static int ParseImageNumber(string fileName, int idOp, int idCargo)
        {
            var nameNoExt = Path.GetFileNameWithoutExtension(fileName);
            // Formato: {idOp}_{idCargo}_{N}_Cargo
            var m = Regex.Match(nameNoExt, $@"^{idOp}_{idCargo}_(\d+)_Cargo$", RegexOptions.IgnoreCase);
            return m.Success ? int.Parse(m.Groups[1].Value) : 1;
        }

        private async Task DownloadToLocalAsync(string relativeUrl, string localPath, CancellationToken ct)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);
            var response = await _http.GetAsync(relativeUrl, ct);
            if (!response.IsSuccessStatusCode) return;

            await using var fs = new FileStream(localPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true);
            await response.Content.CopyToAsync(fs, ct);
        }

        private static async Task SaveLocalAsync(string localPath, Stream stream, CancellationToken ct)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);
            await using var fs = new FileStream(localPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true);
            await stream.CopyToAsync(fs, ct);
        }

        private static string GetExtension(string contentType) =>
            contentType.ToLowerInvariant() switch
            {
                "image/jpeg" or "image/jpg" => ".jpg",
                "image/png"  => ".png",
                "image/gif"  => ".gif",
                "image/webp" => ".webp",
                "image/bmp"  => ".bmp",
                _ => ".jpg"
            };
    }
}
