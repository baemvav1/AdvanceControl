using System.Collections.Generic;
using System.Threading.Tasks;
using Advance_Control.Models;

namespace Advance_Control.Services.Contratos
{
    /// <summary>
    /// Servicio para generar el PDF de un contrato de suscripción (Oro/Plata/Bronce),
    /// reproduciendo el texto legal exacto de los machotes correspondientes.
    /// </summary>
    public interface IContratoPdfService
    {
        /// <summary>
        /// Genera el PDF del contrato y lo guarda localmente en
        /// Documents\Advance Control\Contratos\Contrato_{idContrato}_{fecha}.pdf
        /// </summary>
        /// <returns>Ruta local del archivo PDF generado</returns>
        Task<string> GenerarContratoPdfAsync(
            ContratoSuscripcionDto contrato,
            string clienteRazonSocial,
            List<EquipoDto> equiposCubiertos);
    }
}
