using Advance_Control.Models;
using Advance_Control.Views.Windows;

namespace Advance_Control.Utilities
{
    public static class OperacionVisorNavigator
    {
        public static bool Navigate(OperacionDto operacion)
        {
            OperacionVisorWindow.Open(operacion);
            return true;
        }

        public static bool Navigate(MensajeDto mensaje)
        {
            return OperacionVisorWindow.Open(mensaje) != null;
        }

        public static bool Navigate(int idOperacion)
        {
            OperacionVisorWindow.Open(idOperacion);
            return true;
        }
    }
}
