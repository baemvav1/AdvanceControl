using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;

namespace Advance_Control.Services.LocalStorage
{
    /// <summary>
    /// Interfaz para el servicio de almacenamiento de imágenes de operaciones (Prefacturas y Órdenes de Compra)
    /// </summary>
    public interface IOperacionImageService
    {
        /// <summary>
        /// Sube una imagen de prefactura para una operación específica
        /// </summary>
        /// <param name="idOperacion">ID de la operación</param>
        /// <param name="imageStream">Stream con los datos de la imagen</param>
        /// <param name="contentType">Tipo de contenido de la imagen (ej: image/jpeg)</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Información de la imagen subida</returns>
        Task<OperacionImageDto?> UploadPrefacturaAsync(int idOperacion, Stream imageStream, string contentType, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sube una imagen de orden de compra para una operación específica
        /// </summary>
        /// <param name="idOperacion">ID de la operación</param>
        /// <param name="imageStream">Stream con los datos de la imagen</param>
        /// <param name="contentType">Tipo de contenido de la imagen (ej: image/jpeg)</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Información de la imagen subida</returns>
        Task<OperacionImageDto?> UploadOrdenCompraAsync(int idOperacion, Stream imageStream, string contentType, CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtiene todas las imágenes de prefacturas de una operación
        /// </summary>
        /// <param name="idOperacion">ID de la operación</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Lista de imágenes de prefacturas de la operación</returns>
        Task<List<OperacionImageDto>> GetPrefacturasAsync(int idOperacion, CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtiene todas las imágenes de órdenes de compra de una operación
        /// </summary>
        /// <param name="idOperacion">ID de la operación</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Lista de imágenes de órdenes de compra de la operación</returns>
        Task<List<OperacionImageDto>> GetOrdenesCompraAsync(int idOperacion, CancellationToken cancellationToken = default);

        /// <summary>
        /// Elimina una imagen de operación
        /// </summary>
        /// <param name="fileName">Nombre del archivo a eliminar</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>True si se eliminó correctamente</returns>
        Task<bool> DeleteImageAsync(string fileName, CancellationToken cancellationToken = default);
    }
}
