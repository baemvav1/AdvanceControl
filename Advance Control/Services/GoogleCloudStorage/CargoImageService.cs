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
using Advance_Control.Services.EndPointProvider;
using Advance_Control.Services.Logging;
using Advance_Control.Utilities;

namespace Advance_Control.Services.GoogleCloudStorage
{
    /// <summary>
    /// Implementación del servicio de almacenamiento de imágenes de cargos.
    /// Utiliza la API del backend como proxy para operaciones con Google Cloud Storage,
    /// ya que GCS requiere autenticación OAuth 2.0 que el backend maneja de forma segura.
    /// </summary>
    public class CargoImageService : ICargoImageService
    {
        private readonly HttpClient _http;
        private readonly IApiEndpointProvider _endpoints;
        private readonly ILoggingService _logger;

        public CargoImageService(
            HttpClient http,
            IApiEndpointProvider endpoints,
            ILoggingService logger)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Sube una imagen a Google Cloud Storage para un cargo específico a través del backend API
        /// </summary>
        public async Task<CargoImageDto?> UploadImageAsync(int idCargo, Stream imageStream, string contentType, CancellationToken cancellationToken = default)
        {
            try
            {
                await _logger.LogInformationAsync($"Subiendo imagen para cargo {idCargo}", "CargoImageService", "UploadImageAsync");

                // Construir la URL del endpoint del backend
                var url = _endpoints.GetEndpoint("api", "CargoImages", idCargo.ToString());

                // Leer el stream en un byte array
                byte[] imageBytes;
                using (var memoryStream = new MemoryStream())
                {
                    await imageStream.CopyToAsync(memoryStream, cancellationToken);
                    imageBytes = memoryStream.ToArray();
                }

                // Crear contenido multipart/form-data para enviar la imagen
                using var formContent = new MultipartFormDataContent();
                var imageContent = new ByteArrayContent(imageBytes);
                imageContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
                
                // Obtener extensión para el nombre del archivo
                var extension = GetExtensionFromContentType(contentType);
                formContent.Add(imageContent, "image", $"cargo_image{extension}");

                await _logger.LogInformationAsync($"Enviando imagen al backend: {url}", "CargoImageService", "UploadImageAsync");

                // Realizar la solicitud POST al backend
                var response = await _http.PostAsync(url, formContent, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    await _logger.LogErrorAsync(
                        $"Error al subir imagen. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "CargoImageService",
                        "UploadImageAsync");
                    return null;
                }

                // Parsear la respuesta del backend
                var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
                var cargoImage = JsonSerializer.Deserialize<CargoImageDto>(responseText, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (cargoImage != null)
                {
                    await _logger.LogInformationAsync($"Imagen subida exitosamente: {cargoImage.FileName}", "CargoImageService", "UploadImageAsync");
                }

                return cargoImage;
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al subir imagen", ex, "CargoImageService", "UploadImageAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al subir imagen", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al subir imagen", ex, "CargoImageService", "UploadImageAsync");
                throw;
            }
        }

        /// <summary>
        /// Obtiene todas las imágenes de un cargo desde el backend API
        /// </summary>
        public async Task<List<CargoImageDto>> GetImagesAsync(int idCargo, CancellationToken cancellationToken = default)
        {
            var images = new List<CargoImageDto>();

            try
            {
                await _logger.LogInformationAsync($"Obteniendo imágenes para cargo {idCargo}", "CargoImageService", "GetImagesAsync");

                // Construir la URL del endpoint del backend
                var url = _endpoints.GetEndpoint("api", "CargoImages", idCargo.ToString());

                var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    await _logger.LogErrorAsync(
                        $"Error al obtener imágenes. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "CargoImageService",
                        "GetImagesAsync");
                    return images;
                }

                var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
                images = JsonSerializer.Deserialize<List<CargoImageDto>>(responseText, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new List<CargoImageDto>();

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
        /// Elimina una imagen a través del backend API
        /// </summary>
        public async Task<bool> DeleteImageAsync(string fileName, CancellationToken cancellationToken = default)
        {
            try
            {
                await _logger.LogInformationAsync($"Eliminando imagen: {fileName}", "CargoImageService", "DeleteImageAsync");

                // Construir la URL del endpoint del backend
                var url = _endpoints.GetEndpoint("api", "CargoImages", Uri.EscapeDataString(fileName));

                var response = await _http.DeleteAsync(url, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    await _logger.LogErrorAsync(
                        $"Error al eliminar imagen. Status: {response.StatusCode}, Content: {errorContent}",
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
                throw new InvalidOperationException("Error de comunicación con el servidor al eliminar imagen", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al eliminar imagen", ex, "CargoImageService", "DeleteImageAsync");
                throw;
            }
        }

        /// <summary>
        /// Obtiene la extensión de archivo apropiada basada en el tipo de contenido
        /// </summary>
        private static string GetExtensionFromContentType(string contentType)
        {
            return ImageContentTypeHelper.GetExtensionFromContentType(contentType);
        }
    }
}
