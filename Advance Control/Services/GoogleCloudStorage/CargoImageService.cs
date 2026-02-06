using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.GoogleMaps;
using Advance_Control.Services.Logging;

namespace Advance_Control.Services.GoogleCloudStorage
{
    /// <summary>
    /// Implementación del servicio de almacenamiento de imágenes de cargos en Google Cloud Storage.
    /// Usa la API JSON de Google Cloud Storage para operaciones directas cliente-GCS.
    /// </summary>
    public class CargoImageService : ICargoImageService
    {
        private readonly HttpClient _http;
        private readonly IGoogleMapsConfigService _googleMapsConfigService;
        private readonly ILoggingService _logger;
        
        // Google Cloud Storage bucket name
        private const string BucketName = "advance-control-cargo-images";
        private const string GcsApiBaseUrl = "https://storage.googleapis.com/storage/v1";
        private const string GcsUploadUrl = "https://storage.googleapis.com/upload/storage/v1";
        private const string GcsPublicUrl = "https://storage.googleapis.com";

        public CargoImageService(
            HttpClient http,
            IGoogleMapsConfigService googleMapsConfigService,
            ILoggingService logger)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _googleMapsConfigService = googleMapsConfigService ?? throw new ArgumentNullException(nameof(googleMapsConfigService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Sube una imagen a Google Cloud Storage para un cargo específico
        /// </summary>
        public async Task<CargoImageDto?> UploadImageAsync(int idCargo, Stream imageStream, string contentType, CancellationToken cancellationToken = default)
        {
            try
            {
                await _logger.LogInformationAsync($"Subiendo imagen para cargo {idCargo}", "CargoImageService", "UploadImageAsync");

                // Obtener la API key
                var apiKey = await _googleMapsConfigService.GetApiKeyAsync(cancellationToken);
                if (string.IsNullOrEmpty(apiKey))
                {
                    await _logger.LogErrorAsync("No se pudo obtener la API key de Google", null, "CargoImageService", "UploadImageAsync");
                    return null;
                }

                // Obtener el próximo número de imagen para este cargo
                var existingImages = await GetImagesAsync(idCargo, cancellationToken);
                var nextImageNumber = existingImages.Count > 0 
                    ? existingImages.Max(i => i.ImageNumber) + 1 
                    : 1;

                // Generar nombre del archivo: Cargo_Id_{idCargo}_{numero}
                var fileName = $"Cargo_Id_{idCargo}_{nextImageNumber}";
                
                // Determinar la extensión según el tipo de contenido
                var extension = GetExtensionFromContentType(contentType);
                var fullFileName = $"{fileName}{extension}";

                // URL para subir a GCS usando la API JSON
                var uploadUrl = $"{GcsUploadUrl}/b/{BucketName}/o?uploadType=media&name={Uri.EscapeDataString(fullFileName)}&key={apiKey}";

                // Leer el stream en un byte array
                byte[] imageBytes;
                using (var memoryStream = new MemoryStream())
                {
                    await imageStream.CopyToAsync(memoryStream, cancellationToken);
                    imageBytes = memoryStream.ToArray();
                }

                // Crear el contenido de la solicitud
                using var content = new ByteArrayContent(imageBytes);
                content.Headers.ContentType = new MediaTypeHeaderValue(contentType);

                // Realizar la solicitud POST para subir la imagen
                var response = await _http.PostAsync(uploadUrl, content, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    await _logger.LogErrorAsync(
                        $"Error al subir imagen a GCS. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "CargoImageService",
                        "UploadImageAsync");
                    return null;
                }

                // Parsear la respuesta para obtener información del objeto
                var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
                var responseObj = JsonSerializer.Deserialize<JsonElement>(responseText);

                // Construir la URL pública de la imagen
                var publicUrl = $"{GcsPublicUrl}/{BucketName}/{fullFileName}";

                var cargoImage = new CargoImageDto
                {
                    FileName = fullFileName,
                    Url = publicUrl,
                    IdCargo = idCargo,
                    ImageNumber = nextImageNumber
                };

                await _logger.LogInformationAsync($"Imagen subida exitosamente: {fullFileName}", "CargoImageService", "UploadImageAsync");
                return cargoImage;
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al subir imagen", ex, "CargoImageService", "UploadImageAsync");
                throw new InvalidOperationException("Error de comunicación con Google Cloud Storage al subir imagen", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al subir imagen", ex, "CargoImageService", "UploadImageAsync");
                throw;
            }
        }

        /// <summary>
        /// Obtiene todas las imágenes de un cargo desde Google Cloud Storage
        /// </summary>
        public async Task<List<CargoImageDto>> GetImagesAsync(int idCargo, CancellationToken cancellationToken = default)
        {
            var images = new List<CargoImageDto>();

            try
            {
                await _logger.LogInformationAsync($"Obteniendo imágenes para cargo {idCargo}", "CargoImageService", "GetImagesAsync");

                // Obtener la API key
                var apiKey = await _googleMapsConfigService.GetApiKeyAsync(cancellationToken);
                if (string.IsNullOrEmpty(apiKey))
                {
                    await _logger.LogErrorAsync("No se pudo obtener la API key de Google", null, "CargoImageService", "GetImagesAsync");
                    return images;
                }

                // Prefijo para buscar las imágenes de este cargo
                var prefix = $"Cargo_Id_{idCargo}_";

                // URL para listar objetos en el bucket con el prefijo
                var listUrl = $"{GcsApiBaseUrl}/b/{BucketName}/o?prefix={Uri.EscapeDataString(prefix)}&key={apiKey}";

                var response = await _http.GetAsync(listUrl, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    await _logger.LogErrorAsync(
                        $"Error al listar imágenes de GCS. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "CargoImageService",
                        "GetImagesAsync");
                    return images;
                }

                var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
                var responseObj = JsonSerializer.Deserialize<JsonElement>(responseText);

                // Procesar la lista de objetos
                if (responseObj.TryGetProperty("items", out var items))
                {
                    foreach (var item in items.EnumerateArray())
                    {
                        if (item.TryGetProperty("name", out var nameProp))
                        {
                            var fileName = nameProp.GetString();
                            if (!string.IsNullOrEmpty(fileName))
                            {
                                // Extraer el número de imagen del nombre del archivo
                                var imageNumber = ExtractImageNumber(fileName, idCargo);
                                
                                images.Add(new CargoImageDto
                                {
                                    FileName = fileName,
                                    Url = $"{GcsPublicUrl}/{BucketName}/{fileName}",
                                    IdCargo = idCargo,
                                    ImageNumber = imageNumber
                                });
                            }
                        }
                    }
                }

                // Ordenar por número de imagen
                images = images.OrderBy(i => i.ImageNumber).ToList();

                await _logger.LogInformationAsync($"Se obtuvieron {images.Count} imágenes para cargo {idCargo}", "CargoImageService", "GetImagesAsync");
                return images;
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al obtener imágenes", ex, "CargoImageService", "GetImagesAsync");
                return images;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al obtener imágenes", ex, "CargoImageService", "GetImagesAsync");
                return images;
            }
        }

        /// <summary>
        /// Elimina una imagen de Google Cloud Storage
        /// </summary>
        public async Task<bool> DeleteImageAsync(string fileName, CancellationToken cancellationToken = default)
        {
            try
            {
                await _logger.LogInformationAsync($"Eliminando imagen: {fileName}", "CargoImageService", "DeleteImageAsync");

                // Obtener la API key
                var apiKey = await _googleMapsConfigService.GetApiKeyAsync(cancellationToken);
                if (string.IsNullOrEmpty(apiKey))
                {
                    await _logger.LogErrorAsync("No se pudo obtener la API key de Google", null, "CargoImageService", "DeleteImageAsync");
                    return false;
                }

                // URL para eliminar el objeto
                var deleteUrl = $"{GcsApiBaseUrl}/b/{BucketName}/o/{Uri.EscapeDataString(fileName)}?key={apiKey}";

                var response = await _http.DeleteAsync(deleteUrl, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    await _logger.LogErrorAsync(
                        $"Error al eliminar imagen de GCS. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "CargoImageService",
                        "DeleteImageAsync");
                    return false;
                }

                await _logger.LogInformationAsync($"Imagen eliminada exitosamente: {fileName}", "CargoImageService", "DeleteImageAsync");
                return true;
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al eliminar imagen", ex, "CargoImageService", "DeleteImageAsync");
                throw new InvalidOperationException("Error de comunicación con Google Cloud Storage al eliminar imagen", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al eliminar imagen", ex, "CargoImageService", "DeleteImageAsync");
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

        /// <summary>
        /// Obtiene la extensión de archivo apropiada basada en el tipo de contenido
        /// </summary>
        private static string GetExtensionFromContentType(string contentType)
        {
            return contentType.ToLowerInvariant() switch
            {
                "image/jpeg" => ".jpg",
                "image/jpg" => ".jpg",
                "image/png" => ".png",
                "image/gif" => ".gif",
                "image/webp" => ".webp",
                "image/bmp" => ".bmp",
                _ => ".jpg"
            };
        }
    }
}
