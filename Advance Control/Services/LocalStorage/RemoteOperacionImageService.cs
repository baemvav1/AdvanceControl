using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
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
    /// Implementación remota de <see cref="IOperacionImageService"/>.
    /// Sube y descarga imágenes hacia el VPS, manteniendo una caché local
    /// en la misma ruta que <see cref="LocalOperacionImageService"/> para que
    /// QuoteService y LevantamientoReportService sigan recibiendo rutas locales.
    /// </summary>
    public sealed class RemoteOperacionImageService : IOperacionImageService
    {
        private readonly HttpClient _http;
        private readonly ILoggingService _logger;
        private readonly string _basePath;

        public RemoteOperacionImageService(HttpClient http, ILoggingService logger)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _basePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "Advance Control");
            Directory.CreateDirectory(_basePath);
        }

        // ── Upload ────────────────────────────────────────────────────────────

        public Task<OperacionImageDto?> UploadPrefacturaAsync(int idOperacion, Stream imageStream, string contentType, CancellationToken ct = default)
            => UploadAsync(idOperacion, imageStream, contentType, "prefactura", ct);

        public Task<OperacionImageDto?> UploadHojaServicioAsync(int idOperacion, Stream imageStream, string contentType, CancellationToken ct = default)
            => UploadAsync(idOperacion, imageStream, contentType, "hoja_servicio", ct);

        public Task<OperacionImageDto?> UploadOrdenCompraAsync(int idOperacion, Stream imageStream, string contentType, CancellationToken ct = default)
            => UploadAsync(idOperacion, imageStream, contentType, "orden_compra", ct);

        public Task<OperacionImageDto?> UploadLevantamientoAsync(int idOperacion, Stream imageStream, string contentType, CancellationToken ct = default)
            => UploadAsync(idOperacion, imageStream, contentType, "levantamiento", ct);

        public async Task<OperacionImageDto?> UploadFacturaAsync(int idOperacion, Stream pdfStream, CancellationToken ct = default)
        {
            try
            {
                // Bufferear para poder guardar localmente después
                using var ms = new MemoryStream();
                await pdfStream.CopyToAsync(ms, ct);

                ms.Position = 0;
                var dto = await PostFileAsync(idOperacion, ms, "application/pdf", "factura", 0, ct);
                if (dto == null) return null;

                // Guardar caché local
                ms.Position = 0;
                var localPath = Path.Combine(GetOperacionFolder(idOperacion), dto.FileName);
                await SaveLocalAsync(localPath, ms, ct);

                return BuildDto(idOperacion, dto);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error al subir factura al VPS", ex,
                    nameof(RemoteOperacionImageService), nameof(UploadFacturaAsync));
                return null;
            }
        }

        // ── Get ───────────────────────────────────────────────────────────────

        public Task<List<OperacionImageDto>> GetPrefacturasAsync(int idOperacion, long? mensajeReferenciaId = null, CancellationToken ct = default)
            => GetListAsync(idOperacion, "prefactura", mensajeReferenciaId, ct);

        public Task<List<OperacionImageDto>> GetHojasServicioAsync(int idOperacion, long? mensajeReferenciaId = null, CancellationToken ct = default)
            => GetListAsync(idOperacion, "hoja_servicio", mensajeReferenciaId, ct);

        public Task<List<OperacionImageDto>> GetOrdenComprasAsync(int idOperacion, long? mensajeReferenciaId = null, CancellationToken ct = default)
            => GetListAsync(idOperacion, "orden_compra", mensajeReferenciaId, ct);

        public Task<List<OperacionImageDto>> GetLevantamientosAsync(int idOperacion, long? mensajeReferenciaId = null, CancellationToken ct = default)
            => GetListAsync(idOperacion, "levantamiento", mensajeReferenciaId, ct);

        public async Task<OperacionImageDto?> GetFacturaAsync(int idOperacion, long? mensajeReferenciaId = null, CancellationToken ct = default)
        {
            var list = await GetListAsync(idOperacion, "factura", mensajeReferenciaId, ct);
            return list.FirstOrDefault();
        }

        public async Task<bool> HasFacturaAsync(int idOperacion, long? mensajeReferenciaId = null, CancellationToken ct = default)
        {
            var dto = await GetFacturaAsync(idOperacion, mensajeReferenciaId, ct);
            return dto != null;
        }

        // ── Delete ────────────────────────────────────────────────────────────

        public async Task<bool> DeleteImageAsync(int idOperacion, string fileName, CancellationToken ct = default)
        {
            try
            {
                var response = await _http.DeleteAsync(
                    $"api/uploads/operaciones/{idOperacion}/{Uri.EscapeDataString(fileName)}", ct);

                // Eliminar caché local si existe
                var localPath = Path.Combine(GetOperacionFolder(idOperacion), fileName);
                if (File.Exists(localPath))
                    File.Delete(localPath);

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error al eliminar imagen {fileName}", ex,
                    nameof(RemoteOperacionImageService), nameof(DeleteImageAsync));
                return false;
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private async Task<OperacionImageDto?> UploadAsync(int idOperacion, Stream imageStream,
            string contentType, string tipo, CancellationToken ct)
        {
            try
            {
                using var ms = new MemoryStream();
                await imageStream.CopyToAsync(ms, ct);

                ms.Position = 0;
                var dto = await PostFileAsync(idOperacion, ms, contentType, tipo, 0, ct);
                if (dto == null) return null;

                ms.Position = 0;
                var localPath = Path.Combine(GetOperacionFolder(idOperacion), dto.FileName);
                await SaveLocalAsync(localPath, ms, ct);

                return BuildDto(idOperacion, dto);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error al subir imagen {tipo} al VPS", ex,
                    nameof(RemoteOperacionImageService), nameof(UploadAsync));
                return null;
            }
        }

        private async Task<List<OperacionImageDto>> GetListAsync(int idOperacion, string tipo, long? mensajeReferenciaId, CancellationToken ct)
        {
            var result = new List<OperacionImageDto>();
            try
            {
                var url = mensajeReferenciaId.HasValue
                    ? $"api/uploads/operaciones/{idOperacion}?tipo={tipo}&mensajeReferenciaId={mensajeReferenciaId.Value}"
                    : $"api/uploads/operaciones/{idOperacion}?tipo={tipo}";

                using var response = await _http.GetAsync(url, ct);
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(ct);
                    if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
                        throw new UnauthorizedAccessException("No tienes acceso para ver los archivos de esta operación.");

                    throw new InvalidOperationException($"No se pudieron obtener los archivos '{tipo}' porque el servidor respondió con el estado {(int)response.StatusCode} ({response.StatusCode}). {errorContent}");
                }

                var apiFiles = await response.Content.ReadFromJsonAsync<List<UploadFileResponseDto>>(cancellationToken: ct);

                if (apiFiles == null) return result;

                var folder = GetOperacionFolder(idOperacion);
                var localPattern = tipo switch
                {
                    "prefactura"     => $"{idOperacion}_Prefactura_*.*",
                    "hoja_servicio"  => $"{idOperacion}_HojaServicio_*.*",
                    "orden_compra"   => $"{idOperacion}_*_OrdenCompra.*",
                    "levantamiento"  => $"{idOperacion}_Levantamiento_*.*",
                    "factura"        => $"{idOperacion}_Factura.*",
                    _                => null
                };

                // Fallback defensivo: si el VPS regresa lista vacía pero existen archivos en
                // cache local que matchean el patrón, mostrarlos. Esto protege contra el caso de
                // storage del VPS vacío o problemas transitorios donde el listado vuelve vacío.
                if (apiFiles.Count == 0 && localPattern != null)
                {
                    foreach (var localFile in Directory.GetFiles(folder, localPattern).OrderBy(f => f))
                    {
                        var fileName = Path.GetFileName(localFile);
                        result.Add(BuildDto(idOperacion, new UploadFileResponseDto { FileName = fileName, Url = localFile }));
                    }
                    return result;
                }

                // Solo limpiar cache local cuando el VPS confirma listado con datos.
                var vpsNames = new HashSet<string>(apiFiles.Select(f => f.FileName), StringComparer.OrdinalIgnoreCase);
                if (localPattern != null)
                {
                    foreach (var localFile in Directory.GetFiles(folder, localPattern))
                    {
                        if (!vpsNames.Contains(Path.GetFileName(localFile)))
                            try { File.Delete(localFile); } catch { /* ignorar errores de borrado */ }
                    }
                }

                foreach (var file in apiFiles)
                {
                    var localPath = Path.Combine(folder, file.FileName);
                    if (!File.Exists(localPath))
                        await DownloadToLocalAsync(file.Url, localPath, ct);

                    result.Add(BuildDto(idOperacion, file));
                }

                return result;
            }
            catch (UnauthorizedAccessException ex)
            {
                await _logger.LogErrorAsync($"Acceso denegado al obtener lista {tipo} del VPS", ex,
                    nameof(RemoteOperacionImageService), nameof(GetListAsync));
                throw;
            }
            catch (InvalidOperationException ex)
            {
                await _logger.LogErrorAsync($"No se pudo obtener lista {tipo} del VPS", ex,
                    nameof(RemoteOperacionImageService), nameof(GetListAsync));
                throw;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error al obtener lista {tipo} del VPS", ex,
                    nameof(RemoteOperacionImageService), nameof(GetListAsync));
                throw;
            }
        }

        private async Task<UploadFileResponseDto?> PostFileAsync(int idOperacion, Stream stream,
            string contentType, string tipo, int idCargo, CancellationToken ct)
        {
            var ext = contentType.ToLowerInvariant() switch
            {
                "image/jpeg" or "image/jpg" => ".jpg",
                "image/png"  => ".png",
                "image/gif"  => ".gif",
                "image/webp" => ".webp",
                "image/bmp"  => ".bmp",
                "application/pdf" => ".pdf",
                _ => ".jpg"
            };

            using var content = new MultipartFormDataContent();
            var sc = new StreamContent(stream);
            sc.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
            content.Add(sc, "file", $"upload{ext}");

            var query = idCargo > 0
                ? $"api/uploads/operaciones/{idOperacion}?tipo={tipo}&idCargo={idCargo}"
                : $"api/uploads/operaciones/{idOperacion}?tipo={tipo}";

            var response = await _http.PostAsync(query, content, ct);
            if (!response.IsSuccessStatusCode)
            {
                await _logger.LogWarningAsync(
                    $"API devolvió {(int)response.StatusCode} al subir {tipo} para operación {idOperacion}",
                    nameof(RemoteOperacionImageService), nameof(PostFileAsync));
                return null;
            }

            return await response.Content.ReadFromJsonAsync<UploadFileResponseDto>(cancellationToken: ct);
        }

        private async Task DownloadToLocalAsync(string relativeUrl, string localPath, CancellationToken ct)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);
            var response = await _http.GetAsync(relativeUrl, ct);
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"No se pudo descargar el archivo de operación '{Path.GetFileName(localPath)}' desde el servidor.");

            await using var fs = new FileStream(localPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true);
            await response.Content.CopyToAsync(fs, ct);
        }

        private static async Task SaveLocalAsync(string localPath, Stream stream, CancellationToken ct)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);
            await using var fs = new FileStream(localPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true);
            await stream.CopyToAsync(fs, ct);
        }

        private string GetOperacionFolder(int idOperacion)
        {
            var folder = Path.Combine(_basePath, $"Operacion_{idOperacion}");
            Directory.CreateDirectory(folder);
            return folder;
        }

        private OperacionImageDto BuildDto(int idOperacion, UploadFileResponseDto remote)
        {
            var localPath = Path.Combine(GetOperacionFolder(idOperacion), remote.FileName);
            var (tipo, num) = ParseFileName(remote.FileName, idOperacion);
            return new OperacionImageDto
            {
                FileName = remote.FileName,
                Url = localPath,
                IdOperacion = idOperacion,
                ImageNumber = num,
                Tipo = tipo
            };
        }

        private static (string Tipo, int Num) ParseFileName(string fileName, int idOp)
        {
            var nameNoExt = Path.GetFileNameWithoutExtension(fileName);

            // Prefactura: {idOp}_Prefactura_{N}
            var m = Regex.Match(nameNoExt, $@"^{idOp}_Prefactura_(\d+)$", RegexOptions.IgnoreCase);
            if (m.Success) return ("Prefactura", int.Parse(m.Groups[1].Value));

            m = Regex.Match(nameNoExt, $@"^{idOp}_HojaServicio_(\d+)$", RegexOptions.IgnoreCase);
            if (m.Success) return ("HojaServicio", int.Parse(m.Groups[1].Value));

            // Levantamiento: {idOp}_Levantamiento_{N}
            m = Regex.Match(nameNoExt, $@"^{idOp}_Levantamiento_(\d+)$", RegexOptions.IgnoreCase);
            if (m.Success) return ("Levantamiento", int.Parse(m.Groups[1].Value));

            // OrdenCompra: {idOp}_{N}_OrdenCompra
            m = Regex.Match(nameNoExt, $@"^{idOp}_(\d+)_OrdenCompra$", RegexOptions.IgnoreCase);
            if (m.Success) return ("OrdenCompra", int.Parse(m.Groups[1].Value));

            // Cargo: {idOp}_{idCargo}_{N}_Cargo
            m = Regex.Match(nameNoExt, $@"^{idOp}_\d+_(\d+)_Cargo$", RegexOptions.IgnoreCase);
            if (m.Success) return ("Cargo", int.Parse(m.Groups[1].Value));

            if (nameNoExt.EndsWith("_Factura", StringComparison.OrdinalIgnoreCase))
                return ("Factura", 1);

            return ("Desconocido", 1);
        }
    }
}
