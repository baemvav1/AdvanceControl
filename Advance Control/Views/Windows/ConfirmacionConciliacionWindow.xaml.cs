using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Utilities;
using Advance_Control.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Windowing;
using Windows.Graphics;

namespace Advance_Control.Views.Windows
{
    /// <summary>
    /// Confirma propuestas de conciliación para uno o varios pasos en secuencia. Con el
    /// constructor de un solo paso (modo, mismo mes, RFC) se usa desde los botones
    /// individuales de la página. El constructor sin parámetros recorre la receta completa
    /// (cheques + los 8 pasos combinando modo y filtros) — ⚠️ deshabilitado temporalmente en
    /// ConciliacionPage (causaba que la UI se trabara); el código queda listo para retomarlo.
    /// </summary>
    public sealed partial class ConfirmacionConciliacionWindow : Window
    {
        private readonly record struct PasoConciliacion(ConciliacionAutomaticaModo Modo, bool MismoMes, bool Rfc);

        private static readonly PasoConciliacion[] PasosSecuenciaCompleta =
        {
            new(ConciliacionAutomaticaModo.Cheques, MismoMes: false, Rfc: false),
            new(ConciliacionAutomaticaModo.Automatica, MismoMes: true, Rfc: true),
            new(ConciliacionAutomaticaModo.Combinacional, MismoMes: true, Rfc: true),
            new(ConciliacionAutomaticaModo.Abonos, MismoMes: true, Rfc: true),
            new(ConciliacionAutomaticaModo.Automatica, MismoMes: false, Rfc: true),
            new(ConciliacionAutomaticaModo.Combinacional, MismoMes: false, Rfc: true),
            new(ConciliacionAutomaticaModo.Abonos, MismoMes: false, Rfc: true),
            new(ConciliacionAutomaticaModo.Automatica, MismoMes: false, Rfc: false),
            new(ConciliacionAutomaticaModo.Combinacional, MismoMes: false, Rfc: false),
        };

        private readonly IReadOnlyList<PasoConciliacion> _pasos;
        private readonly TaskCompletionSource<IReadOnlyList<ConciliacionMatchPropuestaDto>?> _tcs =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly ConciliacionAutomaticaWindowViewModel _viewModel;
        private readonly List<ConciliacionMatchPropuestaDto> _todasLasAprobadas = new();
        private int _pasoActual;
        private bool _resultadoEntregado;
        private bool _recalculando;
        private CancellationTokenSource? _ctsLoading;

        public Task<IReadOnlyList<ConciliacionMatchPropuestaDto>?> ResultTask => _tcs.Task;

        private PasoConciliacion PasoActual => _pasos[_pasoActual];

        /// <summary>Ejecuta un único paso (usado por los botones individuales de la página).</summary>
        public ConfirmacionConciliacionWindow(ConciliacionAutomaticaModo modo, bool aplicarReglaPueMismoMes, bool usarRfcComoRegla)
            : this(new[] { new PasoConciliacion(modo, aplicarReglaPueMismoMes, usarRfcComoRegla) })
        {
        }

        /// <summary>Ejecuta la receta completa (todos los pasos en secuencia).</summary>
        public ConfirmacionConciliacionWindow()
            : this(PasosSecuenciaCompleta)
        {
        }

        private ConfirmacionConciliacionWindow(IReadOnlyList<PasoConciliacion> pasos)
        {
            InitializeComponent();
            _pasos = pasos;
            _viewModel = AppServices.Get<ConciliacionAutomaticaWindowViewModel>();
            Title = "Conciliación automática";

            // Tamaño inicial generoso para mostrar toda la tabla
            AjustarTamano(1300, 680);

            BtnContinuar.IsEnabled = false;
            TxtResumen.Text = "Preparando la secuencia de conciliación automática...";

            ControlConfirmacion.PropuestaDescartada += ControlConfirmacion_PropuestaDescartada;
            ControlConfirmacion.MovimientoAbonosDescartado += ControlConfirmacion_MovimientoAbonosDescartado;

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
            await EjecutarSecuenciaDesdePasoActualAsync();
        }

