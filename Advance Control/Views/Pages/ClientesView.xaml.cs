using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Extensions.DependencyInjection;
using Advance_Control.ViewModels;
using Advance_Control.Navigation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Advance_Control.Views
{
    /// <summary>
    /// Página para visualizar y gestionar clientes
    /// </summary>
    public sealed partial class ClientesView : Page, IReloadable
    {
        public CustomersViewModel ViewModel { get; }

        public ClientesView()
        {
            // Resolver el ViewModel desde DI (ahora es Singleton, mantiene estado)
            ViewModel = ((App)Application.Current).Host.Services.GetRequiredService<CustomersViewModel>();
            
            this.InitializeComponent();
            
            // Establecer el DataContext para los bindings
            this.DataContext = ViewModel;
            
            // Habilitar caché de navegación para que la página no se destruya al navegar
            this.NavigationCacheMode = NavigationCacheMode.Enabled;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            
            // Solo cargar clientes si la colección está vacía (primera vez)
            // Esto previene recargas innecesarias al navegar de vuelta a la página
            if (ViewModel.Customers.Count == 0)
            {
                await ViewModel.LoadClientesAsync();
            }
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.LoadClientesAsync();
        }

        private async void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.ClearFiltersAsync();
        }

        /// <summary>
        /// Implementación de IReloadable - recarga los datos de la página
        /// </summary>
        public async Task ReloadAsync()
        {
            await ViewModel.LoadClientesAsync();
        }
    }
}
