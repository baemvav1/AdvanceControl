using System.Threading.Tasks;

namespace Advance_Control.Services.ImageViewer
{
    /// <summary>
    /// Servicio reutilizable para mostrar imágenes en un visor con capacidad de zoom.
    /// </summary>
    public interface IImageViewerService
    {
        /// <summary>
        /// Muestra una imagen en el visor con soporte de zoom.
        /// </summary>
        /// <param name="imageUrl">Ruta o URL de la imagen a mostrar.</param>
        /// <param name="title">Título del visor (opcional).</param>
        Task ShowImageAsync(string imageUrl, string? title = null);
    }
}
