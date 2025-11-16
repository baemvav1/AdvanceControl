using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Extensions.DependencyInjection;
using Advance_Control.ViewModels;
using Advance_Control.Navigation;
using System.Threading.Tasks;

namespace Advance_Control.Views
{
    /// <summary>
    /// Página para visualizar y gestionar operaciones de mantenimiento
    /// </summary>
    public sealed partial class MttoView : Page, IReloadable
    {
        public MttoViewModel ViewModel { get; }

        public MttoView()
        {
            // Resolver el ViewModel desde DI (ahora es Singleton, mantiene estado)
            ViewModel = ((App)Application.Current).Host.Services.GetRequiredService<MttoViewModel>();
            
            this.InitializeComponent();
            
            // Establecer el DataContext para los bindings
            this.DataContext = ViewModel;
            
            // Habilitar caché de navegación para que la página no se destruya al navegar
            this.NavigationCacheMode = NavigationCacheMode.Enabled;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            
            // Inicializar la vista cuando se navega a esta página
            await ViewModel.InitializeAsync();
        }

        /// <summary>
        /// Implementación de IReloadable - recarga los datos de la página
        /// </summary>
        public async Task ReloadAsync()
        {
            await ViewModel.InitializeAsync(forceReload: true);
        }
    }
}
