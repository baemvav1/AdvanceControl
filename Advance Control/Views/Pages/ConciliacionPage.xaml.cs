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

        private async void BtnConciliacion_Click(object sender, RoutedEventArgs e) =>
            await AbrirPasoConciliacionAsync(ConciliacionAutomaticaModo.Automatica);

        private async void BtnCombinacion_Click(object sender, RoutedEventArgs e) =>
            await AbrirPasoConciliacionAsync(ConciliacionAutomaticaModo.Combinacional);

        private async void BtnAbonos_Click(object sender, RoutedEventArgs e)
        {
            // En Abonos el RFC siempre aplica: se enciende el botón antes de calcular.
            ViewModel.UsarRfcComoRegla = true;
            await AbrirPasoConciliacionAsync(ConciliacionAutomaticaModo.Abonos);
        }

        private async void BtnComplementos_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.IsConciliacionEnProceso = true;
            try
            {
                await ViewModel.VincularComplementosNuevosAsync();
            }
            finally
            {
                ViewModel.IsConciliacionEnProceso = false;
            }
            await AbrirPasoConciliacionAsync(ConciliacionAutomaticaModo.Complementos);
        }

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
                    ViewModel.AplicarDtc,
                    ViewModel.UsarRfcComoRegla,
                    ViewModel.SoloCheques);
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

        /// <summary>
        /// "Borrar por fechas": facturas emitidas entre dos fechas y/o el estado de cuenta de un mes.
        /// Primero simula y muestra qué se borraría; solo con una segunda confirmación borra.
        /// </summary>
        private async void BtnBorrarPorFechas_Click(object sender, RoutedEventArgs e)
        {
            var hoy = DateTimeOffset.Now;
            var inicioMes = new DateTimeOffset(hoy.Year, hoy.Month, 1, 0, 0, 0, hoy.Offset);

            var chkFacturas = new CheckBox { Content = "Facturas emitidas entre", IsChecked = true };
            var desde = new CalendarDatePicker { Header = "Desde", Date = inicioMes.AddMonths(-1), HorizontalAlignment = HorizontalAlignment.Stretch };
            var hasta = new CalendarDatePicker { Header = "Hasta", Date = inicioMes.AddDays(-1), HorizontalAlignment = HorizontalAlignment.Stretch };
            var rangoGrid = new Grid { ColumnSpacing = 12, Margin = new Thickness(28, 0, 0, 0) };
            rangoGrid.ColumnDefinitions.Add(new ColumnDefinition());
            rangoGrid.ColumnDefinitions.Add(new ColumnDefinition());
            Grid.SetColumn(hasta, 1);
            rangoGrid.Children.Add(desde);
            rangoGrid.Children.Add(hasta);

            var chkEstado = new CheckBox { Content = "Movimientos del estado de cuenta de", IsChecked = false };
            var mes = new DatePicker
            {
                DayVisible = false,
                Date = inicioMes.AddMonths(-1),
                MinYear = new DateTimeOffset(2020, 1, 1, 0, 0, 0, hoy.Offset),
                MaxYear = hoy,
                Margin = new Thickness(28, 0, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            void Sincronizar()
            {
                rangoGrid.Opacity = chkFacturas.IsChecked == true ? 1 : 0.4;
                desde.IsEnabled = hasta.IsEnabled = chkFacturas.IsChecked == true;
                mes.IsEnabled = chkEstado.IsChecked == true;
                mes.Opacity = chkEstado.IsChecked == true ? 1 : 0.4;
            }
            chkFacturas.Checked += (_, _) => Sincronizar();
            chkFacturas.Unchecked += (_, _) => Sincronizar();
            chkEstado.Checked += (_, _) => Sincronizar();
            chkEstado.Unchecked += (_, _) => Sincronizar();
            Sincronizar();

            var error = new InfoBar { Severity = InfoBarSeverity.Error, IsClosable = false, IsOpen = false };
            var panel = new StackPanel { Spacing = 10, Width = 460 };
            panel.Children.Add(new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
                Text = "Borra los pagos que la conciliación ligó a movimientos bancarios (también los heredados del motor " +
                       "anterior) para volver a conciliar. Si marcas los dos filtros se borra lo que cumpla cualquiera. " +
                       "Los pagos capturados a mano no se borran: solo se desligan del movimiento."
            });
            panel.Children.Add(chkFacturas);
            panel.Children.Add(rangoGrid);
            panel.Children.Add(chkEstado);
            panel.Children.Add(mes);
            panel.Children.Add(error);

            ConciliacionBorrarRequestDto? request = null;
            var dialogo = new ContentDialog
            {
                Title = "Borrar conciliaciones por fechas",
                Content = panel,
                PrimaryButtonText = "Ver qué se borraría",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = XamlRoot
            };
            dialogo.PrimaryButtonClick += (_, args) =>
            {
                error.IsOpen = false;
                var usaFacturas = chkFacturas.IsChecked == true;
                var usaMes = chkEstado.IsChecked == true;
                string? mensaje = null;
                if (!usaFacturas && !usaMes)
                    mensaje = "Marca al menos un filtro.";
                else if (usaFacturas && (desde.Date == null || hasta.Date == null))
                    mensaje = "Elige las dos fechas de facturas.";
                else if (usaFacturas && desde.Date!.Value.Date > hasta.Date!.Value.Date)
                    mensaje = "La fecha inicial es posterior a la final.";

                if (mensaje != null)
                {
                    error.Message = mensaje;
                    error.IsOpen = true;
                    args.Cancel = true;
                    return;
                }

                request = new ConciliacionBorrarRequestDto
                {
                    FacturaDesde = usaFacturas ? desde.Date!.Value.Date : null,
                    FacturaHasta = usaFacturas ? hasta.Date!.Value.Date : null,
                    MesEstadoCuenta = usaMes ? new DateTime(mes.Date.Year, mes.Date.Month, 1) : null,
                    Simular = true
                };
            };

            if (await dialogo.ShowAsync() != ContentDialogResult.Primary || request == null)
                return;

            ConciliacionBorrarResponseDto simulacion;
            try
            {
                simulacion = await ViewModel.BorrarPorFechasAsync(request);
            }
            catch (Exception ex)
            {
                ViewModel.ErrorMessage = $"No se pudo calcular qué se borraría: {ex.Message}";
                return;
            }

            var mx = new System.Globalization.CultureInfo("es-MX");
            var filtros = new List<string>();
            if (request.FacturaDesde.HasValue)
                filtros.Add($"facturas emitidas del {request.FacturaDesde:dd/MM/yyyy} al {request.FacturaHasta:dd/MM/yyyy}");
            if (request.MesEstadoCuenta.HasValue)
                filtros.Add($"movimientos del estado de cuenta de {request.MesEstadoCuenta.Value.ToString("MMMM yyyy", mx)}");

            if (simulacion.PagosBorrados + simulacion.IngresosDesligados == 0)
            {
                await ConfirmarAsync("Nada que borrar", $"No hay conciliaciones con {string.Join(" o ", filtros)}.", "Aceptar");
                return;
            }

            var detalle = $"Con {string.Join(" o ", filtros)}:\n\n" +
                          $"• Pagos de conciliación que se borran: {simulacion.PagosBorrados}\n" +
                          $"• Pagos capturados a mano que solo se desligan del movimiento: {simulacion.IngresosDesligados}\n" +
                          $"• Monto total: {simulacion.MontoTotal.ToString("C2", mx)}\n" +
                          $"• Facturas que vuelven a quedar pendientes (o con menos abonado): {simulacion.FacturasAfectadas}\n" +
                          $"• Movimientos que vuelven a quedar sin conciliar: {simulacion.MovimientosAfectados}\n" +
                          (simulacion.ComplementosSoltados > 0
                              ? $"• Complementos de pago timbrados que quedan sin ligar a su pago: {simulacion.ComplementosSoltados} (el CFDI sigue vigente)\n"
                              : string.Empty) +
                          "\nEsto no se puede deshacer con \"Deshacer\"; después hay que volver a conciliar.";

            if (!await ConfirmarAsync("¿Borrar estas conciliaciones?", detalle, "Borrar"))
                return;

            request.Simular = false;
            try
            {
                await ViewModel.BorrarPorFechasAsync(request);
            }
            catch (Exception ex)
            {
                ViewModel.ErrorMessage = $"No se pudieron borrar las conciliaciones: {ex.Message}";
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
