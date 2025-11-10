using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Extensions.DependencyInjection;
using Advance_Control.ViewModels;

namespace Advance_Control.Views
{
    /// <summary>
    /// Página para visualizar y gestionar operaciones de mantenimiento
    /// </summary>
    public sealed partial class MttoView : Page
    {
        public MttoViewModel ViewModel { get; }

        public MttoView()
        {
            // Resolver el ViewModel desde DI
            ViewModel = ((App)Application.Current).Host.Services.GetRequiredService<MttoViewModel>();
            
            this.InitializeComponent();
            
            // Establecer el DataContext para los bindings
            this.DataContext = ViewModel;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            
            // Inicializar la vista cuando se navega a esta página
            await ViewModel.InitializeAsync();
        }
    }
}
