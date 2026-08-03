using System;
using System.Collections.Generic;
using System.Linq;
using Advance_Control.Models;

namespace Advance_Control.Services.Facturacion
{
    /// <summary>
    /// Sugiere facturas ya cargadas (sin operacion) como candidatas para una
    /// operacion sin factura: mismo RFC que el cliente de la operacion, mismo
    /// monto exacto, y fecha de factura no anterior a la fecha de la operacion
    /// (no se puede facturar antes de que el trabajo termine).
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
                .Where(factura => decimal.Round(factura.Total, 2) == montoOperacion)
                .Where(factura => EsFechaValida(operacion, factura))
                .OrderBy(factura => DistanciaFecha(operacion, factura))
                .ThenBy(factura => factura.Fecha)
                .ThenBy(factura => factura.IdFactura)
                .ToList();
        }

        /// <summary>
        /// La operacion no puede ser posterior a la factura: el trabajo debe haber
        /// terminado antes (o el mismo dia) de que se emita la factura que lo cobra.
        /// </summary>
        public bool EsFechaValida(OperacionSinFacturaDto operacion, FacturaResumenDto factura)
        {
            if (!operacion.FechaFinal.HasValue)
            {
                return true;
            }

            return factura.Fecha.Date >= operacion.FechaFinal.Value.Date;
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
