using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Extensions.DependencyInjection;
using Advance_Control.ViewModels;

namespace Advance_Control.Views
{
    /// <summary>
    /// Página para visualizar y gestionar proveedores del sistema
    /// </summary>
    public sealed partial class ProveedoresView : Page
    {
        public ProveedoresViewModel ViewModel { get; }

        public ProveedoresView()
        {
            // Resolver el ViewModel desde DI
            ViewModel = ((App)Application.Current).Host.Services.GetRequiredService<ProveedoresViewModel>();
            
            this.InitializeComponent();
            
            // Establecer el DataContext para los bindings
            this.DataContext = ViewModel;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            
            // Cargar los proveedores cuando se navega a esta página
            await ViewModel.LoadProveedoresAsync();
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.LoadProveedoresAsync();
        }

        private async void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.ClearFiltersAsync();
        }

        private void HeadGrid_Tapped(object sender, TappedRoutedEventArgs e)
        {
            // Get the ProveedorDto from the sender's Tag property
            if (sender is FrameworkElement element && element.Tag is Models.ProveedorDto proveedor)
            {
                proveedor.Expand = !proveedor.Expand;
            }
        }

        private void ToggleExpandButton_Click(object sender, RoutedEventArgs e)
        {
            // Get the ProveedorDto from the sender's Tag property
            if (sender is FrameworkElement element && element.Tag is Models.ProveedorDto proveedor)
            {
                proveedor.Expand = !proveedor.Expand;
            }
        }
    }
}
