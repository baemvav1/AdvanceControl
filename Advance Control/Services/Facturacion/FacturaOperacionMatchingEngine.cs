using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Advance_Control.Models;

namespace Advance_Control.Services.Facturacion
{
    /// <summary>
    /// Sugiere facturas ya cargadas (sin operacion) como candidatas para una operacion
    /// sin factura. Dos señales, en orden de confianza:
    ///   1. Numero de cotizacion/reporte/operacion: los PDFs de cotizacion y reporte que
    ///      genera la app se titulan "Cotizacion No: {IdOperacion}" / "Reporte No:
    ///      {IdOperacion}" (ver QuoteService), y ese numero termina copiado en
    ///      condiciones_de_pago de la factura (ej. "COTIZACION NUM. 33" o "Cotizaciones
    ///      Nums. 831 y 832"). Tambien se ve como "OPERACION DE EMERGENCIA #23" para
    ///      trabajos sin cotizacion previa. Si el texto de la factura menciona el ID
    ///      exacto de la operacion (y el RFC coincide), es una referencia directa, mas
    ///      confiable que el monto.
    ///   2. RFC + monto: mismo RFC que el cliente de la operacion y mismo monto exacto --
    ///      comparado contra el SUBTOTAL de la factura (sin IVA), porque operacion.Monto
    ///      tampoco incluye IVA.
    /// No hay filtro de fecha: en la practica fecha_final de la operacion no es confiable
    /// para bloquear el vinculo.
    /// </summary>
    public sealed class FacturaOperacionMatchingEngine
    {
        private static readonly Regex NumerosRegex = new(@"\d+", RegexOptions.Compiled);

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

            var facturasDelCliente = facturasSinOperacion
                .Where(factura => !factura.IdOperacion.HasValue || factura.IdOperacion.Value <= 0)
                .Where(factura => string.Equals(factura.ReceptorRfc?.Trim(), rfcCliente, StringComparison.OrdinalIgnoreCase))
                .ToList();

            // Cada bucket se ordena por separado (fecha/id como desempate) y LUEGO se
            // concatena -- si se ordenara todo junto, una factura mas vieja que solo
            // coincide por monto podria colarse antes que la que de verdad menciona el
            // numero de esta operacion (le paso con clientes que tienen varias facturas
            // del mismo monto exacto, ej. "OPERACION DE EMERGENCIA #N" repetido).
            var porNumeroCotizacion = facturasDelCliente
                .Where(factura => ReferenciaContieneOperacion(factura.CondicionesDePago, operacion.IdOperacion))
                .OrderBy(factura => DistanciaFecha(operacion, factura))
                .ThenBy(factura => factura.Fecha)
                .ThenBy(factura => factura.IdFactura)
                .ToList();

            var porMonto = facturasDelCliente
                .Where(factura => decimal.Round(factura.SubTotal, 2) == montoOperacion)
                .Except(porNumeroCotizacion)
                .OrderBy(factura => DistanciaFecha(operacion, factura))
                .ThenBy(factura => factura.Fecha)
                .ThenBy(factura => factura.IdFactura)
                .ToList();

            return porNumeroCotizacion.Concat(porMonto).ToList();
        }

        /// <summary>
        /// True si el texto de condiciones_de_pago de la factura menciona "cotizacion",
        /// "reporte" u "operacion" junto con el numero exacto del ID de la operacion.
        /// Se usa tambien desde la UI para mostrar de donde salio la sugerencia.
        /// </summary>
        public static bool ReferenciaContieneOperacion(string? condicionesDePago, int idOperacion)
        {
            if (string.IsNullOrWhiteSpace(condicionesDePago))
            {
                return false;
            }

            if (!condicionesDePago.Contains("cotiz", StringComparison.OrdinalIgnoreCase)
                && !condicionesDePago.Contains("reporte", StringComparison.OrdinalIgnoreCase)
                && !condicionesDePago.Contains("operaci", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            foreach (Match match in NumerosRegex.Matches(condicionesDePago))
            {
                if (int.TryParse(match.Value, out var numero) && numero == idOperacion)
                {
                    return true;
                }
            }

            return false;
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
