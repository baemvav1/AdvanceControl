using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;

namespace Advance_Control.Services.Equipos
{
    /// <summary>
    /// Servicio para gestionar operaciones con equipos
    /// </summary>
    public interface IEquipoService
    {
        /// <summary>
        /// Obtiene una lista de equipos según los criterios de búsqueda proporcionados
        /// </summary>
        /// <param name="query">Parámetros de búsqueda opcionales</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Lista de equipos que cumplen con los criterios</returns>
        Task<List<EquipoDto>> GetEquiposAsync(EquipoQueryDto? query = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Elimina (soft delete) un equipo por su ID
        /// </summary>
        /// <param name="id">ID del equipo a eliminar</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Resultado de la operación</returns>
        Task<bool> DeleteEquipoAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Actualiza un equipo existente
        /// </summary>
        /// <param name="id">ID del equipo a actualizar</param>
        /// <param name="query">Datos a actualizar</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Resultado de la operación</returns>
        Task<bool> UpdateEquipoAsync(int id, EquipoQueryDto query, CancellationToken cancellationToken = default);

        /// <summary>
        /// Crea un nuevo equipo
        /// </summary>
        /// <param name="marca">Marca del equipo (obligatorio)</param>
        /// <param name="creado">Año de creación (opcional, default 0)</param>
        /// <param name="paradas">Número de paradas (opcional, default 0)</param>
        /// <param name="kilogramos">Capacidad en kilogramos (opcional, default 0)</param>
        /// <param name="personas">Capacidad de personas (opcional, default 0)</param>
        /// <param name="descripcion">Descripción del equipo (opcional)</param>
        /// <param name="identificador">Identificador único del equipo (obligatorio)</param>
        /// <param name="estatus">Estatus del equipo (opcional, default true)</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Resultado de la operación</returns>
        Task<bool> CreateEquipoAsync(string marca, int creado = 0, int paradas = 0, int kilogramos = 0, int personas = 0, string? descripcion = null, string identificador = "", bool estatus = true, CancellationToken cancellationToken = default);
    }
}
