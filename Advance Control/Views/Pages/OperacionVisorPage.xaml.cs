using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.Activity;
using Advance_Control.Services.CheckOperacion;
using Advance_Control.Services.Session;
using Advance_Control.Services.Aprobaciones;
using Advance_Control.Services.Cargos;
using Advance_Control.Services.ImageViewer;
using Advance_Control.Services.LocalStorage;
using Microsoft.UI.Xaml.Input;
using WinUI.TableView;
using Advance_Control.Services.Contactos;
using Advance_Control.Services.Notificacion;
using Advance_Control.Utilities;
using Advance_Control.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Advance_Control.Services.Portal;

namespace Advance_Control.Views.Pages
{
    /// <summary>
    /// Visor de una operación: cargos editables en la tabla, galería con todos los documentos y
    /// fotos, línea de tiempo del check y acciones (generar, cargar, reabrir, finalizar, compartir).
    /// Se aloja en su propia ventana (ver OperacionVisorWindow / OperacionVisorNavigator). Abierta
    /// desde una referencia del chat sin permisos propios queda en solo lectura.
    /// </summary>
    public sealed partial class OperacionVisorPage : Page
    {
        /// <summary>Operación que muestra esta ventana (del listado, del chat o por id).</summary>
        private int _idOperacion;
        /// <summary>Referencia del chat con la que se abrió en solo lectura (viaja en cada consulta).</summary>
        private long? _mensajeReferenciaId;
        private bool _cargada;

        /// <summary>Ventana propia que hospeda esta página (ver OperacionVisorWindow).</summary>
        internal Window? HostWindow { get; set; }

        private readonly OperacionesViewModel _operaciones = AppServices.Get<OperacionesViewModel>();
        private readonly IAprobacionService _aprobaciones = AppServices.Get<IAprobacionService>();
        private readonly IContactoService _contactos = AppServices.Get<IContactoService>();
        private readonly INotificacionService _notificaciones = AppServices.Get<INotificacionService>();

        private OperacionDto? _operacion;
        private OperacionAprobacionDto? _aprobacion;
        private bool _cargandoCombo;

        private readonly ICargoService _cargos = AppServices.Get<ICargoService>();
        /// <summary>Último cargo seleccionado (la galería se desplaza a sus fotos).</summary>
        private CargoFila? _cargoActual;

        public OperacionVisorPage()
        {
            InitializeComponent();

            // Un solo cargo seleccionado a la vez; los renglones no se mueven. Esc limpia la selección.
            var esc = new KeyboardAccelerator { Key = global::Windows.System.VirtualKey.Escape };
            esc.Invoked += (_, args) =>
            {
                args.Handled = true;
                TablaCargos.DeselectAll();
            };
            KeyboardAccelerators.Add(esc);

            TablaCargos.RegisterPropertyChangedCallback(TableView.CellsHorizontalOffsetProperty, (_, _) => AjustarColumnaDetalle());
            Loaded += async (_, _) =>
            {
                if (_cargada) return;
                _cargada = true;
                await CargarOperacionAsync(primeraVez: true);
            };
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            switch (e.Parameter)
            {
                case OperacionVisorNavigationContext contexto:
                    _idOperacion = contexto.IdOperacion ?? contexto.Operacion?.IdOperacion ?? 0;
                    _mensajeReferenciaId = contexto.MensajeReferenciaId;
                    break;
                case OperacionDto operacion:
                    _idOperacion = operacion.IdOperacion ?? 0;
                    _mensajeReferenciaId = null;
                    break;
            }
        }

        private void CerrarVentana() => HostWindow?.Close();

