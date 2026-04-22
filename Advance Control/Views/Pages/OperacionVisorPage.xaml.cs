using Advance_Control.Services.Quotes;
using Advance_Control.Models;
using Advance_Control.Services.Activity;
using Advance_Control.Services.Cargos;
using Advance_Control.Services.Contactos;
using Advance_Control.Services.ImageViewer;
using Advance_Control.Services.LocalStorage;
using Advance_Control.Services.Mensajeria;
using Advance_Control.Services.Notificacion;
using Advance_Control.Services.Session;
using Advance_Control.Utilities;
using Advance_Control.ViewModels;
using Advance_Control.Views.Dialogs;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using global::Windows.Globalization.NumberFormatting;
using global::Windows.Storage.Pickers;
// Alias para evitar colisión con el namespace Advance_Control.Views.Windows
using WinStorage = global::Windows.Storage;
using WinSystem = global::Windows.System;

namespace Advance_Control.Views.Pages
{
    /// <summary>
    /// Página visor para gestionar una operación con tres paneles:
    /// información + acciones/documentos (izquierdo), cargos (central) y tareas (derecho).
    /// Reemplaza a OperacionVisorWindow para funcionar dentro del frame de navegación principal.
    /// </summary>
    public sealed partial class OperacionVisorPage : Microsoft.UI.Xaml.Controls.Page
    {
        private static readonly Brush CheckCompletedBrush = new SolidColorBrush(global::Windows.UI.Color.FromArgb(255, 0x22, 0xC5, 0x5E));
        private static readonly Brush CheckPendingBrush = new SolidColorBrush(global::Windows.UI.Color.FromArgb(255, 0x6B, 0x72, 0x80));
        private static readonly Brush ChecklistPendingBrush = new SolidColorBrush(global::Windows.UI.Color.FromArgb(255, 0x80, 0x80, 0x80));

        private readonly OperacionesViewModel _viewModel;
        private readonly INotificacionService _notificacionService;
        private readonly ICargoService _cargoService;
        private readonly IUserSessionService _userSessionService;
        private readonly ICargoImageService _cargoImageService;
        private readonly IOperacionImageService _operacionImageService;
        private readonly IImageViewerService _imageViewerService;
        private readonly IActivityService _activityService;
        private readonly IContactoService _contactoService;
        private readonly IFirmaService _firmaService;
        private readonly IMensajeriaService _mensajeriaService;
        private readonly ChatPanelViewModel _chatPanelViewModel;

        private XamlRoot? _xamlRoot;
        private IntPtr _hwnd;
        private long? _mensajeReferenciaId;
        private OperacionVisorNavigationContext? _navigationContext;

        /// <summary>La operación que se visualiza en esta página.</summary>
        public OperacionDto Operacion { get; private set; } = null!;

        /// <summary>Formateador de moneda MXN para el NumberBox de total.</summary>
        public INumberFormatter2 CurrencyFormatter { get; }

        public OperacionVisorPage()
        {
            _viewModel            = AppServices.Get<OperacionesViewModel>();
            _notificacionService  = AppServices.Get<INotificacionService>();
            _cargoService         = AppServices.Get<ICargoService>();
            _userSessionService   = AppServices.Get<IUserSessionService>();
            _cargoImageService    = AppServices.Get<ICargoImageService>();
            _operacionImageService = AppServices.Get<IOperacionImageService>();
            _imageViewerService   = AppServices.Get<IImageViewerService>();
            _activityService      = AppServices.Get<IActivityService>();
            _contactoService      = AppServices.Get<IContactoService>();
            _firmaService         = AppServices.Get<IFirmaService>();
            _mensajeriaService    = AppServices.Get<IMensajeriaService>();
            _chatPanelViewModel   = AppServices.Get<ChatPanelViewModel>();

            var fmt = new CurrencyFormatter("MXN");
            fmt.FractionDigits = 2;
            CurrencyFormatter = fmt;

            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is OperacionVisorNavigationContext contexto)
            {
                _navigationContext = contexto;
                _mensajeReferenciaId = contexto.MensajeReferenciaId;

                if (contexto.Operacion != null)
                {
                    Operacion = contexto.Operacion;
                    Operacion.IsSharedReadOnly = false;
                }
            }
            else if (e.Parameter is OperacionDto operacion)
            {
                Operacion = operacion;
                Operacion.IsSharedReadOnly = false;
                _mensajeReferenciaId = null;
            }

            // Obtener el handle de la ventana principal para los pickers
            if (App.MainWindow != null)
                _hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);

