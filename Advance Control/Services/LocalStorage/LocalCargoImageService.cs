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
    /// Las imágenes se guardan en la carpeta Assets/Cargos.
    /// </summary>
    public class LocalCargoImageService : ICargoImageService
    {
        private readonly ILoggingService _logger;
        private readonly string _basePath;

        public LocalCargoImageService(ILoggingService logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Obtener la ruta base de la aplicación y construir la ruta a Assets/Cargos
            var appDirectory = AppContext.BaseDirectory;
            _basePath = Path.Combine(appDirectory, "Assets", "Cargos");
            
            // Asegurar que el directorio existe
            if (!Directory.Exists(_basePath))
            {
                Directory.CreateDirectory(_basePath);
            }
        }

        /// <summary>
        /// Sube una imagen al almacenamiento local para un cargo específico
        /// </summary>
        public async Task<CargoImageDto?> UploadImageAsync(int idCargo, Stream imageStream, string contentType, CancellationToken cancellationToken = default)
        {
            try
            {
                await _logger.LogInformationAsync($"Guardando imagen para cargo {idCargo}", "LocalCargoImageService", "UploadImageAsync");

                // Asegurar que el directorio existe
                if (!Directory.Exists(_basePath))
                {
                    Directory.CreateDirectory(_basePath);
                }

                // Obtener el próximo número de imagen para este cargo
                var existingImages = await GetImagesAsync(idCargo, cancellationToken);
                var nextImageNumber = existingImages.Count > 0 
                    ? existingImages.Max(i => i.ImageNumber) + 1 
                    : 1;

                // Generar nombre del archivo: Cargo_Id_{idCargo}_{numero}
                var fileName = $"Cargo_Id_{idCargo}_{nextImageNumber}";
                
                // Determinar la extensión según el tipo de contenido
                var extension = ImageContentTypeHelper.GetExtensionFromContentType(contentType);
                var fullFileName = $"{fileName}{extension}";
                var fullPath = Path.Combine(_basePath, fullFileName);

                // Guardar el archivo
                using (var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await imageStream.CopyToAsync(fileStream, cancellationToken);
                }

                var cargoImage = new CargoImageDto
                {
                    FileName = fullFileName,
                    Url = fullPath, // Ruta local en lugar de URL
                    IdCargo = idCargo,
                    ImageNumber = nextImageNumber
                };

                await _logger.LogInformationAsync($"Imagen guardada exitosamente: {fullFileName}", "LocalCargoImageService", "UploadImageAsync");
                return cargoImage;
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
        public async Task<List<CargoImageDto>> GetImagesAsync(int idCargo, CancellationToken cancellationToken = default)
        {
            var images = new List<CargoImageDto>();

            try
            {
                await _logger.LogInformationAsync($"Obteniendo imágenes para cargo {idCargo}", "LocalCargoImageService", "GetImagesAsync");

                // Asegurar que el directorio existe
                if (!Directory.Exists(_basePath))
                {
                    Directory.CreateDirectory(_basePath);
                    return images;
                }

                // Prefijo para buscar las imágenes de este cargo
                var prefix = $"Cargo_Id_{idCargo}_";

                // Buscar archivos que coincidan con el prefijo
                var files = Directory.GetFiles(_basePath, $"{prefix}*");

                foreach (var filePath in files)
                {
                    var fileName = Path.GetFileName(filePath);
                    var imageNumber = ExtractImageNumber(fileName, idCargo);
                    
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
        public async Task<bool> DeleteImageAsync(string fileName, CancellationToken cancellationToken = default)
        {
            try
            {
                await _logger.LogInformationAsync($"Eliminando imagen: {fileName}", "LocalCargoImageService", "DeleteImageAsync");

                var fullPath = Path.Combine(_basePath, fileName);

                if (!File.Exists(fullPath))
                {
                    await _logger.LogWarningAsync($"El archivo no existe: {fileName}", "LocalCargoImageService", "DeleteImageAsync");
                    return false;
                }

                File.Delete(fullPath);

                await _logger.LogInformationAsync($"Imagen eliminada exitosamente: {fileName}", "LocalCargoImageService", "DeleteImageAsync");
                return true;
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
        private static int ExtractImageNumber(string fileName, int idCargo)
        {
            // Formato esperado: Cargo_Id_{idCargo}_{numero}.extension
            var prefix = $"Cargo_Id_{idCargo}_";
            if (!fileName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return 0;

            var remainder = fileName.Substring(prefix.Length);
            
            // Remover la extensión si existe
            var dotIndex = remainder.LastIndexOf('.');
            if (dotIndex > 0)
            {
                remainder = remainder.Substring(0, dotIndex);
            }

            if (int.TryParse(remainder, out var number))
            {
                return number;
            }

            return 0;
        }
    }
}