        /// <summary>Correos de los contactos del cliente con login del portal (menos el dirigido), para CC.</summary>
        internal static async Task<List<string>> CorreosConLoginAsync(int idCliente, long idDirigido)
        {
            try
            {
                var logins = await AppServices.Get<IClienteLoginService>().ObtenerAsync(idCliente);
                return logins
                    .Where(l => l.ContactoId != idDirigido && !string.IsNullOrWhiteSpace(l.ContactoCorreo))
                    .Select(l => l.ContactoCorreo!)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
            catch (InvalidOperationException)
            {
                // Sin permiso para ver logins o sin conexión: solo se sugiere al dirigido.
                return new List<string>();
            }
        }

        private async Task CargarCargosAsync()
        {
            var cargos = await _cargos.GetCargosAsync(new CargoEditDto { IdOperacion = _idOperacion }, _mensajeReferenciaId);
            // Generar cotización / reporte / nota leen los cargos de la operación.
            if (_operacion != null)
            {
                _operacion.Cargos.Clear();
                foreach (var cargo in cargos)
                {
                    _operacion.Cargos.Add(cargo);
                }
            }

            TablaCargos.ItemsSource = new ObservableCollection<CargoFila>(cargos.Select(CargoFila.Desde));
            _cargoActual = null;
            ActualizarBotonesCargo();
            ActualizarTotal();
            await MostrarGaleriaAsync();
            ActualizarBotones();
        }

        private IEnumerable<CargoFila> FilasCargos =>
            TablaCargos.ItemsSource as IEnumerable<CargoFila> ?? Enumerable.Empty<CargoFila>();

        // ---------------- Agregar cargo (botón de la galería) ----------------

        private readonly IUserSessionService _sesion = AppServices.Get<IUserSessionService>();
        private readonly IActivityService _actividad = AppServices.Get<IActivityService>();

        private void TablaCargos_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Sin recargar: se resalta la sección del seleccionado, la galería se desplaza a sus
            // fotos, Agregar pasa a "Agregar fotos" y Eliminar se habilita.
            _cargoActual = TablaCargos.SelectedItem as CargoFila;
            ResaltarCargosSeleccionados();
            ActualizarBotonesCargo();
            if (_cargoActual != null)
            {
                DesplazarACargo(_cargoActual);
            }
        }

        private async Task AgregarCargoAsync()
        {
            if (!await PuedeEditarCargosAsync("agregar cargos"))
            {
                return;
            }

            var ctrl = new Dialogs.AgregarCargoUserControl(_idOperacion, _sesion.IdProveedor > 0 ? _sesion.IdProveedor : null);
            var dialogo = new ContentDialog
            {
                Title = "Agregar Cargo",
                Content = ctrl,
                PrimaryButtonText = "Agregar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = XamlRoot
            };
            dialogo.PrimaryButtonClick += (_, args) => { if (!ctrl.IsValid) args.Cancel = true; };
            if (await dialogo.ShowAsync() != ContentDialogResult.Primary)
            {
                return;
            }

            try
            {
                var nuevo = await _cargos.CreateCargoAsync(ctrl.GetCargoEditDto());
                if (nuevo == null)
                {
                    await _notificaciones.MostrarAsync("Agregar cargo", "No se pudo crear el cargo.");
                    return;
                }

                _actividad.Registrar("Operaciones", "Cargo agregado");
                await CargarCargosAsync();
                await CargosCambiaronAsync();
            }
            catch (Exception ex)
            {
                await _notificaciones.MostrarAsync("Agregar cargo", ex.Message);
            }
        }

        /// <summary>Igual que el visor: los PDF quedan viejos y el monto de la operación se recalcula.</summary>
        /// <summary>Suma de los montos de los cargos, como el "Total:" del visor.</summary>
        private void ActualizarTotal()
        {
            var total = FilasCargos?.Sum(f => f.Monto) ?? 0d;
            TotalText.Text = total.ToString("C2", new System.Globalization.CultureInfo("es-MX"));
        }

        private async Task CargosCambiaronAsync()
        {
            ActualizarTotal();
            _operaciones.DeleteOperacionPdfs(_idOperacion, "*");
            var total = (decimal)FilasCargos.Sum(f => f.Monto);
            if (await _operaciones.UpdateOperacionAsync(_idOperacion, monto: total) && _operacion != null)
            {
                _operacion.Monto = total;
            }
        }

