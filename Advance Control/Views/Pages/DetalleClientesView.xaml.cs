using System;
using System.Linq;
using Advance_Control.Models;
using Advance_Control.Services.Notificacion;
using Advance_Control.Utilities;
using Advance_Control.ViewModels;
using Advance_Control.Views.Windows;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;

namespace Advance_Control.Views.Pages
{
    public sealed partial class DetalleClientesView : Page
    {
        private static readonly string[] TiposContacto = { "Llamada", "Visita", "Correo", "WhatsApp", "Otro" };

        public DetalleClientesViewModel ViewModel { get; }
        private readonly INotificacionService _notificacionService;

        public DetalleClientesView()
        {
            ViewModel = AppServices.Get<DetalleClientesViewModel>();
            _notificacionService = AppServices.Get<INotificacionService>();
            InitializeComponent();
            DataContext = ViewModel;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            await ViewModel.InitializeAsync();
            ActualizarGrafico();
        }

        private async void ClienteCard_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is ClienteSaludCardItem item)
            {
                await ViewModel.SeleccionarClienteAsync(item.Cliente);
                ActualizarGrafico();
            }
        }

        private void DetalleTreeView_SelectionChanged(TreeView sender, TreeViewSelectionChangedEventArgs args)
        {
            var nodo = args.AddedItems.FirstOrDefault() as DetalleClienteTreeItem;
            ViewModel.SeleccionarNodoDetalle(nodo);
            ActualizarGrafico();
        }

        /// <summary>
        /// Clic derecho sobre un nodo de factura (Facturado &gt; Historial de facturas, o
        /// Adeudo &gt; Facturas sin pagar) abre un menú con "Ver factura" y, si la factura tiene
        /// una operación ligada, "Ver operación" — cada opción solo aparece cuando aplica. Se usa
        /// clic derecho (no clic simple) a propósito, para evitar que un misclick sobre el árbol
        /// abra una ventana sin querer.
        /// </summary>
        private void NodoFactura_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (sender is not FrameworkElement element || element.Tag is not DetalleClienteTreeItem nodo)
            {
                return;
            }

            if (!nodo.IdFactura.HasValue && !nodo.IdOperacion.HasValue)
            {
                return;
            }

            var menu = new MenuFlyout();

            if (nodo.IdFactura.HasValue)
            {
                var verFacturaItem = new MenuFlyoutItem { Text = "Ver factura" };
                verFacturaItem.Click += (_, _) => AbrirFactura(nodo.IdFactura.Value, nodo.Etiqueta);
                menu.Items.Add(verFacturaItem);
            }

            if (nodo.IdOperacion.HasValue)
            {
                var verOperacionItem = new MenuFlyoutItem { Text = "Ver operación" };
                verOperacionItem.Click += (_, _) => OperacionVisorNavigator.Navigate(nodo.IdOperacion.Value);
                menu.Items.Add(verOperacionItem);
            }

            menu.ShowAt(element, new FlyoutShowOptions { Position = e.GetPosition(element) });
        }

        private void AbrirFactura(int idFactura, string folio)
        {
            var factura = new FacturaResumenDto { IdFactura = idFactura, Folio = folio };
            var ventana = new DetailFacturaWindow(factura);
            ventana.Activate();
        }

        private async void BtnAgregarSeguimiento_Click(object sender, RoutedEventArgs e)
        {
            var tipoComboBox = new ComboBox
            {
                PlaceholderText = "Selecciona un tipo",
                Margin = new Thickness(0, 0, 0, 8),
                ItemsSource = TiposContacto
            };

            var notaTextBox = new TextBox
            {
                PlaceholderText = "Nota del contacto...",
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                MinHeight = 80,
                MaxHeight = 160,
                Margin = new Thickness(0, 0, 0, 8)
            };

            var promesaMontoTextBox = new TextBox
            {
                PlaceholderText = "Monto de la promesa (opcional)",
                InputScope = new InputScope { Names = { new InputScopeName(InputScopeNameValue.Number) } },
                Margin = new Thickness(0, 0, 0, 8)
            };

            var promesaFechaPicker = new DatePicker();

            var dialogContent = new StackPanel
            {
                Spacing = 8,
                Children =
                {
                    new TextBlock { Text = "Tipo de contacto:" },
                    tipoComboBox,
                    new TextBlock { Text = "Nota:" },
                    notaTextBox,
                    new TextBlock { Text = "Promesa de pago (opcional):" },
                    promesaMontoTextBox,
                    promesaFechaPicker
                }
            };

            var dialog = new ContentDialog
            {
                Title = "Agregar seguimiento",
                Content = dialogContent,
                PrimaryButtonText = "Guardar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary)
            {
                return;
            }

            if (tipoComboBox.SelectedItem is not string tipoContacto)
            {
                await _notificacionService.MostrarAsync("Campo requerido", "Selecciona un tipo de contacto.");
                return;
            }

            decimal? promesaMonto = null;
            if (!string.IsNullOrWhiteSpace(promesaMontoTextBox.Text))
            {
                if (!decimal.TryParse(promesaMontoTextBox.Text, out var monto))
                {
                    await _notificacionService.MostrarAsync("Monto inválido", "El monto de la promesa no es un número válido.");
                    return;
                }
                promesaMonto = monto;
            }

            // El DatePicker siempre trae una fecha seleccionada (hoy por defecto) aunque el usuario
            // no lo haya tocado, así que la fecha de promesa solo cuenta si además se capturó un monto.
            var dto = new CobranzaSeguimientoCreateDto
            {
                TipoContacto = tipoContacto,
                Nota = string.IsNullOrWhiteSpace(notaTextBox.Text) ? null : notaTextBox.Text,
                PromesaPagoMonto = promesaMonto,
                PromesaPagoFecha = promesaMonto.HasValue ? promesaFechaPicker.SelectedDate?.Date : null
            };

            await ViewModel.AgregarSeguimientoAsync(dto);
        }

        private void ActualizarGrafico()
        {
            var plot = GraficoPlot.Plot;
            plot.Clear();

            var filas = ViewModel.TimelineFilas;
            if (filas.Count == 0)
            {
                GraficoPlot.Refresh();
                return;
            }

            var etiquetas = new string[filas.Count];
            var yIndices = new double[filas.Count];

            for (var i = 0; i < filas.Count; i++)
            {
                var fila = filas[i];
                etiquetas[i] = fila.Etiqueta;
                yIndices[i] = i;
                var color = ScottPlot.Color.FromHex(fila.ColorHex);

                if (fila.Inicio.HasValue && fila.Fin.HasValue)
                {
                    var xs = new[] { fila.Inicio.Value, fila.Fin.Value };
                    var ys = new[] { (double)i, (double)i };
                    var segmento = plot.Add.Scatter(xs, ys, color);
                    segmento.LineWidth = 4;
                    segmento.MarkerSize = 6;
                }

                if (fila.Puntos.Count > 0)
                {
                    var xs = fila.Puntos.ToArray();
                    var ys = Enumerable.Repeat((double)i, fila.Puntos.Count).ToArray();
                    var puntos = plot.Add.Scatter(xs, ys, color);
                    puntos.LineWidth = 0;
                    puntos.MarkerSize = 8;
                }
            }

            plot.Axes.Left.SetTicks(yIndices, etiquetas);
            plot.Axes.DateTimeTicksBottom();
            plot.Axes.InvertY();

            GraficoPlot.Refresh();
        }
    }
}
