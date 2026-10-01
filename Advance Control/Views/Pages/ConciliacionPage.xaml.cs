using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Utilities;
using Advance_Control.ViewModels;
using Advance_Control.Views.Dialogs;
using Advance_Control.Views.Helpers;
using Advance_Control.Views.Windows;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using WinUI.TableView;

namespace Advance_Control.Views.Pages
{
    /// <summary>
    /// Conciliación: facturas y movimientos pendientes con filtros por encabezado (WinUI.TableView);
    /// lo seleccionado se fija arriba y es lo que usa "Abonar". Los pasos automáticos abren
    /// ConfirmacionConciliacionWindow con las reglas de la cinta.
    /// Atajos: Esc = limpiar selección, F5 = recargar.
    /// </summary>
    public sealed partial class ConciliacionPage : Page
    {
        private readonly FijadorSeleccionTableView _fijadorFacturas;
        private readonly FijadorSeleccionTableView _fijadorMovimientos;

        public ConciliacionViewModel ViewModel { get; }

        public ConciliacionPage()
        {
            ViewModel = AppServices.Get<ConciliacionViewModel>();
            InitializeComponent();
            DataContext = ViewModel;

            // Al seleccionar un renglón sube a la posición 1 y se queda fijo hasta deseleccionarlo.
            _fijadorFacturas = new FijadorSeleccionTableView(
                TablaFacturasPendientes, fila => ((ConciliacionFacturaFila)fila).Orden);
            _fijadorMovimientos = new FijadorSeleccionTableView(
                TablaMovimientosPendientes, fila => ((ConciliacionMovimientoFila)fila).Orden);
            _fijadorFacturas.FijadosCambiados += (_, _) => SincronizarSeleccion();
            _fijadorMovimientos.FijadosCambiados += (_, _) => SincronizarSeleccion();

            AgregarAtajo(global::Windows.System.VirtualKey.Escape, LimpiarSeleccion);
            AgregarAtajo(global::Windows.System.VirtualKey.F5, () =>
            {
                if (ViewModel.PuedeOperar)
                {
                    _ = ViewModel.CargarDatosAsync();
                }
            });

            TablaFacturasPendientes.RegisterPropertyChangedCallback(
                TableView.CellsHorizontalOffsetProperty,
                (_, _) => AjustarColumnaElastica(TablaFacturasPendientes, ColumnaRazonSocial));
            TablaMovimientosPendientes.RegisterPropertyChangedCallback(
                TableView.CellsHorizontalOffsetProperty,
                (_, _) => AjustarColumnaElastica(TablaMovimientosPendientes, ColumnaMetadatos, AnchoMaximoMetadatos));
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            await ViewModel.CargarDatosAsync();
        }

