using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;

namespace Advance_Control.Services.CargoImages
{
    /// <summary>
    /// Interface for cargo image service that handles image operations with Google Cloud Storage
    /// </summary>
    public interface ICargoImageService
    {
        /// <summary>
        /// Gets all images for a specific cargo
        /// </summary>
        /// <param name="idCargo">The cargo ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of cargo images</returns>
        Task<List<CargoImageDto>> GetCargoImagesAsync(int idCargo, CancellationToken cancellationToken = default);

        /// <summary>
        /// Uploads an image for a cargo to Google Cloud Storage
        /// </summary>
        /// <param name="idCargo">The cargo ID</param>
        /// <param name="imageStream">The image data stream</param>
        /// <param name="fileName">Original file name</param>
        /// <param name="contentType">MIME content type of the image</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The created cargo image DTO, or null if failed</returns>
        Task<CargoImageDto?> UploadCargoImageAsync(int idCargo, Stream imageStream, string fileName, string contentType, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a cargo image from Google Cloud Storage
        /// </summary>
        /// <param name="idCargoImage">The cargo image ID to delete</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> DeleteCargoImageAsync(int idCargoImage, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the next available image number for a cargo
        /// </summary>
        /// <param name="idCargo">The cargo ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Next available image number</returns>
        Task<int> GetNextImageNumberAsync(int idCargo, CancellationToken cancellationToken = default);
    }
}
