namespace Advance_Control.Models
{
    public class OperacionVisorNavigationContext
    {
        public OperacionDto? Operacion { get; init; }
        public int? IdOperacion { get; init; }
        public long? MensajeReferenciaId { get; init; }

        public static OperacionVisorNavigationContext FromOperacion(OperacionDto operacion)
        {
            return new OperacionVisorNavigationContext
            {
                Operacion = operacion,
                IdOperacion = operacion.IdOperacion
            };
        }

        public static OperacionVisorNavigationContext FromMensaje(MensajeDto mensaje)
        {
            return new OperacionVisorNavigationContext
            {
                IdOperacion = mensaje.IdReferencia,
                MensajeReferenciaId = mensaje.Id
            };
        }
    }
}