            RootGrid.Loaded += async (_, _) =>
            {
                _xamlRoot = RootGrid.XamlRoot;
                if (_navigationContext?.IdOperacion.HasValue == true || Operacion != null)
                {
                    try
                    {
                        await EnsureOperacionContextAsync();

                        if (Operacion.IsSharedReadOnly)
                            await _notificacionService.MostrarAsync(
                                "Modo solo lectura",
                                "Esta operación fue compartida contigo por chat. Puedes consultar y descargar, pero no editar.");

                        await CargarDatosInicialesAsync();
                    }
                    catch (Exception ex)
                    {
                        LogDebugError(nameof(CargarDatosInicialesAsync), ex);
                        await MostrarErrorAsync("Acceso no disponible", ex is UnauthorizedAccessException
                            ? ex.Message
                            : "No se pudo abrir la operación solicitada.");
                        if (Frame.CanGoBack)
                            Frame.GoBack();
                    }
                }
            };
        }

        private async Task EnsureOperacionContextAsync()
        {
            var idOperacion = _navigationContext?.IdOperacion ?? Operacion?.IdOperacion;
            if (!idOperacion.HasValue || idOperacion.Value <= 0)
                throw new InvalidOperationException("No se pudo determinar la operación a visualizar.");

            if (_mensajeReferenciaId.HasValue || _navigationContext?.Operacion == null)
            {
                var acceso = await _viewModel.GetOperacionVisorAsync(idOperacion.Value, _mensajeReferenciaId);
                if (acceso?.Operacion == null)
                    throw new UnauthorizedAccessException("No tienes acceso a esta operación o la referencia de chat ya no está disponible.");

                Operacion = acceso.Operacion;
                Operacion.BuildCheckFromInlineFields();
                Operacion.IsSharedReadOnly = acceso.SoloLectura;
                _mensajeReferenciaId = acceso.SoloLectura ? acceso.MensajeReferenciaId : null;
                return;
            }

            Operacion.IsSharedReadOnly = false;
            _mensajeReferenciaId = null;
        }

        private async Task<bool> EnsureCanMutateAsync(string accion)
        {
            if (Operacion.CanMutateOperation)
                return true;

            if (Operacion.IsSharedReadOnly)
            {
                await MostrarErrorAsync("Solo lectura", $"Esta operación fue compartida por chat en modo solo lectura. No puedes {accion}.");
                return false;
            }

            if (Operacion.IsFinalized)
            {
                await MostrarErrorAsync("Operación finalizada", "La operación está finalizada y no permite esta acción.");
                return false;
            }

            await MostrarErrorAsync("Acción no disponible", "La operación no permite esta acción.");
            return false;
        }

        /// <summary>Navega de vuelta a la lista de operaciones.</summary>
        private void VolverButton_Click(object sender, RoutedEventArgs e)
        {
            if (Frame.CanGoBack)
                Frame.GoBack();
        }

        private async void CompartirOperacionButton_Click(object sender, RoutedEventArgs e)
        {
            if (!Operacion.IdOperacion.HasValue)
                return;

            var xamlRoot = _xamlRoot ?? RootGrid.XamlRoot;
            if (_chatPanelViewModel.UsuarioSeleccionado == null)
            {
                var sinChatDialog = new ContentDialog
                {
                    Title = "Compartir operación",
                    Content = "Debes seleccionar un chat",
                    CloseButtonText = "Cerrar",
                    XamlRoot = xamlRoot
                };
                await sinChatDialog.ShowAsync();
                return;
            }

            var comentarioBox = new TextBox
            {
                PlaceholderText = "Comentario opcional",
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                MinHeight = 96
            };

            var dialogContent = new StackPanel { Spacing = 8 };
            dialogContent.Children.Add(new TextBlock
            {
                Text = $"Se enviará al chat con {_chatPanelViewModel.UsuarioSeleccionado.NombreVisible}.",
                TextWrapping = TextWrapping.Wrap
            });
            dialogContent.Children.Add(comentarioBox);

            var dialog = new ContentDialog
            {
                Title = "Compartir operación",
                Content = dialogContent,
                PrimaryButtonText = "Enviar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = xamlRoot
            };

            if (await dialog.ShowAsync() != ContentDialogResult.Primary)
                return;

            if (!_mensajeriaService.EstaConectado)
            {
                await MostrarErrorAsync("Chat sin conexión", "No se pudo enviar la referencia porque el chat no está conectado.");
                return;
            }

            var contenido = string.IsNullOrWhiteSpace(comentarioBox.Text)
                ? $"Referencia de operación {Operacion.Identificador ?? Operacion.IdOperacion.Value.ToString()}"
                : comentarioBox.Text.Trim();

            await _mensajeriaService.EnviarMensajeAsync(
                _chatPanelViewModel.UsuarioSeleccionado.CredencialId,
                contenido,
                idReferencia: Operacion.IdOperacion.Value,
                tipoReferencia: "Operacion");

            await _notificacionService.MostrarAsync(
                "Referencia enviada",
                $"La operación se envió al chat con {_chatPanelViewModel.UsuarioSeleccionado.NombreVisible}.");
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Carga inicial
        // ─────────────────────────────────────────────────────────────────────

        private async Task CargarDatosInicialesAsync()
        {
            // Resetear flags para garantizar datos frescos en cada apertura del visor.
            // Los DTOs se reutilizan desde el ViewModel cacheado; sin este reset los
            // guards de ImagesLoaded/CargosLoaded evitarían recargar del VPS.
            Operacion.ImagesLoaded  = false;
            Operacion.CargosLoaded  = false;
            foreach (var c in Operacion.Cargos) c.ImagesLoaded = false;

            await LoadCargosAsync();
            await RefreshImageIndicatorsAsync();

            if (!Operacion.TieneCheck)
            {
                Operacion.IsLoadingCheck = true;
                try { await _viewModel.LoadCheckAsync(Operacion); }
                finally { Operacion.IsLoadingCheck = false; }
            }

            _viewModel.RefreshPdfPaths(Operacion);

            // Suscribir CollectionChanged para actualizar TotalMonto
            Operacion.Cargos.CollectionChanged -= OnCargosCollectionChanged;
            Operacion.Cargos.CollectionChanged += OnCargosCollectionChanged;
        }

        private void OnCargosCollectionChanged(object? s, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
            {
                foreach (CargoDto cargo in e.NewItems)
                    cargo.PropertyChanged += (_, args) =>
                    {
                        if (args.PropertyName == nameof(CargoDto.Monto))
                            Operacion.OnPropertyChanged(nameof(Operacion.TotalMonto));
                    };
            }
            Operacion.OnPropertyChanged(nameof(Operacion.TotalMonto));
        }

        private async Task LoadCargosAsync()
        {
            if (!Operacion.IdOperacion.HasValue) return;
            try
            {
                if (!Operacion.CargosLoaded && !Operacion.IsLoadingCargos)
                {
                    Operacion.IsLoadingCargos = true;
                    var cargos = await _cargoService.GetCargosAsync(new CargoEditDto { IdOperacion = Operacion.IdOperacion.Value }, _mensajeReferenciaId);
                    Operacion.Cargos.Clear();
                    foreach (var c in cargos)
                    {
                        Operacion.Cargos.Add(c);
                        c.PropertyChanged += (_, args) =>
                        {
                            if (args.PropertyName == nameof(CargoDto.Monto))
                                Operacion.OnPropertyChanged(nameof(Operacion.TotalMonto));
                        };
                        _ = LoadImagesForCargoSafeAsync(c);
                    }
                    Operacion.OnPropertyChanged(nameof(Operacion.TotalMonto));
                    Operacion.CargosLoaded = true;
                }
            }
            catch (Exception ex)
            {
                LogDebugError(nameof(LoadCargosAsync), ex);
                await MostrarErrorAsync("Error al cargar cargos", ex.Message);
            }
            finally
            {
                Operacion.IsLoadingCargos = false;
            }
        }

        private async Task RefreshImageIndicatorsAsync()
        {
            if (!Operacion.IdOperacion.HasValue) return;
            try
            {
                var prefacturas     = await _operacionImageService.GetPrefacturasAsync(Operacion.IdOperacion.Value, _mensajeReferenciaId);
                var hojasServicio   = await _operacionImageService.GetHojasServicioAsync(Operacion.IdOperacion.Value, _mensajeReferenciaId);
                var ordenesCompra   = await _operacionImageService.GetOrdenComprasAsync(Operacion.IdOperacion.Value, _mensajeReferenciaId);
                var levantamientos  = await _operacionImageService.GetLevantamientosAsync(Operacion.IdOperacion.Value, _mensajeReferenciaId);
                var hasFactura      = await _operacionImageService.HasFacturaAsync(Operacion.IdOperacion.Value, _mensajeReferenciaId);

                Operacion.HasPrefactura    = prefacturas.Count > 0;
                Operacion.HasHojaServicio  = hojasServicio.Count > 0;
                Operacion.HasOrdenCompra   = ordenesCompra.Count > 0;
                Operacion.HasLevantamiento = levantamientos.Count > 0;
                Operacion.HasFactura       = hasFactura;

                // Limpiar y repoblar las colecciones existentes en vez de reemplazar
                // para que ItemsRepeater detecte los cambios vía CollectionChanged.
                ReplaceItems(Operacion.ImagenesPrefactura, prefacturas);
                ReplaceItems(Operacion.ImagenesHojaServicio, hojasServicio);
                ReplaceItems(Operacion.ImagenesOrdenCompra, ordenesCompra);
                ReplaceItems(Operacion.ImagenesLevantamiento, levantamientos);

                Operacion.ImagesLoaded = true;
                Operacion.NotifyDocumentsChanged();
            }
            catch (Exception ex)
            {
                LogDebugError(nameof(RefreshImageIndicatorsAsync), ex);
                await MostrarErrorAsync("Error al cargar archivos", ex.Message);
            }
            finally
            {
                // Asegurar que la UI no quede con spinner infinito aunque haya excepcion
                Operacion.ImagesLoaded = true;
                Operacion.NotifyDocumentsChanged();
            }
        }

        /// <summary>
        /// Limpia y repobla una ObservableCollection manteniendo la misma instancia.
        /// </summary>
        private static void ReplaceItems<T>(System.Collections.ObjectModel.ObservableCollection<T> collection, System.Collections.Generic.IList<T> newItems)
        {
            collection.Clear();
            foreach (var item in newItems)
                collection.Add(item);
        }

        /// <summary>
        /// Fuerza al ItemsRepeater a re-leer su ItemsSource desvinculando y re-asignando
        /// la colección. Soluciona un problema en WinUI 3 donde ObservableCollection.Add
        /// no siempre genera los contenedores visuales dentro de DataTemplates complejos.
        /// </summary>
        private static void ForceRefreshRepeater(ItemsRepeater repeater, object itemsSource)
        {
            repeater.ItemsSource = null;
            repeater.ItemsSource = itemsSource;
        }

        private async Task ActualizarMontoEnServidorAsync()
        {
            if (Operacion.IdOperacion.HasValue != true) return;
            Operacion.OnPropertyChanged(nameof(Operacion.TotalMonto));
            var nuevoMonto = (decimal)Operacion.TotalMonto;
            await _viewModel.UpdateOperacionAsync(Operacion.IdOperacion.Value, monto: nuevoMonto);
            Operacion.Monto = nuevoMonto;
        }

        private Task MostrarErrorAsync(string titulo, string mensaje) =>
            _notificacionService.MostrarAsync(titulo, mensaje);

        private static void LogDebugError(string contexto, Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OperacionVisorPage::{contexto}: {ex.GetType().Name} - {ex.Message}");
        }

        private async void MainTabView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Tab 1 = "Documentos" — refrescar imágenes del VPS cada vez que se activa
            if (MainTabView.SelectedIndex == 1 && Operacion.IdOperacion.HasValue)
            {
                Operacion.ImagesLoaded = false;
                try
                {
                    await RefreshImageIndicatorsAsync();
                }
                catch (Exception ex)
                {
                    LogDebugError(nameof(MainTabView_SelectionChanged), ex);
                    await MostrarErrorAsync("Error al cargar documentos", ex.Message);
                }
            }
        }

        public Visibility ToBoolVisibility(bool value) => value ? Visibility.Visible : Visibility.Collapsed;

        public Visibility ToInverseBoolVisibility(bool value) => value ? Visibility.Collapsed : Visibility.Visible;

        public Visibility ToTextVisibility(string? value) => string.IsNullOrWhiteSpace(value) ? Visibility.Collapsed : Visibility.Visible;

        private static readonly CultureInfo MxCulture = new("es-MX");

        public string FormatDecimalCurrency(decimal value) => value.ToString("C2", MxCulture);

        public string FormatNullableDecimalCurrency(decimal? value) => (value ?? 0m).ToString("C2", MxCulture);

        public string FormatDoubleCurrency(double value) => value.ToString("C2", MxCulture);

        public string FormatNullableDoubleCurrency(double? value) => (value ?? 0d).ToString("C2", MxCulture);

        public Brush ToCheckBrush(bool completed) => completed ? CheckCompletedBrush : CheckPendingBrush;

        public Brush ToChecklistBrush(bool completed) => completed ? CheckCompletedBrush : ChecklistPendingBrush;

        /// <summary>
        /// Muestra el badge "Trabajo Finalizado" solo si TFinalizado=true y la operación no está facturada
        /// </summary>
        public Visibility ToTrabajoFinalizadoBadgeVisibility(bool isTrabajoFinalizado, bool isFinalized)
            => (isTrabajoFinalizado && !isFinalized) ? Visibility.Visible : Visibility.Collapsed;

        /// <summary>
        /// Texto del botón toggle: "Finalizar" o "Deshacer Finalización"
        /// </summary>
        public string ToFinalizarTrabajoText(bool tFinalizado)
            => tFinalizado ? "Deshacer Finalización" : "Finalizar";

        /// <summary>
        /// Glyph del botón toggle: ✓ para finalizar, ↩ para deshacer
        /// </summary>
        public string ToFinalizarTrabajoGlyph(bool tFinalizado)
            => tFinalizado ? "\uE10B" : "\uE73E";

        /// <summary>
        /// Tooltip del botón toggle
        /// </summary>
        public string ToFinalizarTrabajoTooltip(bool tFinalizado)
            => tFinalizado ? "Desmarcar trabajo como finalizado" : "Marcar trabajo como finalizado";

        // ─────────────────────────────────────────────────────────────────────
        //  Operación
        // ─────────────────────────────────────────────────────────────────────

        private async void ReabrirOperacionButton_Click(object sender, RoutedEventArgs e)
        {
            if (!Operacion.IdOperacion.HasValue) return;
            if (!await EnsureCanMutateAsync("reabrir esta operación")) return;
            var dialog = new ContentDialog
            {
                Title = "Reabrir operación",
                Content = "¿Desea reabrir esta operación? Se eliminará la fecha de finalización.",
                PrimaryButtonText = "Reabrir",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = _xamlRoot
            };
            if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;
            try
            {
                var ok = await _viewModel.ReopenOperacionAsync(Operacion.IdOperacion.Value);
                if (ok) { _activityService.Registrar("Operaciones", "Operación reabierta"); Operacion.FechaFinal = null; await _notificacionService.MostrarAsync("Operación reabierta", "La operación fue reabierta correctamente."); }
                else    await MostrarErrorAsync("Error", "No se pudo reabrir la operación.");
            }
            catch (Exception ex) { LogDebugError(nameof(ReabrirOperacionButton_Click), ex); await MostrarErrorAsync("Error", "Ocurrió un error al reabrir la operación."); }
        }

        private async void FinalizarTrabajoButton_Click(object sender, RoutedEventArgs e)
        {
            if (!Operacion.IdOperacion.HasValue) return;
            if (!await EnsureCanMutateAsync("cambiar el estado del trabajo")) return;

            bool estaFinalizado = Operacion.TFinalizado;
            var dialog = new ContentDialog
            {
                Title = estaFinalizado ? "Deshacer finalización de trabajo" : "Finalizar trabajo",
                Content = estaFinalizado
                    ? "¿Desea desmarcar el trabajo de esta operación como finalizado?"
                    : "¿Desea marcar el trabajo de esta operación como finalizado?",
                PrimaryButtonText = estaFinalizado ? "Deshacer" : "Finalizar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = _xamlRoot
            };
            if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;

            try
            {
                bool ok;
                if (estaFinalizado)
                {
                    ok = await _viewModel.DesfinalizarTrabajoAsync(Operacion.IdOperacion.Value);
                    if (ok)
                    {
                        _activityService.Registrar("Operaciones", "Trabajo desmarcado como finalizado");
                        Operacion.TFinalizado = false;
                        await _notificacionService.MostrarAsync("Trabajo desmarcado", "El trabajo fue desmarcado como finalizado.");
                    }
                    else await MostrarErrorAsync("Error", "No se pudo desmarcar el trabajo como finalizado.");
                }
                else
                {
                    ok = await _viewModel.FinalizarTrabajoAsync(Operacion.IdOperacion.Value);
                    if (ok)
                    {
                        _activityService.Registrar("Operaciones", "Trabajo marcado como finalizado");
                        Operacion.TFinalizado = true;
                        await _notificacionService.MostrarAsync("Trabajo finalizado", "El trabajo fue marcado como finalizado correctamente.");
                    }
                    else await MostrarErrorAsync("Error", "No se pudo marcar el trabajo como finalizado.");
                }
            }
            catch (Exception ex) { LogDebugError(nameof(FinalizarTrabajoButton_Click), ex); await MostrarErrorAsync("Error", "Ocurrió un error al cambiar el estado del trabajo."); }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Cargos
        // ─────────────────────────────────────────────────────────────────────

        private async void AddCargoButton_Click(object sender, RoutedEventArgs e)
        {
            if (!Operacion.IdOperacion.HasValue) return;
            if (!await EnsureCanMutateAsync("agregar cargos")) return;
            var ctrl = new Dialogs.AgregarCargoUserControl(Operacion.IdOperacion.Value, _userSessionService.IdProveedor > 0 ? _userSessionService.IdProveedor : (int?)null);
            var dialog = new ContentDialog { Title = "Agregar Cargo", Content = ctrl, PrimaryButtonText = "Agregar", CloseButtonText = "Cancelar", DefaultButton = ContentDialogButton.Primary, XamlRoot = _xamlRoot };
            dialog.PrimaryButtonClick += (d, args) => { if (!ctrl.IsValid) args.Cancel = true; };
            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                try
                {
                    var dto = ctrl.GetCargoEditDto();
                    var newCargo = await _cargoService.CreateCargoAsync(dto);
                    if (newCargo != null)
                    {
                        _activityService.Registrar("Operaciones", "Cargo agregado");
                        _viewModel.DeleteOperacionPdfs(Operacion.IdOperacion.Value, "*");
                        Operacion.CotizacionPdfPath = null;
                        Operacion.ReportePdfPath = null;
                        Operacion.CargosLoaded = false;
                        await LoadCargosAsync();
                        if (Operacion.TotalMonto > 0)
                        {
                            await _viewModel.UpdateOperacionAsync(Operacion.IdOperacion.Value, monto: (decimal)Operacion.TotalMonto);
                            Operacion.Monto = (decimal)Operacion.TotalMonto;
                        }
                        await _notificacionService.MostrarAsync("Cargo creado", $"El cargo {newCargo.IdCargo} se creó correctamente.");
                    }
                    else await MostrarErrorAsync("Error", "No se pudo crear el cargo.");
                }
                catch (Exception ex) { LogDebugError(nameof(AddCargoButton_Click), ex); await MostrarErrorAsync("Error", "Ocurrió un error al crear el cargo."); }
            }
        }

        private void CargoRow_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (sender is FrameworkElement el && el.DataContext is CargoDto cargo && cargo.HasImages)
                cargo.IsGalleryExpanded = !cargo.IsGalleryExpanded;
        }

        private async void CargoField_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key != WinSystem.VirtualKey.Enter) return;
            if (sender is not TextBox tb || tb.Tag is not CargoDto cargo || cargo.IdCargo <= 0) return;
            if (!await EnsureCanMutateAsync("editar cargos")) return;
            try
            {
                // {x:Bind Mode=TwoWay} en TextBox actualiza el modelo en LostFocus, no en KeyDown.
                // Forzamos la lectura desde tb.Text antes de que el binding haya podido sincronizar.
                int col = (int)tb.GetValue(Grid.ColumnProperty);
                if (col == 2)
                {
                    if (double.TryParse(tb.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var cant) ||
                        double.TryParse(tb.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out cant))
                        cargo.Cantidad = cant;
                }
                else if (col == 3)
                {
                    if (double.TryParse(tb.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var unit) ||
                        double.TryParse(tb.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out unit))
                        cargo.Unitario = unit;
                }
                else if (col == 5)
                {
                    cargo.Nota = tb.Text;
                }

                if (cargo.TipoCargo == "Servicio" && cargo.Cantidad != 1)
                {
                    cargo.Cantidad = 1;
                    await _notificacionService.MostrarAsync("Información", "Para cargos de tipo Servicio, la cantidad siempre es 1.");
                }
                var query = new CargoEditDto { IdCargo = cargo.IdCargo, IdTipoCargo = cargo.IdTipoCargo, IdRelacionCargo = cargo.IdRelacionCargo, Monto = cargo.Monto, Nota = cargo.Nota, Cantidad = cargo.Cantidad, Unitario = cargo.Unitario };
                var ok = await _cargoService.UpdateCargoAsync(query);
                if (ok)
                {
                    _activityService.Registrar("Operaciones", "Cargo modificado");
                    cargo.IsEditing = false;
                    _viewModel.DeleteOperacionPdfs(Operacion.IdOperacion!.Value, "*");
                    Operacion.CotizacionPdfPath = null;
                    Operacion.ReportePdfPath = null;
                    await ActualizarMontoEnServidorAsync();
                    await _notificacionService.MostrarAsync("Cargo actualizado", "El cargo se actualizó correctamente.");
                }
                else await MostrarErrorAsync("Error", "No se pudo actualizar el cargo.");
            }
            catch (Exception ex) { LogDebugError(nameof(CargoField_KeyDown), ex); await MostrarErrorAsync("Error", "Ocurrió un error al actualizar el cargo."); }
        }

        private async void CargoCheckBox_Checked(object sender, RoutedEventArgs e)
        {   
            if (sender is not CheckBox cb || cb.Tag is not CargoDto selectedCargo) return;
            foreach (var c in Operacion.Cargos)
                if (c != selectedCargo) { c.IsSelected = false; c.IsGalleryExpanded = false; }

            if (!selectedCargo.ImagesLoaded)
                await LoadImagesForCargoSafeAsync(selectedCargo);
            selectedCargo.IsGalleryExpanded = true;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Cargos seleccionados
        // ─────────────────────────────────────────────────────────────────────

        private List<CargoDto> GetSelectedCargos() => Operacion.Cargos.Where(c => c.IsSelected).ToList();

        private async void UploadSelectedCargoImageButton_Click(object sender, RoutedEventArgs e)
        {
            if (!await EnsureCanMutateAsync("cargar imágenes al cargo")) return;
            var sel = GetSelectedCargos();
            if (sel.Count == 0) { await MostrarErrorAsync("Sin selección", "Seleccione un cargo para cargar imagen."); return; }
            var cargo = sel[0];
            if (sel.Count > 1) await _notificacionService.MostrarAsync("Múltiples selecciones", "Se usará solo el primer cargo seleccionado.");
            if (cargo.IdCargo <= 0 || !cargo.IdOperacion.HasValue) { await MostrarErrorAsync("Error", "El cargo no tiene un ID válido."); return; }
            try
            {
                var picker = new FileOpenPicker();
                WinRT.Interop.InitializeWithWindow.Initialize(picker, _hwnd);
                picker.ViewMode = PickerViewMode.Thumbnail;
                picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
                picker.FileTypeFilter.Add(".jpg"); picker.FileTypeFilter.Add(".jpeg"); picker.FileTypeFilter.Add(".png"); picker.FileTypeFilter.Add(".bmp");
                var file = await picker.PickSingleFileAsync();
                if (file == null) return;
                cargo.IsLoadingImages = true;
                using var stream = await file.OpenStreamForReadAsync();
                var result = await _cargoImageService.UploadImageAsync(cargo.IdOperacion.Value, cargo.IdCargo, stream, ImageContentTypeHelper.GetContentTypeFromExtension(file.FileType));
                if (result != null) { cargo.Images.Add(result); cargo.NotifyImagesChanged(); await _notificacionService.MostrarAsync("Imagen cargada", $"Imagen {result.FileName} guardada correctamente."); }
                else await MostrarErrorAsync("Error", "No se pudo guardar la imagen.");
            }
            catch (Exception ex) { LogDebugError(nameof(UploadSelectedCargoImageButton_Click), ex); await MostrarErrorAsync("Error", "Ocurrió un error al cargar la imagen."); }
            finally { cargo.IsLoadingImages = false; }
        }

        private async void EditSelectedCargoButton_Click(object sender, RoutedEventArgs e)
        {
            if (!await EnsureCanMutateAsync("editar cargos")) return;
            var sel = GetSelectedCargos();
            if (sel.Count == 0) { await MostrarErrorAsync("Sin selección", "Seleccione un cargo para editar."); return; }
            if (sel.Count > 1) await _notificacionService.MostrarAsync("Múltiples selecciones", "Se usará solo el primer cargo seleccionado.");
            sel[0].IsEditing = !sel[0].IsEditing;
        }

        private async void DeleteSelectedCargosButton_Click(object sender, RoutedEventArgs e)
        {
            if (!await EnsureCanMutateAsync("eliminar cargos")) return;
            var sel = GetSelectedCargos();
            if (sel.Count == 0) { await MostrarErrorAsync("Sin selección", "Seleccione al menos un cargo para eliminar."); return; }
            var msg = sel.Count == 1 ? $"¿Eliminar el cargo {sel[0].IdCargo}?" : $"¿Eliminar {sel.Count} cargos seleccionados?";
            var dialog = new ContentDialog { Title = "Confirmar eliminación", Content = msg, PrimaryButtonText = "Eliminar", CloseButtonText = "Cancelar", DefaultButton = ContentDialogButton.Close, XamlRoot = _xamlRoot };
            if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;
            var ok = 0; var err = 0;
            foreach (var c in sel)
            {
                try { if (await _cargoService.DeleteCargoAsync(c.IdCargo)) { Operacion.Cargos.Remove(c); ok++; } else err++; }
                catch (Exception ex) { LogDebugError(nameof(DeleteSelectedCargosButton_Click), ex); err++; }
            }
            if (ok > 0) { await ActualizarMontoEnServidorAsync(); await _notificacionService.MostrarAsync("Eliminados", $"{ok} cargo(s) eliminado(s)."); }
            if (err > 0) await MostrarErrorAsync("Error parcial", $"{err} cargo(s) no pudieron eliminarse.");
        }

        private async void ViewSelectedRefaccionButton_Click(object sender, RoutedEventArgs e)
        {
            var sel = GetSelectedCargos();
            if (sel.Count == 0) { await MostrarErrorAsync("Sin selección", "Seleccione un cargo de tipo Refaccion."); return; }
            var cargo = sel[0];
            if (cargo.TipoCargo != "Refaccion") { await MostrarErrorAsync("Tipo incorrecto", "Solo se pueden ver refacciones para cargos de tipo 'Refaccion'."); return; }
            try
            {
                var cargos = await _cargoService.GetCargosAsync(new CargoEditDto { IdCargo = cargo.IdCargo });
                var updated = cargos?.FirstOrDefault();
                if (updated == null || !updated.IdRelacionCargo.HasValue) { await MostrarErrorAsync("Error", "El cargo no tiene una relación de refacción válida."); return; }
                var dialog = new ContentDialog { Title = "Detalles de la Refacción", Content = new Dialogs.RefaccionesViewerUserControl(updated.IdRelacionCargo.Value), CloseButtonText = "Cerrar", XamlRoot = _xamlRoot };
                await dialog.ShowAsync();
            }
            catch (Exception ex) { LogDebugError(nameof(ViewSelectedRefaccionButton_Click), ex); await MostrarErrorAsync("Error", "No se pudo cargar la refacción."); }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Imágenes de cargo
        // ─────────────────────────────────────────────────────────────────────

        private async Task LoadImagesForCargoSafeAsync(CargoDto cargo)
        {
            try { await LoadImagesForCargoAsync(cargo); }
            catch (Exception ex)
            {
                LogDebugError($"{nameof(LoadImagesForCargoSafeAsync)}[{cargo.IdCargo}]", ex);
                await MostrarErrorAsync("Error al cargar imágenes", $"No se pudieron cargar las imágenes del cargo {cargo.IdCargo}. {ex.Message}");
            }
        }

        private async Task LoadImagesForCargoAsync(CargoDto cargo)
        {
            if (cargo.IdCargo <= 0 || !cargo.IdOperacion.HasValue || cargo.IsLoadingImages) return;
            try
            {
                cargo.IsLoadingImages = true;
                var imgs = await _cargoImageService.GetImagesAsync(cargo.IdOperacion.Value, cargo.IdCargo, _mensajeReferenciaId);
                cargo.Images.Clear();
                foreach (var img in imgs) cargo.Images.Add(img);
                cargo.NotifyImagesChanged();
                cargo.ImagesLoaded = true;
            }
            catch (Exception ex) { LogDebugError($"{nameof(LoadImagesForCargoAsync)}[{cargo.IdCargo}]", ex); throw; }
            finally { cargo.IsLoadingImages = false; }
        }

        private async void DeleteCargoImageButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement el || el.Tag is not CargoImageDto image || string.IsNullOrEmpty(image.FileName)) return;
            if (!await EnsureCanMutateAsync("eliminar imágenes del cargo")) return;
            var dialog = new ContentDialog { Title = "Confirmar eliminación", Content = $"¿Eliminar la imagen {image.FileName}?", PrimaryButtonText = "Eliminar", CloseButtonText = "Cancelar", DefaultButton = ContentDialogButton.Close, XamlRoot = _xamlRoot };
            if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;
            try
            {
                var cargo = Operacion.Cargos.FirstOrDefault(c => c.IdCargo == image.IdCargo);
                if (cargo == null || !cargo.IdOperacion.HasValue) { await MostrarErrorAsync("Error", "No se pudo determinar el cargo de la imagen."); return; }
                if (await _cargoImageService.DeleteImageAsync(cargo.IdOperacion.Value, image.FileName))
                {
                    _activityService.Registrar("Operaciones", "Imagen eliminada");
                    var img = cargo.Images.FirstOrDefault(i => i.FileName == image.FileName);
                    if (img != null) { cargo.Images.Remove(img); cargo.NotifyImagesChanged(); }
                    await _notificacionService.MostrarAsync("Imagen eliminada", "La imagen se eliminó correctamente.");
                }
                else await MostrarErrorAsync("Error", "No se pudo eliminar la imagen.");
            }
            catch (Exception ex) { LogDebugError(nameof(DeleteCargoImageButton_Click), ex); await MostrarErrorAsync("Error", "Ocurrió un error al eliminar la imagen."); }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  PDFs
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Verifica si existen las firmas necesarias para el PDF.
        /// Si alguna falta, abre el ConfigurarFirmasDialog para que el usuario las cargue.
        /// Devuelve true si se debe continuar con la generación, false si el usuario canceló.
        /// </summary>
        private async Task<bool> VerificarFirmasAntesDePdfAsync()
        {
            int? idAtiende = Operacion.IdAtiende;
            bool faltaDireccion = !_firmaService.ExisteFirmaDireccion();
            bool faltaOperador  = idAtiende.HasValue && !_firmaService.ExisteFirmaOperador(idAtiende.Value);

            if (!faltaDireccion && !faltaOperador)
                return true;

            var dialog = new ConfigurarFirmasDialog(
                idAtiende,
                Operacion.Atiende ?? string.Empty,
                _firmaService,
                _xamlRoot!,
                _hwnd);

            var resultado = await dialog.ShowAsync();
            return resultado == ContentDialogResult.Primary;
        }


        private async void GenerarCotizacionButton_Click(object sender, RoutedEventArgs e)
        {
            if (!Operacion.IdOperacion.HasValue) return;
            if (Operacion.Cargos == null || Operacion.Cargos.Count == 0) { await MostrarErrorAsync("Sin cargos", "No hay cargos para generar la cotización."); return; }
            try
            {
                if (!string.IsNullOrEmpty(_viewModel.FindExistingPdf(Operacion.IdOperacion.Value, "Cotizacion")))
                    _viewModel.DeleteOperacionPdfs(Operacion.IdOperacion.Value, "Cotizacion");

                string? dirigidoA = null;
                ContactoDto? contactoParaCorreo = null;
                List<ContactoDto> contactosCliente = [];
                if (Operacion.IdCliente.HasValue && Operacion.IdCliente.Value > 0)
                {
                    try
                    {
                        var contactos = await _contactoService.GetContactosAsync(new ContactoQueryDto { IdCliente = Operacion.IdCliente.Value });
                        if (contactos?.Count > 0)
                        {
                            contactosCliente = contactos;
                            var lv = new ListView { ItemsSource = contactos, DisplayMemberPath = "NombreCompleto", SelectionMode = ListViewSelectionMode.Single, MaxHeight = 300 };
                            var sel = new ContentDialog { Title = "¿A quién va dirigida la cotización?", Content = new ScrollViewer { Content = lv, MaxHeight = 320 }, PrimaryButtonText = "Seleccionar", SecondaryButtonText = "Omitir", DefaultButton = ContentDialogButton.Primary, XamlRoot = _xamlRoot };
                            if (await sel.ShowAsync() == ContentDialogResult.Primary && lv.SelectedItem is ContactoDto c)
                            {
                                contactoParaCorreo = c;
                                dirigidoA = string.Join(" ", new[] { c.Tratamiento, c.Nombre, c.Apellido }.Where(s => !string.IsNullOrWhiteSpace(s)));
                            }
                        }
                    }
                    catch (Exception ex) { LogDebugError("ContactosCotizacion", ex); }
                }

                // Verificar firmas antes de generar el PDF
                if (!await VerificarFirmasAntesDePdfAsync()) return;

                var filePath = await _viewModel.GenerateQuoteAsync(Operacion, dirigidoA);
                if (!string.IsNullOrEmpty(filePath))
                {
                    _activityService.Registrar("Operaciones", "Cotización generada");
                    Operacion.CotizacionPdfPath = filePath;
                    await _viewModel.UpdateCheckAsync(Operacion, "cotizacion_generada");
                    var visor = new CotizacionVisorDialog(filePath, contactoParaCorreo, contactosCliente, Operacion.RazonSocial ?? string.Empty, _xamlRoot!);
                    visor.NotificarResultado(await visor.ShowAsync());
                    if (visor.Resultado == CotizacionVisorResultado.EnviarCorreo)
                    {
                        var email = new EnviarCotizacionDialog(filePath, contactoParaCorreo, contactosCliente, Operacion.RazonSocial ?? string.Empty, _xamlRoot!, idOperacion: Operacion.IdOperacion);
                        if (await email.ShowAsync() == ContentDialogResult.Primary)
                        {
                            await _viewModel.UpdateCheckAsync(Operacion, "cotizacion_enviada");
                            await _notificacionService.MostrarAsync("Correo enviado", "La cotización fue enviada correctamente.");
                        }
                    }
                    else if (visor.Resultado == CotizacionVisorResultado.AbrirExterno)
                    {
                        var file = await WinStorage.StorageFile.GetFileFromPathAsync(filePath);
                        await WinSystem.Launcher.LaunchFileAsync(file);
                    }
                }
                else await MostrarErrorAsync("Error", "No se pudo generar la cotización.");
            }
            catch (Exception ex) { LogDebugError(nameof(GenerarCotizacionButton_Click), ex); await MostrarErrorAsync("Error", "Ocurrió un error al generar la cotización."); }
        }

        private async void GenerarReporteButton_Click(object sender, RoutedEventArgs e)
        {
            if (!Operacion.IdOperacion.HasValue) return;
            if (Operacion.Cargos == null || Operacion.Cargos.Count == 0) { await MostrarErrorAsync("Sin cargos", "No hay cargos para generar el reporte."); return; }
            try
            {
                if (!string.IsNullOrEmpty(_viewModel.FindExistingPdf(Operacion.IdOperacion.Value, "Reporte")))
                    _viewModel.DeleteOperacionPdfs(Operacion.IdOperacion.Value, "Reporte");

                string? dirigidoA = null;
                ContactoDto? contactoReporte = null;
                List<ContactoDto> contactosReporte = [];
                if (Operacion.IdCliente.HasValue && Operacion.IdCliente.Value > 0)
                {
                    try
                    {
                        var contactos = await _contactoService.GetContactosAsync(new ContactoQueryDto { IdCliente = Operacion.IdCliente.Value });
                        if (contactos?.Count > 0)
                        {
                            contactosReporte = contactos;
                            var lv = new ListView { ItemsSource = contactos, DisplayMemberPath = "NombreCompleto", SelectionMode = ListViewSelectionMode.Single, MaxHeight = 300 };
                            var sel = new ContentDialog { Title = "¿A quién va dirigido el reporte?", Content = new ScrollViewer { Content = lv, MaxHeight = 320 }, PrimaryButtonText = "Seleccionar", SecondaryButtonText = "Omitir", DefaultButton = ContentDialogButton.Primary, XamlRoot = _xamlRoot };
                            if (await sel.ShowAsync() == ContentDialogResult.Primary && lv.SelectedItem is ContactoDto c)
                            {
                                contactoReporte = c;
                                dirigidoA = string.Join(" ", new[] { c.Tratamiento, c.Nombre, c.Apellido }.Where(s => !string.IsNullOrWhiteSpace(s)));
                            }
                        }
                    }
                    catch (Exception ex) { LogDebugError("ContactosReporte", ex); }
                }

                // Verificar firmas antes de generar el PDF
                if (!await VerificarFirmasAntesDePdfAsync()) return;

                var filePath = await _viewModel.GenerateReporteAsync(Operacion, dirigidoA);
                if (!string.IsNullOrEmpty(filePath))
                {
                    _activityService.Registrar("Operaciones", "Reporte generado");
                    Operacion.ReportePdfPath = filePath;
                    await _viewModel.UpdateCheckAsync(Operacion, "reporte_generado");
                    var visor = new CotizacionVisorDialog(filePath, contactoReporte, contactosReporte, Operacion.RazonSocial ?? string.Empty, _xamlRoot!, tipo: "Reporte");
                    visor.NotificarResultado(await visor.ShowAsync());
                    if (visor.Resultado == CotizacionVisorResultado.EnviarCorreo)
                    {
                        var email = new EnviarCotizacionDialog(filePath, contactoReporte, contactosReporte, Operacion.RazonSocial ?? string.Empty, _xamlRoot!, tipo: "Reporte", idOperacion: Operacion.IdOperacion, tFinalizado: Operacion.IsTrabajoFinalizado);
                        if (await email.ShowAsync() == ContentDialogResult.Primary)
                        {
                            await _viewModel.UpdateCheckAsync(Operacion, "reporte_enviado");
                            await _notificacionService.MostrarAsync("Correo enviado", "El reporte fue enviado correctamente.");
                        }
                    }
                    else if (visor.Resultado == CotizacionVisorResultado.AbrirExterno)
                    {
                        var file = await WinStorage.StorageFile.GetFileFromPathAsync(filePath);
                        await WinSystem.Launcher.LaunchFileAsync(file);
                    }
                }
                else
                {
                    var msg = !string.IsNullOrWhiteSpace(_viewModel.ErrorMessage) ? _viewModel.ErrorMessage : "No se pudo generar el reporte.";
                    await MostrarErrorAsync("Error al generar reporte", msg);
                }
            }
            catch (Exception ex) { LogDebugError(nameof(GenerarReporteButton_Click), ex); await MostrarErrorAsync("Error", "Ocurrió un error al generar el reporte."); }
        }

        private async void AbrirCotizacionPdfButton_Click(object sender, RoutedEventArgs e)
        {
            var path = Operacion.CotizacionPdfPath;
            if (string.IsNullOrEmpty(path)) return;
            try { var file = await WinStorage.StorageFile.GetFileFromPathAsync(path); await WinSystem.Launcher.LaunchFileAsync(file); }
            catch (Exception ex) { LogDebugError(nameof(AbrirCotizacionPdfButton_Click), ex); Operacion.CotizacionPdfPath = null; }
        }

        private async void AbrirReportePdfButton_Click(object sender, RoutedEventArgs e)
        {
            var path = Operacion.ReportePdfPath;
            if (string.IsNullOrEmpty(path)) return;
            try { var file = await WinStorage.StorageFile.GetFileFromPathAsync(path); await WinSystem.Launcher.LaunchFileAsync(file); }
            catch (Exception ex) { LogDebugError(nameof(AbrirReportePdfButton_Click), ex); Operacion.ReportePdfPath = null; }
        }

        private async void GenerarNotaButton_Click(object sender, RoutedEventArgs e)
        {
            if (!Operacion.IdOperacion.HasValue) return;
            if (!Operacion.IsTrabajoFinalizado) { await MostrarErrorAsync("Trabajo no finalizado", "El trabajo debe estar finalizado para generar una nota."); return; }
            if (Operacion.Cargos == null || Operacion.Cargos.Count == 0) { await MostrarErrorAsync("Sin cargos", "No hay cargos para generar la nota."); return; }
            try
            {
                if (!string.IsNullOrEmpty(_viewModel.FindExistingPdf(Operacion.IdOperacion.Value, "Nota")))
                    _viewModel.DeleteOperacionPdfs(Operacion.IdOperacion.Value, "Nota");

                string? dirigidoA = null;
                ContactoDto? contactoNota = null;
                List<ContactoDto> contactosNota = [];
                if (Operacion.IdCliente.HasValue && Operacion.IdCliente.Value > 0)
                {
                    try
                    {
                        var contactos = await _contactoService.GetContactosAsync(new ContactoQueryDto { IdCliente = Operacion.IdCliente.Value });
                        if (contactos?.Count > 0)
                        {
                            contactosNota = contactos;
                            var lv = new ListView { ItemsSource = contactos, DisplayMemberPath = "NombreCompleto", SelectionMode = ListViewSelectionMode.Single, MaxHeight = 300 };
                            var sel = new ContentDialog { Title = "¿A quién va dirigida la nota?", Content = new ScrollViewer { Content = lv, MaxHeight = 320 }, PrimaryButtonText = "Seleccionar", SecondaryButtonText = "Omitir", DefaultButton = ContentDialogButton.Primary, XamlRoot = _xamlRoot };
                            if (await sel.ShowAsync() == ContentDialogResult.Primary && lv.SelectedItem is ContactoDto c)
                            {
                                contactoNota = c;
                                dirigidoA = string.Join(" ", new[] { c.Tratamiento, c.Nombre, c.Apellido }.Where(s => !string.IsNullOrWhiteSpace(s)));
                            }
                        }
                    }
                    catch (Exception ex) { LogDebugError("ContactosNota", ex); }
                }

                if (!await VerificarFirmasAntesDePdfAsync()) return;

                var filePath = await _viewModel.GenerateNotaAsync(Operacion, dirigidoA);
                if (!string.IsNullOrEmpty(filePath))
                {
                    _activityService.Registrar("Operaciones", "Nota generada");
                    Operacion.NotaPdfPath = filePath;
                    var visor = new CotizacionVisorDialog(filePath, contactoNota, contactosNota, Operacion.RazonSocial ?? string.Empty, _xamlRoot!, tipo: "Nota");
                    visor.NotificarResultado(await visor.ShowAsync());
                    if (visor.Resultado == CotizacionVisorResultado.EnviarCorreo)
                    {
                        var email = new EnviarCotizacionDialog(filePath, contactoNota, contactosNota, Operacion.RazonSocial ?? string.Empty, _xamlRoot!, tipo: "Nota", idOperacion: Operacion.IdOperacion);
                        if (await email.ShowAsync() == ContentDialogResult.Primary)
                        {
                            await _notificacionService.MostrarAsync("Correo enviado", "La nota fue enviada correctamente.");
                        }
                    }
                    else if (visor.Resultado == CotizacionVisorResultado.AbrirExterno)
                    {
                        var file = await WinStorage.StorageFile.GetFileFromPathAsync(filePath);
                        await WinSystem.Launcher.LaunchFileAsync(file);
                    }
                }
                else
                {
                    var msg = !string.IsNullOrWhiteSpace(_viewModel.ErrorMessage) ? _viewModel.ErrorMessage : "No se pudo generar la nota.";
                    await MostrarErrorAsync("Error al generar nota", msg);
                }
            }
            catch (Exception ex) { LogDebugError(nameof(GenerarNotaButton_Click), ex); await MostrarErrorAsync("Error", "Ocurrió un error al generar la nota."); }
        }

        private async void AbrirNotaPdfButton_Click(object sender, RoutedEventArgs e)
        {
            var path = Operacion.NotaPdfPath;
            if (string.IsNullOrEmpty(path)) return;
            try { var file = await WinStorage.StorageFile.GetFileFromPathAsync(path); await WinSystem.Launcher.LaunchFileAsync(file); }
            catch (Exception ex) { LogDebugError(nameof(AbrirNotaPdfButton_Click), ex); Operacion.NotaPdfPath = null; }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Imágenes de operación (Prefactura, Hoja Servicio, Orden Compra, Factura)
        // ─────────────────────────────────────────────────────────────────────

        private async Task UploadOperacionImageAsync(string imageType)
        {
            if (!Operacion.IdOperacion.HasValue) return;
            if (!await EnsureCanMutateAsync($"cargar {imageType.ToLowerInvariant()}")) return;
            try
            {
                // Preguntar al usuario el tipo de archivo
                var tipoDialog = new ContentDialog
                {
                    Title = "Tipo de archivo",
                    Content = "¿Qué tipo de archivo desea cargar?",
                    PrimaryButtonText = "Imagen",
                    SecondaryButtonText = "PDF",
                    CloseButtonText = "Cancelar",
                    DefaultButton = ContentDialogButton.Primary,
                    XamlRoot = _xamlRoot
                };
                var tipoResult = await tipoDialog.ShowAsync();
                if (tipoResult == ContentDialogResult.None) return;

                bool esPdf = tipoResult == ContentDialogResult.Secondary;

                var picker = new FileOpenPicker();
                WinRT.Interop.InitializeWithWindow.Initialize(picker, _hwnd);
                picker.ViewMode = PickerViewMode.Thumbnail;
                if (esPdf)
                {
                    picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                    picker.FileTypeFilter.Add(".pdf");
                }
                else
                {
                    picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
                    picker.FileTypeFilter.Add(".jpg");
                    picker.FileTypeFilter.Add(".jpeg");
                    picker.FileTypeFilter.Add(".png");
                    picker.FileTypeFilter.Add(".bmp");
                    picker.FileTypeFilter.Add(".webp");
                }

                var file = await picker.PickSingleFileAsync();
                if (file == null) return;

                using var stream = await file.OpenStreamForReadAsync();
                var contentType = esPdf
                    ? "application/pdf"
                    : ImageContentTypeHelper.GetContentTypeFromExtension(file.FileType);

                OperacionImageDto? result = imageType switch
                {
                    "Prefactura"    => await _operacionImageService.UploadPrefacturaAsync(Operacion.IdOperacion.Value, stream, contentType),
                    "HojaServicio"  => await _operacionImageService.UploadHojaServicioAsync(Operacion.IdOperacion.Value, stream, contentType),
                    "OrdenCompra"   => await _operacionImageService.UploadOrdenCompraAsync(Operacion.IdOperacion.Value, stream, contentType),
                    "Levantamiento" => await _operacionImageService.UploadLevantamientoAsync(Operacion.IdOperacion.Value, stream, contentType),
                    _               => null
                };
                if (result != null)
                {
                    _activityService.Registrar("Operaciones", $"{imageType} cargada");
                    var campo = imageType switch { "Prefactura" => "prefactura_cargada", "HojaServicio" => "hoja_servicio_cargada", "OrdenCompra" => "orden_compra_cargada", _ => null };
                    if (campo != null) await _viewModel.UpdateCheckAsync(Operacion, campo);

                    switch (imageType)
                    {
                        case "Prefactura":
                            Operacion.ImagenesPrefactura.Add(result);
                            Operacion.HasPrefactura = true;
                            ForceRefreshRepeater(PrefacturaRepeater, Operacion.ImagenesPrefactura);
                            break;
                        case "HojaServicio":
                            Operacion.ImagenesHojaServicio.Add(result);
                            Operacion.HasHojaServicio = true;
                            ForceRefreshRepeater(HojaServicioRepeater, Operacion.ImagenesHojaServicio);
                            break;
                        case "OrdenCompra":
                            Operacion.ImagenesOrdenCompra.Add(result);
                            Operacion.HasOrdenCompra = true;
                            ForceRefreshRepeater(OrdenCompraRepeater, Operacion.ImagenesOrdenCompra);
                            break;
                        case "Levantamiento":
                            Operacion.ImagenesLevantamiento.Add(result);
                            Operacion.HasLevantamiento = true;
                            ForceRefreshRepeater(LevantamientoRepeater, Operacion.ImagenesLevantamiento);
                            break;
                    }
                    Operacion.NotifyDocumentsChanged();

                    await _notificacionService.MostrarAsync($"{imageType} cargada", $"{result.FileName} guardada correctamente.");
                }
                else await MostrarErrorAsync("Error", $"No se pudo guardar la {imageType.ToLower()}.");
            }
            catch (Exception ex) { LogDebugError($"{nameof(UploadOperacionImageAsync)}[{imageType}]", ex); await MostrarErrorAsync("Error", $"Ocurrió un error al cargar la {imageType.ToLower()}."); }
        }

        private async void UploadPrefacturaButton_Click(object sender, RoutedEventArgs e)   => await UploadOperacionImageAsync("Prefactura");
        private async void UploadHojaServicioButton_Click(object sender, RoutedEventArgs e) => await UploadOperacionImageAsync("HojaServicio");
        private async void UploadOrdenCompraButton_Click(object sender, RoutedEventArgs e)  => await UploadOperacionImageAsync("OrdenCompra");
        private async void UploadLevantamientoButton_Click(object sender, RoutedEventArgs e) => await UploadOperacionImageAsync("Levantamiento");

        private async void ViewOperacionImageButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement el || el.Tag is not OperacionImageDto img || string.IsNullOrEmpty(img.Url))
                return;
            try
            {
                if (img.IsPdf)
                {
                    // Cargar contactos del cliente para el visor con opción de correo
                    List<ContactoDto> contactos = [];
                    ContactoDto? contactoPrincipal = null;
                    if (Operacion.IdCliente.HasValue && Operacion.IdCliente.Value > 0)
                    {
                        try
                        {
                            contactos = await _contactoService.GetContactosAsync(new ContactoQueryDto { IdCliente = Operacion.IdCliente.Value });
                            contactoPrincipal = contactos.FirstOrDefault();
                        }
                        catch (Exception ex) { LogDebugError("ContactosDocumento", ex); }
                    }
                    var tipoDisplay = img.Tipo switch
                    {
                        "HojaServicio" => "Hoja de Servicio",
                        "OrdenCompra"  => "Orden de Compra",
                        _              => img.Tipo ?? "Documento"
                    };
                    var visor = new CotizacionVisorDialog(img.Url, contactoPrincipal, contactos, Operacion.RazonSocial ?? string.Empty, _xamlRoot!, tipo: tipoDisplay);
                    visor.NotificarResultado(await visor.ShowAsync());
                    if (visor.Resultado == CotizacionVisorResultado.EnviarCorreo)
                    {
                        var email = new EnviarCotizacionDialog(img.Url, contactoPrincipal, contactos, Operacion.RazonSocial ?? string.Empty, _xamlRoot!, tipo: tipoDisplay, idOperacion: Operacion.IdOperacion);
                        await email.ShowAsync();
                    }
                    else if (visor.Resultado == CotizacionVisorResultado.AbrirExterno)
                    {
                        var file = await WinStorage.StorageFile.GetFileFromPathAsync(img.Url);
                        await WinSystem.Launcher.LaunchFileAsync(file);
                    }
                }
                else
                {
                    await _imageViewerService.ShowImageAsync(img.Url, img.Tipo);
                }
            }
            catch (Exception ex) { LogDebugError(nameof(ViewOperacionImageButton_Click), ex); await MostrarErrorAsync("Error", "No se pudo abrir el archivo."); }
        }

        private async void ViewCargoImageButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement el && el.Tag is CargoImageDto img && !string.IsNullOrEmpty(img.Url))
                await _imageViewerService.ShowImageAsync(img.Url, "Cargo");
        }

        private async void DeleteOperacionImageButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement el || el.Tag is not OperacionImageDto image || string.IsNullOrEmpty(image.FileName)) return;
            if (!await EnsureCanMutateAsync("eliminar archivos de la operación")) return;
            var dialog = new ContentDialog { Title = "Confirmar eliminación", Content = $"¿Eliminar la imagen {image.FileName}?", PrimaryButtonText = "Eliminar", CloseButtonText = "Cancelar", DefaultButton = ContentDialogButton.Close, XamlRoot = _xamlRoot };
            if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;
            try
            {
                if (await _operacionImageService.DeleteImageAsync(image.IdOperacion, image.FileName))
                {
                    _activityService.Registrar("Operaciones", "Doc. op. eliminado");
                    Operacion.ImagesLoaded = false;
                    await RefreshImageIndicatorsAsync();
                    await _notificacionService.MostrarAsync("Imagen eliminada", "La imagen se eliminó correctamente.");
                }
                else await MostrarErrorAsync("Error", "No se pudo eliminar la imagen.");
            }
            catch (Exception ex) { LogDebugError(nameof(DeleteOperacionImageButton_Click), ex); await MostrarErrorAsync("Error", "Ocurrió un error al eliminar la imagen."); }
        }

        private  void CargoCheckBox_UnChecked(object sender, RoutedEventArgs e)
        {
            if (sender is not CheckBox cb || cb.Tag is not CargoDto selectedCargo) return;
            selectedCargo.IsGalleryExpanded = false;
        }
    }

}
