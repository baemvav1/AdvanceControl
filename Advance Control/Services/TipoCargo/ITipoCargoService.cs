using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;

namespace Advance_Control.Services.TipoCargo
{
    /// <summary>Claves SAT y tasa de IVA recordadas por tipo de cargo (Refacción/Servicio).</summary>
    public interface ITipoCargoService
    {
        Task<List<TipoCargoDefaultDto>> ObtenerDefaultsAsync(CancellationToken cancellationToken = default);
        Task<bool> GuardarDefaultAsync(int id, string? claveProdServ, string? claveUnidad, decimal? tasaIva, CancellationToken cancellationToken = default);
    }
}
