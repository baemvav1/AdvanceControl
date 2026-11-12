namespace Advance_Control.Models
{
    public sealed class ConciliacionAbonoMovimientoItemDto
    {
        public required int IdFactura { get; init; }
        public required string FolioFactura { get; init; }
        public required ConciliacionMovimientoResumenDto Movimiento { get; init; }

        /// <summary>Muestra "Descartar y reintentar" por movimiento (solo en el paso de Cheques; Abonos descarta la propuesta completa).</summary>
        public bool PermiteDescartar { get; init; } = true;

        public int IdMovimiento => Movimiento.IdMovimiento;
        public string FechaTexto => Movimiento.FechaTexto;
        public string GrupoId => Movimiento.GrupoId;
        public string MetadatosTexto => Movimiento.MetadatosTexto;
        public string AbonoTexto => Movimiento.AbonoTexto;
    }
}
