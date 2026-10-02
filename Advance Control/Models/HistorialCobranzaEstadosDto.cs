namespace Advance_Control.Models
{
    /// <summary>
    /// Checks de Cobranza que deciden qué operaciones entran al historial. Una operación cae en
    /// exactamente una categoría (o en ninguna):
    /// - Abiertas: sin T-Finalizado, sin factura, CON orden de compra.
    /// - Abiertas S/Orden: sin T-Finalizado, sin factura, SIN orden de compra.
    /// - Finalizada C/Orden: T-Finalizado, sin factura, CON orden de compra.
    /// - Finalizada S/Orden: T-Finalizado, sin factura, SIN orden de compra.
    /// - Pendientes: con factura no cubierta en su totalidad (sin pago o pago parcial).
    /// Quedan fuera siempre: facturadas ya pagadas.
    /// </summary>
    public class HistorialCobranzaEstadosDto
    {
        public bool Abiertas { get; set; } = true;
        public bool AbiertasSinOrden { get; set; } = true;
        public bool FinalizadaConOrden { get; set; } = true;
        public bool FinalizadaSinOrden { get; set; } = true;
        public bool Pendientes { get; set; } = true;

        public bool AlgunoMarcado => Abiertas || AbiertasSinOrden || FinalizadaConOrden || FinalizadaSinOrden || Pendientes;
    }
}
