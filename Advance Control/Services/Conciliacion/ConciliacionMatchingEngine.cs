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
            bool aplicarReglaPueMismoMes = true,
            bool aplicarReglaPpdSiguienteMes = false)
        {
            if (montoObjetivo <= 0 || facturas.Count == 0)
            {
                return null;
            }

            return movimientos
                .Where(movimiento =>
                    decimal.Round(movimiento.Abono, 2) > 0
                    && decimal.Round(movimiento.Abono, 2) == decimal.Round(montoObjetivo, 2)
                    && facturas.All(factura => EsMovimientoCompatibleSegunMetodoPago(factura, movimiento.Fecha, aplicarReglaPueMismoMes, aplicarReglaPpdSiguienteMes)))
                .OrderBy(movimiento => Math.Abs((movimiento.Fecha - fechaReferencia).Ticks))
                .ThenByDescending(movimiento => movimiento.Fecha)
                .ThenBy(movimiento => movimiento.IdMovimiento)
                .FirstOrDefault();
        }

        public List<ConciliacionMovimientoResumenDto> ObtenerMovimientosCandidatosParaFactura(
            IReadOnlyList<ConciliacionMovimientoResumenDto> movimientosDisponibles,
            FacturaResumenDto facturaObjetivo,
            decimal saldoFactura,
            bool aplicarReglaPueMismoMes = true,
            bool aplicarReglaPpdSiguienteMes = false)
        {
            // Sin tope: el motor (BuscarCombinacionMovimientosParaFactura) decide internamente
            // si usa backtracking exacto o meet-in-the-middle segun cuantos candidatos haya,
            // en vez de descartar candidatos legitimos de antemano.
            return movimientosDisponibles
                .Where(movimiento =>
                    decimal.Round(movimiento.Abono, 2) > 0
                    && decimal.Round(movimiento.Abono, 2) <= decimal.Round(saldoFactura, 2)
                    && EsMovimientoCompatibleSegunMetodoPago(facturaObjetivo, movimiento.Fecha, aplicarReglaPueMismoMes, aplicarReglaPpdSiguienteMes))
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
            return BuscarCombinacionExacta(
                movimientos,
                movimiento => movimiento.Abono,
                movimiento => movimiento.Fecha,
                montoObjetivo,
                fechaFactura,
                _rules.Abonos.MinimoMovimientosPorCombinacion,
                ct);
        }

        /// <summary>
        /// Inverso de <see cref="BuscarCombinacionMovimientosParaFactura"/>: dado UN movimiento
        /// (p. ej. un cheque que liquido varias facturas), busca el subconjunto exacto de
        /// facturas de un mismo RFC cuyo saldo pendiente sume el abono. Entre combinaciones
        /// validas prefiere las facturas mas cercanas en fecha al movimiento (y emitidas antes
        /// que el), de modo que dos facturas de igual monto no se "roben" entre cheques de
        /// distintos meses. Devuelve null si ningun RFC tiene una combinacion exacta.
        /// </summary>
        public List<FacturaResumenDto>? BuscarCombinacionFacturasParaMovimiento(
            IReadOnlyList<FacturaResumenDto> facturas,
            ConciliacionMovimientoResumenDto movimiento,
            bool aplicarReglaPueMismoMes,
            bool aplicarReglaPpdSiguienteMes,
            CancellationToken ct = default)
        {
            var abono = decimal.Round(movimiento.Abono, 2);
            if (abono <= 0)
            {
                return null;
            }

            List<FacturaResumenDto>? mejorCombinacion = null;
            long mejorScore = long.MaxValue;

            var gruposPorRfc = facturas
                .Where(factura => !string.IsNullOrWhiteSpace(factura.ReceptorRfc))
                .Where(factura =>
                {
                    var saldo = ObtenerMontoPendienteFactura(factura);
                    return saldo > 0 && saldo <= abono;
                })
                .Where(factura => EsMovimientoCompatibleSegunMetodoPago(factura, movimiento.Fecha, aplicarReglaPueMismoMes, aplicarReglaPpdSiguienteMes))
                .GroupBy(factura => factura.ReceptorRfc!.Trim(), StringComparer.OrdinalIgnoreCase);

            foreach (var grupo in gruposPorRfc)
            {
                ct.ThrowIfCancellationRequested();

                var candidatas = grupo
                    .OrderBy(factura => CalcularDistanciaFacturaMovimiento(factura.Fecha, movimiento.Fecha))
                    .ThenBy(factura => factura.IdFactura)
                    .Take(_rules.Combinacional.MaximoFacturasCandidatasPorMovimiento)
                    .ToList();

                var combinacion = BuscarCombinacionExacta(
                    candidatas,
                    ObtenerMontoPendienteFactura,
                    factura => factura.Fecha,
                    abono,
                    movimiento.Fecha,
                    _rules.Combinacional.MinimoFacturasPorGrupo,
                    ct,
                    CalcularDistanciaFacturaMovimiento);

                if (combinacion == null)
                {
                    continue;
                }

                var score = combinacion.Sum(factura => CalcularDistanciaFacturaMovimiento(factura.Fecha, movimiento.Fecha));
                if (mejorCombinacion == null || score < mejorScore)
                {
                    mejorScore = score;
                    mejorCombinacion = combinacion;
                }
            }

            return mejorCombinacion?
                .OrderBy(factura => factura.Fecha)
                .ThenBy(factura => factura.IdFactura)
                .ToList();
        }

        /// <summary>
        /// Busca una factura 1 a 1 de monto exacto para el movimiento (sin reservarla): sirve
        /// para saber si un cheque tiene candidata directa antes de intentar combinaciones.
        /// </summary>
        public bool ExisteFacturaUnoAUnoParaMovimiento(
            IEnumerable<FacturaResumenDto> facturas,
            ConciliacionMovimientoResumenDto movimiento,
            bool aplicarReglaPueMismoMes,
            bool aplicarReglaPpdSiguienteMes)
        {
            var abono = decimal.Round(movimiento.Abono, 2);
            return abono > 0 && facturas.Any(factura =>
                EsFacturaElegibleParaConciliacionUnoAUno(factura)
                && ObtenerTotalFactura(factura) == abono
                && EsMovimientoCompatibleSegunMetodoPago(factura, movimiento.Fecha, aplicarReglaPueMismoMes, aplicarReglaPpdSiguienteMes));
        }

        // Una factura emitida despues del movimiento (mismo mes, permitido por la regla PPD/PUE)
        // es menos probable que la emitida antes: se penaliza sumando un año de distancia.
        private static long CalcularDistanciaFacturaMovimiento(DateTime fechaFactura, DateTime fechaMovimiento)
        {
            var distancia = Math.Abs((fechaMovimiento.Date - fechaFactura.Date).Ticks);
            return fechaFactura.Date > fechaMovimiento.Date
                ? distancia + TimeSpan.FromDays(365).Ticks
                : distancia;
        }

        /// <summary>
        /// Busqueda exacta de subconjunto: hasta <see cref="ConciliacionAbonosRules.UmbralBusquedaExhaustiva"/>
        /// elementos usa backtracking de un solo hilo; por encima, "meet-in-the-middle" en paralelo.
        /// Minimiza la suma de distancias en fecha a <paramref name="fechaReferencia"/> y, a igual
        /// distancia, el numero de elementos.
        /// </summary>
        private List<T>? BuscarCombinacionExacta<T>(
            IReadOnlyList<T> elementos,
            Func<T, decimal> monto,
            Func<T, DateTime> fecha,
            decimal montoObjetivo,
            DateTime fechaReferencia,
            int minimoElementos,
            CancellationToken ct,
            Func<DateTime, DateTime, long>? distancia = null)
        {
            if (elementos.Count < minimoElementos || montoObjetivo <= 0)
            {
                return null;
            }

            distancia ??= (fechaElemento, referencia) => Math.Abs((fechaElemento - referencia).Ticks);
            var buscador = new BuscadorCombinacionExacta<T>(monto, item => distancia(fecha(item), fechaReferencia), minimoElementos, ct);

            return elementos.Count <= _rules.Abonos.UmbralBusquedaExhaustiva
                ? buscador.BuscarExhaustivo(elementos, montoObjetivo)
                : buscador.BuscarMeetInTheMiddle(elementos, montoObjetivo);
        }

        private static long MontoACentavos(decimal monto) => (long)decimal.Round(monto * 100m, 0);

        public List<FacturaResumenDto>? BuscarCombinacionFacturasCompatible(
            IReadOnlyList<FacturaResumenDto> facturas,
            IReadOnlyList<ConciliacionMovimientoResumenDto> movimientosDisponibles,
            decimal maximoAbonoDisponible,
            out ConciliacionMovimientoResumenDto? movimientoObjetivo,
            bool aplicarReglaPueMismoMes = true,
            bool aplicarReglaPpdSiguienteMes = false)
        {
            movimientoObjetivo = null;

            // Fase 1: suma incremental desde la factura más antigua.
            // Cubre el caso más común: un pago liquida la deuda más vieja más algunas consecutivas.
            var combinacionSecuencial = BuscarCombinacionSecuencialDesdeAntigua(
                facturas,
                movimientosDisponibles,
                maximoAbonoDisponible,
                out movimientoObjetivo,
                aplicarReglaPueMismoMes,
                aplicarReglaPpdSiguienteMes);

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
                    aplicarReglaPueMismoMes,
                    aplicarReglaPpdSiguienteMes);

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
            bool aplicarReglaPueMismoMes,
            bool aplicarReglaPpdSiguienteMes)
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
                    aplicarReglaPueMismoMes,
                    aplicarReglaPpdSiguienteMes);

                if (movimientoObjetivo != null)
                {
                    return candidatas;
                }
            }

            return null;
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
            bool aplicarReglaPueMismoMes,
            bool aplicarReglaPpdSiguienteMes)
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
                    aplicarReglaPueMismoMes,
                    aplicarReglaPpdSiguienteMes);
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
                    aplicarReglaPueMismoMes,
                    aplicarReglaPpdSiguienteMes);

                if (combinacionEncontrada != null)
                {
                    return combinacionEncontrada;
                }

                combinacionActual.RemoveAt(combinacionActual.Count - 1);
            }

            return null;
        }

        private bool EsMovimientoCompatibleSegunMetodoPago(FacturaResumenDto factura, DateTime fechaMovimiento, bool aplicarReglaPueMismoMes, bool aplicarReglaPpdSiguienteMes)
        {
            var metodoPago = factura.MetodoPago?.Trim();
            if (_rules.MetodoPago.PermitirMesesPosterioresParaPagoDiferido
                && string.Equals(metodoPago, _rules.MetodoPago.MetodoPagoDiferido, StringComparison.OrdinalIgnoreCase))
            {
                return aplicarReglaPpdSiguienteMes
                    ? EsMesSiguiente(factura.Fecha, fechaMovimiento)
                    : EsMismoMesOPosterior(factura.Fecha, fechaMovimiento);
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

        // Regla "PPD Sig. mes": el pago debe caer exactamente en el mes calendario siguiente al
        // de la factura, sin importar el dia (dic-2025 -> ene-2026 cuenta como siguiente).
        private static bool EsMesSiguiente(DateTime fechaFactura, DateTime fechaMovimiento)
        {
            var indiceFactura = fechaFactura.Year * 12 + fechaFactura.Month;
            var indiceMovimiento = fechaMovimiento.Year * 12 + fechaMovimiento.Month;
            return indiceMovimiento == indiceFactura + 1;
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

        /// <summary>
        /// Busqueda de subconjunto de suma exacta (en centavos) que minimiza la suma de
        /// distancias de fecha y, a igual distancia, el numero de elementos. Mismo algoritmo
        /// que ya usaba Abonos (varios movimientos a una factura), generalizado para poder
        /// buscar tambien varias facturas para un movimiento.
        /// </summary>
        private sealed class BuscadorCombinacionExacta<T>
        {
            private readonly Func<T, decimal> _monto;
            private readonly Func<T, long> _distancia;
            private readonly int _minimoElementos;
            private readonly CancellationToken _ct;

            public BuscadorCombinacionExacta(Func<T, decimal> monto, Func<T, long> distancia, int minimoElementos, CancellationToken ct)
            {
                _monto = monto;
                _distancia = distancia;
                _minimoElementos = minimoElementos;
                _ct = ct;
            }

            public List<T>? BuscarExhaustivo(IReadOnlyList<T> elementos, decimal montoObjetivo)
            {
                var objetivoCentavos = MontoACentavos(montoObjetivo);
                var combinacionActual = new List<T>();
                List<T>? mejorCombinacion = null;
                long mejorScore = long.MaxValue;
                long iteraciones = 0;

                void Buscar(int indiceInicio, long sumaActual, long scoreActual)
                {
                    if (++iteraciones % 4096 == 0)
                    {
                        _ct.ThrowIfCancellationRequested();
                    }

                    if (combinacionActual.Count >= _minimoElementos && sumaActual == objetivoCentavos)
                    {
                        if (mejorCombinacion == null
                            || scoreActual < mejorScore
                            || (scoreActual == mejorScore && combinacionActual.Count < mejorCombinacion.Count))
                        {
                            mejorScore = scoreActual;
                            mejorCombinacion = new List<T>(combinacionActual);
                        }

                        return;
                    }

                    for (var indice = indiceInicio; indice < elementos.Count; indice++)
                    {
                        var nuevaSuma = sumaActual + MontoACentavos(_monto(elementos[indice]));
                        if (nuevaSuma > objetivoCentavos)
                        {
                            continue;
                        }

                        combinacionActual.Add(elementos[indice]);
                        Buscar(indice + 1, nuevaSuma, scoreActual + _distancia(elementos[indice]));
                        combinacionActual.RemoveAt(combinacionActual.Count - 1);
                    }
                }

                Buscar(0, 0L, 0L);
                return mejorCombinacion;
            }

            /// <summary>
            /// Divide los candidatos en dos mitades, enumera todos los subconjuntos de cada una
            /// en paralelo (con poda: descarta en cuanto la suma parcial supera el objetivo) y
            /// combina los resultados buscando el complemento exacto. Como el score de cercania
            /// es aditivo por elemento, conservar solo la mejor combinacion por cada suma
            /// alcanzable en cada mitad es exacto (no una aproximacion).
            /// </summary>
            public List<T>? BuscarMeetInTheMiddle(IReadOnlyList<T> elementos, decimal montoObjetivo)
            {
                var objetivoCentavos = MontoACentavos(montoObjetivo);
                var mitadIndice = elementos.Count / 2;
                var mitadA = elementos.Take(mitadIndice).ToList();
                var mitadB = elementos.Skip(mitadIndice).ToList();

                var tareaA = Task.Run(() => EnumerarSubconjuntosPorSuma(mitadA, objetivoCentavos), _ct);
                var tareaB = Task.Run(() => EnumerarSubconjuntosPorSuma(mitadB, objetivoCentavos), _ct);
                Task.WaitAll(new Task[] { tareaA, tareaB }, _ct);

                var sumasA = tareaA.Result;
                var sumasB = tareaB.Result;

                List<T>? mejorCombinacion = null;
                long mejorScore = long.MaxValue;

                foreach (var (sumaB, datoB) in sumasB)
                {
                    _ct.ThrowIfCancellationRequested();

                    var complemento = objetivoCentavos - sumaB;
                    if (complemento < 0 || !sumasA.TryGetValue(complemento, out var datoA))
                    {
                        continue;
                    }

                    var totalElementos = datoA.Combinacion.Count + datoB.Combinacion.Count;
                    if (totalElementos < _minimoElementos)
                    {
                        continue;
                    }

                    var scoreCombinado = datoA.Score + datoB.Score;
                    if (mejorCombinacion != null
                        && (scoreCombinado > mejorScore
                            || (scoreCombinado == mejorScore && totalElementos >= mejorCombinacion.Count)))
                    {
                        continue;
                    }

                    var combinacion = new List<T>(datoA.Combinacion);
                    combinacion.AddRange(datoB.Combinacion);
                    mejorScore = scoreCombinado;
                    mejorCombinacion = combinacion;
                }

                return mejorCombinacion;
            }

            private Dictionary<long, (List<T> Combinacion, long Score)> EnumerarSubconjuntosPorSuma(
                IReadOnlyList<T> mitad,
                long objetivoCentavos)
            {
                var mejoresPorSuma = new Dictionary<long, (List<T> Combinacion, long Score)>();
                var combinacionActual = new List<T>();
                long iteraciones = 0;

                void Enumerar(int indice, long sumaActual, long score)
                {
                    if (++iteraciones % 4096 == 0)
                    {
                        _ct.ThrowIfCancellationRequested();
                    }

                    if (!mejoresPorSuma.TryGetValue(sumaActual, out var existente)
                        || score < existente.Score
                        || (score == existente.Score && combinacionActual.Count < existente.Combinacion.Count))
                    {
                        mejoresPorSuma[sumaActual] = (new List<T>(combinacionActual), score);
                    }

                    for (var i = indice; i < mitad.Count; i++)
                    {
                        var nuevaSuma = sumaActual + MontoACentavos(_monto(mitad[i]));
                        if (nuevaSuma > objetivoCentavos)
                        {
                            continue;
                        }

                        combinacionActual.Add(mitad[i]);
                        Enumerar(i + 1, nuevaSuma, score + _distancia(mitad[i]));
                        combinacionActual.RemoveAt(combinacionActual.Count - 1);
                    }
                }

                Enumerar(0, 0L, 0L);
                return mejoresPorSuma;
            }
        }
    }
}
