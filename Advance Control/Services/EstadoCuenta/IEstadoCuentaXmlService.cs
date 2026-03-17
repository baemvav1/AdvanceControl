using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;
using System.Collections.Generic;

namespace Advance_Control.Services.EstadoCuenta
{
    public interface IEstadoCuentaXmlService
    {
        Task<GuardarEstadoCuentaResponseDto> GuardarEstadoCuentaAsync(GuardarEstadoCuentaRequestDto request, CancellationToken cancellationToken = default);
        Task<List<EstadoCuentaResumenDto>> ObtenerEstadosCuentaAsync(CancellationToken cancellationToken = default);
        Task<EstadoCuentaDetalleDto?> ObtenerDetalleEstadoCuentaAsync(int idEstadoCuenta, CancellationToken cancellationToken = default);
        Task<ConciliacionAutomaticaResponseDto> ConciliarAutomaticamenteAsync(ConciliacionAutomaticaRequestDto request, CancellationToken cancellationToken = default);
    }
}