        private async Task<bool> PuedeEditarCargosAsync(string accion)
        {
            if (_operacion == null)
            {
                return false;
            }

            if (_operacion.IsSharedReadOnly)
            {
                await _notificaciones.MostrarAsync("Solo lectura", $"Esta operación fue compartida por chat en modo solo lectura. No puedes {accion}.");
                return false;
            }

            if (_operacion.IsFinalized)
            {
                await _notificaciones.MostrarAsync("Operación finalizada", $"La operación está finalizada; reábrela para {accion}.");
                return false;
            }

            if (_operacion.CotFinalizada)
            {
                await _notificaciones.MostrarAsync("Cotización cerrada", $"La cotización está cerrada; reábrela para {accion}.");
                return false;
            }

            return true;
        }

        // ---------------- Acciones: Eliminar / Reabrir / Finalizar ----------------

        private void ActualizarBotones()
        {
            var op = _operacion;

            ReabrirButton.IsEnabled = op != null && (op.IsFinalized || op.CotFinalizada);
            ToolTipService.SetToolTip(ReabrirButton, op?.IsFinalized == true
                ? "Reabrir operación"
                : op?.CotFinalizada == true ? "Reabrir cotización para modificar cargos" : "Reabrir cotización (ya está abierta)");

            ToolTipService.SetToolTip(FinalizarButton, op?.TFinalizado == true ? "Deshacer finalización del trabajo" : "Finalizar trabajo");
            FinalizarText.Text = op?.TFinalizado == true ? "Deshacer finalización" : "Finalizar";
            FinalizarButton.IsEnabled = op is { CanMutateOperation: true };

            var edicion = op?.IsSharedReadOnly == true ? Visibility.Collapsed : Visibility.Visible;
            ReabrirButton.Visibility = edicion;
            FinalizarButton.Visibility = edicion;
            ActualizarBotonesCotizacion();
        }

        private async Task<bool> ConfirmarAsync(string titulo, string mensaje, string boton)
        {
            var dialogo = new ContentDialog
            {
                Title = titulo,
                Content = mensaje,
                PrimaryButtonText = boton,
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = XamlRoot
            };
            return await dialogo.ShowAsync() == ContentDialogResult.Primary;
        }

        private async void EliminarButton_Click(object sender, RoutedEventArgs e)
        {
            var seleccion = TablaCargos.SelectedItem is CargoFila seleccionada ? new List<CargoFila> { seleccionada } : new List<CargoFila>();
            if (seleccion.Count == 0)
            {
                await _notificaciones.MostrarAsync("Eliminar", "Selecciona un cargo.");
                return;
            }

            if (!await PuedeEditarCargosAsync("eliminar cargos"))
            {
                return;
            }

            var mensaje = seleccion.Count == 1 ? $"¿Eliminar el cargo «{seleccion[0].TituloGaleria}»?" : $"¿Eliminar {seleccion.Count} cargos seleccionados?";
            if (!await ConfirmarAsync("Confirmar eliminación", mensaje, "Eliminar"))
            {
                return;
            }

            var errores = 0;
            foreach (var fila in seleccion)
            {
                try
                {
                    if (!await _cargos.DeleteCargoAsync(fila.IdCargo))
                    {
                        errores++;
                    }
                }
                catch (Exception)
                {
                    errores++;
                }
            }

            if (errores < seleccion.Count)
            {
                _actividad.Registrar("Operaciones", "Cargo eliminado");
            }

            await CargarCargosAsync();
            await CargosCambiaronAsync();
            if (errores > 0)
            {
                await _notificaciones.MostrarAsync("Eliminar", $"{errores} cargo(s) no pudieron eliminarse.");
            }
        }