        private void AbrirFactura_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { Tag: ConciliacionFacturaFila fila })
            {
                new FacturaVisorWindow(fila.Factura).Activate();
            }
        }

        private async void AbrirMovimiento_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { Tag: ConciliacionMovimientoFila fila })
            {
                await new MovimientoVisorDialog(fila, XamlRoot).ShowAsync();
            }
        }

        private void TablaFacturasPendientes_SizeChanged(object sender, SizeChangedEventArgs e) =>
            AjustarColumnaElastica(TablaFacturasPendientes, ColumnaRazonSocial);

        private void TablaMovimientosPendientes_SizeChanged(object sender, SizeChangedEventArgs e) =>
            AjustarColumnaElastica(TablaMovimientosPendientes, ColumnaMetadatos, AnchoMaximoMetadatos);

        /// <summary>
        /// TableView calcula las columnas "*" sobre su ancho menos 32 px fijos, y lo que sobra
        /// después de la última columna se ve como una columna vacía. Para evitarla, las demás
        /// columnas tienen ancho fijo y a esta se le asigna el espacio restante: ancho de la tabla
        /// menos donde empiezan las celdas (CellsHorizontalOffset, la columna de selección), las
        /// columnas fijas y el espacio de la barra de scroll vertical. Si se pasa, la última
        /// columna queda cortada y se pierde su botón de filtro.
        /// </summary>
        private const double EspacioBarraScroll = 18;

        /// <summary>Metadatos no pasa de este ancho (el texto completo está en el tooltip).</summary>
        private const double AnchoMaximoMetadatos = 180;

        // Ancho original (el del XAML) de cada columna fija, para repartir el sobrante sin que
        // los anchos se vayan acumulando en cada redimensión.
        private readonly Dictionary<TableViewColumn, double> _anchosBase = new();

        /// <param name="anchoMaximo">
        /// Si la columna elástica llegaría a más de esto, se queda en este ancho y el sobrante se
        /// reparte por igual entre las demás columnas (así no queda hueco al final).
        /// </param>
        private void AjustarColumnaElastica(TableView tabla, TableViewColumn columnaElastica, double? anchoMaximo = null)
        {
            if (tabla.ActualWidth <= 0)
            {
                return;
            }

            var columnasFijas = tabla.Columns.Where(columna => columna != columnaElastica).ToList();
            foreach (var columna in columnasFijas)
            {
                _anchosBase.TryAdd(columna, columna.Width.Value);
            }

            var anchoFijo = columnasFijas.Sum(columna => _anchosBase[columna]);
            var restante = tabla.ActualWidth - tabla.CellsHorizontalOffset - anchoFijo - EspacioBarraScroll;
            var anchoElastica = Math.Max(restante, 120);
            var sobrantePorColumna = 0d;

            if (anchoMaximo is double maximo && anchoElastica > maximo && columnasFijas.Count > 0)
            {
                sobrantePorColumna = Math.Floor((anchoElastica - maximo) / columnasFijas.Count);
                anchoElastica = maximo;
            }

            foreach (var columna in columnasFijas)
            {
                columna.Width = new GridLength(_anchosBase[columna] + sobrantePorColumna);
            }

            columnaElastica.Width = new GridLength(anchoElastica);
        }

        private void AgregarAtajo(global::Windows.System.VirtualKey tecla, Action accion)
        {
            var atajo = new KeyboardAccelerator { Key = tecla };
            atajo.Invoked += (_, args) =>
            {
                args.Handled = true;
                accion();
            };
            KeyboardAccelerators.Add(atajo);
        }

        private void SincronizarSeleccion() =>
            ViewModel.ActualizarSeleccion(
                _fijadorFacturas.Fijados.OfType<ConciliacionFacturaFila>(),
                _fijadorMovimientos.Fijados.OfType<ConciliacionMovimientoFila>());

        private void LimpiarSeleccion()
        {
            _fijadorFacturas.Limpiar();
            _fijadorMovimientos.Limpiar();
        }

        // ---------------- Conciliación (pasos automáticos) ----------------

        private async void BtnCheques_Click(object sender, RoutedEventArgs e) =>
            await AbrirPasoConciliacionAsync(ConciliacionAutomaticaModo.Cheques);

        private async void BtnConciliacion_Click(object sender, RoutedEventArgs e) =>
            await AbrirPasoConciliacionAsync(ConciliacionAutomaticaModo.Automatica);

        private async void BtnCombinacion_Click(object sender, RoutedEventArgs e) =>
            await AbrirPasoConciliacionAsync(ConciliacionAutomaticaModo.Combinacional);

        private async void BtnAbonos_Click(object sender, RoutedEventArgs e) =>
            await AbrirPasoConciliacionAsync(ConciliacionAutomaticaModo.Abonos);

        private async void BtnComplementos_Click(object sender, RoutedEventArgs e) =>
            await AbrirPasoConciliacionAsync(ConciliacionAutomaticaModo.Complementos);

        private async void BtnIngresosManuales_Click(object sender, RoutedEventArgs e) =>
            await AbrirPasoConciliacionAsync(ConciliacionAutomaticaModo.IngresosManuales);

        /// <summary>
        /// Ventana de propuestas de conciliación automática, con las reglas de la cinta. Si se aprobó
        /// algo, recarga las tablas y el contador de la bitácora.
        /// </summary>
        private async Task AbrirPasoConciliacionAsync(ConciliacionAutomaticaModo modo)
        {
            ViewModel.IsConciliacionEnProceso = true;
            try
            {
                var ventana = new ConfirmacionConciliacionWindow(
                    modo,
                    ViewModel.AplicarReglaPueMismoMes,
                    ViewModel.UsarRfcComoRegla,
                    ViewModel.AplicarReglaPpdSiguienteMes);
                ventana.Activate();

                var aprobadas = await ventana.ResultTask;
                if (aprobadas is { Count: > 0 })
                {
                    await ViewModel.CargarDatosAsync();
                }
            }
            finally
            {
                ViewModel.IsConciliacionEnProceso = false;
            }
        }

        // ---------------- Acciones ----------------

        private async void BtnAbonar_Click(object sender, RoutedEventArgs e)
        {
            var plan = ViewModel.PlanearAbonos(out var motivo);
            if (plan is null)
            {
                ViewModel.ErrorMessage = motivo;
                return;
            }

            var confirmar = await ConfirmarAsync(
                plan.Count == 1 ? "¿Registrar el abono?" : $"¿Registrar {plan.Count} abonos?",
                ViewModel.DescribirPlan(plan),
                "Abonar");

            if (confirmar)
            {
                await ViewModel.AbonarAsync(plan);
            }
        }

        private void BtnLimpiar_Click(object sender, RoutedEventArgs e) => LimpiarSeleccion();

        // ---------------- Revertir ----------------

        private async void BtnDeshacerUltimo_Click(object sender, RoutedEventArgs e) =>
            await ViewModel.DeshacerUltimoAsync();

        private async void BtnDeshacerTodo_Click(object sender, RoutedEventArgs e)
        {
            // La bitácora es global (no solo de esta sesión): se confirma con el conteo real.
            var confirmar = await ConfirmarAsync(
                "¿Deshacer todas las operaciones?",
                $"Se revertirán las {ViewModel.OperacionesConciliacionPendientes} operaciones de conciliación registradas en la bitácora, "
                    + "de cualquier usuario y sesión. Los abonos de conciliación se eliminan (los capturados a mano solo se "
                    + "desligan del movimiento y los Complementos de Pago timbrados quedan sin ligar) y las facturas y "
                    + "movimientos vuelven a quedar pendientes.",
                "Deshacer todo");

            if (confirmar)
            {
                await ViewModel.DeshacerTodoAsync();
            }
        }

        private async Task<bool> ConfirmarAsync(string titulo, string contenido, string textoAceptar)
        {
            var dialogo = new ContentDialog
            {
                Title = titulo,
                Content = new ScrollViewer
                {
                    MaxHeight = 420,
                    Content = new TextBlock { Text = contenido, TextWrapping = TextWrapping.Wrap, IsTextSelectionEnabled = true }
                },
                PrimaryButtonText = textoAceptar,
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = XamlRoot
            };

            return await dialogo.ShowAsync() == ContentDialogResult.Primary;
        }
    }
}
