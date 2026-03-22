using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Advance_Control.Views.Windows;

namespace Advance_Control.Services.ImageViewer
{
    public class ImageViewerService : IImageViewerService
    {
        private readonly List<ImageViewerWindow> _openWindows = new();

        public ImageViewerService()
        {
        }

        public async Task ShowImageAsync(string imageUrl, string? title = null)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                throw new ArgumentException("La ruta de la imagen es obligatoria.", nameof(imageUrl));
            }

            var window = new ImageViewerWindow(imageUrl, title);
            window.Closed += (_, _) => _openWindows.Remove(window);
            _openWindows.Add(window);
            window.Activate();
            await Task.CompletedTask;
        }
    }
}
