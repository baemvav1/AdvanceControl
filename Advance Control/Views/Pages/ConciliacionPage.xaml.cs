using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Utilities;
using Advance_Control.ViewModels;
using Advance_Control.Views.Windows;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Advance_Control.Views.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ConciliacionPage : Page
    {
        private bool _isResizingMovimientos;
        private bool _isResizingConciliacion;
        private bool _isResizingFacturaAbono;
        private readonly ProgressRing _conciliacionProgressRing;
        public ConciliacionViewModel ViewModel { get; }

        public ConciliacionPage()
        {
            ViewModel = AppServices.Get<ConciliacionViewModel>();
            InitializeComponent();
            DataContext = ViewModel;

            _conciliacionProgressRing = new ProgressRing
            {
                Width = 56,
                Height = 56,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                IsHitTestVisible = false
            };
            ConciliacionPanelGrid.Children.Add(_conciliacionProgressRing);

            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            ActualizarEstadoConciliacionAutomatica();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            await ViewModel.CargarDatosAsync();
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(e.PropertyName)
                || e.PropertyName == nameof(ConciliacionViewModel.IsConciliacionAutomaticaEnProceso)
                || e.PropertyName == nameof(ConciliacionViewModel.ConciliacionPanelHabilitado)
                || e.PropertyName == nameof(ConciliacionViewModel.CanEjecutarConciliacionAutomatica)
                || e.PropertyName == nameof(ConciliacionViewModel.CanEjecutarConciliacionAutomaticaConvinacional)
                || e.PropertyName == nameof(ConciliacionViewModel.CanEjecutarConciliacionAutomaticaAbonos)
                || e.PropertyName == nameof(ConciliacionViewModel.CanDeshacerUltimaOperacionConciliacion)
                || e.PropertyName == nameof(ConciliacionViewModel.CanDeshacerTodasOperacionesConciliacion))
            {
                ActualizarEstadoConciliacionAutomatica();
            }
        }

        private void ActualizarEstadoConciliacionAutomatica()
        {
            BtnConciliacionAutomatica.IsEnabled = ViewModel.CanEjecutarConciliacionAutomatica;
            BtnConciliacionAutomaticaConvinacional.IsEnabled = ViewModel.CanEjecutarConciliacionAutomaticaConvinacional;
            BtnConciliacionAutomaticaAbonos.IsEnabled = ViewModel.CanEjecutarConciliacionAutomaticaAbonos;
            BtnDeshacerUltimo.IsEnabled = ViewModel.CanDeshacerUltimaOperacionConciliacion;
            BtnDeshacerTodo.IsEnabled = ViewModel.CanDeshacerTodasOperacionesConciliacion;
            ConciliacionPanelGrid.IsHitTestVisible = ViewModel.ConciliacionPanelHabilitado;
            ConciliacionPanelGrid.Opacity = ViewModel.ConciliacionPanelHabilitado ? 1d : 0.55d;
            _conciliacionProgressRing.IsActive = ViewModel.IsConciliacionAutomaticaEnProceso;
            _conciliacionProgressRing.Opacity = ViewModel.OpacidadIndicadorConciliacion;
        }

        private void MovimientosResizeHandle_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            _isResizingMovimientos = true;
            MovimientosResizeHandle.CapturePointer(e.Pointer);
        }

        private void MovimientosResizeHandle_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (!_isResizingMovimientos)
            {
                return;
            }

            var point = e.GetCurrentPoint(RootGrid);
            var ancho = Math.Max(280, Math.Min(point.Position.X, RootGrid.ActualWidth - 288));
            RootGrid.ColumnDefinitions[0].Width = new GridLength(ancho);
        }

        private void MovimientosResizeHandle_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            FinalizarResizeMovimientos(e.Pointer);
        }

        private void MovimientosResizeHandle_PointerCanceled(object sender, PointerRoutedEventArgs e)
        {
            FinalizarResizeMovimientos(e.Pointer);
        }

        private void MovimientosResizeHandle_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
        {
            FinalizarResizeMovimientos(e.Pointer);
        }

        private void ConciliacionResizeHandle_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            _isResizingConciliacion = true;
            ConciliacionResizeHandle.CapturePointer(e.Pointer);
        }

        private void ConciliacionResizeHandle_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (!_isResizingConciliacion)
            {
                return;
            }

            var point = e.GetCurrentPoint(RootGrid);
            var alto = Math.Max(220, Math.Min(point.Position.Y, RootGrid.ActualHeight - 188));
            RootGrid.RowDefinitions[0].Height = new GridLength(alto);
        }

        private void ConciliacionResizeHandle_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            FinalizarResizeConciliacion(e.Pointer);
        }

        private void ConciliacionResizeHandle_PointerCanceled(object sender, PointerRoutedEventArgs e)
        {
            FinalizarResizeConciliacion(e.Pointer);
        }

        private void ConciliacionResizeHandle_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
        {
            FinalizarResizeConciliacion(e.Pointer);
        }

        private void FinalizarResizeMovimientos(Pointer pointer)
        {
            _isResizingMovimientos = false;
            MovimientosResizeHandle.ReleasePointerCapture(pointer);
        }

        private void FinalizarResizeConciliacion(Pointer pointer)
        {
            _isResizingConciliacion = false;
            ConciliacionResizeHandle.ReleasePointerCapture(pointer);
        }

        private void FacturaAbonoResizeHandle_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            _isResizingFacturaAbono = true;
            FacturaAbonoResizeHandle.CapturePointer(e.Pointer);
        }

        private void FacturaAbonoResizeHandle_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (!_isResizingFacturaAbono)
            {
                return;
            }

            var point = e.GetCurrentPoint(ConciliacionPanelGrid);
            var ancho = Math.Max(260, Math.Min(point.Position.X, ConciliacionPanelGrid.ActualWidth - 268));
            ConciliacionPanelGrid.ColumnDefinitions[0].Width = new GridLength(ancho);
        }

        private void FacturaAbonoResizeHandle_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            FinalizarResizeFacturaAbono(e.Pointer);
        }

        private void FacturaAbonoResizeHandle_PointerCanceled(object sender, PointerRoutedEventArgs e)
        {
            FinalizarResizeFacturaAbono(e.Pointer);
        }

        private void FacturaAbonoResizeHandle_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
        {
            FinalizarResizeFacturaAbono(e.Pointer);
        }

        private void FinalizarResizeFacturaAbono(Pointer pointer)
        {
            _isResizingFacturaAbono = false;
            FacturaAbonoResizeHandle.ReleasePointerCapture(pointer);
        }

        private async void FacturaPendienteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is FacturaResumenDto factura)
            {
                await ViewModel.CargarDetalleFacturaAsync(factura.IdFactura);
            }
        }

        private void MovimientoPendienteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is ConciliacionMovimientoResumenDto movimiento)
            {
                ViewModel.CargarDetalleMovimiento(movimiento);
            }
        }

        private void BtnLimpiarMovimientoCargado_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.LimpiarMovimientoCargado();
        }

        private async void BtnAbonarMovimientoCargado_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.AbonarMovimientoCargadoAsync();
        }

        private async void BtnConciliacionAutomatica_Click(object sender, RoutedEventArgs e)
        {
            await AbrirVentanaConciliacionAutomaticaAsync(ConciliacionAutomaticaModo.Automatica);
        }

        private void BtnLimpiarFiltrosMovimientos_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.LimpiarFiltrosMovimientos();
        }

        private void BtnLimpiarFiltrosFacturas_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.LimpiarFiltrosFacturas();
        }

        private async void BtnConciliacionAutomaticaConvinacional_Click(object sender, RoutedEventArgs e)
        {
            await AbrirVentanaConciliacionAutomaticaAsync(ConciliacionAutomaticaModo.Combinacional);
        }

        private async void BtnConciliacionAutomaticaAbonos_Click(object sender, RoutedEventArgs e)
        {
            await AbrirVentanaConciliacionAutomaticaAsync(ConciliacionAutomaticaModo.Abonos);
        }

        private async void BtnDeshacerUltimo_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.DeshacerUltimaOperacionConciliacionAsync();
        }

        private async void BtnDeshacerTodo_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.DeshacerTodasOperacionesConciliacionAsync();
        }

        private async Task AbrirVentanaConciliacionAutomaticaAsync(ConciliacionAutomaticaModo modo)
        {
            var ventana = new ConfirmacionConciliacionWindow(
                modo,
                ViewModel.AplicarReglaPueMismoMes,
                ViewModel.UsarRfcComoRegla);
            ventana.Activate();

            var aprobadas = await ventana.ResultTask;
            if (aprobadas is { Count: > 0 })
            {
                await ViewModel.CargarDatosAsync();
            }
        }
    }
}
