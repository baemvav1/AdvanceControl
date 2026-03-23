using System;
using Advance_Control.Navigation;
using Advance_Control.Services.Levantamiento;
using Advance_Control.Services.Logging;
using Advance_Control.Utilities;
using Advance_Control.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Advance_Control.Views.Pages
{
    public sealed partial class LevantamientosView : Page
    {
        public LevantamientosViewModel ViewModel { get; }

        private readonly INavigationService _navigationService;
        private readonly ILoggingService _logger;

        public LevantamientosView()
        {
            ViewModel = AppServices.Get<LevantamientosViewModel>();
            _navigationService = AppServices.Get<INavigationService>();
            _logger = AppServices.Get<ILoggingService>();
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            await ViewModel.LoadLevantamientosAsync();
        }

        private void NuevoLevantamiento_Click(object sender, RoutedEventArgs e)
        {
            _navigationService.Navigate("Levantamiento");
        }

        private async void Refrescar_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.LoadLevantamientosAsync();
        }

        private void AbrirLevantamiento_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is LevantamientoListItemResponse item)
            {
                _navigationService.Navigate("Levantamiento", item.IdLevantamiento);
            }
        }

        private async void AbrirReporte_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is LevantamientoListItemResponse item)
            {
                var pdfPath = ViewModel.BuscarReportePdf(item.IdLevantamiento);
                if (string.IsNullOrEmpty(pdfPath))
                {
                    await ShowMessageAsync("Sin reporte",
                        "No se encontro un reporte PDF generado para este levantamiento. Abra el levantamiento y genere el reporte primero.");
                    return;
                }

                try
                {
                    var storageFile = await global::Windows.Storage.StorageFile.GetFileFromPathAsync(pdfPath);
                    await global::Windows.System.Launcher.LaunchFileAsync(storageFile);
                }
                catch (Exception ex)
                {
                    await _logger.LogErrorAsync("Error al abrir PDF", ex,
                        nameof(LevantamientosView), nameof(AbrirReporte_Click));
                    await ShowMessageAsync("Error", $"No se pudo abrir el reporte: {ex.Message}");
                }
            }
        }

        private async void EliminarLevantamiento_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is LevantamientoListItemResponse item)
            {
                var dialog = new ContentDialog
                {
                    Title = "Confirmar eliminacion",
                    Content = $"¿Desea eliminar el levantamiento #{item.IdLevantamiento} del equipo {item.EquipoIdentificador}?",
                    PrimaryButtonText = "Eliminar",
                    CloseButtonText = "Cancelar",
                    DefaultButton = ContentDialogButton.Close,
                    XamlRoot = this.XamlRoot
                };

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    var deleted = await ViewModel.DeleteLevantamientoAsync(item.IdLevantamiento);
                    if (!deleted)
                        await ShowMessageAsync("Error", "No se pudo eliminar el levantamiento.");
                }
            }
        }

        private async System.Threading.Tasks.Task ShowMessageAsync(string title, string message)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = "Aceptar",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }
    }
}