        /// <summary>
        /// Calcula propuestas para el paso actual y avanza automáticamente por los pasos
        /// sin propuestas; se detiene (deja el botón "Continuar" habilitado) en el primer
        /// paso que sí tenga algo que revisar, o cierra la ventana si ya no quedan pasos.
        /// </summary>
        private async Task EjecutarSecuenciaDesdePasoActualAsync()
        {
            BtnCancelar.IsEnabled = true;

            while (_pasoActual < _pasos.Count)
            {
                var paso = PasoActual;
                TxtResumen.Text = $"{DescribirPaso(paso)} — calculando propuestas...";

                _ctsLoading = new CancellationTokenSource();
                IReadOnlyList<ConciliacionMatchPropuestaDto> propuestas;
                try
                {
                    propuestas = await _viewModel.CargarPropuestasAsync(
                        paso.Modo, paso.MismoMes, paso.Rfc, _ctsLoading.Token);
                }
                catch (OperationCanceledException)
                {
                    TxtResumen.Text = "Proceso cancelado.";
                    return;
                }

                if (propuestas.Count == 0)
                {
                    _pasoActual++;
                    continue;
                }

                ControlConfirmacion.SetModo(paso.Modo);
                ControlConfirmacion.SetPropuestas(propuestas);
                ActualizarResumen(propuestas.Count, paso);
                BtnContinuar.IsEnabled = true;
                return;
            }

            // Se agotaron los pasos sin nunca pausar (ningún paso tuvo propuestas). En la
            // secuencia completa esto es normal entre pasos intermedios, pero si la ventana
            // nunca mostró nada, avisar antes de cerrar -- si no, parece que "no hizo nada".
            if (_todasLasAprobadas.Count == 0)
            {
                var dialog = new Microsoft.UI.Xaml.Controls.ContentDialog
                {
                    Title = "Sin propuestas",
                    Content = "No se encontraron conciliaciones automáticas para revisar.",
                    CloseButtonText = "Aceptar",
                    XamlRoot = Content.XamlRoot
                };
                await dialog.ShowAsync();
            }

            _resultadoEntregado = true;
            _tcs.TrySetResult(_todasLasAprobadas);
            Close();
        }

        /// <summary>
        /// Concilia las propuestas seleccionadas (si hay alguna) e ignora el resto; la
        /// selección es opcional en todo momento — con 0 seleccionadas simplemente avanza
        /// al siguiente paso de la secuencia sin conciliar nada de este.
        /// </summary>
        private async void BtnContinuar_Click(object sender, RoutedEventArgs e)
        {
            var seleccionadas = ControlConfirmacion.ObtenerAprobadas();
            var paso = PasoActual;

            BtnContinuar.IsEnabled = false;
            try
            {
                if (seleccionadas.Count > 0)
                {
                    await _viewModel.AplicarPropuestasAprobadasAsync(paso.Modo, seleccionadas);
                    _todasLasAprobadas.AddRange(seleccionadas);
                }
            }
            catch (Exception ex)
            {
                BtnContinuar.IsEnabled = true;
                var dialog = new Microsoft.UI.Xaml.Controls.ContentDialog
                {
                    Title = "Error al aplicar conciliacion",
                    Content = ex.Message,
                    CloseButtonText = "Aceptar",
                    XamlRoot = Content.XamlRoot
                };
                await dialog.ShowAsync();
                return;
            }

            _pasoActual++;
            await EjecutarSecuenciaDesdePasoActualAsync();
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            _ctsLoading?.Cancel();
            _resultadoEntregado = true;
            _tcs.TrySetResult(_todasLasAprobadas.Count > 0 ? _todasLasAprobadas : null);
            Close();
        }

        private void TxtFiltroMetadato_TextChanged(object sender, Microsoft.UI.Xaml.Controls.TextChangedEventArgs e)
        {
            ControlConfirmacion.AplicarFiltroMetadato(TxtFiltroMetadato.Text);
        }

        /// <summary>
        /// Descarta una propuesta (de cualquier tipo — 1 a 1, combinacional o abonos) y
        /// recalcula el paso actual: la(s) factura(s) involucradas ya no se vuelven a proponer
        /// el resto del asistente, y su(s) movimiento(s) regresan al pool para otras facturas.
        /// </summary>
        private async void ControlConfirmacion_PropuestaDescartada(ConciliacionMatchPropuestaDto propuestaDescartada)
        {
            if (_recalculando)
            {
                return;
            }

            _recalculando = true;
            try
            {
                EstablecerInteraccionHabilitada(false);
                TxtResumen.Text = "Recalculando propuestas...";

                var propuestas = await _viewModel.DescartarYRecalcularPropuestaAsync(PasoActual.Modo, propuestaDescartada);
                ControlConfirmacion.SetPropuestas(propuestas);
                ActualizarResumen(propuestas.Count, PasoActual);
                BtnContinuar.IsEnabled = true;
            }
            finally
            {
                EstablecerInteraccionHabilitada(true);
                _recalculando = false;
            }
        }

