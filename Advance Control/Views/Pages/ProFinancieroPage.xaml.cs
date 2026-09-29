using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.Clientes;
using Advance_Control.Services.Contactos;
using Advance_Control.Services.Facturas;
using Advance_Control.Utilities;
using Advance_Control.ViewModels;
using Advance_Control.Views.Dialogs;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Windows.Storage.Pickers;

namespace Advance_Control.Views.Pages
{
    /// <summary>
    /// Visor de factura del grupo Financiero -- reemplaza a DetailFacturaWindow como pantalla de
    /// detalle abierta desde el botón "Abrir" de Facturas (vía <see cref="Views.Windows.FacturaVisorWindow"/>).
    /// Sigue registrada también como página "ProFinanciero" en el navbar de prototipos (solo
    /// nivel 1/devs) para poder navegar a ella sin una factura seleccionada durante desarrollo.
    /// </summary>
    public sealed partial class ProFinancieroPage : Page
    {
        private readonly IClienteService _clienteService;
        private readonly IContactoService _contactoService;
        private int? _idFacturaPendiente;

        public ProFinancieroViewModel ViewModel { get; }

        public ProFinancieroPage()
        {
            ViewModel = AppServices.Get<ProFinancieroViewModel>();
            _clienteService = AppServices.Get<IClienteService>();
            _contactoService = AppServices.Get<IContactoService>();
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            _idFacturaPendiente = e.Parameter switch
            {
                FacturaResumenDto factura => factura.IdFactura,
                int idFactura => idFactura,
                _ => null
            };
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (_idFacturaPendiente is not int idFactura)
            {
                ViewModel.MostrarMensajeSinFactura();
                return;
            }

            await ViewModel.CargarFacturaAsync(idFactura);
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
            PdfScrollViewer.ChangeView(0, 0, 1f, disableAnimation: true);

            try
            {
                // Render a mayor resolución que el default para que el texto siga nítido con zoom.
                var paginas = await PdfPreviewRenderer.RenderizarTodasLasPaginasAsync(ViewModel.PdfPath!, anchoPixeles: 2400);
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

        private const float PasoZoom = 1.25f;

        private void PdfScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Con scroll horizontal habilitado el contenido se mediría a ancho infinito (imagen a
            // tamaño nativo); fijarlo al viewport hace que zoom 100 % sea "ajustar al ancho".
            PdfPagesPanel.Width = e.NewSize.Width;
        }

        private void PdfScrollViewer_ViewChanged(object? sender, ScrollViewerViewChangedEventArgs e)
        {
            ZoomTexto.Text = $"{Math.Round(PdfScrollViewer.ZoomFactor * 100)} %";
        }

        private void ZoomAcercar_Click(object sender, RoutedEventArgs e) => CambiarZoom(PdfScrollViewer.ZoomFactor * PasoZoom);

        private void ZoomAlejar_Click(object sender, RoutedEventArgs e) => CambiarZoom(PdfScrollViewer.ZoomFactor / PasoZoom);

        private void ZoomAjustar_Click(object sender, RoutedEventArgs e) => CambiarZoom(1f);

        /// <summary>Cambia el zoom manteniendo fijo el punto al centro del viewport.</summary>
        private void CambiarZoom(float zoomDeseado)
        {
            var sv = PdfScrollViewer;
            var zoomActual = sv.ZoomFactor;
            var zoomNuevo = Math.Clamp(zoomDeseado, sv.MinZoomFactor, sv.MaxZoomFactor);

            var centroX = (sv.HorizontalOffset + sv.ViewportWidth / 2) / zoomActual;
            var centroY = (sv.VerticalOffset + sv.ViewportHeight / 2) / zoomActual;

            sv.ChangeView(
                Math.Max(0, centroX * zoomNuevo - sv.ViewportWidth / 2),
                Math.Max(0, centroY * zoomNuevo - sv.ViewportHeight / 2),
                zoomNuevo);
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

            if (!factura.PuedeCapturarPagoManual)
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
            var abonosRelevantes = AbonosComplementoHelper.FiltrarRelevantes(abonosPendientes, factura.IdFactura);
            if (abonosRelevantes.Count == 0)
            {
                await MostrarMensajeAsync("Generar complemento de pago", "No hay abonos PPD pendientes de complementar.");
                return;
            }

            var dialog = new GenerarComplementoPagoDialog(factura, abonosRelevantes, XamlRoot);
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

        /// <summary>
        /// Envía la factura por correo, calcado del flujo de EnviarCotizacionDialog usado por
        /// Cotización/Reporte: matchea contactos por el RFC del receptor y, si hay más de uno,
        /// deja elegir a cuál dirigirlo.
        /// </summary>
        private async void EnviarHistorial_Click(object sender, RoutedEventArgs e)
        {
            var factura = ViewModel.Factura;
            if (factura == null || !ViewModel.HasPdf)
            {
                return;
            }

            var (contactoPrincipal, contactos) = await ObtenerContactosClienteAsync(factura.ReceptorRfc);

            var partesSaludo = new[] { contactoPrincipal?.Tratamiento, contactoPrincipal?.Nombre, contactoPrincipal?.Apellido }
                .Where(p => !string.IsNullOrWhiteSpace(p));
            var destinatario = string.Join(" ", partesSaludo);
            if (string.IsNullOrWhiteSpace(destinatario))
            {
                destinatario = "cliente";
            }

            var mensaje =
                $"Estimado: {destinatario}.\n\n" +
                $"En el siguiente correo, adjuntamos la factura {factura.FolioTitulo}.\n\n" +
                "Saludos Cordiales";

            var email = new EnviarCotizacionDialog(
                ViewModel.PdfPath!,
                contactoPrincipal,
                contactos,
                factura.ReceptorNombre ?? string.Empty,
                XamlRoot,
                tipo: "Factura",
                asuntoPersonalizado: $"Factura {factura.FolioTitulo}",
                mensajePersonalizado: mensaje);

            if (await email.ShowAsync() == ContentDialogResult.Primary)
            {
                await MostrarMensajeAsync("Enviar por correo", "La factura fue enviada correctamente.");
            }
        }

        /// <summary>
        /// Resuelve el cliente por RFC del receptor y trae sus contactos, igual que hacen los
        /// botones de enviar cotización/reporte por operación. Si hay más de un contacto, deja
        /// elegir a cuál dirigir el correo (o continuar sin destinatario preseleccionado).
        /// </summary>
        private async Task<(ContactoDto? Principal, List<ContactoDto> Todos)> ObtenerContactosClienteAsync(string? receptorRfc)
        {
            if (string.IsNullOrWhiteSpace(receptorRfc))
            {
                return (null, new List<ContactoDto>());
            }

            try
            {
                var clientes = await _clienteService.GetClientesAsync(new ClienteQueryDto { Rfc = receptorRfc });
                var cliente = clientes.FirstOrDefault(c => string.Equals(c.Rfc, receptorRfc, StringComparison.OrdinalIgnoreCase));
                if (cliente == null)
                {
                    return (null, new List<ContactoDto>());
                }

                var contactos = await _contactoService.GetContactosAsync(new ContactoQueryDto { IdCliente = cliente.IdCliente });
                if (contactos.Count == 0)
                {
                    return (null, contactos);
                }

                if (contactos.Count == 1)
                {
                    return (contactos[0], contactos);
                }

                var lv = new ListView
                {
                    ItemsSource = contactos,
                    DisplayMemberPath = "NombreCompleto",
                    SelectionMode = ListViewSelectionMode.Single,
                    MaxHeight = 300
                };
                var seleccion = new ContentDialog
                {
                    Title = "¿A quién va dirigido el correo?",
                    Content = new ScrollViewer { Content = lv, MaxHeight = 320 },
                    PrimaryButtonText = "Seleccionar",
                    SecondaryButtonText = "Omitir",
                    DefaultButton = ContentDialogButton.Primary,
                    XamlRoot = XamlRoot
                };

                var principal = await seleccion.ShowAsync() == ContentDialogResult.Primary && lv.SelectedItem is ContactoDto elegido
                    ? elegido
                    : null;

                return (principal, contactos);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ProFinancieroPage::ObtenerContactosClienteAsync: {ex.GetType().Name} - {ex.Message}");
                return (null, new List<ContactoDto>());
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
