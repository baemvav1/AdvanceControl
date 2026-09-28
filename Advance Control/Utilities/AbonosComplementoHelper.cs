using System.Collections.Generic;
using System.Linq;
using Advance_Control.Models;

namespace Advance_Control.Utilities
{
    /// <summary>
    /// fn_abonos_pendientes_complemento regresa TODOS los abonos pendientes de complementar del
    /// RFC receptor (para poder armar un complemento que cubra varias facturas pagadas con un
    /// mismo evento de pago). Pero "mismo RFC" no es suficiente justificación para mezclarlas en
    /// el mismo complemento -- solo tiene sentido agrupar facturas que se pagaron con el MISMO
    /// movimiento bancario. Sin este filtro, un cliente con muchas facturas históricas (ej.
    /// igualas mensuales, cada una pagada por separado en su propio movimiento) satura el
    /// checklist de "Generar complemento" con pagos de meses distintos que no tienen nada que ver
    /// entre sí -- ambigüedad reportada en vivo con la factura 1032.
    /// </summary>
    public static class AbonosComplementoHelper
    {
        public static List<AbonoPendienteComplementoDto> FiltrarRelevantes(
            List<AbonoPendienteComplementoDto> abonosPendientes, int idFacturaObjetivo)
        {
            var idsMovimiento = abonosPendientes
                .Where(a => a.IdFactura == idFacturaObjetivo && a.IdMovimiento.HasValue)
                .Select(a => a.IdMovimiento!.Value)
                .ToHashSet();

            return abonosPendientes
                .Where(a => a.IdFactura == idFacturaObjetivo
                    || (a.IdMovimiento.HasValue && idsMovimiento.Contains(a.IdMovimiento.Value)))
                .ToList();
        }
    }
}
