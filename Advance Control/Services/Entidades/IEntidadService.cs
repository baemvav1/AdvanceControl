using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;

namespace Advance_Control.Services.Entidades
{
    /// <summary>
    /// Servicio para gestionar operaciones con entidades
    /// </summary>
    public interface IEntidadService
    {
        /// <summary>
        /// Obtiene una lista de entidades según los criterios de búsqueda proporcionados
        /// </summary>
        /// <param name="query">Parámetros de búsqueda opcionales</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Lista de entidades que cumplen con los criterios</returns>
        Task<List<EntidadDto>> GetEntidadesAsync(EntidadQueryDto? query = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Crea una nueva entidad
        /// </summary>
        /// <param name="nombreComercial">Nombre comercial (obligatorio)</param>
        /// <param name="razonSocial">Razón social (obligatorio)</param>
        /// <param name="rfc">RFC (opcional)</param>
        /// <param name="cp">Código postal (opcional)</param>
        /// <param name="estado">Estado (opcional)</param>
        /// <param name="ciudad">Ciudad (opcional)</param>
        /// <param name="pais">País (opcional)</param>
        /// <param name="calle">Calle (opcional)</param>
        /// <param name="numExt">Número exterior (opcional)</param>
        /// <param name="numInt">Número interior (opcional)</param>
        /// <param name="colonia">Colonia (opcional)</param>
        /// <param name="apoderado">Apoderado (opcional)</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Resultado de la operación</returns>
        Task<ApiResponse> CreateEntidadAsync(
            string nombreComercial,
            string razonSocial,
            string? rfc = null,
            string? cp = null,
            string? estado = null,
            string? ciudad = null,
            string? pais = null,
            string? calle = null,
            string? numExt = null,
            string? numInt = null,
            string? colonia = null,
            string? apoderado = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Actualiza una entidad por su ID
        /// </summary>
        /// <param name="id">ID de la entidad</param>
        /// <param name="nombreComercial">Nombre comercial (opcional)</param>
        /// <param name="razonSocial">Razón social (opcional)</param>
        /// <param name="rfc">RFC (opcional)</param>
        /// <param name="cp">Código postal (opcional)</param>
        /// <param name="estado">Estado (opcional)</param>
        /// <param name="ciudad">Ciudad (opcional)</param>
        /// <param name="pais">País (opcional)</param>
        /// <param name="calle">Calle (opcional)</param>
        /// <param name="numExt">Número exterior (opcional)</param>
        /// <param name="numInt">Número interior (opcional)</param>
        /// <param name="colonia">Colonia (opcional)</param>
        /// <param name="apoderado">Apoderado (opcional)</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Resultado de la operación</returns>
        Task<ApiResponse> UpdateEntidadAsync(
            int id,
            string? nombreComercial = null,
            string? razonSocial = null,
            string? rfc = null,
            string? cp = null,
            string? estado = null,
            string? ciudad = null,
            string? pais = null,
            string? calle = null,
            string? numExt = null,
            string? numInt = null,
            string? colonia = null,
            string? apoderado = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Elimina una entidad por su ID
        /// </summary>
        /// <param name="id">ID de la entidad a eliminar</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Resultado de la operación</returns>
        Task<ApiResponse> DeleteEntidadAsync(int id, CancellationToken cancellationToken = default);
    }
}
