using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Extensions.DependencyInjection;
using Advance_Control.Models;
using Advance_Control.Services.Logging;
using Advance_Control.Utilities;
using Advance_Control.ViewModels;
using Advance_Control.Views.Dialogs;
using System;

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

        private async void HotspotButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { Tag: string hotspotKey })
            {
                return;
            }

            var hotspot = ViewModel.TryGetHotspot(hotspotKey);
            if (hotspot is null)
            {
                return;
            }

            ViewModel.SelectHotspot(hotspot);

            var dialog = new LevantamientoFallaDialog(hotspot.Titulo, hotspot.DescripcionFalla, XamlRoot);
            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary)
            {
                return;
            }

            ViewModel.RegisterFailure(hotspot, dialog.DescripcionCapturada);
        }

    }
}
