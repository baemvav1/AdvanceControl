using Advance_Control.Models;
using Advance_Control.Views.Details;
using Microsoft.UI.Xaml;

namespace Advance_Control.Views.Windows
{
    public sealed class DetailEstadoCuentaWindow : Window
    {
        private readonly DetailEstadoCuentaView _detailView;

        public DetailEstadoCuentaWindow(EstadoCuentaResumenDto estadoCuenta)
        {
            _detailView = new DetailEstadoCuentaView();
            Content = _detailView;
            Title = $"Estado de cuenta - {estadoCuenta.CuentaTitulo}";

            Activated += DetailEstadoCuentaWindow_Activated;
            Closed += DetailEstadoCuentaWindow_Closed;

            _ = _detailView.InicializarAsync(estadoCuenta.IdEstadoCuenta);
        }

        private void DetailEstadoCuentaWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            Activated -= DetailEstadoCuentaWindow_Activated;
        }

        private void DetailEstadoCuentaWindow_Closed(object sender, WindowEventArgs args)
        {
            Closed -= DetailEstadoCuentaWindow_Closed;
        }
    }
}
