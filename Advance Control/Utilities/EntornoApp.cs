using System;
using System.IO;

namespace Advance_Control.Utilities
{
    /// <summary>
    /// Entorno al que se conecta Advance Control: Producción (por omisión) o Pruebas
    /// (https://advance-elevadores.mx/pruebas/, BD de pruebas, sin timbrado). Se elige en el
    /// login y se guarda en %LOCALAPPDATA%\Advance Control\entorno.txt; como la URL de la API
    /// se fija al arrancar, cambiar de entorno reinicia la aplicación.
    /// En Pruebas también se separan las sesiones guardadas y la carpeta de Documentos, para
    /// que los ids de pruebas nunca se mezclen con archivos de operaciones reales.
    /// </summary>
    public static class EntornoApp
    {
        private static readonly string Archivo = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Advance Control",
            "entorno.txt");

        /// <summary>Entorno con el que arrancó la aplicación.</summary>
        public static bool EsPruebas { get; } = Leer();

        /// <summary>Carpeta dentro de Documentos donde se guardan PDFs, fotos y firmas.</summary>
        public static string CarpetaDocumentos => EsPruebas ? "Advance Control (Pruebas)" : "Advance Control";

        public static string Nombre => EsPruebas ? "Pruebas" : "Producción";

        /// <summary>Guarda el entorno para el siguiente arranque.</summary>
        public static void Guardar(bool pruebas)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Archivo)!);
            File.WriteAllText(Archivo, pruebas ? "Pruebas" : "Produccion");
        }

        private static bool Leer()
        {
            try
            {
                return File.Exists(Archivo)
                       && string.Equals(File.ReadAllText(Archivo).Trim(), "Pruebas", StringComparison.OrdinalIgnoreCase);
            }
            catch (IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }
    }
}
