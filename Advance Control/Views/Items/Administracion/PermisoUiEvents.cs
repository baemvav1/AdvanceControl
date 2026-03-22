using System;

namespace Advance_Control.Views.Items.Administracion
{
    public class PermisoModuloNivelEditRequestedEventArgs : EventArgs
    {
        public int IdPermisoModulo { get; init; }
    }

    public class PermisoAccionNivelEditRequestedEventArgs : EventArgs
    {
        public int IdPermisoAccionModulo { get; init; }
    }
}
