using System.Threading.Tasks;
using global::Windows.Storage;

namespace Advance_Control.Services.Quotes
{
    /// <summary>
    /// Gestiona los archivos de imagen de firma usados en los PDFs de cotización y reporte.
    /// Las firmas se almacenan localmente en Documents\Advance Control\Firmas\.
    /// </summary>
    public interface IFirmaService
    {
        /// <summary>Ruta de la carpeta local de firmas.</summary>
        string GetFirmasFolder();

        /// <summary>Ruta absoluta donde se espera FirmaDireccion.png (exista o no).</summary>
        string GetFirmaDireccionPath();

        /// <summary>
        /// Ruta absoluta de la firma del operador si existe, o null si no se encontró.
        /// Los archivos tienen el formato {idAtiende}_{nombre}.png.
        /// </summary>
        string? GetFirmaOperadorPath(int idAtiende);

        /// <summary>Indica si existe el archivo FirmaDireccion.png.</summary>
        bool ExisteFirmaDireccion();

        /// <summary>Indica si existe algún archivo de firma para el operador dado.</summary>
        bool ExisteFirmaOperador(int idAtiende);

        /// <summary>
        /// Copia el archivo seleccionado como FirmaDireccion.png en la carpeta de firmas.
        /// Sobreescribe si ya existe.
        /// </summary>
        Task GuardarFirmaDireccionAsync(StorageFile archivo);

        /// <summary>
        /// Copia el archivo seleccionado como {idAtiende}_{nombre}.png en la carpeta de firmas.
        /// Elimina cualquier firma anterior del mismo operador antes de guardar.
        /// </summary>
        Task GuardarFirmaOperadorAsync(int idAtiende, string nombre, StorageFile archivo);
    }
}
