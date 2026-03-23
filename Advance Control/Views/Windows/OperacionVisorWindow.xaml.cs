using Advance_Control.Models;
using Advance_Control.Services.Activity;
using Advance_Control.Services.Cargos;
using Advance_Control.Services.Contactos;
using Advance_Control.Services.ImageViewer;
using Advance_Control.Services.LocalStorage;
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
using System.Threading.Tasks;
using global::Windows.Globalization.NumberFormatting;
using global::Windows.Storage.Pickers;
// Alias para evitar colisión con el namespace Advance_Control.Views.Windows
using WinStorage = global::Windows.Storage;
using WinSystem = global::Windows.System;

namespace Advance_Control.Views.Windows
{
    /// <summary>
    /// Ventana visor dedicado para gestionar una operación con tres paneles:
    /// información + acciones/documentos (izquierdo), cargos (central) y tareas (derecho).
    /// </summary>
    public sealed partial class OperacionVisorWindow : Microsoft.UI.Xaml.Window
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

        private XamlRoot? _xamlRoot;
        private IntPtr _hwnd;

        /// <summary>La operación que se visualiza en esta ventana.</summary>
        public OperacionDto Operacion { get; }

        /// <summary>Formateador de moneda MXN para el NumberBox de total.</summary>
        public INumberFormatter2 CurrencyFormatter { get; }

