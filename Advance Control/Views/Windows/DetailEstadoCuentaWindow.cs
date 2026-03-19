using System;
using Advance_Control.Models;
using Advance_Control.Views.Details;
using Microsoft.UI.Xaml;

namespace Advance_Control.Views.Windows
{
    public sealed class DetailEstadoCuentaWindow : Window
    {
        private readonly DetailEstadoCuentaView _detailView;
        private readonly int _idEstadoCuenta;

        public DetailEstadoCuentaWindow(EstadoCuentaResumenDto estadoCuenta)
        {
            if (estadoCuenta == null)
            {
                throw new ArgumentNullException(nameof(estadoCuenta));
            }

            _idEstadoCuenta = estadoCuenta.IdEstadoCuenta;
            _detailView = new DetailEstadoCuentaView();
            Content = _detailView;
            Title = $"Estado de cuenta - {estadoCuenta.CuentaTitulo}";

            Activated += DetailEstadoCuentaWindow_Activated;
            Closed += DetailEstadoCuentaWindow_Closed;
        }

        private async void DetailEstadoCuentaWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            Activated -= DetailEstadoCuentaWindow_Activated;
            await _detailView.InicializarAsync(_idEstadoCuenta);
        }

        private void DetailEstadoCuentaWindow_Closed(object sender, WindowEventArgs args)
        {
            Closed -= DetailEstadoCuentaWindow_Closed;
        }
    }
}
