using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.Logging;
using Advance_Control.Utilities;

namespace Advance_Control.Services.LocalStorage
{
    /// <summary>
    /// Implementación del servicio de almacenamiento de imágenes de operaciones (Prefacturas, Hojas de Servicio y Órdenes de Compra) en el sistema de archivos local.
    /// Las imágenes se guardan en la carpeta Operacion_{idOperacion} dentro de Documentos/Advance Control.
    /// </summary>
    public class LocalOperacionImageService : IOperacionImageService
    {
        private readonly ILoggingService _logger;
        private readonly string _basePath;

        public LocalOperacionImageService(ILoggingService logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Obtener la ruta base: Documentos/Advance Control
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            _basePath = Path.Combine(documentsPath, "Advance Control");
            
            // Asegurar que el directorio base existe
            if (!Directory.Exists(_basePath))
            {
                Directory.CreateDirectory(_basePath);
            }
        }

        /// <summary>
        /// Obtiene la ruta de la carpeta de la operación: Operacion_{idOperacion}
        /// </summary>
        private string GetOperacionFolder(int idOperacion)
        {
            var folder = Path.Combine(_basePath, $"Operacion_{idOperacion}");
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            return folder;
        }

        /// <summary>
        /// Sube una imagen de prefactura para una operación específica
        /// </summary>
        public async Task<OperacionImageDto?> UploadPrefacturaAsync(int idOperacion, Stream imageStream, string contentType, CancellationToken cancellationToken = default)
        {
            return await UploadImageAsync(idOperacion, imageStream, contentType, "Prefactura", cancellationToken);
        }

        /// <summary>
        /// Sube una imagen de hoja de servicio para una operación específica
        /// </summary>
        public async Task<OperacionImageDto?> UploadHojaServicioAsync(int idOperacion, Stream imageStream, string contentType, CancellationToken cancellationToken = default)
        {
            return await UploadImageAsync(idOperacion, imageStream, contentType, "HojaServicio", cancellationToken);
        }

        /// <summary>
        /// Sube una imagen de orden de compra para una operación específica.
        /// El archivo se guarda con el formato: {idOperacion}_{numeroImagen}_OrdenCompra
        /// </summary>
        public async Task<OperacionImageDto?> UploadOrdenCompraAsync(int idOperacion, Stream imageStream, string contentType, CancellationToken cancellationToken = default)
        {
            return await UploadImageAsync(idOperacion, imageStream, contentType, "OrdenCompra", cancellationToken);
        }