        private async void ReabrirButton_Click(object sender, RoutedEventArgs e)
        {
            if (_operacion == null)
            {
                return;
            }

            try
            {
                if (_operacion.IsFinalized)
                {
                    if (!await ConfirmarAsync("Reabrir operación", "¿Desea reabrir esta operación? Se eliminará la fecha de finalización.", "Reabrir"))
                    {
                        return;
                    }

                    if (!await _operaciones.ReopenOperacionAsync(_idOperacion))
                    {
                        await _notificaciones.MostrarAsync("Reabrir operación", "No se pudo reabrir la operación.");
                        return;
                    }

                    _actividad.Registrar("Operaciones", "Operación reabierta");
                }
                else if (_operacion.CotFinalizada)
                {
                    var aprobada = _aprobacion?.Estado == "Aprobada";
                    var mensaje = aprobada
                        ? "La cotización ya fue aprobada por el cliente. Al reabrirla, esa aprobación queda como reemplazada y, cuando la vuelvas a cerrar, el cliente tendrá que aprobarla de nuevo.\n\nMientras esté abierta, la operación no aparecerá en el Portal de Clientes."
                        : "Podrás modificar los cargos. Mientras esté abierta, la operación no aparecerá en el Portal de Clientes.";
                    if (!await ConfirmarAsync("Reabrir cotización", mensaje, "Reabrir"))
                    {
                        return;
                    }

                    await _aprobaciones.ReabrirCotizacionAsync(_idOperacion);
                    _actividad.Registrar("Operaciones", aprobada ? "Cotización aprobada reabierta" : "Cotización reabierta");
                }
                else
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                await _notificaciones.MostrarAsync("Reabrir", ex.Message);
                return;
            }

            await CargarOperacionAsync(primeraVez: false);
        }

        private async void FinalizarButton_Click(object sender, RoutedEventArgs e)
        {
            if (_operacion is not { CanMutateOperation: true })
            {
                return;
            }

            var deshacer = _operacion.TFinalizado;
            if (!await ConfirmarAsync(
                    deshacer ? "Deshacer finalización de trabajo" : "Finalizar trabajo",
                    deshacer ? "¿Desea desmarcar el trabajo de esta operación como finalizado?" : "¿Desea marcar el trabajo de esta operación como finalizado?",
                    deshacer ? "Deshacer" : "Finalizar"))
            {
                return;
            }

            try
            {
                var ok = deshacer
                    ? await _operaciones.DesfinalizarTrabajoAsync(_idOperacion)
                    : await _operaciones.FinalizarTrabajoAsync(_idOperacion);
                if (!ok)
                {
                    await _notificaciones.MostrarAsync("Finalizar", "No se pudo cambiar el estado del trabajo.");
                    return;
                }

                _actividad.Registrar("Operaciones", deshacer ? "Trabajo desmarcado como finalizado" : "Trabajo marcado como finalizado");
                _operacion.TFinalizado = !deshacer;
                ActualizarBotones();
            }
            catch (Exception ex)
            {
                await _notificaciones.MostrarAsync("Finalizar", ex.Message);
            }
        }

        // ---------------- Línea de tiempo ----------------

        private static readonly SolidColorBrush BrochaCumplido = new(ColorHelper.FromArgb(0xFF, 0x22, 0xC5, 0x5E));
        private static readonly SolidColorBrush BrochaPendiente = new(ColorHelper.FromArgb(0xFF, 0x9E, 0x9E, 0x9E));

