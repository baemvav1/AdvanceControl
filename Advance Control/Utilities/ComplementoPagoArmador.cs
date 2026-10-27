using System;
using System.Collections.Generic;
using System.Linq;
using Advance_Control.Models;

namespace Advance_Control.Utilities
{
    /// <summary>
    /// Arma los nodos &lt;Pago&gt; del Complemento de Pago a partir de los abonos marcados en
    /// GenerarComplementoPagoDialog. Parcialidades y saldos los vuelve a calcular el API antes de
    /// timbrar; aquí solo se decide cómo se agrupan y con qué forma de pago.
    /// </summary>
    public static class ComplementoPagoArmador
    {
        /// <summary>Mensaje para el usuario si la selección no se puede timbrar; null si está bien.</summary>
        public static string? Validar(IReadOnlyCollection<AbonoPendienteComplementoDto> seleccionados, string? formaPagoCombo)
        {
            if (seleccionados.Count == 0)
            {
                return "Selecciona al menos un abono para incluir en el complemento.";
            }

            // Cada abono usa la forma de pago con que se registró; el combo solo cubre los que no
            // la tienen (conciliación, históricos).
            if (string.IsNullOrWhiteSpace(formaPagoCombo) && seleccionados.Any(a => a.FormaPagoSat == null))
            {
                return "Hay abonos sin forma de pago registrada: selecciona la forma de pago para ellos.";
            }

            return null;
        }

        /// <summary>
        /// Abonos con la misma fecha (día) y forma de pago van en un solo nodo &lt;Pago&gt; -- así un
        /// pago que cubrió varias facturas queda en un nodo con varios &lt;DoctoRelacionado&gt;, y
        /// pagos reales distintos quedan separados. Llamar después de <see cref="Validar"/>.
        /// </summary>
        public static List<GrupoPagoComplementoDto> Armar(
            IEnumerable<AbonoPendienteComplementoDto> seleccionados, string? formaPagoCombo, string? numOperacionCapturado)
        {
            var numOperacion = string.IsNullOrWhiteSpace(numOperacionCapturado) ? null : numOperacionCapturado.Trim();

            return seleccionados
                .GroupBy(a => (Fecha: a.FechaAbono.Date, FormaPago: a.FormaPagoSat ?? formaPagoCombo!))
                .OrderBy(g => g.Key.Fecha)
                .Select(g => new GrupoPagoComplementoDto
                {
                    FechaPago = g.Key.Fecha,
                    FormaPago = g.Key.FormaPago,
                    Moneda = g.First().Moneda,
                    TipoCambio = 1m,
                    NumOperacion = numOperacion ?? ReferenciaComun(g),
                    Doctos = g.Select(a => new DoctoRelacionadoComplementoDto
                    {
                        IdAbonoFactura = a.IdAbonoFactura,
                        IdFacturaPagada = a.IdFactura,
                        NumParcialidad = a.NumParcialidad,
                        ImpSaldoAnt = a.ImpSaldoAnt,
                        ImpPagado = a.MontoAbono,
                        ImpSaldoInsoluto = a.ImpSaldoInsoluto,
                        ObjetoImp = "02"
                    }).ToList()
                })
                .ToList();
        }

        /// <summary>
        /// Referencia capturada a mano en "Registrar abono" (núm. de transferencia o cheque) si
        /// todos los abonos del pago comparten la misma. Solo se toman abonos con forma de pago
        /// registrada: en los de conciliación la referencia es el texto del banco, no el número
        /// de operación. Respeta el patrón de NumOperacion del SAT ([^|]{1,100}).
        /// </summary>
        private static string? ReferenciaComun(IEnumerable<AbonoPendienteComplementoDto> abonos)
        {
            var referencias = abonos
                .Where(a => a.FormaPagoSat != null)
                .Select(a => a.Referencia?.Trim())
                .Distinct()
                .ToList();

            return referencias.Count == 1
                && !string.IsNullOrEmpty(referencias[0])
                && referencias[0]!.Length <= 100
                && !referencias[0]!.Contains('|')
                ? referencias[0]
                : null;
        }
    }
}
