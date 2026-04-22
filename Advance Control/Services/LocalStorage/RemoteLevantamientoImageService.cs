using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.Logging;

namespace Advance_Control.Services.LocalStorage
{
    /// <summary>
    /// Implementación remota de <see cref="ILevantamientoImageService"/>.
    /// Sube imágenes al VPS y las cachea localmente en la misma ruta que
    /// <see cref="LocalLevantamientoImageService"/> para que los reportes PDF sigan funcionando.
    /// </summary>
    public sealed class RemoteLevantamientoImageService : ILevantamientoImageService
    {
        private readonly HttpClient _http;
        private readonly ILoggingService _logger;
        private readonly string _basePath;

        public RemoteLevantamientoImageService(HttpClient http, ILoggingService logger)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _basePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "Advance Control", "Levantamientos");
            Directory.CreateDirectory(_basePath);
        }

        public string GetLevantamientoFolder(int idLevantamiento) =>
            Path.Combine(_basePath, $"Levantamiento{idLevantamiento}");

        public async Task<LevantamientoImageResult> SaveImageAsync(
            int idLevantamiento, string infoNodo, Stream imageStream,
            string contentType, CancellationToken ct = default)
        {
            if (idLevantamiento <= 0)
                throw new ArgumentException("IdLevantamiento debe ser mayor que 0.", nameof(idLevantamiento));
            if (string.IsNullOrWhiteSpace(infoNodo))
                throw new ArgumentException("InfoNodo no puede estar vacío.", nameof(infoNodo));
            ArgumentNullException.ThrowIfNull(imageStream);

            try
            {
                using var ms = new MemoryStream();
                await imageStream.CopyToAsync(ms, ct);

                var ext = GetExtension(contentType);
                ms.Position = 0;
                using var form = new MultipartFormDataContent();
                var sc = new StreamContent(ms);
                sc.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
                form.Add(sc, "file", $"upload{ext}");

                var url = $"api/uploads/levantamientos/{idLevantamiento}?infoNodo={Uri.EscapeDataString(infoNodo)}";
                var response = await _http.PostAsync(url, form, ct);

                if (!response.IsSuccessStatusCode)
                {
                    await _logger.LogWarningAsync(
                        $"API devolvió {(int)response.StatusCode} al subir imagen de levantamiento {idLevantamiento}",
                        nameof(RemoteLevantamientoImageService), nameof(SaveImageAsync));
                    throw new InvalidOperationException($"Error al subir imagen al VPS: {response.StatusCode}");
                }

                var dto = await response.Content.ReadFromJsonAsync<UploadFileResponseDto>(cancellationToken: ct);
                if (dto == null) throw new InvalidOperationException("Respuesta vacía del servidor al subir imagen.");

                // Guardar caché local
                ms.Position = 0;
                var folder = GetLevantamientoFolder(idLevantamiento);
                Directory.CreateDirectory(folder);
                var localPath = Path.Combine(folder, dto.FileName);
                await using (var fs = new FileStream(localPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
                {
                    await ms.CopyToAsync(fs, ct);
                }

                var num = ExtractImageNumber(dto.FileName, SanitizeSegment(infoNodo), idLevantamiento);
                return new LevantamientoImageResult
                {
                    FileName = dto.FileName,
                    FilePath = localPath,
                    Title = $"{infoNodo} - Levantamiento {idLevantamiento} - Imagen {num}",
                    ImageNumber = num
                };
            }
            catch (Exception ex) when (ex is not ArgumentException and not InvalidOperationException)
            {
                await _logger.LogErrorAsync($"Error al guardar imagen de levantamiento {idLevantamiento}", ex,
                    nameof(RemoteLevantamientoImageService), nameof(SaveImageAsync));
                throw;
            }
        }

        public async Task<List<LevantamientoImageResult>> GetImagesAsync(
            int idLevantamiento, string infoNodo, CancellationToken ct = default)
        {
            var result = new List<LevantamientoImageResult>();
            try
            {
                var apiFiles = await _http.GetFromJsonAsync<List<UploadFileResponseDto>>(
                    $"api/uploads/levantamientos/{idLevantamiento}", ct);

                if (apiFiles == null) return result;

                var safeNodo = SanitizeSegment(infoNodo);
                var folder = GetLevantamientoFolder(idLevantamiento);
                Directory.CreateDirectory(folder);

                // Fallback defensivo: si el VPS no devuelve archivos pero hay cache local del nodo,
                // mostrarlos. Protege contra storage del VPS vacío o respuestas transitoriamente vacías.
                if (apiFiles.Count == 0)
                {
                    foreach (var localFile in Directory.GetFiles(folder, $"{safeNodo}_*").OrderBy(f => f))
                    {
                        var fileName = Path.GetFileName(localFile);
                        var num = ExtractImageNumber(fileName, safeNodo, idLevantamiento);
                        result.Add(new LevantamientoImageResult
                        {
                            FileName = fileName,
                            FilePath = localFile,
                            Title = $"{infoNodo} - Levantamiento {idLevantamiento} - Imagen {num}",
                            ImageNumber = num
                        });
                    }
                    return result;
                }

                // Filtrar solo los que corresponden al nodo solicitado
                var relevant = apiFiles.Where(f =>
                    f.FileName.StartsWith(safeNodo + "_", StringComparison.OrdinalIgnoreCase)).ToList();

                foreach (var file in relevant.OrderBy(f => f.FileName))
                {
                    var localPath = Path.Combine(folder, file.FileName);
                    if (!File.Exists(localPath))
                        await DownloadToLocalAsync(file.Url, localPath, ct);

                    var num = ExtractImageNumber(file.FileName, safeNodo, idLevantamiento);
                    result.Add(new LevantamientoImageResult
                    {
                        FileName = file.FileName,
                        FilePath = localPath,
                        Title = $"{infoNodo} - Levantamiento {idLevantamiento} - Imagen {num}",
                        ImageNumber = num
                    });
                }
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync(
                    $"Error al obtener imágenes de levantamiento {idLevantamiento} desde el VPS", ex,
                    nameof(RemoteLevantamientoImageService), nameof(GetImagesAsync));
            }
            return result;
        }

        public async Task DeleteImageAsync(string filePath, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return;

            var fileName = Path.GetFileName(filePath);
            var idLev = ParseIdLevFromPath(filePath);

            if (idLev > 0)
            {
                try
                {
                    await _http.DeleteAsync(
                        $"api/uploads/levantamientos/{idLev}/{Uri.EscapeDataString(fileName)}", ct);
                }
                catch (Exception ex)
                {
                    await _logger.LogWarningAsync($"Error al eliminar imagen del VPS: {fileName} - {ex.Message}",
                        nameof(RemoteLevantamientoImageService), nameof(DeleteImageAsync));
                }
            }

            if (File.Exists(filePath))
                File.Delete(filePath);
        }

        public async Task DeleteImagesAsync(IEnumerable<string> filePaths, CancellationToken ct = default)
        {
            if (filePaths is null) return;
            foreach (var path in filePaths.Where(p => !string.IsNullOrWhiteSpace(p)))
                await DeleteImageAsync(path, ct);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private async Task DownloadToLocalAsync(string relativeUrl, string localPath, CancellationToken ct)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);
            var response = await _http.GetAsync(relativeUrl, ct);
            if (!response.IsSuccessStatusCode) return;
            await using var fs = new FileStream(localPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true);
            await response.Content.CopyToAsync(fs, ct);
        }

        private static int ExtractImageNumber(string fileName, string safeNodo, int idLev)
        {
            var prefix = $"{safeNodo}_{idLev}_";
            if (!fileName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return 0;
            var rest = Path.GetFileNameWithoutExtension(fileName[prefix.Length..]);
            return int.TryParse(rest, out var n) ? n : 0;
        }

        private static int ParseIdLevFromPath(string filePath)
        {
            // Folder name: "Levantamiento{id}"
            var dir = Path.GetDirectoryName(filePath);
            if (dir == null) return 0;
            var folderName = Path.GetFileName(dir);
            const string prefix = "Levantamiento";
            if (folderName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(folderName[prefix.Length..], out var id))
                return id;
            return 0;
        }

        private static string SanitizeSegment(string value)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var s = new string(value.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
            return string.IsNullOrWhiteSpace(s) ? "nodo" : s;
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
