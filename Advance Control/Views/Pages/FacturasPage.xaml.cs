using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.PermisosUi;
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
            PestanaComplementos.Visibility = PuedeVerComplementos() ? Visibility.Visible : Visibility.Collapsed;
            await ViewModel.CargarFacturasAsync();
        }

        // --- Pestañas: Facturas | Complementos de pago ---

        /// <summary>
        /// La pestaña respeta el permiso del módulo ComplementosPagoAuditoriaPage, igual que lo hacía
        /// su antigua entrada del menú (MainViewModel.ShouldDisplayNavigationTag).
        /// </summary>
        private static bool PuedeVerComplementos()
        {
            var permisos = AppServices.Get<IPermisoUiRuntimeService>();
            if (!permisos.IsInitialized)
                return true;

            var moduleKey = permisos.BuildModuleKey(typeof(ComplementosPagoAuditoriaPage));
            return !permisos.TryGetModulo(moduleKey, out _) || permisos.CanAccessModule(moduleKey);
        }

        private void PestanasSelectorBar_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
        {
            // IsSelected="True" en el XAML dispara este evento mientras se arma la página,
            // antes de que existan los paneles.
            if (FacturasContenido == null || ComplementosFrame == null)
                return;

            var complementos = sender.SelectedItem == PestanaComplementos;
            FacturasContenido.Visibility = complementos ? Visibility.Collapsed : Visibility.Visible;
            ComplementosFrame.Visibility = complementos ? Visibility.Visible : Visibility.Collapsed;

            if (complementos && ComplementosFrame.Content == null)
            {
                ComplementosFrame.Navigate(typeof(ComplementosPagoAuditoriaPage));
                if (ComplementosFrame.Content is Page pagina)
                {
                    // Los permisos de botones solo se aplican solos al Frame principal.
                    pagina.Loaded += (s, _) =>
                        PermisoUiVisualBinder.ApplyToPage((Page)s, AppServices.Get<IPermisoUiRuntimeService>());
                }
            }
        }

        // --- Factura nueva desde cero (timbrado libre) ---

        private void BtnNuevaFactura_Click(SplitButton sender, SplitButtonClickEventArgs e)
        {
            var ventana = new NuevaFacturaWindow();
            ventana.FacturaTimbrada += async (s, resultado) =>
            {
                ViewModel.SuccessMessage = $"Factura timbrada correctamente. {resultado.Message}".Trim();
                await ViewModel.CargarFacturasAsync();
            };
            ventana.Activate();
        }

        // --- Carga de facturas externas (portal de Bilkon, folio sin serie) ---

        /// <summary>
        /// PermisoUiVisualBinder no alcanza los MenuFlyout (no están en el árbol visual), así que
        /// los permisos de las cargas se aplican al abrir el menú. También se respeta la clave
        /// "Button::..." de cuando eran botones sueltos, para no perder el nivel ya configurado.
        /// </summary>
        private void MenuCargas_Opening(object sender, object e)
        {
            var permisos = AppServices.Get<IPermisoUiRuntimeService>();
            if (!permisos.IsInitialized)
                return;

            var moduleKey = permisos.BuildModuleKey(GetType());
            foreach (var item in ((MenuFlyout)sender).Items)
            {
                if (item is MenuFlyoutItem opcion && !string.IsNullOrWhiteSpace(opcion.Text))
                {
                    opcion.IsEnabled =
                        permisos.CanAccessAction(permisos.BuildActionKey(moduleKey, nameof(MenuFlyoutItem), opcion.Text))
                        && permisos.CanAccessAction(permisos.BuildActionKey(moduleKey, nameof(Button), opcion.Text));
                }
            }
        }

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

        private async void BtnImportacionAutomatica_Click(object sender, RoutedEventArgs e)
        {
            var dialogo = new ImportacionAutomaticaDialog(XamlRoot);
            if (await dialogo.ShowAsync() != ContentDialogResult.Primary || dialogo.Resultado == null)
            {
                return;
            }

            var resumen = await ViewModel.ImportacionAutomaticaAsync(dialogo.Resultado, XamlRoot);
            var fallo = resumen == null;
            if (fallo && string.IsNullOrEmpty(ViewModel.ErrorMessage))
            {
                return;
            }

            // El error también queda en la barra de la página; aquí se muestra completo y copiable.
            var resumenDialogo = new ContentDialog
            {
                XamlRoot = XamlRoot,
                Title = fallo ? "La importación automática falló" : "Resultado de la importación automática",
                CloseButtonText = "Cerrar",
                Content = new ScrollViewer
                {
                    MaxHeight = 480,
                    Content = new TextBlock
                    {
                        Text = resumen ?? ViewModel.ErrorMessage,
                        TextWrapping = TextWrapping.Wrap,
                        IsTextSelectionEnabled = true
                    }
                }
            };
            await resumenDialogo.ShowAsync();
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

            var visor = new FacturaVisorWindow(factura);
            visor.Activate();
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
            if (ObtenerFactura(sender) is not FacturaResumenDto factura || !factura.PuedeCapturarPagoManual)
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

        private async void BtnGenerarComplementoPago_Click(object sender, RoutedEventArgs e)
        {
            if (ObtenerFactura(sender) is not FacturaResumenDto factura || !factura.PuedeGenerarComplementoPago)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(factura.ReceptorRfc))
            {
                return;
            }

            var abonosPendientes = await ViewModel.ObtenerAbonosPendientesComplementoAsync(factura.ReceptorRfc);
            var abonosRelevantes = AbonosComplementoHelper.FiltrarRelevantes(abonosPendientes, factura.IdFactura);
            if (abonosRelevantes.Count == 0)
            {
                return;
            }

            var dialog = new GenerarComplementoPagoDialog(factura, abonosRelevantes, XamlRoot);
            var resultado = await dialog.ShowAsync();
            if (resultado == ContentDialogResult.Primary && dialog.ResultadoRequest != null)
            {
                await ViewModel.GenerarComplementoPagoAsync(dialog.ResultadoRequest);
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
