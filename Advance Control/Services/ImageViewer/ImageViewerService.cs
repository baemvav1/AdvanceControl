using System;
using System.Threading.Tasks;
using Advance_Control.Services.Dialog;
using Advance_Control.Views.Dialogs;

namespace Advance_Control.Services.ImageViewer
{
    /// <summary>
    /// Implementación del servicio de visualización de imágenes.
    /// Muestra un diálogo reutilizable con la imagen seleccionada y soporte de zoom.
    /// </summary>
    public class ImageViewerService : IImageViewerService
    {
        private readonly IDialogService _dialogService;

        public ImageViewerService(IDialogService dialogService)
        {
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        }

        /// <inheritdoc />
        public async Task ShowImageAsync(string imageUrl, string? title = null)
        {
            if (string.IsNullOrEmpty(imageUrl))
                return;

            await _dialogService.ShowDialogAsync<ImageViewerUserControl>(
                configureControl: control => control.SetImageSource(imageUrl),
                title: title,
                closeButtonText: "Cerrar"
            );
        }
    }
}
