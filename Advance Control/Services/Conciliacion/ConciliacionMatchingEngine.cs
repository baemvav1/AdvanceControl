using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Rules;

namespace Advance_Control.Services.Conciliacion
{
    public sealed class ConciliacionMatchingEngine
    {
        private readonly ConciliacionRules _rules;

        public int TiempoLimitePorFacturaSegundos => _rules.Abonos.TiempoLimitePorFacturaSegundos;

        public ConciliacionMatchingEngine(IConciliacionRulesProvider rulesProvider)
            : this(rulesProvider?.GetCurrentRules() ?? throw new ArgumentNullException(nameof(rulesProvider)))
        {
        }

        internal ConciliacionMatchingEngine(ConciliacionRules rules)
        {
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
        }

        public bool CanRunUnoAUno(
            IReadOnlyCollection<FacturaResumenDto> facturas,
            IReadOnlyCollection<ConciliacionMovimientoResumenDto> movimientos)
        {
            return facturas.Any(EsFacturaElegibleParaConciliacionUnoAUno)
                && movimientos.Any(movimiento => decimal.Round(movimiento.Abono, 2) > 0);
        }

        public bool CanRunCombinacional(
            IReadOnlyCollection<FacturaResumenDto> facturas,
            IReadOnlyCollection<ConciliacionMovimientoResumenDto> movimientos)
        {
            return movimientos.Count > 0
                && facturas
                    .Where(factura => TieneTotalConciliable(factura)
                        && ObtenerMontoPendienteFactura(factura) > 0
                        && !string.IsNullOrWhiteSpace(factura.ReceptorRfc))
                    .GroupBy(factura => factura.ReceptorRfc!.Trim(), StringComparer.OrdinalIgnoreCase)
                    .Any(grupo => grupo.Count() >= _rules.Combinacional.MinimoFacturasPorGrupo);
        }

        public bool CanRunAbonos(
            IReadOnlyCollection<FacturaResumenDto> facturas,
            IReadOnlyCollection<ConciliacionMovimientoResumenDto> movimientos)
        {
            return facturas.Any(factura => TieneTotalConciliable(factura) && ObtenerMontoPendienteFactura(factura) > 0)
                && movimientos.Count(movimiento => decimal.Round(movimiento.Abono, 2) > 0) >= _rules.Abonos.MinimoMovimientosPorCombinacion;
        }

        public bool EsFacturaElegibleParaConciliacionUnoAUno(FacturaResumenDto factura)
        {
            var totalFactura = ObtenerTotalFactura(factura);
            if (totalFactura <= 0)
            {
                return false;
            }

            var saldoPendiente = decimal.Round(factura.SaldoPendiente, 2);
            var totalAbonado = decimal.Round(factura.TotalAbonado, 2);

            if (_rules.UnoAUno.RequiereSaldoPendienteIgualAlTotal && saldoPendiente != totalFactura)
            {
                return false;
            }

            if (_rules.UnoAUno.RequiereSinAbonosPrevios
                && (totalAbonado != 0 || factura.NumeroAbonos != 0))
            {
                return false;
            }

            if (_rules.UnoAUno.RequiereFacturaNoFiniquitada && factura.Finiquito == true)
            {
                return false;
            }

            return true;
        }

        public decimal ObtenerTotalFactura(FacturaResumenDto factura)
        {
            return decimal.Round(factura.Total, 2);
        }

        public decimal ObtenerMontoPendienteFactura(FacturaResumenDto factura)
        {
            if (!TieneTotalConciliable(factura))
            {
                return 0m;
            }

            var montoPendiente = factura.SaldoPendiente <= 0
                ? 0m
                : factura.SaldoPendiente;
            return decimal.Round(montoPendiente, 2);
        }

        public ConciliacionMovimientoResumenDto? BuscarMovimientoCoincidente(
            IEnumerable<ConciliacionMovimientoResumenDto> movimientos,
            decimal montoObjetivo,
            IReadOnlyCollection<FacturaResumenDto> facturas,
            DateTime fechaReferencia,
            bool aplicarReglaPueMismoMes = true)
        {
            if (montoObjetivo <= 0 || facturas.Count == 0)
            {
                return null;
            }

            return movimientos
                .Where(movimiento =>
                    decimal.Round(movimiento.Abono, 2) > 0
                    && decimal.Round(movimiento.Abono, 2) == decimal.Round(montoObjetivo, 2)
                    && facturas.All(factura => EsMovimientoCompatibleSegunMetodoPago(factura, movimiento.Fecha, aplicarReglaPueMismoMes)))
                .OrderBy(movimiento => Math.Abs((movimiento.Fecha - fechaReferencia).Ticks))
                .ThenByDescending(movimiento => movimiento.Fecha)
                .ThenBy(movimiento => movimiento.IdMovimiento)
                .FirstOrDefault();
        }

