using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage.Pickers;

namespace Advance_Control.Views.Viewers
{
    public sealed partial class ViewerImagenes : UserControl
    {
        /// <summary>
        /// Handle de la ventana que contiene este control.
        /// Necesario para inicializar el FileSavePicker.
        /// Se establece desde ImageViewerWindow después de configurar la ventana.
        /// </summary>
        public nint WindowHandle { get; set; }

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

        private async void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ImagePath) || !File.Exists(ImagePath))
            {
                await MostrarMensajeAsync("No hay imagen disponible para guardar.");
                return;
            }

            var ext = Path.GetExtension(ImagePath).TrimStart('.').ToLowerInvariant();
            var nombreSugerido = Path.GetFileName(ImagePath);

            var picker = new FileSavePicker();

            // Obtener HWND: primero el asignado, luego el de MainWindow como respaldo
            var hwnd = WindowHandle != 0
                ? WindowHandle
                : WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow!);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            picker.SuggestedStartLocation = PickerLocationId.Downloads;
            picker.SuggestedFileName = nombreSugerido;

            switch (ext)
            {
                case "jpg" or "jpeg":
                    picker.FileTypeChoices.Add("Imagen JPEG", new List<string> { ".jpg", ".jpeg" });
                    break;
                case "png":
                    picker.FileTypeChoices.Add("Imagen PNG", new List<string> { ".png" });
                    break;
                case "gif":
                    picker.FileTypeChoices.Add("Imagen GIF", new List<string> { ".gif" });
                    break;
                case "bmp":
                    picker.FileTypeChoices.Add("Imagen BMP", new List<string> { ".bmp" });
                    break;
                case "webp":
                    picker.FileTypeChoices.Add("Imagen WebP", new List<string> { ".webp" });
                    break;
                case "pdf":
                    picker.FileTypeChoices.Add("Documento PDF", new List<string> { ".pdf" });
                    break;
                default:
                    picker.FileTypeChoices.Add("Todos los archivos", new List<string> { "." });
                    break;
            }

            var archivo = await picker.PickSaveFileAsync();
            if (archivo == null) return; // Usuario canceló

            try
            {
                DownloadButton.IsEnabled = false;
                File.Copy(ImagePath, archivo.Path, overwrite: true);
            }
            catch (Exception ex)
            {
                await MostrarMensajeAsync($"Error al guardar la imagen: {ex.Message}");
            }
            finally
            {
                DownloadButton.IsEnabled = true;
            }
        }

        private async System.Threading.Tasks.Task MostrarMensajeAsync(string mensaje)
        {
            var dialog = new ContentDialog
            {
                Title = "Visor de imágenes",
                Content = mensaje,
                CloseButtonText = "Cerrar",
                XamlRoot = XamlRoot
            };
            await dialog.ShowAsync();
        }
    }
}
