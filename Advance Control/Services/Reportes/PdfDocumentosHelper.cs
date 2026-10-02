using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Advance_Control.Models;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using Windows.Storage.Streams;

namespace Advance_Control.Services.Reportes
{
    /// <summary>
    /// Utilidades compartidas por los PDF que incrustan evidencias (hojas de servicio, levantamientos,
    /// fotos): convertir adjuntos PDF a imágenes y colocar imágenes sin que se partan entre páginas.
    /// Pensado para página carta con márgenes de 2 cm (QuoteService y el reporte ejecutivo).
    /// </summary>
    internal static class PdfDocumentosHelper
    {
        private const float CONTENT_WIDTH_CM = 15.59f;   // 21.59 - 2*2 - 2*1 (padding horizontal 1cm)
        private const float MAX_PAGE_HEIGHT_CM = 21f;    // 27.94 - 4(márgenes) - 2(padding contenido) - margen seguridad

        /// <summary>
        /// Expande las rutas de documentos: imágenes directamente, PDFs convertidos a PNGs temporales
        /// (una imagen por página). Los temporales se agregan a <paramref name="tempFiles"/> para que el
        /// llamador los borre al terminar.
        /// </summary>
        public static async Task<List<string>> ExpandirDocumentosAsync(
            IEnumerable<OperacionImageDto> docs, List<string> tempFiles)
        {
            var result = new List<string>();
            foreach (var dto in docs.Where(x => !string.IsNullOrWhiteSpace(x.Url) && File.Exists(x.Url)))
            {
                if (dto.IsPdf)
                {
                    var pages = await ConvertirPdfAImagenesAsync(dto.Url!);
                    result.AddRange(pages);
                    lock (tempFiles)
                    {
                        tempFiles.AddRange(pages);
                    }
                }
                else
                {
                    result.Add(dto.Url!);
                }
            }
            return result;
        }

        /// <summary>
        /// Renderiza cada página de un PDF a un PNG temporal usando Windows.Data.Pdf (API nativa Windows, sin dependencias extra).
        /// El nombre lleva un identificador único para que dos adjuntos con el mismo nombre de distintas
        /// operaciones, procesados en paralelo, no se pisen.
        /// </summary>
        public static async Task<List<string>> ConvertirPdfAImagenesAsync(string pdfPath)
        {
            var tempFiles = new List<string>();
            try
            {
                var file    = await Windows.Storage.StorageFile.GetFileFromPathAsync(pdfPath);
                var pdfDoc  = await Windows.Data.Pdf.PdfDocument.LoadFromFileAsync(file);
                var tempFolder = Path.GetTempPath();
                var baseName   = Path.GetFileNameWithoutExtension(pdfPath);
                var unico      = Guid.NewGuid().ToString("N")[..8];

                for (uint i = 0; i < pdfDoc.PageCount; i++)
                {
                    using var page     = pdfDoc.GetPage(i);
                    var tempPath       = Path.Combine(tempFolder, $"__acreporte_{baseName}_{unico}_p{i}.png");
                    using var memStream = new InMemoryRandomAccessStream();
                    await page.RenderToStreamAsync(memStream);
                    memStream.Seek(0);
                    using var fileStream = File.Create(tempPath);
                    await memStream.AsStreamForRead().CopyToAsync(fileStream);
                    tempFiles.Add(tempPath);
                }
            }
            catch { /* Si falla la conversión del PDF, se omite sin romper el reporte */ }
            return tempFiles;
        }

        /// <summary>
        /// Agrega una imagen adaptativa al contenedor: si cabe en ancho completo dentro de la página,
        /// usa FitWidth; si es muy alta, limita la altura y ajusta el ancho proporcionalmente.
        /// Siempre usa ShowEntire para evitar que se divida entre páginas. <paramref name="altoMaximoCm"/>
        /// permite limitarla más (p. ej. para que quepa debajo de un encabezado en la misma hoja).
        /// </summary>
        public static void AgregarImagenAdaptativa(IContainer container, string imagePath, float altoMaximoCm = MAX_PAGE_HEIGHT_CM)
        {
            try
            {
                using var stream = File.OpenRead(imagePath);
                using var codec = SkiaSharp.SKCodec.Create(stream);
                if (codec == null)
                {
                    // Sin dimensiones: ancho completo como fallback
                    container.ShowEntire().PaddingTop(4).PaddingHorizontal(1, Unit.Centimetre)
                        .Image(imagePath).FitWidth();
                    return;
                }

                var info = codec.Info;
                float imgW = info.Width;
                float imgH = info.Height;

                // Altura resultante si se fuerza al ancho disponible
                float alturaResultanteCm = (imgH / imgW) * CONTENT_WIDTH_CM;

                if (alturaResultanteCm <= altoMaximoCm)
                {
                    // Cabe en la página: forzar ancho completo
                    container.ShowEntire().PaddingTop(4).PaddingHorizontal(1, Unit.Centimetre)
                        .Image(imagePath).FitWidth();
                }
                else
                {
                    // Muy alta: calcular el ancho que hace que la altura sea exactamente altoMaximoCm
                    float anchoAjustadoCm = (imgW / imgH) * altoMaximoCm;
                    container.ShowEntire().PaddingTop(4).AlignCenter()
                        .Width(anchoAjustadoCm, Unit.Centimetre)
                        .Image(imagePath).FitWidth();
                }
            }
            catch
            {
                // Fallback seguro: ancho completo
                container.ShowEntire().PaddingTop(4).PaddingHorizontal(1, Unit.Centimetre)
                    .Image(imagePath).FitWidth();
            }
        }
    }
}
