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
        public int MaximoMovimientosCandidatos { get; init; } = 15;
        public int MinimoMovimientosPorCombinacion { get; init; } = 2;
    }

    public sealed class ConciliacionMetodoPagoRules
    {
        public string MetodoPagoDiferido { get; init; } = "PPD";
        public bool PermitirMesesPosterioresParaPagoDiferido { get; init; } = true;
    }
}
