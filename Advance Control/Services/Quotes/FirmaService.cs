using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using global::Windows.Storage;
namespace Advance_Control.Services.Quotes
{
    /// <summary>
    /// Implementación de <see cref="IFirmaService"/> que gestiona firmas como
    /// archivos locales en Documents\Advance Control\Firmas\.
    /// </summary>
    public class FirmaService : IFirmaService
    {
        /// <inheritdoc/>
        public string GetFirmasFolder()
        {
            var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            return Path.Combine(docs, "Advance Control", "Firmas");
        }

        /// <inheritdoc/>
        public string GetFirmaDireccionPath()
            => Path.Combine(GetFirmasFolder(), "FirmaDireccion.png");

        /// <inheritdoc/>
        public string? GetFirmaOperadorPath(int idAtiende)
        {
            var folder = GetFirmasFolder();
            if (!Directory.Exists(folder))
                return null;

            foreach (var file in Directory.EnumerateFiles(folder, "*.png"))
            {
                var nombre = Path.GetFileNameWithoutExtension(file);
                var partes = nombre.Split('_');
                if (partes.Length >= 2 && int.TryParse(partes[0], out int fileId) && fileId == idAtiende)
                    return file;
            }
            return null;
        }

        /// <inheritdoc/>
        public bool ExisteFirmaDireccion()
            => File.Exists(GetFirmaDireccionPath());

        /// <inheritdoc/>
        public bool ExisteFirmaOperador(int idAtiende)
            => GetFirmaOperadorPath(idAtiende) != null;

        /// <inheritdoc/>
        public Task GuardarFirmaDireccionAsync(StorageFile archivo)
        {
            var folder = GetFirmasFolder();
            Directory.CreateDirectory(folder);
            File.Copy(archivo.Path, GetFirmaDireccionPath(), overwrite: true);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task GuardarFirmaOperadorAsync(int idAtiende, string nombre, StorageFile archivo)
        {
            var folder = GetFirmasFolder();
            Directory.CreateDirectory(folder);

            // Eliminar firmas previas del mismo operador
            foreach (var old in Directory.EnumerateFiles(folder, $"{idAtiende}_*.png").ToList())
            {
                try { File.Delete(old); } catch { /* ignorar si está en uso */ }
            }

            var nombreLimpio = string.Concat(nombre.Split(Path.GetInvalidFileNameChars()));
            var destino = Path.Combine(folder, $"{idAtiende}_{nombreLimpio}.png");
            File.Copy(archivo.Path, destino, overwrite: true);
            return Task.CompletedTask;
        }
    }
}
