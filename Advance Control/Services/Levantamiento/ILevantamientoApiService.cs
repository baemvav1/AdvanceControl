using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Advance_Control.Services.Levantamiento
{
    public interface ILevantamientoApiService
    {
        Task<LevantamientoResultResponse?> CrearLevantamientoAsync(
            LevantamientoCreateRequest request,
            CancellationToken cancellationToken = default);

        Task<LevantamientoResultResponse?> ActualizarLevantamientoAsync(
            LevantamientoUpdateRequest request,
            CancellationToken cancellationToken = default);

        Task<LevantamientoDetailResponse?> ObtenerLevantamientoAsync(
            int idLevantamiento,
            CancellationToken cancellationToken = default);

        Task<List<LevantamientoListItemResponse>> ListarLevantamientosAsync(
            CancellationToken cancellationToken = default);

        Task<bool> EliminarLevantamientoAsync(
            int idLevantamiento,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Request para crear un levantamiento (se envia como JSON body).
    /// </summary>
    public class LevantamientoCreateRequest
    {
        public int IdEquipo { get; set; }
        public string? Introduccion { get; set; }
        public string? Conclusion { get; set; }
        public string TipoConfiguracion { get; set; } = "ElevadorDeTraccion";
        public List<LevantamientoNodoRequest> Nodos { get; set; } = new();
    }

    /// <summary>
    /// Request para actualizar un levantamiento existente.
    /// </summary>
    public class LevantamientoUpdateRequest
    {
        public int IdLevantamiento { get; set; }
        public string? Introduccion { get; set; }
        public string? Conclusion { get; set; }
        public string TipoConfiguracion { get; set; } = "ElevadorDeTraccion";
        public List<LevantamientoNodoRequest> Nodos { get; set; } = new();
    }

    /// <summary>
    /// Nodo del arbol de levantamiento (recursivo).
    /// </summary>
    public class LevantamientoNodoRequest
    {
        public string Clave { get; set; } = string.Empty;
        public string Etiqueta { get; set; } = string.Empty;
        public string? DescripcionFalla { get; set; }
        public bool TieneFalla { get; set; }
        public List<LevantamientoNodoRequest> Hijos { get; set; } = new();
    }

    /// <summary>
    /// Respuesta del servidor al crear un levantamiento.
    /// </summary>
    public class LevantamientoResultResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int IdLevantamiento { get; set; }
    }

    public class LevantamientoListItemResponse
    {
        public int IdLevantamiento { get; set; }
        public int IdEquipo { get; set; }
        public int CredencialId { get; set; }
        public string? Introduccion { get; set; }
        public string? Conclusion { get; set; }
        public string? TipoConfiguracion { get; set; }
        public DateTime FechaCreacion { get; set; }
        public string? EquipoIdentificador { get; set; }
        public string? EquipoMarca { get; set; }
    }

    /// <summary>
    /// Respuesta de detalle de un levantamiento con nodos jerarquicos.
    /// </summary>
    public class LevantamientoDetailResponse
    {
        public int IdLevantamiento { get; set; }
        public int IdEquipo { get; set; }
        public int CredencialId { get; set; }
        public string? Introduccion { get; set; }
        public string? Conclusion { get; set; }
        public string? TipoConfiguracion { get; set; }
        public DateTime FechaCreacion { get; set; }
        public string? EquipoIdentificador { get; set; }
        public string? EquipoMarca { get; set; }
        public List<LevantamientoNodoDetailResponse> Nodos { get; set; } = new();
    }

    /// <summary>
    /// Nodo de levantamiento con hijos (recursivo).
    /// </summary>
    public class LevantamientoNodoDetailResponse
    {
        public int IdLevantamientoNodo { get; set; }
        public int? IdNodoPadre { get; set; }
        public string Clave { get; set; } = string.Empty;
        public string Etiqueta { get; set; } = string.Empty;
        public string? DescripcionFalla { get; set; }
        public bool TieneFalla { get; set; }
        public int Orden { get; set; }
        public List<LevantamientoNodoDetailResponse> Hijos { get; set; } = new();
    }
}
