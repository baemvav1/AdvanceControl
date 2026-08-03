using System;
using System.Collections.Generic;
using System.Linq;
using Advance_Control.Models;

namespace Advance_Control.Services.Facturacion
{
    /// <summary>
    /// Sugiere facturas ya cargadas (sin operacion) como candidatas para una
    /// operacion sin factura: mismo RFC que el cliente de la operacion y mismo
    /// monto exacto -- comparado contra el SUBTOTAL de la factura (sin IVA),
    /// porque operacion.Monto tampoco incluye IVA. No hay filtro de fecha: en
    /// la practica fecha_final de la operacion no es confiable para bloquear
    /// el vinculo.
    /// </summary>
    public sealed class FacturaOperacionMatchingEngine
    {
        public IReadOnlyList<FacturaResumenDto> ObtenerCandidatos(
            OperacionSinFacturaDto operacion,
            IReadOnlyCollection<FacturaResumenDto> facturasSinOperacion)
        {
            if (operacion == null)
            {
                throw new ArgumentNullException(nameof(operacion));
            }

            if (facturasSinOperacion == null)
            {
                throw new ArgumentNullException(nameof(facturasSinOperacion));
            }

            if (string.IsNullOrWhiteSpace(operacion.RfcCliente))
            {
                return Array.Empty<FacturaResumenDto>();
            }

            var rfcCliente = operacion.RfcCliente.Trim();
            var montoOperacion = decimal.Round((decimal)operacion.Monto, 2);

            return facturasSinOperacion
                .Where(factura => !factura.IdOperacion.HasValue || factura.IdOperacion.Value <= 0)
                .Where(factura => string.Equals(factura.ReceptorRfc?.Trim(), rfcCliente, StringComparison.OrdinalIgnoreCase))
                .Where(factura => decimal.Round(factura.SubTotal, 2) == montoOperacion)
                .OrderBy(factura => DistanciaFecha(operacion, factura))
                .ThenBy(factura => factura.Fecha)
                .ThenBy(factura => factura.IdFactura)
                .ToList();
        }

        private static int DistanciaFecha(OperacionSinFacturaDto operacion, FacturaResumenDto factura)
        {
            if (!operacion.FechaFinal.HasValue)
            {
                return 0;
            }

            return Math.Abs((factura.Fecha.Date - operacion.FechaFinal.Value.Date).Days);
        }
    }
}
