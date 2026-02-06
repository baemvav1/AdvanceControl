using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;

namespace Advance_Control.Services.GoogleCloudStorage
{
    /// <summary>
    /// Interfaz para el servicio de almacenamiento de imágenes de cargos en Google Cloud Storage
    /// </summary>
    public interface ICargoImageService
    {
        /// <summary>
        /// Sube una imagen a Google Cloud Storage para un cargo específico
        /// </summary>
        /// <param name="idCargo">ID del cargo</param>
        /// <param name="imageStream">Stream con los datos de la imagen</param>
        /// <param name="contentType">Tipo de contenido de la imagen (ej: image/jpeg)</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Información de la imagen subida</returns>
        Task<CargoImageDto?> UploadImageAsync(int idCargo, Stream imageStream, string contentType, CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtiene todas las imágenes de un cargo
        /// </summary>
        /// <param name="idCargo">ID del cargo</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Lista de imágenes del cargo</returns>
        Task<List<CargoImageDto>> GetImagesAsync(int idCargo, CancellationToken cancellationToken = default);

        /// <summary>
        /// Elimina una imagen de un cargo
        /// </summary>
        /// <param name="fileName">Nombre del archivo a eliminar</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>True si se eliminó correctamente</returns>
        Task<bool> DeleteImageAsync(string fileName, CancellationToken cancellationToken = default);
    }
}
