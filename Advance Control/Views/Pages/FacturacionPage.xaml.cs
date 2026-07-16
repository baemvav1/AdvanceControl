using System;
using System.Runtime.InteropServices.WindowsRuntime;
using global::Windows.Foundation;
using Advance_Control.Models;
using Advance_Control.Services.Logging;
using Advance_Control.Utilities;
using Advance_Control.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using WinRT.Interop;

namespace Advance_Control.Views.Pages
{
    public sealed partial class FacturacionPage : Page
    {
        public FacturacionViewModel ViewModel { get; }

        public FacturacionPage()
        {
            InitializeComponent();
            ButtonClickLogger.Attach(this, AppServices.Get<ILoggingService>(), nameof(FacturacionPage));
            ViewModel = AppServices.Get<FacturacionViewModel>();
            DataContext = ViewModel;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            await ViewModel.CargarAsync();
        }

        private async void BtnActualizar_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.CargarAsync();
        }

        private async void BtnCargarXml_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Tag is not OperacionSinFacturaDto operacion)
            {
                return;
            }

            var hwnd = WindowNative.GetWindowHandle(App.MainWindow);
            await ViewModel.CargarXmlParaOperacionAsync(hwnd, XamlRoot, operacion);
        }

        private async void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Tag is not OperacionFacturadaDto operacion)
            {
                return;
            }

            var mensaje = operacion.TieneAbonos
                ? $"La factura {operacion.Folio} de la operación #{operacion.IdOperacion} ya tiene abonos registrados. ¿Aun así deseas cancelarla? Se eliminará el registro y el XML por completo."
                : $"¿Deseas cancelar la factura {operacion.Folio} de la operación #{operacion.IdOperacion}? Se eliminará el registro y el XML por completo.";

            var dialog = new ContentDialog
            {
                Title = "Confirmar cancelación",
                Content = mensaje,
                PrimaryButtonText = "Cancelar factura",
                CloseButtonText = "Volver",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await ViewModel.CancelarFacturaAsync(operacion);
            }
        }
    }
}
