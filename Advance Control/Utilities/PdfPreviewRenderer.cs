using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Data.Pdf;
using Windows.Storage;
using Windows.Storage.Streams;

namespace Advance_Control.Utilities
{
    /// <summary>
    /// Renderiza un PDF de disco como una serie de imágenes, para previsualizarlo embebido
    /// dentro de cualquier contenedor XAML (sin depender de un diálogo emergente).
    /// Mismo mecanismo que <see cref="Views.Dialogs.CotizacionVisorDialog"/> (Windows.Data.Pdf),
    /// extraído aquí para poder reusarse fuera de ese diálogo.
    /// </summary>
    public static class PdfPreviewRenderer
    {
        /// <summary>Renderiza todas las páginas del PDF en <paramref name="pdfPath"/> como controles listos para insertar en un panel.</summary>
        public static async Task<List<FrameworkElement>> RenderizarTodasLasPaginasAsync(string pdfPath)
        {
            var archivo = await ObtenerArchivoPdfAsync(pdfPath);
            var documento = await PdfDocument.LoadFromFileAsync(archivo);

            var paginas = new List<FrameworkElement>();
            for (uint i = 0; i < documento.PageCount; i++)
            {
                using var pagina = documento.GetPage(i);
                paginas.Add(await RenderizarPaginaAsync(pagina));
            }

            return paginas;
        }

        private static async Task<StorageFile> ObtenerArchivoPdfAsync(string pdfPath)
        {
            if (string.IsNullOrWhiteSpace(pdfPath))
                throw new ArgumentException("La ruta del PDF no puede estar vacía.", nameof(pdfPath));

            if (Uri.TryCreate(pdfPath, UriKind.Absolute, out var uriExistente) && uriExistente.IsFile)
            {
                return await StorageFile.GetFileFromPathAsync(uriExistente.LocalPath);
            }

            var fullPath = Path.GetFullPath(pdfPath);
            return await StorageFile.GetFileFromPathAsync(fullPath);
        }

        private static async Task<FrameworkElement> RenderizarPaginaAsync(PdfPage pagina)
        {
            using var renderStream = new InMemoryRandomAccessStream();
            var opciones = new PdfPageRenderOptions { DestinationWidth = 1400 };

            await pagina.RenderToStreamAsync(renderStream, opciones);
            renderStream.Seek(0);

            var imagenOrigen = new BitmapImage();
            await imagenOrigen.SetSourceAsync(renderStream);

            var imagen = new Image
            {
                Source = imagenOrigen,
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            return new Border
            {
                Padding = new Thickness(8),
                Background = (Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"],
                BorderBrush = (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Child = imagen
            };
        }
    }
}
