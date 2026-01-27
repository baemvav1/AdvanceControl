using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;

namespace Advance_Control.Services.Cargos
{
    /// <summary>
    /// Interfaz para el servicio de cargos
    /// </summary>
    public interface ICargoService
    {
        /// <summary>
        /// Obtiene cargos según los criterios especificados
        /// </summary>
        Task<List<CargoDto>> GetCargosAsync(CargoEditDto query, CancellationToken cancellationToken = default);

        /// <summary>
        /// Crea un nuevo cargo
        /// </summary>
        Task<CargoDto?> CreateCargoAsync(CargoEditDto query, CancellationToken cancellationToken = default);

        /// <summary>
        /// Actualiza un cargo existente
        /// </summary>
        Task<bool> UpdateCargoAsync(CargoEditDto query, CancellationToken cancellationToken = default);

        /// <summary>
        /// Elimina un cargo
        /// </summary>
        Task<bool> DeleteCargoAsync(int idCargo, CancellationToken cancellationToken = default);
    }
}
