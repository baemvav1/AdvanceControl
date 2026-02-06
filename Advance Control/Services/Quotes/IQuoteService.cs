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
        /// <param name="nombreEmpresa">Nombre comercial de la empresa (opcional, usa nombre por defecto si es null)</param>
        /// <returns>La ruta del archivo PDF generado</returns>
        Task<string> GenerateQuotePdfAsync(OperacionDto operacion, IEnumerable<CargoDto> cargos, string? ubicacionNombre = null, string? nombreEmpresa = null);

        /// <summary>
        /// Genera un PDF de reporte de cotización con fotos de cargos
        /// </summary>
        /// <param name="operacion">La operación que contiene la información</param>
        /// <param name="cargos">Lista de cargos a incluir en el reporte</param>
        /// <returns>La ruta del archivo PDF generado</returns>
        Task<string> GenerateReportePdfAsync(OperacionDto operacion, IEnumerable<CargoDto> cargos);
    }
}
