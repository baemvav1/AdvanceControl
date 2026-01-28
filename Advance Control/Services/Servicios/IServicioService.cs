using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;

namespace Advance_Control.Services.Servicios
{
    /// <summary>
    /// Interfaz para el servicio de servicios
    /// </summary>
    public interface IServicioService
    {
        /// <summary>
        /// Obtiene una lista de servicios según los criterios de búsqueda proporcionados
        /// </summary>
        /// <param name="query">Parámetros de consulta opcionales
        /// <param name="cancellationToken">Token de cancelación
        /// <returns>Lista de servicios</returns>
        Task<List<ServicioDto>> GetServiciosAsync(ServicioQueryDto? query = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Elimina (soft delete) un servicio por su ID
        /// </summary>
        /// <param name="id">ID del servicio a eliminar
        /// <param name="cancellationToken">Token de cancelación
        /// <returns>True si se eliminó correctamente, false en caso contrario</returns>
        Task<bool> DeleteServicioAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Actualiza un servicio existente
        /// </summary>
        /// <param name="id">ID del servicio a actualizar
        /// <param name="query">Datos del servicio a actualizar
        /// <param name="cancellationToken">Token de cancelación
        /// <returns>True si se actualizó correctamente, false en caso contrario</returns>
        Task<bool> UpdateServicioAsync(int id, ServicioQueryDto query, CancellationToken cancellationToken = default);

        /// <summary>
        /// Crea un nuevo servicio
        /// </summary>
        /// <param name="concepto">Concepto del servicio (obligatorio)
        /// <param name="descripcion">Descripción del servicio (obligatorio)
        /// <param name="costo">Costo del servicio (obligatorio)
        /// <param name="estatus">Estatus del servicio (opcional, default true)
        /// <param name="cancellationToken">Token de cancelación
        /// <returns>True si se creó correctamente, false en caso contrario</returns>
        Task<bool> CreateServicioAsync(string concepto, string descripcion, double costo, bool estatus = true, CancellationToken cancellationToken = default);
    }
}