        /// <summary>
        /// Un paso por columna: punto sobre la línea, nombre del paso y, si ya se cumplió, el último
        /// que lo cumplió (botón que abre el historial del paso). Los pasos marcados antes de que
        /// existiera el historial muestran "Sin registro".
        /// </summary>
        private void ConstruirLineaTiempo()
        {
            LineaTiempo.Children.Clear();
            LineaTiempo.ColumnDefinitions.Clear();
            if (_operacion == null)
            {
                return;
            }

            var pasos = _operacion.CkPasos;
            for (var i = 0; i < pasos.Count; i++)
            {
                var paso = pasos[i];
                var siguienteCumplido = i + 1 < pasos.Count && pasos[i + 1].Completado;
                LineaTiempo.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                // Tramo de línea: la mitad izquierda llega a este punto, la derecha sale al siguiente.
                var tramo = new Grid { Height = 16 };
                tramo.ColumnDefinitions.Add(new ColumnDefinition());
                tramo.ColumnDefinitions.Add(new ColumnDefinition());
                if (i > 0)
                {
                    tramo.Children.Add(new Border { Height = 2, Background = paso.Completado ? BrochaCumplido : BrochaPendiente });
                }

                if (i + 1 < pasos.Count)
                {
                    var derecha = new Border { Height = 2, Background = siguienteCumplido ? BrochaCumplido : BrochaPendiente };
                    Grid.SetColumn(derecha, 1);
                    tramo.Children.Add(derecha);
                }

                var punto = new Microsoft.UI.Xaml.Shapes.Ellipse
                {
                    Width = 14,
                    Height = 14,
                    Fill = paso.Completado ? BrochaCumplido : new SolidColorBrush(Colors.White),
                    Stroke = paso.Completado ? BrochaCumplido : BrochaPendiente,
                    StrokeThickness = 2
                };
                Grid.SetColumnSpan(punto, 2);
                tramo.Children.Add(punto);

                var columna = new StackPanel { Spacing = 2 };
                columna.Children.Add(tramo);
                columna.Children.Add(new TextBlock
                {
                    Text = paso.Nombre,
                    FontSize = 11,
                    FontWeight = paso.Completado ? Microsoft.UI.Text.FontWeights.SemiBold : Microsoft.UI.Text.FontWeights.Normal,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextAlignment = TextAlignment.Center,
                    TextWrapping = TextWrapping.Wrap
                });
                if (paso.Completado)
                {
                    var quien = UltimoQueCumplio(paso.Campo);
                    var boton = new HyperlinkButton
                    {
                        Content = new TextBlock { Text = quien ?? "Sin registro", FontSize = 10, TextTrimming = TextTrimming.CharacterEllipsis },
                        Padding = new Thickness(4, 0, 4, 0),
                        MinHeight = 0,
                        Opacity = quien == null ? 0.6 : 1,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Tag = paso
                    };
                    ToolTipService.SetToolTip(boton, "Ver historial");
                    boton.Click += PasoHistorial_Click;
                    columna.Children.Add(boton);
                }

                Grid.SetColumn(columna, i);
                LineaTiempo.Children.Add(columna);
            }
        }

        // ---------------- Historial de los pasos ----------------

        private readonly ICheckOperacionService _checks = AppServices.Get<ICheckOperacionService>();
        private List<CheckHistorialDto> _historialChecks = new();

        private async Task CargarHistorialChecksAsync()
        {
            try
            {
                _historialChecks = await _checks.GetHistorialAsync(_idOperacion);
            }
            catch (Exception)
            {
                // Sin historial (p. ej. API sin desplegar): la línea muestra "Sin registro".
                _historialChecks = new();
            }
        }

        /// <summary>El último que marcó el paso (el historial viene del más reciente al más viejo).</summary>
        private string? UltimoQueCumplio(string campo) =>
            _historialChecks.FirstOrDefault(h => h.Campo == campo && h.Valor && !string.IsNullOrWhiteSpace(h.RealizadoPor))?.RealizadoPor;

        private async void PasoHistorial_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement { Tag: CheckPasoItem paso })
            {
                return;
            }

            var eventos = _historialChecks.Where(h => h.Campo == paso.Campo).ToList();
            var lista = new StackPanel { Spacing = 10, MinWidth = 380 };
            if (eventos.Count == 0)
            {
                lista.Children.Add(new TextBlock
                {
                    Text = "No hay eventos registrados. El paso se marcó antes de que se guardara el historial.",
                    TextWrapping = TextWrapping.Wrap,
                    Opacity = 0.7
                });
            }

