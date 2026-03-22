using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Extensions.DependencyInjection;
using Advance_Control.Services.Logging;
using Advance_Control.Utilities;
using Advance_Control.ViewModels;

namespace Advance_Control.Views.Pages
{
    /// <summary>
    /// Página base para el módulo de levantamiento.
    /// </summary>
    public sealed partial class LevantamientoView : Page
    {
        public LevantamientoViewModel ViewModel { get; }

        public LevantamientoView()
        {
            ViewModel = ((App)Application.Current).Host.Services.GetRequiredService<LevantamientoViewModel>();

            InitializeComponent();
            ButtonClickLogger.Attach(this, ((App)Application.Current).Host.Services.GetRequiredService<ILoggingService>(), nameof(LevantamientoView));

            DataContext = ViewModel;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            await ViewModel.InitializeAsync();
        }

    }
}
