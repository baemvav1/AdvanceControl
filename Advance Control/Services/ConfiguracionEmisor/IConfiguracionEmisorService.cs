using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;

namespace Advance_Control.Services.ConfiguracionEmisor
{
    /// <summary>
    /// Servicio para consultar los datos fiscales propios (emisor) y el estado del CSD
    /// usados para timbrar CFDI directamente desde el ERP.
    /// </summary>
    public interface IConfiguracionEmisorService
    {
        /// <summary>Obtiene la configuración del emisor, o null si no hay ninguna guardada (404).</summary>
        Task<ConfiguracionEmisorDto?> ObtenerConfiguracionAsync(CancellationToken cancellationToken = default);

        /// <summary>Obtiene el estado del CSD cargado (vigente, número de certificado, etc.).</summary>
        Task<CsdEmisorEstadoDto> ObtenerEstadoCsdAsync(CancellationToken cancellationToken = default);

        /// <summary>Sube un CSD nuevo (.cer + .key + contraseña) para la entidad activa.</summary>
        Task<CsdUploadResultDto> GuardarCsdAsync(byte[] certificado, byte[] llavePrivada, string password, CancellationToken cancellationToken = default);

        /// <summary>Obtiene el estado de la FIEL/PFX cargado (vigente, número de certificado, etc.).</summary>
        Task<FielEmisorEstadoDto> ObtenerEstadoFielAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Sube una FIEL (.cer + .key + contraseña) para la entidad activa y genera el PFX de
        /// cancelación. La API exige que ya haya un CSD cargado para la misma entidad.
        /// </summary>
        Task<FielUploadResultDto> GuardarFielAsync(byte[] certificado, byte[] llavePrivada, string password, CancellationToken cancellationToken = default);
    }
}
