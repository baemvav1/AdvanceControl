using System;
using System.IO;
using System.Threading.Tasks;

namespace Advance_Control.Services.Email;

/// <summary>
/// Helper para gestionar imágenes de firma de correo.
/// Las firmas se guardan en: Documentos\Advance Control\Firmas Correos\
/// El nombre del archivo es el email con "@" reemplazado por "_".
/// </summary>
public static class FirmaCorreoHelper
{
    private static string CarpetaFirmas =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Advance Control",
            "Firmas Correos");

    private static readonly string[] ExtensionesPermitidas = [".png", ".jpg", ".jpeg", ".gif"];

    /// <summary>Devuelve la ruta del archivo de firma para el email dado, o cadena vacía si no existe.</summary>
    public static string GetFirmaPath(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return string.Empty;

        var nombreBase = email.Trim().Replace("@", "_");
        foreach (var ext in ExtensionesPermitidas)
        {
            var path = Path.Combine(CarpetaFirmas, nombreBase + ext);
            if (File.Exists(path)) return path;
        }
        return string.Empty;
    }

    /// <summary>
    /// Copia la imagen de <paramref name="sourcePath"/> como firma del email.
    /// Elimina cualquier firma previa del mismo email.
    /// </summary>
    /// <returns>Ruta destino de la firma guardada.</returns>
    public static async Task<string> GuardarFirmaAsync(string email, string sourcePath)
    {
        if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("Email vacío.", nameof(email));
        if (!File.Exists(sourcePath)) throw new FileNotFoundException("Archivo de firma no encontrado.", sourcePath);

        var ext = Path.GetExtension(sourcePath).ToLowerInvariant();
        if (Array.IndexOf(ExtensionesPermitidas, ext) < 0)
            throw new InvalidOperationException($"Formato de imagen no admitido: {ext}. Use PNG, JPG o GIF.");

        Directory.CreateDirectory(CarpetaFirmas);

        // Eliminar firmas previas del mismo email
        var nombreBase = email.Trim().Replace("@", "_");
        foreach (var ext2 in ExtensionesPermitidas)
        {
            var old = Path.Combine(CarpetaFirmas, nombreBase + ext2);
            if (File.Exists(old)) File.Delete(old);
        }

        var destino = Path.Combine(CarpetaFirmas, nombreBase + ext);
        await Task.Run(() => File.Copy(sourcePath, destino, overwrite: true));
        return destino;
    }

    /// <summary>Elimina la firma del email dado si existe.</summary>
    public static void EliminarFirma(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return;

        var nombreBase = email.Trim().Replace("@", "_");
        foreach (var ext in ExtensionesPermitidas)
        {
            var path = Path.Combine(CarpetaFirmas, nombreBase + ext);
            if (File.Exists(path)) File.Delete(path);
        }
    }

    /// <summary>
    /// Devuelve el fragmento HTML de la firma usando referencia CID (compatible con todos los clientes de correo).
    /// Usar junto con <see cref="EmailMessage.FirmaImagePath"/> para que el servicio adjunte la imagen.
    /// </summary>
    public static string GetFirmaCidHtml() =>
        "<br/><br/><img src=\"cid:email-firma\" style=\"max-width:450px;height:auto;display:block;\" alt=\"Firma\"/>";

    /// <summary>
    /// Devuelve el fragmento HTML de la firma (imagen base64 inline).
    /// NOTA: Muchos clientes de correo bloquean data URIs. Preferir <see cref="GetFirmaCidHtml"/> con CID.
    /// </summary>
    [Obsolete("Usar GetFirmaCidHtml() + EmailMessage.FirmaImagePath para máxima compatibilidad.")]
    public static async Task<string> GetFirmaHtmlAsync(string email)
    {
        var firmaPath = GetFirmaPath(email);
        if (string.IsNullOrEmpty(firmaPath)) return string.Empty;

        var ext = Path.GetExtension(firmaPath).TrimStart('.').ToLowerInvariant();
        var mimeType = (ext == "jpg" || ext == "jpeg") ? "jpeg" : ext;

        var bytes = await Task.Run(() => File.ReadAllBytes(firmaPath));
        var base64 = Convert.ToBase64String(bytes);

        return $"<br/><br/><img src=\"data:image/{mimeType};base64,{base64}\" style=\"max-width:450px;height:auto;\" alt=\"Firma\"/>";
    }
}
