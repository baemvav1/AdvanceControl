using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;

namespace Advance_Control.Services.SatCatalogo
{
    /// <summary>Búsqueda y alta de claves de los catálogos SAT c_ClaveProdServ / c_ClaveUnidad.</summary>
    public interface ISatCatalogoService
    {
        Task<List<SatClaveDto>> BuscarClaveProdServAsync(string? texto, CancellationToken cancellationToken = default);
        Task<SatClaveDto?> AgregarClaveProdServAsync(string clave, string descripcion, CancellationToken cancellationToken = default);

        Task<List<SatClaveDto>> BuscarClaveUnidadAsync(string? texto, CancellationToken cancellationToken = default);
        Task<SatClaveDto?> AgregarClaveUnidadAsync(string clave, string nombre, CancellationToken cancellationToken = default);

        Task<List<SatCatalogoItemDto>> ListarRegimenFiscalAsync(bool incluirInactivos = false, CancellationToken cancellationToken = default);
        Task<SatCatalogoItemDto?> GuardarRegimenFiscalAsync(SatCatalogoItemDto item, CancellationToken cancellationToken = default);
        Task<bool> InactivarRegimenFiscalAsync(string clave, bool estatus, CancellationToken cancellationToken = default);

        Task<List<SatCatalogoItemDto>> ListarUsoCfdiAsync(bool incluirInactivos = false, CancellationToken cancellationToken = default);
        Task<SatCatalogoItemDto?> GuardarUsoCfdiAsync(SatCatalogoItemDto item, CancellationToken cancellationToken = default);
        Task<bool> InactivarUsoCfdiAsync(string clave, bool estatus, CancellationToken cancellationToken = default);
    }
}