        public List<ConciliacionMovimientoResumenDto> ObtenerMovimientosCandidatosParaFactura(
            IReadOnlyList<ConciliacionMovimientoResumenDto> movimientosDisponibles,
            FacturaResumenDto facturaObjetivo,
            decimal saldoFactura,
            bool aplicarReglaPueMismoMes = true)
        {
            // Sin tope: el motor (BuscarCombinacionMovimientosParaFactura) decide internamente
            // si usa backtracking exacto o meet-in-the-middle segun cuantos candidatos haya,
            // en vez de descartar candidatos legitimos de antemano.
            return movimientosDisponibles
                .Where(movimiento =>
                    decimal.Round(movimiento.Abono, 2) > 0
                    && decimal.Round(movimiento.Abono, 2) <= decimal.Round(saldoFactura, 2)
                    && EsMovimientoCompatibleSegunMetodoPago(facturaObjetivo, movimiento.Fecha, aplicarReglaPueMismoMes))
                .OrderBy(movimiento => Math.Abs((movimiento.Fecha - facturaObjetivo.Fecha).Ticks))
                .ThenBy(movimiento => movimiento.Fecha)
                .ThenBy(movimiento => movimiento.IdMovimiento)
                .ToList();
        }

        /// <summary>
        /// Busca la mejor combinacion exacta de movimientos que sume el monto objetivo
        /// (fecha mas cercana a la factura, luego menos movimientos). Hasta
        /// <see cref="ConciliacionAbonosRules.UmbralBusquedaExhaustiva"/> candidatos usa
        /// backtracking exhaustivo de un solo hilo; por encima usa busqueda
        /// "meet-in-the-middle" en paralelo para no truncar candidatos ni colgar la UI.
        /// </summary>
        public List<ConciliacionMovimientoResumenDto>? BuscarCombinacionMovimientosParaFactura(
            IReadOnlyList<ConciliacionMovimientoResumenDto> movimientos,
            decimal montoObjetivo,
            DateTime fechaFactura,
            CancellationToken ct = default)
        {
            if (movimientos.Count < _rules.Abonos.MinimoMovimientosPorCombinacion || montoObjetivo <= 0)
            {
                return null;
            }

            if (movimientos.Count <= _rules.Abonos.UmbralBusquedaExhaustiva)
            {
                var combinacionActual = new List<ConciliacionMovimientoResumenDto>();
                List<ConciliacionMovimientoResumenDto>? mejorCombinacion = null;
                long mejorScore = long.MaxValue;
                long iteraciones = 0;

                BuscarCombinacionMovimientosRecursiva(
                    movimientos,
                    montoObjetivo,
                    fechaFactura,
                    0,
                    0m,
                    combinacionActual,
                    ref mejorCombinacion,
                    ref mejorScore,
                    ref iteraciones,
                    ct);

                return mejorCombinacion;
            }

            return BuscarCombinacionMovimientosMeetInTheMiddle(movimientos, montoObjetivo, fechaFactura, ct);
        }

