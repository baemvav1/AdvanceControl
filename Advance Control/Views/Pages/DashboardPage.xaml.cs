using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Extensions.DependencyInjection;
using Advance_Control.ViewModels;
using Advance_Control.Navigation;
using Advance_Control.Services.Logging;
using Advance_Control.Utilities;

namespace Advance_Control.Views.Pages
{
    /// <summary>
    /// Página de inicio del sistema.
    /// Muestra bienvenida personalizada al usuario autenticado y el grid "Ops"
    /// con las operaciones/facturas pendientes de resolver.
    /// </summary>
    public sealed partial class DashboardPage : Page
    {
        public DashboardViewModel ViewModel { get; }
        private readonly INavigationService _navigationService;

        public DashboardPage()
        {
            ViewModel = AppServices.Get<DashboardViewModel>();
            _navigationService = AppServices.Get<INavigationService>();
            this.InitializeComponent();
            ButtonClickLogger.Attach(this, AppServices.Get<ILoggingService>(), nameof(DashboardPage));
            this.DataContext = ViewModel;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            await ViewModel.LoadAsync();
        }

        private void OpsAbiertasPendientes_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
            => _navigationService.Navigate("Operaciones");

        private void OpsTFinalizadoPendientes_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
            => _navigationService.Navigate("Operaciones");

        private void OpsFinalizadasPendientes_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
            => _navigationService.Navigate("Facturacion");
    }
}

