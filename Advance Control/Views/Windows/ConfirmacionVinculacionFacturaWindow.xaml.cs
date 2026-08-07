using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Utilities;
using Advance_Control.ViewModels;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Graphics;

namespace Advance_Control.Views.Windows
{
    public sealed partial class ConfirmacionVinculacionFacturaWindow : Window
    {
        private readonly TaskCompletionSource<int> _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly FacturacionViewModel _viewModel;
        private bool _resultadoEntregado;

        public Task<int> ResultTask => _tcs.Task;

        public ConfirmacionVinculacionFacturaWindow()
        {
            InitializeComponent();
            Title = "Vincular facturas a operaciones";
            AjustarTamano(1100, 620);

            _viewModel = AppServices.Get<FacturacionViewModel>();

            BtnConfirmar.IsEnabled = false;
            TxtResumen.Text = "Buscando operaciones sin factura con RFC y monto coincidentes...";

            Activated += ConfirmacionVinculacionFacturaWindow_Activated;
            Closed += ConfirmacionVinculacionFacturaWindow_Closed;
        }

        private void AjustarTamano(int ancho, int alto)
        {
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);
            appWindow.Resize(new SizeInt32(ancho, alto));
        }

        private async void ConfirmacionVinculacionFacturaWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            Activated -= ConfirmacionVinculacionFacturaWindow_Activated;

            try
            {
                var propuestas = await _viewModel.CargarPropuestasVinculacionAsync();
                ListaPropuestas.ItemsSource = propuestas;
                ActualizarResumen(propuestas.Count);
                BtnConfirmar.IsEnabled = propuestas.Count > 0;
            }
            catch (Exception ex)
            {
                TxtResumen.Text = $"Error al buscar coincidencias: {ex.Message}";
            }
        }

        private async void BtnConfirmar_Click(object sender, RoutedEventArgs e)
        {
            var todas = (ListaPropuestas.ItemsSource as IEnumerable<FacturaOperacionPropuestaDto>) ?? Array.Empty<FacturaOperacionPropuestaDto>();
            var aprobadas = todas.Where(propuesta => propuesta.Aprobado).ToList();

            if (aprobadas.Count == 0)
            {
                TxtResumen.Text = "Selecciona al menos una propuesta para continuar.";
                return;
            }

            BtnConfirmar.IsEnabled = false;
            BtnCancelar.IsEnabled = false;
            TxtResumen.Text = "Vinculando facturas...";

            var (vinculadas, errores) = await _viewModel.AplicarPropuestasVinculacionAprobadasAsync(aprobadas);

            if (errores.Count > 0)
            {
                var detalle = new StringBuilder();
                detalle.AppendLine($"{vinculadas} de {aprobadas.Count} facturas vinculadas correctamente.");
                detalle.AppendLine("No se pudieron vincular las siguientes:");
                foreach (var error in errores)
                {
                    detalle.AppendLine($"- {error}");
                }

                var dialog = new Microsoft.UI.Xaml.Controls.ContentDialog
                {
                    Title = "Algunas vinculaciones fallaron",
                    Content = detalle.ToString(),
                    CloseButtonText = "Aceptar",
                    XamlRoot = Content.XamlRoot
                };
                await dialog.ShowAsync();

                BtnCancelar.IsEnabled = true;
                if (vinculadas == 0)
                {
                    BtnConfirmar.IsEnabled = true;
                    TxtResumen.Text = "No se vinculó ninguna factura.";
                    return;
                }
            }

            _resultadoEntregado = true;
            _tcs.TrySetResult(vinculadas);
            Close();
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            _resultadoEntregado = true;
            _tcs.TrySetResult(0);
            Close();
        }

        private void ConfirmacionVinculacionFacturaWindow_Closed(object sender, WindowEventArgs args)
        {
            if (!_resultadoEntregado)
            {
                _tcs.TrySetResult(0);
            }
        }

        private void ActualizarResumen(int total)
        {
            TxtResumen.Text = total == 0
                ? "No se encontraron operaciones sin factura con RFC y monto antes de IVA coincidentes."
                : $"{total} coincidencia(s) encontrada(s). Desmarca las que no quieras vincular y elige la factura correcta cuando haya más de una candidata.";
        }
    }
}