        /// <summary>
        /// Divide los candidatos en dos mitades, enumera todos los subconjuntos de cada una
        /// en paralelo (con poda: descarta en cuanto la suma parcial supera el objetivo) y
        /// combina los resultados buscando el complemento exacto. Como el score de cercania
        /// de fecha es aditivo por movimiento, conservar solo la mejor combinacion por cada
        /// suma alcanzable en cada mitad es exacto (no una aproximacion): para una suma total
        /// fija, minimizar el score de cada mitad por separado minimiza el score combinado.
        /// </summary>
        private List<ConciliacionMovimientoResumenDto>? BuscarCombinacionMovimientosMeetInTheMiddle(
            IReadOnlyList<ConciliacionMovimientoResumenDto> movimientos,
            decimal montoObjetivo,
            DateTime fechaFactura,
            CancellationToken ct)
        {
            var objetivoCentavos = MontoACentavos(montoObjetivo);
            var mitadIndice = movimientos.Count / 2;
            var mitadA = movimientos.Take(mitadIndice).ToList();
            var mitadB = movimientos.Skip(mitadIndice).ToList();

            var tareaA = Task.Run(() => EnumerarSubconjuntosPorSuma(mitadA, objetivoCentavos, fechaFactura, ct), ct);
            var tareaB = Task.Run(() => EnumerarSubconjuntosPorSuma(mitadB, objetivoCentavos, fechaFactura, ct), ct);
            Task.WaitAll(new Task[] { tareaA, tareaB }, ct);

            var sumasA = tareaA.Result;
            var sumasB = tareaB.Result;

            List<ConciliacionMovimientoResumenDto>? mejorCombinacion = null;
            long mejorScore = long.MaxValue;

            foreach (var (sumaB, datoB) in sumasB)
            {
                ct.ThrowIfCancellationRequested();

                var complemento = objetivoCentavos - sumaB;
                if (complemento < 0 || !sumasA.TryGetValue(complemento, out var datoA))
                {
                    continue;
                }

                var totalMovimientos = datoA.Combinacion.Count + datoB.Combinacion.Count;
                if (totalMovimientos < _rules.Abonos.MinimoMovimientosPorCombinacion)
                {
                    continue;
                }

                var scoreCombinado = datoA.Score + datoB.Score;
                if (mejorCombinacion != null
                    && (scoreCombinado > mejorScore
                        || (scoreCombinado == mejorScore && totalMovimientos >= mejorCombinacion.Count)))
                {
                    continue;
                }

                var combinacion = new List<ConciliacionMovimientoResumenDto>(datoA.Combinacion);
                combinacion.AddRange(datoB.Combinacion);
                mejorScore = scoreCombinado;
                mejorCombinacion = combinacion;
            }

            return mejorCombinacion;
        }

        /// <summary>
        /// Enumera todos los subconjuntos de <paramref name="mitad"/> cuya suma no excede el
        /// objetivo, y devuelve para cada suma en centavos alcanzada la mejor combinacion vista
        /// (menor score de cercania, luego menos movimientos).
        /// </summary>
        private Dictionary<long, (List<ConciliacionMovimientoResumenDto> Combinacion, long Score)> EnumerarSubconjuntosPorSuma(
            IReadOnlyList<ConciliacionMovimientoResumenDto> mitad,
            long objetivoCentavos,
            DateTime fechaFactura,
            CancellationToken ct)
        {
            var mejoresPorSuma = new Dictionary<long, (List<ConciliacionMovimientoResumenDto> Combinacion, long Score)>();
            var combinacionActual = new List<ConciliacionMovimientoResumenDto>();
            long iteraciones = 0;

            void Enumerar(int indice, long sumaActual)
            {
                if (++iteraciones % 4096 == 0)
                {
                    ct.ThrowIfCancellationRequested();
                }

                var score = CalcularScoreCercania(combinacionActual, fechaFactura);
                if (!mejoresPorSuma.TryGetValue(sumaActual, out var existente)
                    || score < existente.Score
                    || (score == existente.Score && combinacionActual.Count < existente.Combinacion.Count))
                {
                    mejoresPorSuma[sumaActual] = (new List<ConciliacionMovimientoResumenDto>(combinacionActual), score);
                }

                for (var i = indice; i < mitad.Count; i++)
                {
                    var montoCentavos = MontoACentavos(mitad[i].Abono);
                    var nuevaSuma = sumaActual + montoCentavos;
                    if (nuevaSuma > objetivoCentavos)
                    {
                        continue;
                    }

                    combinacionActual.Add(mitad[i]);
                    Enumerar(i + 1, nuevaSuma);
                    combinacionActual.RemoveAt(combinacionActual.Count - 1);
                }
            }

            Enumerar(0, 0L);
            return mejoresPorSuma;
        }

        private static long MontoACentavos(decimal monto) => (long)decimal.Round(monto * 100m, 0);

        public List<FacturaResumenDto>? BuscarCombinacionFacturasCompatible(
            IReadOnlyList<FacturaResumenDto> facturas,
            IReadOnlyList<ConciliacionMovimientoResumenDto> movimientosDisponibles,
            decimal maximoAbonoDisponible,
            out ConciliacionMovimientoResumenDto? movimientoObjetivo,
            bool aplicarReglaPueMismoMes = true)
        {
            movimientoObjetivo = null;

            // Fase 1: suma incremental desde la factura más antigua.
            // Cubre el caso más común: un pago liquida la deuda más vieja más algunas consecutivas.
            var combinacionSecuencial = BuscarCombinacionSecuencialDesdeAntigua(
                facturas,
                movimientosDisponibles,
                maximoAbonoDisponible,
                out movimientoObjetivo,
                aplicarReglaPueMismoMes);

            if (combinacionSecuencial != null)
            {
                return combinacionSecuencial;
            }

            // Fase 2: backtracking completo para combinaciones no consecutivas.
            var buffer = new List<FacturaResumenDto>();
            for (var tamano = _rules.Combinacional.MinimoFacturasPorGrupo; tamano <= facturas.Count; tamano++)
            {
                var combinacion = BuscarCombinacionFacturasCompatibleRecursiva(
                    facturas,
                    movimientosDisponibles,
                    maximoAbonoDisponible,
                    tamano,
                    0,
                    0m,
                    buffer,
                    out movimientoObjetivo,
                    aplicarReglaPueMismoMes);

                if (combinacion != null)
                {
                    return combinacion;
                }
            }

            return null;
        }

