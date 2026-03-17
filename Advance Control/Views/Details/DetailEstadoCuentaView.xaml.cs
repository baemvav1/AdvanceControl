using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using Advance_Control.Services.Logging;
using Advance_Control.Utilities;
using Advance_Control.ViewModels;

namespace Advance_Control.Views.Details
{
    public sealed partial class DetailEstadoCuentaView : Page
    {
        public DetailEstadoCuentaViewModel ViewModel { get; }

        public DetailEstadoCuentaView()
        {
            ViewModel = AppServices.Get<DetailEstadoCuentaViewModel>();
            InitializeComponent();
            ButtonClickLogger.Attach(this, AppServices.Get<ILoggingService>(), nameof(DetailEstadoCuentaView));
            DataContext = ViewModel;
        }

        public async System.Threading.Tasks.Task InicializarAsync(int idEstadoCuenta)
        {
            if (idEstadoCuenta > 0)
            {
                await ViewModel.CargarDetalleAsync(idEstadoCuenta);
            }
            else
            {
                ViewModel.LimpiarFiltros();
            }
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is int idEstadoCuenta)
            {
                await InicializarAsync(idEstadoCuenta);
            }
        }

        private void BtnLimpiarFiltros_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            ViewModel.LimpiarFiltros();
        }

        private void BtnAlternarFiltros_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            ViewModel.AlternarFiltros();
        }
    }
}
