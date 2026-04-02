using System;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;

namespace Advance_Control.Converters
{
    /// <summary>
    /// Convierte una ruta de archivo local a BitmapImage cargando desde stream,
    /// evitando la caché interna de BitmapImage por URI.
    /// Esto soluciona el problema donde imágenes recién escritas muestran
    /// contenido obsoleto de la caché del decodificador.
    /// </summary>
    public sealed class FilePathToImageConverter : IValueConverter
    {
        /// <summary>
        /// Método estático para usar con function-style x:Bind en DataTemplates
        /// dentro de Window (donde StaticResource no funciona con x:Bind converters).
        /// Uso en XAML: Source="{x:Bind converters:FilePathToImageConverter.ToImage(Url), Mode=OneWay}"
        /// </summary>
        public static BitmapImage? ToImage(string? path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return null;

            try
            {
                var bitmap = new BitmapImage();
                _ = LoadFromFileAsync(bitmap, path);
                return bitmap;
            }
            catch
            {
                return null;
            }
        }

        public object? Convert(object value, Type targetType, object parameter, string language)
        {
            return ToImage(value as string);
        }

        /// <summary>
        /// Carga la imagen desde un stream en vez de URI para evitar la caché.
        /// El BitmapImage se retorna vacío y se llena asincrónicamente;
        /// el control Image se redibuja automáticamente al completarse.
        /// </summary>
        private static async System.Threading.Tasks.Task LoadFromFileAsync(BitmapImage bitmap, string path)
        {
            try
            {
                var bytes = await File.ReadAllBytesAsync(path);
                using var ms = new InMemoryRandomAccessStream();
                await ms.WriteAsync(bytes.AsBuffer());
                ms.Seek(0);
                await bitmap.SetSourceAsync(ms);
            }
            catch
            {
                // Si falla la carga, el Image mostrará vacío
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotImplementedException();
    }
}
