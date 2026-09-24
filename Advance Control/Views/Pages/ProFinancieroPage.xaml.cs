using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.Facturas;
using Advance_Control.Utilities;
using Advance_Control.ViewModels;
using Advance_Control.Views.Dialogs;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;

namespace Advance_Control.Views.Pages
{
    /// <summary>
    /// Lienzo de prototipos del grupo Financiero. Visible solo para nivel 1 (devs)
    /// vía el sistema de permisos UI. Primer experimento: visor de factura (candidato a
    /// sustituir FacturasPage/DetailFacturaWindow), de momento hardcodeado a la factura 962.
    /// </summary>
    public sealed partial class ProFinancieroPage : Page
    {
        public ProFinancieroViewModel ViewModel { get; }

        public ProFinancieroPage()
        {
            ViewModel = AppServices.Get<ProFinancieroViewModel>();
            InitializeComponent();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await ViewModel.CargarFacturaAsync(ProFinancieroViewModel.SeriePrototipo, ProFinancieroViewModel.FolioPrototipo);
            await ActualizarVisorPdfAsync();
        }

        private async Task ActualizarVisorPdfAsync()
        {
            if (!ViewModel.HasPdf)
            {
                return;
            }

            await RenderizarPdfEnVisorAsync();
        }

        private async Task RenderizarPdfEnVisorAsync()
        {
            PdfPagesPanel.Children.Clear();

            try
            {
                var paginas = await PdfPreviewRenderer.RenderizarTodasLasPaginasAsync(ViewModel.PdfPath!);
                foreach (var pagina in paginas)
                {
                    PdfPagesPanel.Children.Add(pagina);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ProFinancieroPage::RenderizarPdfEnVisorAsync: {ex.GetType().Name} - {ex.Message}");
            }
        }

        private async void DescargaPDF_Click(object sender, RoutedEventArgs e)
        {
            if (!ViewModel.HasPdf)
            {
                return;
            }

            var nombreSugerido = $"Factura_{ViewModel.FolioTexto}".Replace(" ", "_");
            await GuardarArchivoAsync(ViewModel.PdfPath!, nombreSugerido, "Documento PDF", ".pdf");
        }

        private async void DescargaXml_Click(object sender, RoutedEventArgs e)
        {
            var xml = await ViewModel.ObtenerXmlAsync();
            if (xml == null)
            {
                return;
            }

            var rutaTemporal = Path.Combine(Path.GetTempPath(), $"factura_{ViewModel.FolioTexto}_{Guid.NewGuid():N}.xml");
            await File.WriteAllTextAsync(rutaTemporal, xml, System.Text.Encoding.UTF8);

            try
            {
                var nombreSugerido = $"Factura_{ViewModel.FolioTexto}".Replace(" ", "_");
                await GuardarArchivoAsync(rutaTemporal, nombreSugerido, "Documento XML", ".xml");
            }
            finally
            {
                try { File.Delete(rutaTemporal); } catch { /* archivo temporal, no crítico */ }
            }
        }

        // --- Panel derecho: acciones sobre la factura ---

        private async void RegistrarAbono_Click(object sender, RoutedEventArgs e)
        {
            var factura = ViewModel.Factura;
            if (factura == null)
            {
                return;
            }

            if (!factura.PermiteGestionInterna)
            {
                await MostrarMensajeAsync("Registrar abono", factura.TooltipCapturarPagoTexto);
                return;
            }

            var dialog = new RegistrarComplementoPagoDialog(factura, XamlRoot);
            var resultado = await dialog.ShowAsync();
            if (resultado == ContentDialogResult.Primary && dialog.ResultadoRequest != null)
            {
                await ViewModel.RegistrarAbonoAsync(dialog.ResultadoRequest);
                await ActualizarVisorPdfAsync();
            }
        }

        private async void GenerarComplemento_Click(object sender, RoutedEventArgs e)
        {
            var factura = ViewModel.Factura;
            if (factura == null)
            {
                return;
            }

            if (!factura.PuedeGenerarComplementoPago || string.IsNullOrWhiteSpace(factura.ReceptorRfc))
            {
                await MostrarMensajeAsync("Generar complemento de pago", factura.TooltipGenerarComplementoTexto);
                return;
            }

            var abonosPendientes = await ViewModel.ObtenerAbonosPendientesComplementoAsync(factura.ReceptorRfc);
            if (abonosPendientes.Count == 0)
            {
                await MostrarMensajeAsync("Generar complemento de pago", "No hay abonos PPD pendientes de complementar.");
                return;
            }

            var dialog = new GenerarComplementoPagoDialog(factura, abonosPendientes, XamlRoot);
            var resultado = await dialog.ShowAsync();
            if (resultado == ContentDialogResult.Primary && dialog.ResultadoRequest != null)
            {
                await ViewModel.GenerarComplementoPagoAsync(dialog.ResultadoRequest);
                await ActualizarVisorPdfAsync();
            }
        }

        private async void CancelarFactura_Click(object sender, RoutedEventArgs e)
        {
            var factura = ViewModel.Factura;
            if (factura == null)
            {
                return;
            }

            if (!factura.PuedeCancelarCfdi)
            {
                await MostrarMensajeAsync("Cancelar factura", factura.TooltipCancelarTexto);
                return;
            }

            // CancelarCfdiDialog necesita un FacturasViewModel solo para la búsqueda de la factura
            // que sustituye (motivo "01"); la cancelación en sí la ejecuta ProFinancieroViewModel.
            var facturasViewModel = AppServices.Get<FacturasViewModel>();
            var dialog = new CancelarCfdiDialog(factura, facturasViewModel, XamlRoot);
            var resultado = await dialog.ShowAsync();
            if (resultado == ContentDialogResult.Primary && dialog.ResultadoRequest != null)
            {
                await ViewModel.CancelarCfdiAsync(factura.IdFactura, dialog.ResultadoRequest);
                await ActualizarVisorPdfAsync();
            }
        }

        // --- Renglones de la lista de Complementos de pago ---

        private async void ComplementoPdf_Click(object sender, RoutedEventArgs e)
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

        private async void ComplementoEliminar_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is not ComplementoPagoRelacionadoDto complemento)
            {
                return;
            }

