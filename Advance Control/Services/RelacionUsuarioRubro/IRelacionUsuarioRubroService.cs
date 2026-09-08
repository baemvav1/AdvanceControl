using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;

namespace Advance_Control.Services.RelacionUsuarioRubro
{
    public interface IRelacionUsuarioRubroService
    {
        /// <summary>
        /// Obtiene el catálogo de rubros activos (Elevadores, Inmuebles, ...)
        /// </summary>
        Task<List<RubroDto>> GetRubrosAsync(CancellationToken cancellationToken = default);

        Task<List<RelacionUsuarioRubroDto>> GetRelacionesPorUsuarioAsync(long credencialId, CancellationToken cancellationToken = default);
        Task<RelacionUsuarioRubroDto?> CreateRelacionAsync(long credencialId, int idRubro, CancellationToken cancellationToken = default);
        Task<bool> DeleteRelacionAsync(int id, CancellationToken cancellationToken = default);
    }
}
