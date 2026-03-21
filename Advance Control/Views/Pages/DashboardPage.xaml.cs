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
    /// Muestra bienvenida personalizada al usuario autenticado y secciones
    /// placeholder que a futuro mostrarán métricas, tareas y actividad reciente.
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

        private async void RefreshActividad_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
            => await ViewModel.LoadActividadAsync();

        private async void RefreshTareas_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
            => await ViewModel.LoadOperacionesPendientesAsync();

        private void ToggleTodo_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (sender is Microsoft.UI.Xaml.Controls.Button btn &&
                btn.Tag is Advance_Control.Models.OperacionTodoItem item)
            {
                item.Expand = !item.Expand;
            }
        }

        private void IrOperaciones_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
            => _navigationService.Navigate("Operaciones");

        private void IrMantenimiento_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
            => _navigationService.Navigate("Mantenimiento");

        private void IrClientes_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
            => _navigationService.Navigate("Clientes");
    }
}

