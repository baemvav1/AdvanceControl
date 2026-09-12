using System.Threading.Tasks;

namespace Advance_Control.Services.Quotes
{
    /// <summary>
    /// Interfaz para el servicio de generación del PDF de Mantenimiento Preventivo.
    /// A diferencia de cotización/reporte/nota (datos de cargos), toma directamente
    /// el estado del formulario visual (<see cref="MantenimientoPreventivoPdfData"/>).
    /// </summary>
    public interface IMantenimientoPreventivoPdfService
    {
        /// <summary>
        /// Genera el PDF a partir de los datos capturados en el formulario y lo guarda
        /// localmente en Documents\Advance Control\Operacion_{idOperacion}\.
        /// </summary>
        /// <returns>La ruta del archivo PDF generado.</returns>
        Task<string> GeneratePdfAsync(MantenimientoPreventivoPdfData datos);
    }
}
