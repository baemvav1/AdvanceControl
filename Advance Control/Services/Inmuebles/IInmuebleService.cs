using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;

namespace Advance_Control.Services.Inmuebles
{
    /// <summary>
    /// Servicio para gestionar operaciones con inmuebles
    /// </summary>
    public interface IInmuebleService
    {
        /// <summary>
        /// Obtiene una lista de inmuebles según los criterios de búsqueda proporcionados
        /// </summary>
        Task<List<InmuebleDto>> GetInmueblesAsync(InmuebleQueryDto? query = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Elimina (soft delete) un inmueble por su ID
        /// </summary>
        Task<bool> DeleteInmuebleAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Actualiza un inmueble existente
        /// </summary>
        Task<bool> UpdateInmuebleAsync(int id, InmuebleQueryDto query, CancellationToken cancellationToken = default);

        /// <summary>
        /// Crea un nuevo inmueble
        /// </summary>
        Task<bool> CreateInmuebleAsync(string? descripcion = null, string identificador = "", int? idTipoInmueble = null, double? superficieM2 = null, string? codigoPostal = null, bool estatus = true, int? idUbicacion = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sugiere el siguiente identificador numérico disponible (4 dígitos), buscando el
        /// primer hueco libre desde 0001.
        /// </summary>
        Task<string> GetSiguienteIdentificadorAsync(CancellationToken cancellationToken = default);
    }
}
