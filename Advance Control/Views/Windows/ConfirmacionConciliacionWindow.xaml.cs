using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Utilities;
using Advance_Control.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Windowing;
using Windows.Graphics;

namespace Advance_Control.Views.Windows
{
    public sealed partial class ConfirmacionConciliacionWindow : Window
    {
        private readonly TaskCompletionSource<IReadOnlyList<ConciliacionMatchPropuestaDto>?> _tcs =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly ConciliacionAutomaticaWindowViewModel? _viewModel;
        private readonly ConciliacionAutomaticaModo? _modo;
        private readonly bool _aplicarReglaPueMismoMes;
        private bool _resultadoEntregado;
        private bool _recalculandoAbonos;

        public Task<IReadOnlyList<ConciliacionMatchPropuestaDto>?> ResultTask => _tcs.Task;

        public ConfirmacionConciliacionWindow(IReadOnlyList<ConciliacionMatchPropuestaDto> propuestas)
        {
            InitializeComponent();
            Title = "Confirmar conciliaciones propuestas";

            // Tamaño inicial generoso para mostrar toda la tabla
            AjustarTamano(1300, 680);

            ControlConfirmacion.SetModo(InferirModo(propuestas));
            ControlConfirmacion.SetPropuestas(propuestas);
            ControlConfirmacion.PropuestaAbonosDescartada += ControlConfirmacion_PropuestaAbonosDescartada;
            ControlConfirmacion.MovimientoAbonosDescartado += ControlConfirmacion_MovimientoAbonosDescartado;
            ActualizarResumen(propuestas.Count, ControlConfirmacion.EsModoAbonos);

            Closed += ConfirmacionConciliacionWindow_Closed;
        }

        public ConfirmacionConciliacionWindow(
            ConciliacionAutomaticaModo modo,
            bool aplicarReglaPueMismoMes)
        {
            InitializeComponent();
            _viewModel = AppServices.Get<ConciliacionAutomaticaWindowViewModel>();
            _modo = modo;
            _aplicarReglaPueMismoMes = aplicarReglaPueMismoMes;
            Title = ObtenerTitulo(modo);

            AjustarTamano(1300, 680);
            ControlConfirmacion.SetModo(modo);
            ControlConfirmacion.PropuestaAbonosDescartada += ControlConfirmacion_PropuestaAbonosDescartada;
            ControlConfirmacion.MovimientoAbonosDescartado += ControlConfirmacion_MovimientoAbonosDescartado;

            BtnConfirmar.IsEnabled = false;
            TxtResumen.Text = "Preparando propuestas de conciliacion...";

            Activated += ConfirmacionConciliacionWindow_Activated;
            Closed += ConfirmacionConciliacionWindow_Closed;
        }

        private void AjustarTamano(int ancho, int alto)
        {
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);
            appWindow.Resize(new SizeInt32(ancho, alto));
        }

        private async void ConfirmacionConciliacionWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            Activated -= ConfirmacionConciliacionWindow_Activated;

            if (_viewModel == null || !_modo.HasValue)
            {
                return;
            }

            BtnConfirmar.IsEnabled = false;

