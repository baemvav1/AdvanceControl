using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;

namespace Advance_Control.Services.MantenimientoPreventivo
{
    /// <summary>
    /// Persistencia de hojas de mantenimiento preventivo (empleado/técnico).
    /// </summary>
    public interface IHojaMantenimientoService
    {
        /// <summary>Hojas de una operación, más reciente primero (null si hubo error de red).</summary>
        Task<List<MantenimientoPreventivoHojaDto>?> ObtenerPorOperacionAsync(int idOperacion, CancellationToken cancellationToken = default);

        Task<MantenimientoPreventivoHojaDto?> CrearAsync(MantenimientoPreventivoGuardarRequestDto request, CancellationToken cancellationToken = default);

        Task<MantenimientoPreventivoHojaDto?> ActualizarAsync(long id, MantenimientoPreventivoGuardarRequestDto request, CancellationToken cancellationToken = default);
    }
}
