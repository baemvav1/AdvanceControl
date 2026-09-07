using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;

namespace Advance_Control.Services.Facturas
{
    public interface IFacturaService
    {
        Task<GuardarFacturaResponseDto> GuardarFacturaAsync(GuardarFacturaRequestDto request, CancellationToken cancellationToken = default);
        Task<List<FacturaResumenDto>> ObtenerFacturasAsync(CancellationToken cancellationToken = default);
        Task<FacturaResumenDto?> BuscarFacturaPorFolioAsync(string folio, CancellationToken cancellationToken = default);
        Task<FacturaDetalleDto?> ObtenerDetalleFacturaAsync(int idFactura, CancellationToken cancellationToken = default);
        Task<string?> ObtenerXmlFacturaAsync(int idFactura, CancellationToken cancellationToken = default);
        Task<RegistrarAbonoFacturaResponseDto> RegistrarAbonoAsync(RegistrarAbonoFacturaRequestDto request, CancellationToken cancellationToken = default);
        Task<BitacoraConciliacionResponseDto> InicializarBitacoraConciliacionAsync(CancellationToken cancellationToken = default);
        Task<BitacoraConciliacionResponseDto> DeshacerUltimaOperacionConciliacionAsync(CancellationToken cancellationToken = default);
        Task<BitacoraConciliacionResponseDto> DeshacerTodasOperacionesConciliacionAsync(CancellationToken cancellationToken = default);
        Task<List<OperacionSinFacturaDto>> ObtenerOperacionesSinFacturaAsync(CancellationToken cancellationToken = default);
        Task<List<OperacionFacturadaDto>> ObtenerOperacionesFacturadasAsync(CancellationToken cancellationToken = default);
        Task<CancelarFacturaOperacionResponseDto> CancelarFacturaOperacionAsync(int idOperacion, CancellationToken cancellationToken = default);
        Task VincularFacturaOperacionAsync(int idFactura, int idOperacion, CancellationToken cancellationToken = default);
        Task<CancelarFacturaOperacionResponseDto> DesvincularFacturaOperacionAsync(int idOperacion, CancellationToken cancellationToken = default);

        /// <summary>Construye, sella y timbra un CFDI 4.0 directamente para una operación vía FEL Bilkon.</summary>
        Task<TimbrarResultadoDto> TimbrarOperacionAsync(int idOperacion, CfdiTimbrarRequestDto request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Cancela un CFDI ya timbrado (Serie+Folio) ante el SAT vía FEL Bilkon. Operación
        /// irreversible y sin ambiente de pruebas -- consume 1 timbre si Bilkon confirma código 201.
        /// </summary>
        Task<CancelarCfdiResponseDto> CancelarCfdiAsync(int idFactura, CancelarCfdiRequestDto request, CancellationToken cancellationToken = default);
    }
}
