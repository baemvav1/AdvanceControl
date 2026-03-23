using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Services.Logging;
using Advance_Control.Utilities;

namespace Advance_Control.Services.LocalStorage
{
    public sealed class LocalLevantamientoImageService : ILevantamientoImageService
    {
        private readonly ILoggingService _logger;
        private readonly string _basePath;

        public LocalLevantamientoImageService(ILoggingService logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _basePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "Advance Control", "Levantamientos");
            Directory.CreateDirectory(_basePath);
        }

        public string GetLevantamientoFolder(int idLevantamiento)
        {
            return Path.Combine(_basePath, $"Levantamiento{idLevantamiento}");
        }

        public async Task<LevantamientoImageResult> SaveImageAsync(
            int idLevantamiento, string infoNodo, Stream imageStream,
            string contentType, CancellationToken cancellationToken = default)
        {
            if (idLevantamiento <= 0)
                throw new ArgumentException("IdLevantamiento debe ser mayor que 0.", nameof(idLevantamiento));
            if (string.IsNullOrWhiteSpace(infoNodo))
                throw new ArgumentException("InfoNodo no puede estar vacio.", nameof(infoNodo));
            ArgumentNullException.ThrowIfNull(imageStream);

            var folder = GetLevantamientoFolder(idLevantamiento);
            Directory.CreateDirectory(folder);

            var safeNodo = SanitizePathSegment(infoNodo);
            var extension = ImageContentTypeHelper.GetExtensionFromContentType(contentType);
            var imageNumber = GetNextImageNumber(folder, safeNodo, idLevantamiento);
            var fileName = $"{safeNodo}_{idLevantamiento}_{imageNumber}{extension}";
            var fullPath = Path.Combine(folder, fileName);
            var title = $"{infoNodo} - Levantamiento {idLevantamiento} - Imagen {imageNumber}";

            cancellationToken.ThrowIfCancellationRequested();
            await using (var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
            {
                await imageStream.CopyToAsync(fileStream, cancellationToken);
            }

            await _logger.LogInformationAsync(
                $"Imagen de levantamiento guardada: '{fileName}'",
                nameof(LocalLevantamientoImageService), nameof(SaveImageAsync));

            return new LevantamientoImageResult
            {
                FileName = fileName,
                FilePath = fullPath,
                Title = title,
                ImageNumber = imageNumber
            };
        }

        public Task<List<LevantamientoImageResult>> GetImagesAsync(
            int idLevantamiento, string infoNodo, CancellationToken cancellationToken = default)
        {
            var folder = GetLevantamientoFolder(idLevantamiento);
            var results = new List<LevantamientoImageResult>();

            if (!Directory.Exists(folder))
                return Task.FromResult(results);

            var safeNodo = SanitizePathSegment(infoNodo);
            var pattern = $"{safeNodo}_{idLevantamiento}_*.*";

            foreach (var file in Directory.GetFiles(folder, pattern).OrderBy(f => f))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var fileName = Path.GetFileName(file);
                var num = ExtractImageNumber(fileName, safeNodo, idLevantamiento);

                results.Add(new LevantamientoImageResult
                {
                    FileName = fileName,
                    FilePath = file,
                    Title = $"{infoNodo} - Levantamiento {idLevantamiento} - Imagen {num}",
                    ImageNumber = num
                });
            }

            return Task.FromResult(results);
        }

        public async Task DeleteImageAsync(string filePath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return;

            cancellationToken.ThrowIfCancellationRequested();
            if (!File.Exists(filePath))
            {
                await _logger.LogWarningAsync(
                    $"La imagen no existe: '{filePath}'",
                    nameof(LocalLevantamientoImageService), nameof(DeleteImageAsync));
                return;
            }

            await Task.Run(() => File.Delete(filePath), cancellationToken);
            await _logger.LogInformationAsync(
                $"Imagen eliminada: '{filePath}'",
                nameof(LocalLevantamientoImageService), nameof(DeleteImageAsync));
        }

        public async Task DeleteImagesAsync(IEnumerable<string> filePaths, CancellationToken cancellationToken = default)
        {
            if (filePaths is null) return;

            foreach (var filePath in filePaths.Where(p => !string.IsNullOrWhiteSpace(p)))
            {
                await DeleteImageAsync(filePath, cancellationToken);
            }
        }

        private static int GetNextImageNumber(string folder, string safeNodo, int idLevantamiento)
        {
            var pattern = $"{safeNodo}_{idLevantamiento}_*.*";
            var existing = Directory.GetFiles(folder, pattern);
            int maxNum = 0;

            foreach (var file in existing)
            {
                var num = ExtractImageNumber(Path.GetFileName(file), safeNodo, idLevantamiento);
                if (num > maxNum) maxNum = num;
            }

            return maxNum + 1;
        }

        private static int ExtractImageNumber(string fileName, string safeNodo, int idLevantamiento)
        {
            var prefix = $"{safeNodo}_{idLevantamiento}_";
            if (!fileName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return 0;

            var rest = fileName[prefix.Length..];
            var numStr = new string(rest.TakeWhile(char.IsDigit).ToArray());
            return int.TryParse(numStr, out var num) ? num : 0;
        }

        private static string SanitizePathSegment(string value)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = new string(value.Select(ch => invalidChars.Contains(ch) ? '_' : ch).ToArray());
            return string.IsNullOrWhiteSpace(sanitized) ? "nodo" : sanitized;
        }
    }
}
