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
            string? regimenFiscal = null,
            bool? esPredeterminada = null,
            string? serieFacturas = null,
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
            string? regimenFiscal = null,
            bool? esPredeterminada = null,
            string? serieFacturas = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Elimina una entidad por su ID
        /// </summary>
        /// <param name="id">ID de la entidad a eliminar</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Resultado de la operación</returns>
        Task<ApiResponse> DeleteEntidadAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtiene la primera entidad activa (estatus = true)
        /// </summary>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>La primera entidad activa o null si no existe</returns>
        Task<EntidadDto?> GetActiveEntidadAsync(CancellationToken cancellationToken = default);

        /// <summary>Entidad activa con ese RFC exacto (p. ej. el emisor de una factura); null si no hay.</summary>
        Task<EntidadDto?> GetEntidadPorRfcAsync(string? rfc, CancellationToken cancellationToken = default);

        /// <summary>Cuentas bancarias de una entidad, o de todas si idEntidad es null.</summary>
        Task<List<EntidadCuentaBancariaDto>> GetCuentasBancariasAsync(int? idEntidad = null, CancellationToken cancellationToken = default);

        Task<ApiResponse> CrearCuentaBancariaAsync(int idEntidad, string numeroCuenta, string? clabe, string? banco, CancellationToken cancellationToken = default);

        Task<ApiResponse> EliminarCuentaBancariaAsync(int idCuenta, CancellationToken cancellationToken = default);

        /// <summary>
        /// Entidad (razón social) que atiende a un cliente: la última con la que se le timbró o,
        /// si nunca, la predeterminada. Para encabezados de cotizaciones, reportes e historiales.
        /// </summary>
        Task<EntidadDto?> GetEntidadParaClienteAsync(int? idCliente = null, int? idOperacion = null, CancellationToken cancellationToken = default);

        /// <summary>Entidad de facturación de todos los clientes activos (guardada o predeterminada).</summary>
        Task<List<EntidadFacturacionClienteDto>> GetEntidadesFacturacionClientesAsync(CancellationToken cancellationToken = default);

        /// <summary>Entidad con la que se le factura a un cliente (la guardada al timbrar, o la predeterminada).</summary>
        Task<EntidadFacturacionClienteDto?> GetEntidadFacturacionClienteAsync(int? idCliente = null, int? idOperacion = null, int? idContrato = null, string? rfc = null, CancellationToken cancellationToken = default);
    }
}
