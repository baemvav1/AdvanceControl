using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;

namespace Advance_Control.Services.RelacionUsuarioArea
{
    public interface IRelacionUsuarioAreaService
    {
        Task<List<RelacionUsuarioAreaDto>> GetRelacionesPorUsuarioAsync(long credencialId, CancellationToken cancellationToken = default);
        Task<List<RelacionUsuarioAreaDto>> GetRelacionesPorAreaAsync(int idArea, CancellationToken cancellationToken = default);
        Task<RelacionUsuarioAreaDto?> CreateRelacionAsync(long credencialId, int idArea, string? nota = null, CancellationToken cancellationToken = default);
        Task<bool> DeleteRelacionAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtiene los identificadores de equipos ubicados en las áreas asignadas al usuario
        /// </summary>
        Task<List<string>> GetEquiposEnAreasAsync(long credencialId, CancellationToken cancellationToken = default);
    }
}
