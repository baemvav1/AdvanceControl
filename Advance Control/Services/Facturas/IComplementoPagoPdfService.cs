using System.Threading.Tasks;
using Advance_Control.Models;

namespace Advance_Control.Services.Facturas
{
    /// <summary>
    /// Genera la representación impresa de un Complemento de Pago CFDI: encabezado propio,
    /// emisor/receptor, los pagos y documentos relacionados que documenta, timbre fiscal
    /// (UUID, sellos, cadena original) y código QR de verificación del SAT.
    /// </summary>
    public interface IComplementoPagoPdfService
    {
        /// <summary>Genera el PDF a partir del detalle completo de un Complemento de Pago.</summary>
        /// <returns>La ruta del archivo PDF generado.</returns>
        Task<string> GenerarComplementoPagoPdfAsync(ComplementoPagoDetalleDto detalle);
    }
}
