namespace Advance_Control.Models
{
    public enum ConciliacionAutomaticaModo
    {
        Automatica = 0,
        Combinacional = 1,
        Abonos = 2,

        /// <summary>
        /// Paso combinado (1 a 1 + 1 a varios + varios a 1) restringido a movimientos de
        /// cheque. Corre al inicio de la secuencia automatica, antes de los demas pasos.
        /// </summary>
        Cheques = 3,

        /// <summary>
        /// Liga Complementos de Pago (CFDI Pagos 2.0) ya timbrados, que citan una factura propia
        /// pero todavia no tienen ningun abono interno registrado, contra movimientos bancarios
        /// por monto + fecha.
        /// </summary>
        Complementos = 4
    }
}