        // Fase 1: intenta [F1,F2], [F1,F2,F3], [F1,F2,F3,F4]... en orden creciente.
        // Las facturas deben venir ordenadas por fecha ascendente.
        private List<FacturaResumenDto>? BuscarCombinacionSecuencialDesdeAntigua(
            IReadOnlyList<FacturaResumenDto> facturas,
            IReadOnlyList<ConciliacionMovimientoResumenDto> movimientosDisponibles,
            decimal maximoAbonoDisponible,
            out ConciliacionMovimientoResumenDto? movimientoObjetivo,
            bool aplicarReglaPueMismoMes)
        {
            movimientoObjetivo = null;
            var sumaAcumulada = 0m;

            for (var n = 0; n < facturas.Count; n++)
            {
                sumaAcumulada = decimal.Round(sumaAcumulada + ObtenerMontoPendienteFactura(facturas[n]), 2);

                // No revisar movimientos hasta tener el mínimo de facturas requerido.
                if (n + 1 < _rules.Combinacional.MinimoFacturasPorGrupo)
                {
                    continue;
                }

                if (sumaAcumulada <= 0 || sumaAcumulada > maximoAbonoDisponible)
                {
                    continue;
                }

                var candidatas = facturas.Take(n + 1).ToList();
                var fechaMasNueva = candidatas.Max(f => f.Fecha);

                movimientoObjetivo = BuscarMovimientoCoincidente(
                    movimientosDisponibles,
                    sumaAcumulada,
                    candidatas,
                    fechaMasNueva,
                    aplicarReglaPueMismoMes);

                if (movimientoObjetivo != null)
                {
                    return candidatas;
                }
            }

            return null;
        }

        private void BuscarCombinacionMovimientosRecursiva(
            IReadOnlyList<ConciliacionMovimientoResumenDto> movimientos,
            decimal montoObjetivo,
            DateTime fechaFactura,
            int indiceInicio,
            decimal sumaActual,
            List<ConciliacionMovimientoResumenDto> combinacionActual,
            ref List<ConciliacionMovimientoResumenDto>? mejorCombinacion,
            ref long mejorScore,
            ref long iteraciones,
            CancellationToken ct)
        {
            if (++iteraciones % 4096 == 0)
            {
                ct.ThrowIfCancellationRequested();
            }

            var sumaRedondeada = decimal.Round(sumaActual, 2);
            var objetivoRedondeado = decimal.Round(montoObjetivo, 2);

            if (sumaRedondeada > objetivoRedondeado)
            {
                return;
            }

            if (combinacionActual.Count >= _rules.Abonos.MinimoMovimientosPorCombinacion
                && sumaRedondeada == objetivoRedondeado)
            {
                var scoreActual = CalcularScoreCercania(combinacionActual, fechaFactura);
                if (mejorCombinacion == null
                    || scoreActual < mejorScore
                    || (scoreActual == mejorScore && combinacionActual.Count < mejorCombinacion.Count))
                {
                    mejorScore = scoreActual;
                    mejorCombinacion = new List<ConciliacionMovimientoResumenDto>(combinacionActual);
                }

                return;
            }

            for (var indice = indiceInicio; indice < movimientos.Count; indice++)
            {
                var movimiento = movimientos[indice];
                var nuevaSuma = decimal.Round(sumaActual + movimiento.Abono, 2);
                if (nuevaSuma > objetivoRedondeado)
                {
                    continue;
                }

                combinacionActual.Add(movimiento);
                BuscarCombinacionMovimientosRecursiva(
                    movimientos,
                    montoObjetivo,
                    fechaFactura,
                    indice + 1,
                    nuevaSuma,
                    combinacionActual,
                    ref mejorCombinacion,
                    ref mejorScore,
                    ref iteraciones,
                    ct);
                combinacionActual.RemoveAt(combinacionActual.Count - 1);
            }
        }

