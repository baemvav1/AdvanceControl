namespace Advance_Control.Rules
{
    public sealed class ConciliacionRules
    {
        public ConciliacionUnoAUnoRules UnoAUno { get; init; } = new();
        public ConciliacionCombinacionalRules Combinacional { get; init; } = new();
        public ConciliacionAbonosRules Abonos { get; init; } = new();
        public ConciliacionMetodoPagoRules MetodoPago { get; init; } = new();
    }

    public sealed class ConciliacionUnoAUnoRules
    {
        public bool RequiereSaldoPendienteIgualAlTotal { get; init; } = true;
        public bool RequiereSinAbonosPrevios { get; init; } = true;
        public bool RequiereFacturaNoFiniquitada { get; init; } = true;
    }

    public sealed class ConciliacionCombinacionalRules
    {
        public int MinimoFacturasPorGrupo { get; init; } = 2;
    }

    public sealed class ConciliacionAbonosRules
    {
        /// <summary>
        /// Hasta este numero de candidatos se usa backtracking exacto de un solo hilo
        /// (rapido y ya probado en produccion). Por encima se usa busqueda "meet-in-the-middle"
        /// en paralelo (ver ConciliacionMatchingEngine.BuscarCombinacionMovimientosMeetInTheMiddle),
        /// para no truncar candidatos legitimos ni bloquear la UI con 2^n combinaciones.
        /// </summary>
        public int UmbralBusquedaExhaustiva { get; init; } = 22;

        /// <summary>Tiempo maximo por factura antes de omitirla y continuar con el resto del lote.</summary>
        public int TiempoLimitePorFacturaSegundos { get; init; } = 8;

        public int MinimoMovimientosPorCombinacion { get; init; } = 2;
    }

    public sealed class ConciliacionMetodoPagoRules
    {
        public string MetodoPagoUnaExhibicion { get; init; } = "PUE";
        public string MetodoPagoDiferido { get; init; } = "PPD";
        public bool PermitirMesesPosterioresParaPagoDiferido { get; init; } = true;
    }
}
