using System;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using global::Windows.Foundation;
using Advance_Control.Models;
using Advance_Control.Services.Logging;
using Advance_Control.Utilities;
using Advance_Control.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
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

        private async void BtnVincularAutomatico_Click(object sender, RoutedEventArgs e)
        {
            var ventana = new Windows.ConfirmacionVinculacionFacturaWindow();
            ventana.Activate();

            var vinculadas = await ventana.ResultTask;
            if (vinculadas > 0)
            {
                await ViewModel.CargarAsync();
            }
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

        private void SinFacturaCard_DragOver(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                e.AcceptedOperation = DataPackageOperation.Copy;
                e.DragUIOverride.Caption = "Vincular factura XML";
                e.DragUIOverride.IsGlyphVisible = true;
            }
        }

        private void SinFacturaCard_DragEnter(object sender, DragEventArgs e)
        {
            if (sender is Border border && e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                border.BorderBrush = new SolidColorBrush(Colors.DodgerBlue);
                border.BorderThickness = new Thickness(2);
            }
        }

        private void SinFacturaCard_DragLeave(object sender, DragEventArgs e)
        {
            if (sender is Border border)
            {
                border.ClearValue(Border.BorderBrushProperty);
                border.ClearValue(Border.BorderThicknessProperty);
            }
        }

        private async void SinFacturaCard_Drop(object sender, DragEventArgs e)
        {
            if (sender is not Border border || border.Tag is not OperacionSinFacturaDto operacion)
            {
                return;
            }

            border.ClearValue(Border.BorderBrushProperty);
            border.ClearValue(Border.BorderThicknessProperty);

            if (!e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                return;
            }

            var items = await e.DataView.GetStorageItemsAsync();
            var file = items.OfType<StorageFile>()
                .FirstOrDefault(f => string.Equals(f.FileType, ".xml", StringComparison.OrdinalIgnoreCase));
            if (file == null)
            {
                return;
            }

            await ViewModel.ProcesarArchivoXmlAsync(file, operacion);
        }

        private async void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Tag is not OperacionFacturadaDto operacion)
            {
                return;
            }

            var mensaje = operacion.TieneAbonos
                ? $"La factura {operacion.Folio} de la operación #{operacion.IdOperacion} ya tiene abonos registrados. ¿Aun así deseas eliminarla? Se eliminará el registro y el XML por completo."
                : $"¿Deseas eliminar la factura {operacion.Folio} de la operación #{operacion.IdOperacion}? Se eliminará el registro y el XML por completo.";

            var dialog = new ContentDialog
            {
                Title = "Confirmar eliminación",
                Content = mensaje,
                PrimaryButtonText = "Eliminar factura",
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

        private async void BtnDesligar_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Tag is not OperacionFacturadaDto operacion)
            {
                return;
            }

            var dialog = new ContentDialog
            {
                Title = "Confirmar desvinculación",
                Content = $"¿Deseas desligar la factura {operacion.Folio} de la operación #{operacion.IdOperacion}? " +
                    "La factura NO se borra: vuelve al listado general de facturas para poder vincularla a otra operación.",
                PrimaryButtonText = "Desligar",
                CloseButtonText = "Volver",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await ViewModel.DesligarFacturaAsync(operacion);
            }
        }

        private async void BtnVincularSugerencia_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Tag is not FacturaResumenDto factura)
            {
                return;
            }

            var operacion = FindAncestorDataContext<OperacionSinFacturaDto>(button);
            if (operacion == null)
            {
                return;
            }

            var dialog = new ContentDialog
            {
                Title = "Confirmar vínculo",
                Content = $"¿Vincular la factura {factura.Folio} ({factura.TotalTexto}, {factura.FechaTexto}) a la operación #{operacion.IdOperacion}?",
                PrimaryButtonText = "Vincular",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await ViewModel.VincularSugerenciaAsync(operacion, factura);
            }
        }

        private static T? FindAncestorDataContext<T>(FrameworkElement element) where T : class
        {
            DependencyObject? current = element;
            while (current != null)
            {
                if (current is FrameworkElement fe && fe.DataContext is T match)
                {
                    return match;
                }

                current = VisualTreeHelper.GetParent(current);
            }

            return null;
        }
    }
}