        /// <summary>
        /// Sube una imagen para una operación específica
        /// </summary>
        private async Task<OperacionImageDto?> UploadImageAsync(int idOperacion, Stream imageStream, string contentType, string tipo, CancellationToken cancellationToken = default)
        {
            try
            {
                await _logger.LogInformationAsync($"Guardando imagen de {tipo} para operación {idOperacion}", "LocalOperacionImageService", "UploadImageAsync");

                var operacionFolder = GetOperacionFolder(idOperacion);

                // Obtener el próximo número de imagen para este tipo en esta operación
                List<OperacionImageDto> existingImages;
                if (tipo == "Prefactura")
                    existingImages = await GetPrefacturasAsync(idOperacion, cancellationToken);
                else if (tipo == "HojaServicio")
                    existingImages = await GetHojasServicioAsync(idOperacion, cancellationToken);
                else if (tipo == "OrdenCompra")
                    existingImages = await GetOrdenComprasAsync(idOperacion, cancellationToken);
                else
                    throw new ArgumentException($"Tipo de imagen no válido: {tipo}. Los tipos válidos son 'Prefactura', 'HojaServicio', 'OrdenCompra'.", nameof(tipo));
                
                var nextImageNumber = existingImages.Count > 0 
                    ? existingImages.Max(i => i.ImageNumber) + 1 
                    : 1;

                // Generar nombre del archivo:
                // OrdenCompra: {idOperacion}_{numero}_OrdenCompra
                // Otros tipos: {idOperacion}_{tipo}_{numero}
                var fileName = tipo == "OrdenCompra"
                    ? $"{idOperacion}_{nextImageNumber}_OrdenCompra"
                    : $"{idOperacion}_{tipo}_{nextImageNumber}";
                
                // Determinar la extensión según el tipo de contenido
                var extension = ImageContentTypeHelper.GetExtensionFromContentType(contentType);
                var fullFileName = $"{fileName}{extension}";
                var fullPath = Path.Combine(operacionFolder, fullFileName);

                // Verificar cancelación antes de iniciar operación de archivo
                cancellationToken.ThrowIfCancellationRequested();

                // Guardar el archivo de forma asíncrona
                await using (var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
                {
                    await imageStream.CopyToAsync(fileStream, cancellationToken);
                }

                var operacionImage = new OperacionImageDto
                {
                    FileName = fullFileName,
                    Url = fullPath, // Ruta local
                    IdOperacion = idOperacion,
                    ImageNumber = nextImageNumber,
                    Tipo = tipo
                };

                await _logger.LogInformationAsync($"Imagen de {tipo} guardada exitosamente: {fullFileName}", "LocalOperacionImageService", "UploadImageAsync");
                return operacionImage;
            }
            catch (OperationCanceledException)
            {
                await _logger.LogWarningAsync($"Operación cancelada al guardar imagen de {tipo}", "LocalOperacionImageService", "UploadImageAsync");
                throw;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error al guardar imagen de {tipo}", ex, "LocalOperacionImageService", "UploadImageAsync");
                throw;
            }
        }

        /// <summary>
        /// Obtiene todas las imágenes de prefacturas de una operación
        /// </summary>
        public async Task<List<OperacionImageDto>> GetPrefacturasAsync(int idOperacion, CancellationToken cancellationToken = default)
        {
            return await GetImagesAsync(idOperacion, "Prefactura", cancellationToken);
        }

        /// <summary>
        /// Obtiene todas las imágenes de hojas de servicio de una operación
        /// </summary>
        public async Task<List<OperacionImageDto>> GetHojasServicioAsync(int idOperacion, CancellationToken cancellationToken = default)
        {
            return await GetImagesAsync(idOperacion, "HojaServicio", cancellationToken);
        }

        /// <summary>
        /// Obtiene todas las imágenes de órdenes de compra de una operación
        /// </summary>
        public async Task<List<OperacionImageDto>> GetOrdenComprasAsync(int idOperacion, CancellationToken cancellationToken = default)
        {
            return await GetImagesAsync(idOperacion, "OrdenCompra", cancellationToken);
        }

        /// <summary>
        /// Obtiene todas las imágenes de un tipo específico para una operación desde el almacenamiento local
        /// </summary>
        private async Task<List<OperacionImageDto>> GetImagesAsync(int idOperacion, string tipo, CancellationToken cancellationToken = default)
        {
            var images = new List<OperacionImageDto>();

            try
            {
                await _logger.LogInformationAsync($"Obteniendo imágenes de {tipo} para operación {idOperacion}", "LocalOperacionImageService", "GetImagesAsync");

                var operacionFolder = GetOperacionFolder(idOperacion);

                // OrdenCompra uses format {idOperacion}_{numero}_OrdenCompra.*
                // Other types use format {idOperacion}_{tipo}_*.*
                var pattern = tipo == "OrdenCompra"
                    ? $"{idOperacion}_*_OrdenCompra.*"
                    : $"{idOperacion}_{tipo}_*.*";

                // Buscar archivos que coincidan con el patrón
                var files = Directory.GetFiles(operacionFolder, pattern);

                foreach (var filePath in files)
                {
                    var fileName = Path.GetFileName(filePath);
                    var imageNumber = ExtractImageNumber(fileName, idOperacion, tipo);
                    
                    images.Add(new OperacionImageDto
                    {
                        FileName = fileName,
                        Url = filePath, // Ruta local
                        IdOperacion = idOperacion,
                        ImageNumber = imageNumber,
                        Tipo = tipo
                    });
                }

                // Ordenar por número de imagen
                images = images.OrderBy(i => i.ImageNumber).ToList();

                await _logger.LogInformationAsync($"Se obtuvieron {images.Count} imágenes de {tipo} para operación {idOperacion}", "LocalOperacionImageService", "GetImagesAsync");
                return images;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error al obtener imágenes de {tipo}", ex, "LocalOperacionImageService", "GetImagesAsync");
                return images;
            }
        }

        /// <summary>
        /// Elimina una imagen del almacenamiento local
        /// </summary>
        public async Task<bool> DeleteImageAsync(int idOperacion, string fileName, CancellationToken cancellationToken = default)
        {
            try
            {
                await _logger.LogInformationAsync($"Eliminando imagen: {fileName}", "LocalOperacionImageService", "DeleteImageAsync");

                var operacionFolder = GetOperacionFolder(idOperacion);
                var fullPath = Path.Combine(operacionFolder, fileName);

                if (!File.Exists(fullPath))
                {
                    await _logger.LogWarningAsync($"El archivo no existe: {fileName}", "LocalOperacionImageService", "DeleteImageAsync");
                    return false;
                }

                // Verificar cancelación antes de eliminar
                cancellationToken.ThrowIfCancellationRequested();

                // Eliminar archivo de forma asíncrona para no bloquear el hilo
                await Task.Run(() => File.Delete(fullPath), cancellationToken);

                await _logger.LogInformationAsync($"Imagen eliminada exitosamente: {fileName}", "LocalOperacionImageService", "DeleteImageAsync");
                return true;
            }
            catch (OperationCanceledException)
            {
                await _logger.LogWarningAsync("Operación cancelada al eliminar imagen", "LocalOperacionImageService", "DeleteImageAsync");
                throw;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error al eliminar imagen", ex, "LocalOperacionImageService", "DeleteImageAsync");
                throw;
            }
        }

        /// <summary>
        /// Extrae el número de imagen del nombre del archivo
        /// </summary>
        private static int ExtractImageNumber(string fileName, int idOperacion, string tipo)
        {
            if (tipo == "OrdenCompra")
            {
                // Formato OrdenCompra: {idOperacion}_{numero}_OrdenCompra.extension
                var idPrefix = $"{idOperacion}_";
                const string suffix = "_OrdenCompra";

                if (!fileName.StartsWith(idPrefix, StringComparison.OrdinalIgnoreCase))
                    return 0;

                var afterId = fileName.Substring(idPrefix.Length);
                var suffixIndex = afterId.IndexOf(suffix, StringComparison.OrdinalIgnoreCase);

                if (suffixIndex < 0)
                    return 0;

                var numberPart = afterId.Substring(0, suffixIndex);

                if (int.TryParse(numberPart, out var ordenNum))
                    return ordenNum;

                return 0;
            }

            // Formato estándar: {idOperacion}_{tipo}_{numero}.extension
            var prefix = $"{idOperacion}_{tipo}_";
            
            if (!fileName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return 0;

            // Obtener la parte después del prefijo (número + extensión)
            var startIndex = prefix.Length;
            var dotIndex = fileName.IndexOf('.', startIndex);
            
            if (dotIndex < 0)
                dotIndex = fileName.Length;

            var numberPart = fileName.Substring(startIndex, dotIndex - startIndex);

            if (int.TryParse(numberPart, out var number))
            {
                return number;
            }

            return 0;
        }
    }
}
