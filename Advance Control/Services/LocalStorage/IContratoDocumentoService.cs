using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Advance_Control.Services.LocalStorage
{
    /// <summary>
    /// Servicio para subir los documentos PDF de un contrato de suscripción
    /// (generado por el sistema y firmado escaneado) al almacenamiento del VPS.
    /// </summary>
    public interface IContratoDocumentoService
    {
        /// <summary>
        /// Sube el PDF del contrato generado por el sistema.
        /// </summary>
        /// <returns>URL absoluta del archivo subido, o null si falló</returns>
        Task<string?> SubirGeneradoAsync(int idContrato, Stream pdfStream, string contentType, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sube el PDF del documento firmado escaneado por el cliente.
        /// </summary>
        /// <returns>URL absoluta del archivo subido, o null si falló</returns>
        Task<string?> SubirFirmadoAsync(int idContrato, Stream pdfStream, string contentType, CancellationToken cancellationToken = default);
    }
}
