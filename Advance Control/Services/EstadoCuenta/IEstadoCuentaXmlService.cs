using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;

namespace Advance_Control.Services.EstadoCuenta
{
    public interface IEstadoCuentaXmlService
    {
        Task<GuardarEstadoCuentaResponseDto> GuardarEstadoCuentaAsync(GuardarEstadoCuentaRequestDto request, CancellationToken cancellationToken = default);
    }
}
