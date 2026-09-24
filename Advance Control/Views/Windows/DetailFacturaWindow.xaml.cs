using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Utilities;
using Advance_Control.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;

namespace Advance_Control.Views.Windows
{
    public sealed partial class DetailFacturaWindow : Window
    {
        private readonly int _idFactura;
        public DetailFacturaViewModel ViewModel { get; }

        public DetailFacturaWindow(FacturaResumenDto factura)
        {
            if (factura == null)
            {
                throw new ArgumentNullException(nameof(factura));
            }

            _idFactura = factura.IdFactura;
            ViewModel = AppServices.Get<DetailFacturaViewModel>();

            InitializeComponent();
            RootGrid.DataContext = this;
            Title = factura.FolioTitulo;
            Activated += DetailFacturaWindow_Activated;
        }

        private async void DetailFacturaWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            Activated -= DetailFacturaWindow_Activated;
            await ViewModel.CargarDetalleAsync(_idFactura);
        }

        private void BtnUsarSaldoCompleto_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.UsarSaldoCompleto();
        }

        private async void BtnRegistrarAbono_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.RegistrarAbonoAsync();
        }

        private async void BtnDescargarPdf_Click(object sender, RoutedEventArgs e)
        {
            var rutaPdf = await ViewModel.GenerarPdfAsync();
            if (rutaPdf == null)
            {
                return;
            }

            var nombreSugerido = $"Factura_{ViewModel.Factura?.FolioTitulo}".Replace(" ", "_");
            await GuardarArchivoAsync(rutaPdf, nombreSugerido, "Documento PDF", ".pdf");
        }

        private async void BtnDescargarComplementoPdf_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is not ComplementoPagoRelacionadoDto complemento)
            {
                return;
            }

            var rutaPdf = await ViewModel.GenerarPdfComplementoAsync(complemento.IdFacturaComplemento);
            if (rutaPdf == null)
            {
                return;
            }

            var nombreSugerido = $"ComplementoPago_{complemento.FolioTitulo}".Replace(" ", "_");
            await GuardarArchivoAsync(rutaPdf, nombreSugerido, "Documento PDF", ".pdf");
        }

        private async void BtnDescargarXml_Click(object sender, RoutedEventArgs e)
        {
            var xml = await ViewModel.ObtenerXmlAsync();
            if (xml == null)
            {
                return;
            }

            var rutaTemporal = Path.Combine(Path.GetTempPath(), $"factura_{_idFactura}_{Guid.NewGuid():N}.xml");
            await File.WriteAllTextAsync(rutaTemporal, xml, System.Text.Encoding.UTF8);

            try
            {
                var nombreSugerido = $"Factura_{ViewModel.Factura?.FolioTitulo}".Replace(" ", "_");
                await GuardarArchivoAsync(rutaTemporal, nombreSugerido, "Documento XML", ".xml");
            }
            finally
            {
                try { File.Delete(rutaTemporal); } catch { /* archivo temporal, no crítico */ }
            }
        }

        private async Task GuardarArchivoAsync(string rutaOrigen, string nombreSugerido, string tipoDescripcion, string extension)
        {
            var picker = new FileSavePicker();
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
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
                    XamlRoot = Content.XamlRoot
                };
                await dialog.ShowAsync();
            }
        }
    }
}