        public OperacionVisorWindow(OperacionDto operacion)
        {
            Operacion = operacion;

            _viewModel            = AppServices.Get<OperacionesViewModel>();
            _notificacionService  = AppServices.Get<INotificacionService>();
            _cargoService         = AppServices.Get<ICargoService>();
            _userSessionService   = AppServices.Get<IUserSessionService>();
            _cargoImageService    = AppServices.Get<ICargoImageService>();
            _operacionImageService = AppServices.Get<IOperacionImageService>();
            _imageViewerService   = AppServices.Get<IImageViewerService>();
            _activityService      = AppServices.Get<IActivityService>();
            _contactoService      = AppServices.Get<IContactoService>();

            var fmt = new CurrencyFormatter("MXN");
            fmt.FractionDigits = 2;
            CurrencyFormatter = fmt;

            this.InitializeComponent();

            _hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            Title = $"Visor – {operacion.Identificador}";

            RootGrid.Loaded += async (_, _) =>
            {
                _xamlRoot = RootGrid.XamlRoot;
                try
                {
                    await CargarDatosInicialesAsync();
                }
                catch (Exception ex)
                {
                    LogDebugError(nameof(CargarDatosInicialesAsync), ex);
                }
            };
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Carga inicial
        // ─────────────────────────────────────────────────────────────────────

        private async Task CargarDatosInicialesAsync()
        {
            await LoadCargosAsync();

            if (!Operacion.ImagesLoaded)
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
                    var cargos = await _cargoService.GetCargosAsync(new CargoEditDto { IdOperacion = Operacion.IdOperacion.Value });
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
                await MostrarErrorAsync("Error al cargar cargos", "No se pudieron cargar los cargos. Intente nuevamente.");
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
                var prefacturas   = await _operacionImageService.GetPrefacturasAsync(Operacion.IdOperacion.Value);
                var hojasServicio = await _operacionImageService.GetHojasServicioAsync(Operacion.IdOperacion.Value);
                var ordenesCompra = await _operacionImageService.GetOrdenComprasAsync(Operacion.IdOperacion.Value);
                var hasFactura    = await _operacionImageService.HasFacturaAsync(Operacion.IdOperacion.Value);

                Operacion.HasPrefactura   = prefacturas.Count > 0;
                Operacion.HasHojaServicio = hojasServicio.Count > 0;
                Operacion.HasOrdenCompra  = ordenesCompra.Count > 0;
                Operacion.HasFactura      = hasFactura;

                Operacion.ImagenesPrefactura   = new System.Collections.ObjectModel.ObservableCollection<OperacionImageDto>(prefacturas);
                Operacion.ImagenesHojaServicio = new System.Collections.ObjectModel.ObservableCollection<OperacionImageDto>(hojasServicio);
                Operacion.ImagenesOrdenCompra  = new System.Collections.ObjectModel.ObservableCollection<OperacionImageDto>(ordenesCompra);

                Operacion.ImagesLoaded = true;
            }
            catch (Exception ex)
            {
                LogDebugError(nameof(RefreshImageIndicatorsAsync), ex);
            }
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
            System.Diagnostics.Debug.WriteLine($"OperacionVisorWindow::{contexto}: {ex.GetType().Name} - {ex.Message}");
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

        // ─────────────────────────────────────────────────────────────────────
        //  Operación
        // ─────────────────────────────────────────────────────────────────────

        private async void ReabrirOperacionButton_Click(object sender, RoutedEventArgs e)
        {
            if (!Operacion.IdOperacion.HasValue) return;
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

        // ─────────────────────────────────────────────────────────────────────
        //  Cargos
        // ─────────────────────────────────────────────────────────────────────

        private async void AddCargoButton_Click(object sender, RoutedEventArgs e)
        {
            if (!Operacion.IdOperacion.HasValue) return;
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
            try
            {
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

        private void CargoCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is not CheckBox cb || cb.Tag is not CargoDto selectedCargo) return;
            foreach (var c in Operacion.Cargos)
                if (c != selectedCargo) { c.IsSelected = false; c.IsGalleryExpanded = false; }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Cargos seleccionados
        // ─────────────────────────────────────────────────────────────────────

        private List<CargoDto> GetSelectedCargos() => Operacion.Cargos.Where(c => c.IsSelected).ToList();

        private async void UploadSelectedCargoImageButton_Click(object sender, RoutedEventArgs e)
        {
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
            var sel = GetSelectedCargos();
            if (sel.Count == 0) { await MostrarErrorAsync("Sin selección", "Seleccione un cargo para editar."); return; }
            if (sel.Count > 1) await _notificacionService.MostrarAsync("Múltiples selecciones", "Se usará solo el primer cargo seleccionado.");
            sel[0].IsEditing = !sel[0].IsEditing;
        }

        private async void DeleteSelectedCargosButton_Click(object sender, RoutedEventArgs e)
        {
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

        private async void ExpandSelectedGalleryButton_Click(object sender, RoutedEventArgs e)
        {
            var sel = GetSelectedCargos();
            if (sel.Count == 0) { await MostrarErrorAsync("Sin selección", "Seleccione un cargo para ver imágenes."); return; }
            var cargo = sel[0];
            if (!cargo.HasImages) { await MostrarErrorAsync("Sin imágenes", "El cargo seleccionado no tiene imágenes."); return; }
            cargo.IsGalleryExpanded = !cargo.IsGalleryExpanded;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Imágenes de cargo
        // ─────────────────────────────────────────────────────────────────────

        private async Task LoadImagesForCargoSafeAsync(CargoDto cargo)
        {
            try { await LoadImagesForCargoAsync(cargo); }
            catch (Exception ex) { LogDebugError($"{nameof(LoadImagesForCargoSafeAsync)}[{cargo.IdCargo}]", ex); }
        }

        private async Task LoadImagesForCargoAsync(CargoDto cargo)
        {
            if (cargo.IdCargo <= 0 || !cargo.IdOperacion.HasValue || cargo.ImagesLoaded || cargo.IsLoadingImages) return;
            try
            {
                cargo.IsLoadingImages = true;
                var imgs = await _cargoImageService.GetImagesAsync(cargo.IdOperacion.Value, cargo.IdCargo);
                cargo.Images.Clear();
                foreach (var img in imgs) cargo.Images.Add(img);
                cargo.NotifyImagesChanged();
                cargo.ImagesLoaded = true;
            }
            catch (Exception ex) { LogDebugError($"{nameof(LoadImagesForCargoAsync)}[{cargo.IdCargo}]", ex); }
            finally { cargo.IsLoadingImages = false; }
        }

        private async void DeleteCargoImageButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement el || el.Tag is not CargoImageDto image || string.IsNullOrEmpty(image.FileName)) return;
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

                var filePath = await _viewModel.GenerateQuoteAsync(Operacion, dirigidoA);
                if (!string.IsNullOrEmpty(filePath))
                {
                    _activityService.Registrar("Operaciones", "Cotización generada");
                    Operacion.CotizacionPdfPath = filePath;
                    await _viewModel.UpdateCheckAsync(Operacion, "cotizacionGenerada");
                    var visor = new CotizacionVisorDialog(filePath, contactoParaCorreo, contactosCliente, Operacion.RazonSocial ?? string.Empty, _xamlRoot!);
                    visor.NotificarResultado(await visor.ShowAsync());
                    if (visor.Resultado == CotizacionVisorResultado.EnviarCorreo)
                    {
                        var email = new EnviarCotizacionDialog(filePath, contactoParaCorreo, contactosCliente, Operacion.RazonSocial ?? string.Empty, _xamlRoot!, idOperacion: Operacion.IdOperacion);
                        if (await email.ShowAsync() == ContentDialogResult.Primary)
                        {
                            await _viewModel.UpdateCheckAsync(Operacion, "cotizacionEnviada");
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

                var filePath = await _viewModel.GenerateReporteAsync(Operacion, dirigidoA);
                if (!string.IsNullOrEmpty(filePath))
                {
                    _activityService.Registrar("Operaciones", "Reporte generado");
                    Operacion.ReportePdfPath = filePath;
                    await _viewModel.UpdateCheckAsync(Operacion, "reporteGenerado");
                    var visor = new CotizacionVisorDialog(filePath, contactoReporte, contactosReporte, Operacion.RazonSocial ?? string.Empty, _xamlRoot!, tipo: "Reporte");
                    visor.NotificarResultado(await visor.ShowAsync());
                    if (visor.Resultado == CotizacionVisorResultado.EnviarCorreo)
                    {
                        var email = new EnviarCotizacionDialog(filePath, contactoReporte, contactosReporte, Operacion.RazonSocial ?? string.Empty, _xamlRoot!, tipo: "Reporte", idOperacion: Operacion.IdOperacion);
                        if (await email.ShowAsync() == ContentDialogResult.Primary)
                        {
                            await _viewModel.UpdateCheckAsync(Operacion, "reporteEnviado");
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

        // ─────────────────────────────────────────────────────────────────────
        //  Imágenes de operación (Prefactura, Hoja Servicio, Orden Compra, Factura)
        // ─────────────────────────────────────────────────────────────────────

        private async Task UploadOperacionImageAsync(string imageType)
        {
            if (!Operacion.IdOperacion.HasValue) return;
            try
            {
                var picker = new FileOpenPicker();
                WinRT.Interop.InitializeWithWindow.Initialize(picker, _hwnd);
                picker.ViewMode = PickerViewMode.Thumbnail;
                picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
                picker.FileTypeFilter.Add(".jpg"); picker.FileTypeFilter.Add(".jpeg"); picker.FileTypeFilter.Add(".png"); picker.FileTypeFilter.Add(".bmp");
                var file = await picker.PickSingleFileAsync();
                if (file == null) return;
                using var stream = await file.OpenStreamForReadAsync();
                var contentType = ImageContentTypeHelper.GetContentTypeFromExtension(file.FileType);
                OperacionImageDto? result = imageType switch
                {
                    "Prefactura"   => await _operacionImageService.UploadPrefacturaAsync(Operacion.IdOperacion.Value, stream, contentType),
                    "HojaServicio" => await _operacionImageService.UploadHojaServicioAsync(Operacion.IdOperacion.Value, stream, contentType),
                    "OrdenCompra"  => await _operacionImageService.UploadOrdenCompraAsync(Operacion.IdOperacion.Value, stream, contentType),
                    _              => null
                };
                if (result != null)
                {
                    Operacion.ImagesLoaded = false;
                    await RefreshImageIndicatorsAsync();
                    _activityService.Registrar("Operaciones", $"{imageType} cargada");
                    var campo = imageType switch { "Prefactura" => "prefacturaCargada", "HojaServicio" => "hojaServicioCargada", "OrdenCompra" => "ordenCompraCargada", _ => null };
                    if (campo != null) await _viewModel.UpdateCheckAsync(Operacion, campo);
                    await _notificacionService.MostrarAsync($"{imageType} cargada", $"{result.FileName} guardada correctamente.");
                }
                else await MostrarErrorAsync("Error", $"No se pudo guardar la {imageType.ToLower()}.");
            }
            catch (Exception ex) { LogDebugError($"{nameof(UploadOperacionImageAsync)}[{imageType}]", ex); await MostrarErrorAsync("Error", $"Ocurrió un error al cargar la {imageType.ToLower()}."); }
        }

        private async void UploadPrefacturaButton_Click(object sender, RoutedEventArgs e)   => await UploadOperacionImageAsync("Prefactura");
        private async void UploadHojaServicioButton_Click(object sender, RoutedEventArgs e) => await UploadOperacionImageAsync("HojaServicio");
        private async void UploadOrdenCompraButton_Click(object sender, RoutedEventArgs e)  => await UploadOperacionImageAsync("OrdenCompra");

        private async void UploadFacturaButton_Click(object sender, RoutedEventArgs e)
        {
            if (!Operacion.IdOperacion.HasValue) return;
            try
            {
                var picker = new FileOpenPicker();
                WinRT.Interop.InitializeWithWindow.Initialize(picker, _hwnd);
                picker.ViewMode = PickerViewMode.List;
                picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                picker.FileTypeFilter.Add(".pdf");
                var file = await picker.PickSingleFileAsync();
                if (file == null) return;
                using var stream = await file.OpenStreamForReadAsync();
                var result = await _operacionImageService.UploadFacturaAsync(Operacion.IdOperacion.Value, stream);
                if (result != null)
                {
                    _activityService.Registrar("Operaciones", "Factura cargada");
                    await _viewModel.UpdateCheckAsync(Operacion, "facturaCargada");
                    var fechaFinal = DateTime.Today;
                    if (await _viewModel.UpdateOperacionAsync(Operacion.IdOperacion.Value, fechaFinal: fechaFinal))
                        Operacion.FechaFinal = fechaFinal;
                    Operacion.HasFactura = true;
                    await _notificacionService.MostrarAsync("Factura cargada", "La factura fue guardada y la operación finalizada.");
                }
                else await MostrarErrorAsync("Error", "No se pudo guardar la factura.");
            }
            catch (Exception ex) { LogDebugError(nameof(UploadFacturaButton_Click), ex); await MostrarErrorAsync("Error", "Ocurrió un error al cargar la factura."); }
        }

        private async void VerFacturaButton_Click(object sender, RoutedEventArgs e)
        {
            if (!Operacion.IdOperacion.HasValue) return;
            try
            {
                var factura = await _operacionImageService.GetFacturaAsync(Operacion.IdOperacion.Value);
                if (factura == null || string.IsNullOrEmpty(factura.Url)) { await MostrarErrorAsync("Error", "No se encontró el archivo de factura."); return; }
                var file = await WinStorage.StorageFile.GetFileFromPathAsync(factura.Url);
                await WinSystem.Launcher.LaunchFileAsync(file);
            }
            catch (Exception ex) { LogDebugError(nameof(VerFacturaButton_Click), ex); await MostrarErrorAsync("Error", "No se pudo abrir la factura."); }
        }

        private async void ViewOperacionImageButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement el && el.Tag is OperacionImageDto img && !string.IsNullOrEmpty(img.Url))
                await _imageViewerService.ShowImageAsync(img.Url, img.Tipo);
        }

        private async void ViewCargoImageButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement el && el.Tag is CargoImageDto img && !string.IsNullOrEmpty(img.Url))
                await _imageViewerService.ShowImageAsync(img.Url, "Cargo");
        }

        private async void DeleteOperacionImageButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement el || el.Tag is not OperacionImageDto image || string.IsNullOrEmpty(image.FileName)) return;
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
    }

}