        private long CalcularScoreCercania(
            IReadOnlyList<ConciliacionMovimientoResumenDto> movimientos,
            DateTime fechaFactura)
        {
            long score = 0;
            foreach (var movimiento in movimientos)
            {
                score += Math.Abs((movimiento.Fecha - fechaFactura).Ticks);
            }

            return score;
        }

        private List<FacturaResumenDto>? BuscarCombinacionFacturasCompatibleRecursiva(
            IReadOnlyList<FacturaResumenDto> facturas,
            IReadOnlyList<ConciliacionMovimientoResumenDto> movimientosDisponibles,
            decimal maximoAbonoDisponible,
            int tamanoObjetivo,
            int indiceInicio,
            decimal sumaActual,
            List<FacturaResumenDto> combinacionActual,
            out ConciliacionMovimientoResumenDto? movimientoObjetivo,
            bool aplicarReglaPueMismoMes)
        {
            movimientoObjetivo = null;

            if (combinacionActual.Count == tamanoObjetivo)
            {
                var montoObjetivo = decimal.Round(sumaActual, 2);
                if (montoObjetivo <= 0 || montoObjetivo > maximoAbonoDisponible)
                {
                    return null;
                }

                var fechaMasNueva = combinacionActual.Max(factura => factura.Fecha);
                movimientoObjetivo = BuscarMovimientoCoincidente(
                    movimientosDisponibles,
                    montoObjetivo,
                    combinacionActual,
                    fechaMasNueva,
                    aplicarReglaPueMismoMes);
                return movimientoObjetivo == null ? null : new List<FacturaResumenDto>(combinacionActual);
            }

            var restantesNecesarios = tamanoObjetivo - combinacionActual.Count;
            for (var indice = indiceInicio; indice <= facturas.Count - restantesNecesarios; indice++)
            {
                var factura = facturas[indice];
                var nuevaSuma = decimal.Round(sumaActual + ObtenerMontoPendienteFactura(factura), 2);
                if (nuevaSuma > maximoAbonoDisponible)
                {
                    continue;
                }

                combinacionActual.Add(factura);

                var combinacionEncontrada = BuscarCombinacionFacturasCompatibleRecursiva(
                    facturas,
                    movimientosDisponibles,
                    maximoAbonoDisponible,
                    tamanoObjetivo,
                    indice + 1,
                    nuevaSuma,
                    combinacionActual,
                    out movimientoObjetivo,
                    aplicarReglaPueMismoMes);

                if (combinacionEncontrada != null)
                {
                    return combinacionEncontrada;
                }

                combinacionActual.RemoveAt(combinacionActual.Count - 1);
            }

            return null;
        }

        private bool EsMovimientoCompatibleSegunMetodoPago(FacturaResumenDto factura, DateTime fechaMovimiento, bool aplicarReglaPueMismoMes)
        {
            var metodoPago = factura.MetodoPago?.Trim();
            if (_rules.MetodoPago.PermitirMesesPosterioresParaPagoDiferido
                && string.Equals(metodoPago, _rules.MetodoPago.MetodoPagoDiferido, StringComparison.OrdinalIgnoreCase))
            {
                return EsMismoMesOPosterior(factura.Fecha, fechaMovimiento);
            }

            if (string.Equals(metodoPago, _rules.MetodoPago.MetodoPagoUnaExhibicion, StringComparison.OrdinalIgnoreCase))
            {
                return aplicarReglaPueMismoMes
                    ? EsMismoMes(factura.Fecha, fechaMovimiento)
                    : EsMismoMesOPosterior(factura.Fecha, fechaMovimiento);
            }

            return EsMismoMes(factura.Fecha, fechaMovimiento);
        }

        private static bool EsMismoMes(DateTime fechaFactura, DateTime fechaMovimiento)
        {
            return fechaFactura.Year == fechaMovimiento.Year
                && fechaFactura.Month == fechaMovimiento.Month;
        }

        private static bool EsMismoMesOPosterior(DateTime fechaFactura, DateTime fechaMovimiento)
        {
            if (fechaMovimiento.Year > fechaFactura.Year)
            {
                return true;
            }

            return fechaMovimiento.Year == fechaFactura.Year
                && fechaMovimiento.Month >= fechaFactura.Month;
        }

        private bool TieneTotalConciliable(FacturaResumenDto factura)
        {
            return ObtenerTotalFactura(factura) > 0;
        }
    }
}
