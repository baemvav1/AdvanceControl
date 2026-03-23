using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Advance_Control.Services.LocalStorage
{
    public interface ILevantamientoImageService
    {
        /// <summary>
        /// Guarda una imagen asociada a un nodo de levantamiento.
        /// Retorna la info de la imagen guardada (ruta, nombre, titulo).
        /// </summary>
        Task<LevantamientoImageResult> SaveImageAsync(int idLevantamiento, string infoNodo, Stream imageStream, string contentType, CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtiene todas las imagenes existentes para un nodo especifico.
        /// </summary>
        Task<List<LevantamientoImageResult>> GetImagesAsync(int idLevantamiento, string infoNodo, CancellationToken cancellationToken = default);

        /// <summary>
        /// Elimina una imagen por su ruta completa.
        /// </summary>
        Task DeleteImageAsync(string filePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Elimina multiples imagenes por sus rutas.
        /// </summary>
        Task DeleteImagesAsync(IEnumerable<string> filePaths, CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtiene la carpeta base de un levantamiento.
        /// </summary>
        string GetLevantamientoFolder(int idLevantamiento);
    }

    /// <summary>
    /// Resultado de guardar/listar una imagen de levantamiento.
    /// </summary>
    public class LevantamientoImageResult
    {
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public int ImageNumber { get; set; }
    }
}
