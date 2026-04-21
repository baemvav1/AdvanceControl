using Advance_Control.Services.EndPointProvider;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;

namespace Advance_Control.Utilities
{
    public sealed record ClipboardChatImageResult(string? Url, string? ErrorMessage)
    {
        public bool IsValid => !string.IsNullOrWhiteSpace(Url);
    }

    public sealed record ChatImageDownloadResult(MemoryStream Stream, string ContentType, string FileName);

    /// <summary>
    /// Utilidades para copiar, validar y descargar imágenes del chat.
    /// </summary>
    public static class ChatImageTransferHelper
    {
        private static readonly string[] SupportedImageExtensions =
        [
            ".jpg",
            ".jpeg",
            ".png",
            ".gif",
            ".webp",
            ".bmp"
        ];

        public static void CopyImageUrlToClipboard(string imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
                throw new ArgumentException("La URL de imagen no puede estar vacía.", nameof(imageUrl));

            var package = new DataPackage();
            package.SetText(imageUrl.Trim());
            Clipboard.SetContent(package);
            Clipboard.Flush();
        }

        public static async Task<ClipboardChatImageResult> GetChatImageUrlFromClipboardAsync()
        {
            var data = Clipboard.GetContent();
            if (!data.Contains(StandardDataFormats.Text))
                return new ClipboardChatImageResult(null, "El portapapeles no contiene una URL de imagen del chat.");

            var text = (await data.GetTextAsync()).Trim();
            if (string.IsNullOrWhiteSpace(text))
                return new ClipboardChatImageResult(null, "El portapapeles no contiene una URL válida.");

            if (!Uri.TryCreate(text, UriKind.Absolute, out _))
                return new ClipboardChatImageResult(null, "El contenido del portapapeles no es una URL absoluta válida.");

            if (!IsChatImageUrl(text))
                return new ClipboardChatImageResult(null, "La URL del portapapeles no corresponde a una imagen del chat.");

            return new ClipboardChatImageResult(text, null);
        }

        public static bool IsChatImageUrl(string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl) ||
                !Uri.TryCreate(imageUrl.Trim(), UriKind.Absolute, out var uri))
                return false;

            if (!uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
                !uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
                return false;

            var apiBaseUri = GetApiBaseUri();
            if (!string.Equals(uri.Scheme, apiBaseUri.Scheme, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(uri.Host, apiBaseUri.Host, StringComparison.OrdinalIgnoreCase) ||
                uri.Port != apiBaseUri.Port)
                return false;

            if (!uri.AbsolutePath.StartsWith("/storage/mensajes/", StringComparison.OrdinalIgnoreCase))
                return false;

            var extension = Path.GetExtension(uri.AbsolutePath);
            return SupportedImageExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
        }

        public static async Task<ChatImageDownloadResult?> DownloadChatImageAsync(string imageUrl)
        {
            if (!IsChatImageUrl(imageUrl))
                return null;

            using var httpClient = new HttpClient();
            using var response = await httpClient.GetAsync(imageUrl);
            response.EnsureSuccessStatusCode();

            var bytes = await response.Content.ReadAsByteArrayAsync();
            var uri = new Uri(imageUrl);
            var fileName = Path.GetFileName(uri.AbsolutePath);
            var extension = Path.GetExtension(fileName);
            var contentType = response.Content.Headers.ContentType?.MediaType;

            if (string.IsNullOrWhiteSpace(extension) && !string.IsNullOrWhiteSpace(contentType))
                extension = ImageContentTypeHelper.GetExtensionFromContentType(contentType);

            if (string.IsNullOrWhiteSpace(extension))
                extension = ".jpg";

            if (string.IsNullOrWhiteSpace(fileName))
                fileName = $"imagen_chat{extension}";

            if (string.IsNullOrWhiteSpace(contentType))
                contentType = ImageContentTypeHelper.GetContentTypeFromExtension(extension);

            return new ChatImageDownloadResult(new MemoryStream(bytes), contentType, fileName);
        }

        public static void ClearClipboard() => Clipboard.Clear();

        private static Uri GetApiBaseUri()
        {
            var endpoints = AppServices.Get<IApiEndpointProvider>();
            var baseUrl = endpoints.GetApiBaseUrl().TrimEnd('/');
            return new Uri($"{baseUrl}/");
        }
    }
}
