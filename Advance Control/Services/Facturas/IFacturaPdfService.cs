using System.Threading.Tasks;
using Advance_Control.Models;

namespace Advance_Control.Services.Facturas
{
    /// <summary>
    /// Genera la representación impresa oficial de una factura (CFDI) ya cargada en el
    /// sistema: encabezado propio, datos de emisor/receptor, conceptos, impuestos,
    /// timbre fiscal (UUID, sellos, cadena original) y código QR de verificación del SAT.
    /// </summary>
    public interface IFacturaPdfService
    {
        /// <summary>
        /// Genera el PDF a partir del detalle completo de una factura (factura + conceptos +
        /// traslados). Requiere que el detalle venga de ObtenerDetalleFacturaAsync, que es el
        /// único que trae los sellos (Sello/SelloCfd/SelloSat) necesarios para el QR.
        /// </summary>
        /// <returns>La ruta del archivo PDF generado.</returns>
        Task<string> GenerarFacturaPdfAsync(FacturaDetalleDto detalle);
    }
}
