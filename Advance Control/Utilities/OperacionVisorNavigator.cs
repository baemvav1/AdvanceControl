using Advance_Control.Models;
using Advance_Control.Navigation;

namespace Advance_Control.Utilities
{
    public static class OperacionVisorNavigator
    {
        public static bool Navigate(OperacionDto operacion)
        {
            return AppServices.Get<INavigationService>()
                .Navigate("OperacionVisor", OperacionVisorNavigationContext.FromOperacion(operacion));
        }

        public static bool Navigate(MensajeDto mensaje)
        {
            if (!mensaje.IdReferencia.HasValue)
                return false;

            return AppServices.Get<INavigationService>()
                .Navigate("OperacionVisor", OperacionVisorNavigationContext.FromMensaje(mensaje));
        }
    }
}
