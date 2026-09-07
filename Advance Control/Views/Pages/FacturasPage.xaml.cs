using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Utilities;
using Advance_Control.ViewModels;
using Advance_Control.Views.Dialogs;
using Advance_Control.Views.Windows;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Windows.Storage.Pickers;

namespace Advance_Control.Views.Pages
{
    public sealed partial class FacturasPage : Page
    {
        public FacturasViewModel ViewModel { get; }

        public FacturasPage()
        {
            InitializeComponent();
            ViewModel = AppServices.Get<FacturasViewModel>();
            DataContext = ViewModel;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            await ViewModel.CargarFacturasAsync();
        }

        // --- Carga de facturas externas (portal de Bilkon, folio sin serie) ---

        private async void BtnCargarFacturaXml_Click(object sender, RoutedEventArgs e)
        {
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            await ViewModel.CargarArchivoXmlAsync(hwnd, XamlRoot);
        }

        private async void BtnCargarMultiplesFacturas_Click(object sender, RoutedEventArgs e)
        {
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            await ViewModel.CargarYGuardarMultiplesFacturasAsync(hwnd, XamlRoot);
        }

        // --- Buscador ---

        private void BusquedaAutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                ViewModel.ActualizarSugerencias(sender.Text);
            }
        }

        private void BusquedaAutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            var texto = args.ChosenSuggestion is FacturaResumenDto elegida
                ? elegida.Uuid ?? elegida.FolioTitulo
                : args.QueryText;

            ViewModel.TextoBusqueda = texto;
            ViewModel.Buscar();
        }

        private void BtnBuscar_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.TextoBusqueda = BusquedaAutoSuggestBox.Text;
            ViewModel.Buscar();
        }

        private void BtnLimpiarFiltros_Click(object sender, RoutedEventArgs e)
        {
            BusquedaAutoSuggestBox.Text = string.Empty;
            ViewModel.LimpiarFiltros();
        }

        // --- Paginación ---

        private void BtnPaginaAnterior_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.IrAPaginaAnterior();
        }

        private void BtnPaginaSiguiente_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.IrAPaginaSiguiente();
        }

        // --- Acciones por factura ---

        private void BtnAbrir_Click(object sender, RoutedEventArgs e)
        {
            if (ObtenerFactura(sender) is not FacturaResumenDto factura)
            {
                return;
            }

            var detalleWindow = new DetailFacturaWindow(factura);
            detalleWindow.Activate();
        }

        private async void BtnDescargarPdf_Click(object sender, RoutedEventArgs e)
        {
            if (ObtenerFactura(sender) is not FacturaResumenDto factura)
            {
                return;
            }

            var rutaPdf = await ViewModel.GenerarPdfAsync(factura);
            if (rutaPdf == null)
            {
                return;
            }

            var nombreSugerido = $"Factura_{factura.FolioTitulo}".Replace(" ", "_");
            await GuardarArchivoAsync(rutaPdf, nombreSugerido, "Documento PDF", ".pdf");
        }

        private async void BtnComprobanteCancelacion_Click(object sender, RoutedEventArgs e)
        {
            if (ObtenerFactura(sender) is not FacturaResumenDto factura)
            {
                return;
            }

            var rutaPdf = await ViewModel.GenerarAcuseCancelacionPdfAsync(factura);
            if (rutaPdf == null)
            {
                return;
            }

            var nombreSugerido = $"AcuseCancelacion_{factura.FolioTitulo}".Replace(" ", "_");
            await GuardarArchivoAsync(rutaPdf, nombreSugerido, "Documento PDF", ".pdf");
        }

        private async void BtnDescargarXml_Click(object sender, RoutedEventArgs e)
        {
            if (ObtenerFactura(sender) is not FacturaResumenDto factura)
            {
                return;
            }

            var xml = await ViewModel.ObtenerXmlAsync(factura);
            if (xml == null)
            {
                return;
            }

            var rutaTemporal = Path.Combine(Path.GetTempPath(), $"factura_{factura.IdFactura}_{Guid.NewGuid():N}.xml");
            await File.WriteAllTextAsync(rutaTemporal, xml, System.Text.Encoding.UTF8);

            try
            {
                var nombreSugerido = $"Factura_{factura.FolioTitulo}".Replace(" ", "_");
                await GuardarArchivoAsync(rutaTemporal, nombreSugerido, "Documento XML", ".xml");
            }
            finally
            {
                try { File.Delete(rutaTemporal); } catch { /* archivo temporal, no crítico */ }
            }
        }

        private async void BtnComplementoPago_Click(object sender, RoutedEventArgs e)
        {
            if (ObtenerFactura(sender) is not FacturaResumenDto factura || !factura.PermiteGestionInterna)
            {
                return;
            }

            var dialog = new RegistrarComplementoPagoDialog(factura, XamlRoot);
            var resultado = await dialog.ShowAsync();
            if (resultado == ContentDialogResult.Primary && dialog.ResultadoRequest != null)
            {
                await ViewModel.RegistrarComplementoPagoAsync(dialog.ResultadoRequest);
            }
        }

        private async void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            if (ObtenerFactura(sender) is not FacturaResumenDto factura || !factura.PuedeCancelarCfdi)
            {
                return;
            }
            var dialog = new CancelarCfdiDialog(factura, ViewModel, XamlRoot);
            var resultado = await dialog.ShowAsync();
            if (resultado == ContentDialogResult.Primary && dialog.ResultadoRequest != null)
            {
                await ViewModel.CancelarCfdiAsync(factura, dialog.ResultadoRequest);
            }
        }

        private static FacturaResumenDto? ObtenerFactura(object sender)
            => (sender as FrameworkElement)?.Tag as FacturaResumenDto;

        private async Task GuardarArchivoAsync(string rutaOrigen, string nombreSugerido, string tipoDescripcion, string extension)
        {
            var picker = new FileSavePicker();
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            picker.SuggestedStartLocation = PickerLocationId.Downloads;
            picker.SuggestedFileName = nombreSugerido;
            picker.FileTypeChoices.Add(tipoDescripcion, new List<string> { extension });

            var archivo = await picker.PickSaveFileAsync();
            if (archivo == null)
            {
                return; // Usuario canceló
            }

            try
            {
                File.Copy(rutaOrigen, archivo.Path, overwrite: true);
            }
            catch (Exception ex)
            {
                var dialog = new ContentDialog
                {
                    Title = "Descargar factura",
                    Content = $"Error al guardar el archivo: {ex.Message}",
                    CloseButtonText = "Cerrar",
                    XamlRoot = XamlRoot
                };
                await dialog.ShowAsync();
            }
        }
    }
}
