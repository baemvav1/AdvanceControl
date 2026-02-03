using System.Collections.Generic;
using System.Threading.Tasks;
using Advance_Control.Models;

namespace Advance_Control.Services.Quotes
{
    /// <summary>
    /// Interfaz para el servicio de generación de cotizaciones PDF
    /// </summary>
    public interface IQuoteService
    {
        /// <summary>
        /// Genera un PDF de cotización a partir de una operación y sus cargos
        /// </summary>
        /// <param name="operacion">La operación que contiene la información del cliente y equipo</param>
        /// <param name="cargos">Lista de cargos a incluir en la cotización</param>
        /// <param name="ubicacionNombre">Nombre de la ubicación del equipo (opcional)</param>
        /// <returns>La ruta del archivo PDF generado</returns>
        Task<string> GenerateQuotePdfAsync(OperacionDto operacion, IEnumerable<CargoDto> cargos, string? ubicacionNombre = null);
    }
}
