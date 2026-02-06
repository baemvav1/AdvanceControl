namespace Advance_Control.Utilities
{
    /// <summary>
    /// Utilidades para manejo de tipos de contenido de imágenes
    /// </summary>
    public static class ImageContentTypeHelper
    {
        /// <summary>
        /// Obtiene el tipo de contenido MIME basado en la extensión del archivo
        /// </summary>
        /// <param name="extension">Extensión del archivo (ej: ".jpg", ".png")</param>
        /// <returns>Tipo de contenido MIME</returns>
        public static string GetContentTypeFromExtension(string extension)
        {
            return extension.ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".webp" => "image/webp",
                _ => "image/jpeg"
            };
        }

        /// <summary>
        /// Obtiene la extensión de archivo apropiada basada en el tipo de contenido MIME
        /// </summary>
        /// <param name="contentType">Tipo de contenido MIME</param>
        /// <returns>Extensión de archivo</returns>
        public static string GetExtensionFromContentType(string contentType)
        {
            return contentType.ToLowerInvariant() switch
            {
                "image/jpeg" => ".jpg",
                "image/jpg" => ".jpg",
                "image/png" => ".png",
                "image/gif" => ".gif",
                "image/webp" => ".webp",
                "image/bmp" => ".bmp",
                _ => ".jpg"
            };
        }
    }
}
