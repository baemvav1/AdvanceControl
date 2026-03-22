using System;
using System.IO;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Advance_Control.Views.Viewers
{
    public sealed partial class ViewerImagenes : UserControl
    {
        public ViewerImagenes()
        {
            InitializeComponent();
            Loaded += ViewerImagenes_Loaded;
            Unloaded += ViewerImagenes_Unloaded;
            ViewerImage.ImageOpened += ViewerImage_ImageOpened;
            ViewerImage.ImageFailed += ViewerImage_ImageFailed;
        }

        public string? ImagePath { get; private set; }

        public void SetImageSource(string imagePath)
        {
            ImagePath = imagePath;
            ImagePathTextBlock.Text = imagePath;
            LoadImage();
        }

        private void ViewerImagenes_Loaded(object sender, RoutedEventArgs e)
        {
            ImageScrollViewer.ViewChanged += ImageScrollViewer_ViewChanged;
            UpdateZoomText();
            if (!string.IsNullOrWhiteSpace(ImagePath) && ViewerImage.Source == null)
            {
                LoadImage();
            }
        }

        private void ViewerImagenes_Unloaded(object sender, RoutedEventArgs e)
        {
            ImageScrollViewer.ViewChanged -= ImageScrollViewer_ViewChanged;
        }

        private void LoadImage()
        {
            if (string.IsNullOrWhiteSpace(ImagePath))
            {
                ViewerImage.Source = null;
                StatusTextBlock.Text = "No se proporcionó una imagen para visualizar.";
                StatusTextBlock.Visibility = Visibility.Visible;
                return;
            }

            try
            {
                var imageUri = BuildImageUri(ImagePath);
                ViewerImage.Source = new BitmapImage(imageUri);
                StatusTextBlock.Text = "Cargando imagen...";
                StatusTextBlock.Visibility = Visibility.Visible;
                ResetZoom();
            }
            catch (UriFormatException)
            {
                ViewerImage.Source = null;
                StatusTextBlock.Text = "La ruta de la imagen no es válida.";
                StatusTextBlock.Visibility = Visibility.Visible;
            }
            catch (ArgumentException)
            {
                ViewerImage.Source = null;
                StatusTextBlock.Text = "No fue posible preparar la imagen seleccionada.";
                StatusTextBlock.Visibility = Visibility.Visible;
            }
        }

        private static Uri BuildImageUri(string imagePath)
        {
            if (Uri.TryCreate(imagePath, UriKind.Absolute, out var uri))
            {
                return uri;
            }

            if (Path.IsPathRooted(imagePath))
            {
                return new Uri(Path.GetFullPath(imagePath), UriKind.Absolute);
            }

            throw new UriFormatException("La ruta de la imagen no es válida.");
        }

        private void ViewerImage_ImageOpened(object sender, RoutedEventArgs e)
        {
            StatusTextBlock.Visibility = Visibility.Collapsed;
        }

        private void ViewerImage_ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {
            StatusTextBlock.Text = "No fue posible cargar la imagen seleccionada.";
            StatusTextBlock.Visibility = Visibility.Visible;
        }

        private void ImageScrollViewer_ViewChanged(object? sender, ScrollViewerViewChangedEventArgs e)
        {
            UpdateZoomText();
        }

        private void UpdateZoomText()
        {
            ZoomLevelTextBlock.Text = $"{Math.Round(ImageScrollViewer.ZoomFactor * 100)}%";
        }

        private void ResetZoom()
        {
            ImageScrollViewer.ChangeView(0, 0, 1.0f, true);
            UpdateZoomText();
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
            ResetZoom();
        }

        private void FitButton_Click(object sender, RoutedEventArgs e)
        {
            ResetZoom();
        }
    }
}
