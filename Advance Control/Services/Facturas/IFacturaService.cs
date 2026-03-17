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
        Task<FacturaDetalleDto?> ObtenerDetalleFacturaAsync(int idFactura, CancellationToken cancellationToken = default);
        Task<RegistrarAbonoFacturaResponseDto> RegistrarAbonoAsync(RegistrarAbonoFacturaRequestDto request, CancellationToken cancellationToken = default);
    }
}