            // Un Complemento de Pago es un CFDI timbrado como cualquier factura: no se puede "eliminar",
            // solo cancelar ante el SAT. Se arma un FacturaResumenDto mínimo para reusar el mismo
            // diálogo/flujo de cancelación que ya usa FacturasPage.
            var facturaAdaptada = new FacturaResumenDto
            {
                IdFactura = complemento.IdFacturaComplemento,
                Serie = complemento.Serie,
                Folio = complemento.Folio,
                Uuid = complemento.Uuid,
                TipoDeComprobante = "P",
                ReceptorNombre = ViewModel.Factura?.ReceptorNombre,
                ReceptorRfc = ViewModel.Factura?.ReceptorRfc,
            };

            var facturasViewModel = AppServices.Get<FacturasViewModel>();
            var dialog = new CancelarCfdiDialog(facturaAdaptada, facturasViewModel, XamlRoot);
            var resultado = await dialog.ShowAsync();
            if (resultado == ContentDialogResult.Primary && dialog.ResultadoRequest != null)
            {
                await ViewModel.CancelarCfdiAsync(complemento.IdFacturaComplemento, dialog.ResultadoRequest);
                await ActualizarVisorPdfAsync();
            }
        }

        private async Task MostrarMensajeAsync(string titulo, string mensaje)
        {
            var dialog = new ContentDialog
            {
                Title = titulo,
                Content = mensaje,
                CloseButtonText = "Cerrar",
                XamlRoot = XamlRoot
            };
            await dialog.ShowAsync();
        }

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

            File.Copy(rutaOrigen, archivo.Path, overwrite: true);
        }
    }
}