            var propuestas = await _viewModel.CargarPropuestasAsync(_modo.Value, _aplicarReglaPueMismoMes);
            ControlConfirmacion.SetPropuestas(propuestas);
            ActualizarResumen(propuestas.Count, _modo == ConciliacionAutomaticaModo.Abonos);
            BtnConfirmar.IsEnabled = propuestas.Count > 0;
        }

        private async void BtnConfirmar_Click(object sender, RoutedEventArgs e)
        {
            var aprobadas = ControlConfirmacion.ObtenerAprobadas();

            if (_viewModel != null && _modo.HasValue)
            {
                BtnConfirmar.IsEnabled = false;
                var aplicadas = await _viewModel.AplicarPropuestasAprobadasAsync(_modo.Value, aprobadas);
                if (!aplicadas)
                {
                    BtnConfirmar.IsEnabled = true;
                    return;
                }
            }

            _resultadoEntregado = true;
            _tcs.TrySetResult(aprobadas);
            Close();
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            _resultadoEntregado = true;
            _tcs.TrySetResult(null);
            Close();
        }

        private void TxtFiltroMetadato_TextChanged(object sender, Microsoft.UI.Xaml.Controls.TextChangedEventArgs e)
        {
            ControlConfirmacion.AplicarFiltroMetadato(TxtFiltroMetadato.Text);
        }

        private async void ControlConfirmacion_PropuestaAbonosDescartada(ConciliacionMatchPropuestaDto propuestaDescartada)
        {
            if (_recalculandoAbonos
                || _viewModel == null
                || _modo != ConciliacionAutomaticaModo.Abonos)
            {
                return;
            }

            _recalculandoAbonos = true;
            try
            {
                EstablecerInteraccionHabilitada(false);
                TxtResumen.Text = "Recalculando propuestas de abonos...";

                var propuestas = await _viewModel.DescartarYRecalcularPropuestasAbonosAsync(propuestaDescartada);
                ControlConfirmacion.SetPropuestas(propuestas);
                ActualizarResumen(propuestas.Count, esModoAbonos: true);
                BtnConfirmar.IsEnabled = propuestas.Count > 0;
            }
            finally
            {
                EstablecerInteraccionHabilitada(true);
                _recalculandoAbonos = false;
            }
        }

        private async void ControlConfirmacion_MovimientoAbonosDescartado(ConciliacionAbonoMovimientoItemDto movimientoDescartado)
        {
            if (_recalculandoAbonos
                || _viewModel == null
                || _modo != ConciliacionAutomaticaModo.Abonos)
            {
                return;
            }

            _recalculandoAbonos = true;
            try
            {
                EstablecerInteraccionHabilitada(false);
                TxtResumen.Text = $"Reintentando factura {movimientoDescartado.FolioFactura} sin el movimiento descartado...";

                var propuestas = await _viewModel.DescartarMovimientoYRecalcularFacturaAbonosAsync(
                    movimientoDescartado.IdFactura,
                    movimientoDescartado.IdMovimiento);
                ControlConfirmacion.SetPropuestas(propuestas);
                ActualizarResumen(propuestas.Count, esModoAbonos: true);
                BtnConfirmar.IsEnabled = propuestas.Count > 0;
            }
            finally
            {
                EstablecerInteraccionHabilitada(true);
                _recalculandoAbonos = false;
            }
        }

        private void ConfirmacionConciliacionWindow_Closed(object sender, WindowEventArgs args)
        {
            ControlConfirmacion.PropuestaAbonosDescartada -= ControlConfirmacion_PropuestaAbonosDescartada;
            ControlConfirmacion.MovimientoAbonosDescartado -= ControlConfirmacion_MovimientoAbonosDescartado;
            if (!_resultadoEntregado)
            {
                _tcs.TrySetResult(null);
            }
        }

        private void ActualizarResumen(int total, bool esModoAbonos = false)
        {
            TxtResumen.Text = esModoAbonos
                ? total == 0
                    ? "No quedan propuestas vigentes de abonos despues de los descartes."
                    : $"{total} propuesta(s) vigente(s). Puedes descartar facturas completas o subdescartar movimientos para reintentar la misma factura."
                : $"{total} conciliacion(es) propuesta(s). Desmarca las que deseas rechazar antes de confirmar.";
        }

        private void EstablecerInteraccionHabilitada(bool habilitada)
        {
            TxtFiltroMetadato.IsEnabled = habilitada;
            BtnCancelar.IsEnabled = habilitada;
            ControlConfirmacion.IsEnabled = habilitada;
        }

        private static ConciliacionAutomaticaModo? InferirModo(IReadOnlyList<ConciliacionMatchPropuestaDto> propuestas)
        {
            return propuestas.Count > 0 && propuestas.All(propuesta => propuesta.EsAbonos)
                ? ConciliacionAutomaticaModo.Abonos
                : null;
        }

        private static string ObtenerTitulo(ConciliacionAutomaticaModo modo)
        {
            return modo switch
            {
                ConciliacionAutomaticaModo.Automatica => "Conciliacion automatica",
                ConciliacionAutomaticaModo.Combinacional => "Conciliacion automatica convinacional",
                ConciliacionAutomaticaModo.Abonos => "Conciliacion automatica de abonos",
                _ => "Confirmar conciliaciones propuestas"
            };
        }
    }
}
