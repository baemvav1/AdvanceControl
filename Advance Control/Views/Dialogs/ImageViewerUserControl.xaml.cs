using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Advance_Control.Views.Dialogs
{
    /// <summary>
    /// Control reutilizable para visualizar imágenes con capacidad de zoom.
    /// </summary>
    public sealed partial class ImageViewerUserControl : UserControl
    {
        public ImageViewerUserControl()
        {
            this.InitializeComponent();
            this.Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            ImageScrollViewer.ViewChanged += OnViewChanged;
            UpdateZoomText();
        }

        /// <summary>
        /// Establece la imagen a mostrar en el visor a partir de una ruta o URL.
        /// </summary>
        /// <param name="url">Ruta local o URL de la imagen.</param>
        public void SetImageSource(string url)
        {
            if (string.IsNullOrEmpty(url))
                return;

            try
            {
                var bitmap = new BitmapImage(new Uri(url));
                ViewerImage.Source = bitmap;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ImageViewerUserControl: Error al cargar imagen: {ex.Message}");
            }
        }

        private void OnViewChanged(object? sender, ScrollViewerViewChangedEventArgs _)
        {
            UpdateZoomText();
        }

        private void UpdateZoomText()
        {
            var zoomPercent = (int)(ImageScrollViewer.ZoomFactor * 100);
            ZoomLevelText.Text = $"{zoomPercent}%";
        }

        private void ZoomInButton_Click(object sender, RoutedEventArgs e)
        {
            var newZoom = Math.Min(ImageScrollViewer.ZoomFactor * 1.25f, ImageScrollViewer.MaxZoomFactor);
            ImageScrollViewer.ChangeView(null, null, newZoom);
        }

        private void ZoomOutButton_Click(object sender, RoutedEventArgs e)
        {
            var newZoom = Math.Max(ImageScrollViewer.ZoomFactor / 1.25f, ImageScrollViewer.MinZoomFactor);
            ImageScrollViewer.ChangeView(null, null, newZoom);
        }

        private void ZoomResetButton_Click(object sender, RoutedEventArgs e)
        {
            ImageScrollViewer.ChangeView(null, null, 1.0f);
        }
    }
}
