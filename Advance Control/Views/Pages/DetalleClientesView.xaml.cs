using System;
using System.Linq;
using Advance_Control.Models;
using Advance_Control.Utilities;
using Advance_Control.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;

namespace Advance_Control.Views.Pages
{
    public sealed partial class DetalleClientesView : Page
    {
        public DetalleClientesViewModel ViewModel { get; }

        public DetalleClientesView()
        {
            ViewModel = AppServices.Get<DetalleClientesViewModel>();
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
