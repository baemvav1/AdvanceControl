namespace Advance_Control.Models
{
    /// <summary>
    /// DTO para deserializar la respuesta del API al subir o listar archivos
    /// en los endpoints de /api/uploads/*.
    /// </summary>
    public class UploadFileResponseDto
    {
        /// <summary>Nombre del archivo (sin ruta)</summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>URL relativa pública: /storage/{categoria}/{subRuta}/{archivo}</summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>Tipo MIME del archivo</summary>
        public string ContentType { get; set; } = string.Empty;

        /// <summary>Tamaño del archivo en bytes</summary>
        public long SizeBytes { get; set; }
    }
}
