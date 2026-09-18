using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;

namespace Advance_Control.Services.Suscripciones
{
    /// <summary>
    /// Servicio para gestionar contratos de suscripción (Oro/Plata/Bronce)
    /// </summary>
    public interface IContratoSuscripcionService
    {
        /// <summary>
        /// Obtiene los contratos de suscripción de un cliente
        /// </summary>
        Task<List<ContratoSuscripcionDto>> GetContratosAsync(int idCliente, CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtiene un contrato de suscripción por su ID
        /// </summary>
        Task<ContratoSuscripcionDto?> GetContratoByIdAsync(int idContrato, CancellationToken cancellationToken = default);

        /// <summary>
        /// Crea un nuevo contrato de suscripción
        /// </summary>
        Task<ContratoSuscripcionDto?> CreateContratoAsync(
            int idCliente,
            string nivel,
            string? numeroContrato,
            string? direccionInstalacion,
            decimal montoMensual,
            int numeroUnidades,
            System.DateTime vigenciaInicio,
            System.DateTime vigenciaFin,
            string? nombreFirmante,
            string? telefonoFirmante,
            System.DateTime? fechaFirma,
            string? pdfGeneradoUrl,
            int[] idsEquipos,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Actualiza la URL del PDF generado de un contrato existente
        /// </summary>
        Task<bool> ActualizarPdfGeneradoAsync(int idContrato, string pdfGeneradoUrl, CancellationToken cancellationToken = default);

        /// <summary>
        /// Marca un contrato como firmado, registrando la URL del documento firmado escaneado
        /// </summary>
        Task<bool> MarcarFirmadoAsync(int idContrato, string pdfFirmadoUrl, CancellationToken cancellationToken = default);
    }
}
