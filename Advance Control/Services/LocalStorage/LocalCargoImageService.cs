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
    /// Implementación del servicio de almacenamiento de imágenes de cargos en el sistema de archivos local.
    /// Las imágenes se guardan en la carpeta Operacion_{idOperacion} dentro de Documentos/Advance Control.
    /// </summary>
    public class LocalCargoImageService : ICargoImageService
    {
        private readonly ILoggingService _logger;
        private readonly string _basePath;

        public LocalCargoImageService(ILoggingService logger)
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
        /// Sube una imagen al almacenamiento local para un cargo específico
        /// </summary>
        public async Task<CargoImageDto?> UploadImageAsync(int idOperacion, int idCargo, Stream imageStream, string contentType, CancellationToken cancellationToken = default)
        {
            try
            {
                await _logger.LogInformationAsync($"Guardando imagen para cargo {idCargo} en operación {idOperacion}", "LocalCargoImageService", "UploadImageAsync");

                var operacionFolder = GetOperacionFolder(idOperacion);

                // Obtener el próximo número de imagen para este cargo
                var existingImages = await GetImagesAsync(idOperacion, idCargo, cancellationToken);
                var nextImageNumber = existingImages.Count > 0 
                    ? existingImages.Max(i => i.ImageNumber) + 1 
                    : 1;

                // Generar nombre del archivo: {idOperacion}_{idCargo}_{numero}_Cargo
                var fileName = $"{idOperacion}_{idCargo}_{nextImageNumber}_Cargo";
                
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

                var cargoImage = new CargoImageDto
                {
                    FileName = fullFileName,
                    Url = fullPath, // Ruta local
                    IdCargo = idCargo,
                    ImageNumber = nextImageNumber
                };

                await _logger.LogInformationAsync($"Imagen guardada exitosamente: {fullFileName}", "LocalCargoImageService", "UploadImageAsync");
                return cargoImage;
            }
            catch (OperationCanceledException)
            {
                await _logger.LogWarningAsync("Operación cancelada al guardar imagen", "LocalCargoImageService", "UploadImageAsync");
                throw;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error al guardar imagen", ex, "LocalCargoImageService", "UploadImageAsync");
                throw;
            }
        }

        /// <summary>
        /// Obtiene todas las imágenes de un cargo desde el almacenamiento local
        /// </summary>
        public async Task<List<CargoImageDto>> GetImagesAsync(int idOperacion, int idCargo, CancellationToken cancellationToken = default)
        {
            var images = new List<CargoImageDto>();

            try
            {
                await _logger.LogInformationAsync($"Obteniendo imágenes para cargo {idCargo} en operación {idOperacion}", "LocalCargoImageService", "GetImagesAsync");

                var operacionFolder = GetOperacionFolder(idOperacion);

                // Prefijo para buscar las imágenes de este cargo: {idOperacion}_{idCargo}_*_Cargo.*
                var pattern = $"{idOperacion}_{idCargo}_*_Cargo.*";

                // Buscar archivos que coincidan con el patrón
                var files = Directory.GetFiles(operacionFolder, pattern);

                foreach (var filePath in files)
                {
                    var fileName = Path.GetFileName(filePath);
                    var imageNumber = ExtractImageNumber(fileName, idOperacion, idCargo);
                    
                    images.Add(new CargoImageDto
                    {
                        FileName = fileName,
                        Url = filePath, // Ruta local
                        IdCargo = idCargo,
                        ImageNumber = imageNumber
                    });
                }

                // Ordenar por número de imagen
                images = images.OrderBy(i => i.ImageNumber).ToList();

                await _logger.LogInformationAsync($"Se obtuvieron {images.Count} imágenes para cargo {idCargo}", "LocalCargoImageService", "GetImagesAsync");
                return images;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error al obtener imágenes", ex, "LocalCargoImageService", "GetImagesAsync");
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
                await _logger.LogInformationAsync($"Eliminando imagen: {fileName}", "LocalCargoImageService", "DeleteImageAsync");

                var operacionFolder = GetOperacionFolder(idOperacion);
                var fullPath = Path.Combine(operacionFolder, fileName);

                if (!File.Exists(fullPath))
                {
                    await _logger.LogWarningAsync($"El archivo no existe: {fileName}", "LocalCargoImageService", "DeleteImageAsync");
                    return false;
                }

                // Verificar cancelación antes de eliminar
                cancellationToken.ThrowIfCancellationRequested();

                // Eliminar archivo de forma asíncrona para no bloquear el hilo
                await Task.Run(() => File.Delete(fullPath), cancellationToken);

                await _logger.LogInformationAsync($"Imagen eliminada exitosamente: {fileName}", "LocalCargoImageService", "DeleteImageAsync");
                return true;
            }
            catch (OperationCanceledException)
            {
                await _logger.LogWarningAsync("Operación cancelada al eliminar imagen", "LocalCargoImageService", "DeleteImageAsync");
                throw;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error al eliminar imagen", ex, "LocalCargoImageService", "DeleteImageAsync");
                throw;
            }
        }

        /// <summary>
        /// Extrae el número de imagen del nombre del archivo
        /// </summary>
        private static int ExtractImageNumber(string fileName, int idOperacion, int idCargo)
        {
            // Formato esperado: {idOperacion}_{idCargo}_{numero}_Cargo.extension
            var prefix = $"{idOperacion}_{idCargo}_";
            if (!fileName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return 0;

            // Obtener la parte entre el prefijo y "_Cargo"
            var suffix = "_Cargo";
            var startIndex = prefix.Length;
            var suffixIndex = fileName.IndexOf(suffix, startIndex, StringComparison.OrdinalIgnoreCase);
            
            if (suffixIndex < 0)
                return 0;

            var numberPart = fileName.Substring(startIndex, suffixIndex - startIndex);

            if (int.TryParse(numberPart, out var number))
            {
                return number;
            }

            return 0;
        }
    }
}
