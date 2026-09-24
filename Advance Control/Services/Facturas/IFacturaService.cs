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
        Task<FacturaResumenDto?> BuscarFacturaPorFolioAsync(string folio, string? serie = null, CancellationToken cancellationToken = default);
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
        /// Construye, sella y timbra el CFDI de la iguala mensual de un contrato de suscripción
        /// (periodo en formato "YYYY-MM"), sin operación asociada.
        /// </summary>
        Task<TimbrarResultadoDto> TimbrarIgualaMensualAsync(int idContrato, string periodo, CfdiTimbrarRequestDto request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Cancela un CFDI ya timbrado (Serie+Folio) ante el SAT vía FEL Bilkon. Operación
        /// irreversible y sin ambiente de pruebas -- consume 1 timbre si Bilkon confirma código 201.
        /// </summary>
        Task<CancelarCfdiResponseDto> CancelarCfdiAsync(int idFactura, CancelarCfdiRequestDto request, CancellationToken cancellationToken = default);

        /// <summary>Abonos de facturas PPD propias de un receptor que todavía no entraron a ningún Complemento de Pago.</summary>
        Task<List<AbonoPendienteComplementoDto>> ObtenerAbonosPendientesComplementoAsync(string receptorRfc, CancellationToken cancellationToken = default);

        /// <summary>Arma, sella y timbra vía FEL Bilkon un Complemento de Pago real a partir de abonos ya registrados.</summary>
        Task<TimbrarResultadoDto> GenerarComplementoPagoAsync(GenerarComplementoPagoRequestDto request, CancellationToken cancellationToken = default);

        /// <summary>Lista Complementos de Pago (un renglón por documento relacionado), opcionalmente solo los huérfanos.</summary>
        Task<List<ComplementoPagoResumenDto>> ObtenerComplementosPagoAsync(bool soloHuerfanos = false, CancellationToken cancellationToken = default);

        /// <summary>Detalle completo de un Complemento de Pago (para armar su PDF).</summary>
        Task<ComplementoPagoDetalleDto?> ObtenerComplementoPagoDetalleAsync(int idFacturaComplemento, CancellationToken cancellationToken = default);

        /// <summary>Doctos de Complemento de Pago que ya citan una factura propia pero sin ningún abono interno todavía (candidatos a ligar a un movimiento en Conciliación).</summary>
        Task<List<ComplementoPagoPendienteMovimientoDto>> ObtenerComplementosSinMovimientoAsync(CancellationToken cancellationToken = default);

        /// <summary>Registra el abono real a partir de un docto de Complemento de Pago y un movimiento bancario, y deja el docto ligado a ese abono.</summary>
        Task<RegistrarAbonoFacturaResponseDto> VincularComplementoMovimientoAsync(VincularComplementoMovimientoRequestDto request, CancellationToken cancellationToken = default);

        /// <summary>
        /// "Consolidar Complementos": pide a la API que detecte complementos de pago que llegaron
        /// sin parsear (timbrados a mano en el portal de Bilkon, o traídos por una recarga de FEL)
        /// y los vincule a la factura que pagan por UUID.
        /// </summary>
        Task<ComplementoPagoConsolidarResultDto> ConsolidarComplementosPagoAsync(CancellationToken cancellationToken = default);
    }
}
