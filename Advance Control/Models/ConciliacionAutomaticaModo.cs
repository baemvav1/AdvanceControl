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
        Cheques = 3
    }
}
