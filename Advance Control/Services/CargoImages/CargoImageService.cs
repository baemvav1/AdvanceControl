using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.EndPointProvider;
using Advance_Control.Services.Logging;

namespace Advance_Control.Services.CargoImages
{
    /// <summary>
    /// Service for managing cargo images in Google Cloud Storage via backend API
    /// </summary>
    public class CargoImageService : ICargoImageService
    {
        private readonly HttpClient _http;
        private readonly IApiEndpointProvider _endpoints;
        private readonly ILoggingService _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public CargoImageService(HttpClient http, IApiEndpointProvider endpoints, ILoggingService logger)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        /// <summary>
        /// Gets all images for a specific cargo
        /// </summary>
        public async Task<List<CargoImageDto>> GetCargoImagesAsync(int idCargo, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"{_endpoints.GetEndpoint("api", "CargoImages")}?idCargo={idCargo}";

                await _logger.LogInformationAsync($"Obteniendo imágenes del cargo {idCargo} desde: {url}", "CargoImageService", "GetCargoImagesAsync");

                var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al obtener imágenes del cargo. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "CargoImageService",
                        "GetCargoImagesAsync");
                    return new List<CargoImageDto>();
                }

                var images = await response.Content.ReadFromJsonAsync<List<CargoImageDto>>(cancellationToken: cancellationToken).ConfigureAwait(false);

                await _logger.LogInformationAsync($"Se obtuvieron {images?.Count ?? 0} imágenes para el cargo {idCargo}", "CargoImageService", "GetCargoImagesAsync");

                return images ?? new List<CargoImageDto>();
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al obtener imágenes del cargo", ex, "CargoImageService", "GetCargoImagesAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al obtener imágenes del cargo", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al obtener imágenes del cargo", ex, "CargoImageService", "GetCargoImagesAsync");
                throw;
            }
        }

        /// <summary>
        /// Uploads an image for a cargo to Google Cloud Storage
        /// </summary>
        public async Task<CargoImageDto?> UploadCargoImageAsync(int idCargo, Stream imageStream, string fileName, string contentType, CancellationToken cancellationToken = default)
        {
            try
            {
                // Get the next image number for this cargo
                var imageNumber = await GetNextImageNumberAsync(idCargo, cancellationToken).ConfigureAwait(false);

                // Generate the image name following the format: Cargo_Id_{idCargo}_{imageNumber}
                var extension = Path.GetExtension(fileName);
                var imageName = $"Cargo_Id_{idCargo}_{imageNumber}{extension}";

                var url = _endpoints.GetEndpoint("api", "CargoImages");

                await _logger.LogInformationAsync($"Subiendo imagen {imageName} para cargo {idCargo}", "CargoImageService", "UploadCargoImageAsync");

                using var content = new MultipartFormDataContent();
                
                // Add the image file - do not use 'using' here as MultipartFormDataContent manages disposal
                var streamContent = new StreamContent(imageStream);
                streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
                content.Add(streamContent, "file", imageName);
                
                // Add metadata
                content.Add(new StringContent(idCargo.ToString()), "idCargo");
                content.Add(new StringContent(imageName), "imageName");
                content.Add(new StringContent(imageNumber.ToString()), "imageNumber");

                var response = await _http.PostAsync(url, content, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al subir imagen. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "CargoImageService",
                        "UploadCargoImageAsync");
                    return null;
                }

                var result = await response.Content.ReadFromJsonAsync<CargoImageDto>(cancellationToken: cancellationToken).ConfigureAwait(false);

                await _logger.LogInformationAsync($"Imagen {imageName} subida exitosamente", "CargoImageService", "UploadCargoImageAsync");

                return result;
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al subir imagen", ex, "CargoImageService", "UploadCargoImageAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al subir imagen", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al subir imagen", ex, "CargoImageService", "UploadCargoImageAsync");
                throw;
            }
        }

        /// <summary>
        /// Deletes a cargo image from Google Cloud Storage
        /// </summary>
        public async Task<bool> DeleteCargoImageAsync(int idCargoImage, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"{_endpoints.GetEndpoint("api", "CargoImages")}/{idCargoImage}";

                await _logger.LogInformationAsync($"Eliminando imagen {idCargoImage}", "CargoImageService", "DeleteCargoImageAsync");

                var response = await _http.DeleteAsync(url, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al eliminar imagen. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "CargoImageService",
                        "DeleteCargoImageAsync");
                    return false;
                }

                await _logger.LogInformationAsync($"Imagen {idCargoImage} eliminada exitosamente", "CargoImageService", "DeleteCargoImageAsync");

                return true;
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al eliminar imagen", ex, "CargoImageService", "DeleteCargoImageAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al eliminar imagen", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al eliminar imagen", ex, "CargoImageService", "DeleteCargoImageAsync");
                throw;
            }
        }

        /// <summary>
        /// Gets the next available image number for a cargo
        /// </summary>
        public async Task<int> GetNextImageNumberAsync(int idCargo, CancellationToken cancellationToken = default)
        {
            try
            {
                // Get existing images to determine the next number
                var existingImages = await GetCargoImagesAsync(idCargo, cancellationToken).ConfigureAwait(false);
                
                if (existingImages.Count == 0)
                {
                    return 1;
                }

                // Find the maximum image number and add 1 using LINQ
                var maxNumber = existingImages.Max(i => i.ImageNumber);
                return maxNumber + 1;
            }
            catch (Exception ex)
            {
                await _logger.LogWarningAsync($"Error al obtener siguiente número de imagen, usando 1 por defecto: {ex.Message}", "CargoImageService", "GetNextImageNumberAsync");
                return 1;
            }
        }
    }
}