        private async void ControlConfirmacion_MovimientoAbonosDescartado(ConciliacionAbonoMovimientoItemDto movimientoDescartado)
        {
            // Este evento solo lo dispara el boton "Descartar y reintentar", que solo existe
            // en filas tipo Abonos -- no hace falta condicionar por el modo del paso completo.
            if (_recalculando)
            {
                return;
            }

            _recalculando = true;
            try
            {
                EstablecerInteraccionHabilitada(false);
                TxtResumen.Text = $"Reintentando factura {movimientoDescartado.FolioFactura} sin el movimiento descartado...";

                var propuestas = await _viewModel.DescartarMovimientoYRecalcularFacturaAbonosAsync(
                    PasoActual.Modo,
                    movimientoDescartado.IdFactura,
                    movimientoDescartado.IdMovimiento);
                ControlConfirmacion.SetPropuestas(propuestas);
                ActualizarResumen(propuestas.Count, PasoActual);
                BtnContinuar.IsEnabled = true;
            }
            finally
            {
                EstablecerInteraccionHabilitada(true);
                _recalculando = false;
            }
        }

        private void ConfirmacionConciliacionWindow_Closed(object sender, WindowEventArgs args)
        {
            _ctsLoading?.Cancel();
            _ctsLoading?.Dispose();
            _ctsLoading = null;
            ControlConfirmacion.PropuestaDescartada -= ControlConfirmacion_PropuestaDescartada;
            ControlConfirmacion.MovimientoAbonosDescartado -= ControlConfirmacion_MovimientoAbonosDescartado;
            if (!_resultadoEntregado)
            {
                _tcs.TrySetResult(_todasLasAprobadas.Count > 0 ? _todasLasAprobadas : null);
            }
        }

        private void ActualizarResumen(int total, PasoConciliacion paso)
        {
            var prefijo = $"{DescribirPaso(paso)} — ";
            var esModoAbonos = paso.Modo == ConciliacionAutomaticaModo.Abonos;
            TxtResumen.Text = prefijo + (esModoAbonos
                ? total == 0
                    ? "no quedan propuestas vigentes de abonos despues de los descartes. Continuar pasa al siguiente paso sin conciliar nada de este."
                    : $"{total} propuesta(s) vigente(s). Desmarca las que no quieras conciliar; puedes descartar facturas completas o subdescartar movimientos para reintentar la misma factura."
                : $"{total} conciliacion(es) propuesta(s). Desmarca las que no quieras conciliar: las que queden marcadas se aplicaran al continuar, y si no dejas ninguna marcada se pasa al siguiente paso sin conciliar nada.");
        }

        private void EstablecerInteraccionHabilitada(bool habilitada)
        {
            TxtFiltroMetadato.IsEnabled = habilitada;
            BtnCancelar.IsEnabled = habilitada;
            ControlConfirmacion.IsEnabled = habilitada;
        }

        private string DescribirPaso(PasoConciliacion paso)
        {
            var nombreModo = paso.Modo switch
            {
                ConciliacionAutomaticaModo.Automatica => "Conciliación",
                ConciliacionAutomaticaModo.Combinacional => "Combinación",
                ConciliacionAutomaticaModo.Abonos => "Conciliación de abonos",
                ConciliacionAutomaticaModo.Cheques => "Cheques (1 a 1, 1 a varios, varios a 1)",
                _ => paso.Modo.ToString()
            };
            var filtros = $"Mismo mes: {(paso.MismoMes ? "Sí" : "No")} · RFC: {(paso.Rfc ? "Sí" : "No")}";
            return _pasos.Count == 1
                ? $"{nombreModo} · {filtros}"
                : $"Paso {_pasoActual + 1} de {_pasos.Count} · {nombreModo} · {filtros}";
        }
    }
}