            foreach (var evento in eventos)
            {
                var renglon = new Grid { ColumnSpacing = 10 };
                renglon.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                renglon.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                renglon.Children.Add(new FontIcon
                {
                    Glyph = evento.Valor ? "" : "",
                    FontSize = 14,
                    Foreground = evento.Valor ? BrochaCumplido : BrochaPendiente,
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(0, 2, 0, 0)
                });

                var texto = new StackPanel { Spacing = 1 };
                texto.Children.Add(new TextBlock
                {
                    Text = $"{(evento.Valor ? "Marcado" : "Desmarcado")} por {evento.RealizadoPor ?? "(sin registro)"}",
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    TextWrapping = TextWrapping.Wrap
                });
                texto.Children.Add(new TextBlock
                {
                    Text = evento.Fecha.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
                    FontSize = 12,
                    Opacity = 0.7
                });
                if (!string.IsNullOrWhiteSpace(evento.Detalle))
                {
                    texto.Children.Add(new TextBlock { Text = evento.Detalle, FontSize = 12, Opacity = 0.7, TextWrapping = TextWrapping.Wrap });
                }

                Grid.SetColumn(texto, 1);
                renglon.Children.Add(texto);
                lista.Children.Add(renglon);
            }

            var dialogo = new ContentDialog
            {
                Title = $"Historial · {paso.Nombre}",
                Content = new ScrollViewer { Content = lista, MaxHeight = 420 },
                CloseButtonText = "Cerrar",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = XamlRoot
            };
            await dialogo.ShowAsync();
        }

        // ---------------- Panel Info: galería ----------------

        private readonly ICargoImageService _imagenesCargo = AppServices.Get<ICargoImageService>();
        private readonly IImageViewerService _visorImagenes = AppServices.Get<IImageViewerService>();
        private int _versionFotos;

        private void MostrarMensajeInfo(string? mensaje)
        {
            InfoMensaje.Text = mensaje ?? string.Empty;
            InfoMensaje.Visibility = mensaje == null ? Visibility.Collapsed : Visibility.Visible;
        }

        private void TablaCargos_SizeChanged(object sender, SizeChangedEventArgs e) => AjustarColumnaDetalle();

        // Mismo ajuste que en Conciliación: TableView deja un hueco a la derecha de la última
        // columna, así que Detalle toma el ancho que sobra (sin bajar de 120 px).
        private const double EspacioBarraScroll = 18;
        private readonly Dictionary<TableViewColumn, double> _anchosBase = new();

        private void AjustarColumnaDetalle()
        {
            if (TablaCargos.ActualWidth <= 0)
            {
                return;
            }

            var columnasFijas = TablaCargos.Columns.Where(columna => columna != ColumnaDetalle).ToList();
            foreach (var columna in columnasFijas)
            {
                _anchosBase.TryAdd(columna, columna.Width.Value);
            }

            var restante = TablaCargos.ActualWidth - TablaCargos.CellsHorizontalOffset
                - columnasFijas.Sum(columna => _anchosBase[columna]) - EspacioBarraScroll;
            ColumnaDetalle.Width = new GridLength(Math.Max(restante, 120));
        }

        private async Task CargarOperacionAsync(bool primeraVez)
        {
            if (_idOperacion <= 0)
            {
                await _notificaciones.MostrarAsync("Operación", "No se pudo determinar la operación a visualizar.");
                CerrarVentana();
                return;
            }

            try
            {
                var acceso = await _operaciones.GetOperacionVisorAsync(_idOperacion, _mensajeReferenciaId);
                if (acceso?.Operacion == null)
                {
                    await _notificaciones.MostrarAsync("Acceso no disponible", "No tienes acceso a esta operación o la referencia de chat ya no está disponible.");
                    CerrarVentana();
                    return;
                }

                _operacion = acceso.Operacion;
                _operacion.IsSharedReadOnly = acceso.SoloLectura;
                _mensajeReferenciaId = acceso.SoloLectura ? acceso.MensajeReferenciaId : null;
                NumeroOperacionText.Text = $"#{_operacion.FolioTexto}";
                if (HostWindow != null)
                {
                    HostWindow.Title = $"Operación #{_operacion.FolioTexto}";
                }

                ClienteText.Text = _operacion.RazonSocial ?? "Sin cliente";
                EquipoText.Text = _operacion.Identificador ?? "Sin equipo";
                NotaText.Text = _operacion.Nota ?? string.Empty;
                ToolTipService.SetToolTip(NotaText, string.IsNullOrWhiteSpace(_operacion.Nota) ? null : _operacion.Nota);
                AtiendeText.Text = string.IsNullOrWhiteSpace(_operacion.Atiende) ? "—" : _operacion.Atiende;
                InicioText.Text = _operacion.FechaInicioCorta;
                MostrarEstadoOperacion(_operacion);

                try
                {
                    _aprobacion = await _aprobaciones.ObtenerOperacionAsync(_idOperacion);
                    _operacion.CotFinalizada = _aprobacion.CotFinalizada;
                    CotizacionEstadoText.Text = _aprobacion.EstadoTexto ?? string.Empty;
                }
                catch (Exception) when (_operacion.IsSharedReadOnly)
                {
                    // En solo lectura puede no haber permiso para la aprobación; no impide consultar.
                    _aprobacion = null;
                    CotizacionEstadoText.Text = string.Empty;
                }

                // El check completo (incluye "Cotización aprobada"); si falla quedan los campos en línea.
                _operacion.BuildCheckFromInlineFields();
                await _operaciones.LoadCheckAsync(_operacion);
                await CargarHistorialChecksAsync();
                ConstruirLineaTiempo();
                ActualizarBotones();

                if (!_operacion.IsSharedReadOnly)
                {
                    await CargarContactosAsync();
                }
                else
                {
                    DirigidoCombo.IsEnabled = false;
                }

                await CargarCargosAsync();

                if (primeraVez && _operacion.IsSharedReadOnly)
                {
                    await _notificaciones.MostrarAsync("Modo solo lectura",
                        "Esta operación fue compartida contigo por chat. Puedes consultar y descargar, pero no editar.");
                }
            }
            catch (Exception ex)
            {
                NumeroOperacionText.Text = $"Error al cargar la operación {_idOperacion}";
                await _notificaciones.MostrarAsync("Operación", ex.Message);
            }
        }

        private async Task CargarContactosAsync()
        {
            if (_operacion?.IdCliente is not int idCliente)
            {
                DirigidoCombo.IsEnabled = false;
                return;
            }

            List<ContactoDto> contactos = (await _contactos.GetContactosAsync(new ContactoQueryDto { IdCliente = idCliente }))
                .OrderBy(c => c.NombreCompleto)
                .ToList();

            _cargandoCombo = true;
            DirigidoCombo.ItemsSource = contactos;
            DirigidoCombo.SelectedItem = contactos.FirstOrDefault(c => c.ContactoId == _aprobacion?.IdContactoDirigido);
            DirigidoCombo.IsEnabled = _operacion.CanMutateOperation;
            _cargandoCombo = false;
        }

        /// <summary>Igual que "Cambiar contacto dirigido" del visor: guarda el contacto elegido.</summary>
        private async void DirigidoCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_cargandoCombo || DirigidoCombo.SelectedItem is not ContactoDto contacto
                || contacto.ContactoId == _aprobacion?.IdContactoDirigido)
            {
                return;
            }

            try
            {
                _aprobacion = await _aprobaciones.SetDirigidoAsync(_idOperacion, contacto.ContactoId);
                CotizacionEstadoText.Text = _aprobacion.EstadoTexto ?? string.Empty;
            }
            catch (InvalidOperationException ex)
            {
                await _notificaciones.MostrarAsync("Contacto dirigido", ex.Message);
                await CargarContactosAsync();
            }
        }

        private void MostrarEstadoOperacion(OperacionDto operacion)
        {
            var color = operacion.IsFinalized
                ? ColorHelper.FromArgb(0xFF, 0x80, 0x80, 0x80)
                : ColorHelper.FromArgb(0xFF, 0x2D, 0xA4, 0x4E);
            EstadoBadgeText.Text = (operacion.IsFinalized ? "✓ Finalizada" : "● Abierta")
                + (operacion.IsSharedReadOnly ? " · 🔒 Solo lectura por chat" : string.Empty);
            EstadoBadgeText.Foreground = new SolidColorBrush(color);
            EstadoBadge.BorderBrush = new SolidColorBrush(color);
            EstadoBadge.Background = new SolidColorBrush(ColorHelper.FromArgb(0x15, color.R, color.G, color.B));
        }
    }
}
