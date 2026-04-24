using Advance_Control.Models;
using Advance_Control.Services.Cargos;
using Advance_Control.Services.LocalStorage;
using Advance_Control.Services.Notificacion;
using Advance_Control.Utilities;
using Advance_Control.Services.Logging;
using Advance_Control.ViewModels;
using Advance_Control.Views.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;

namespace Advance_Control.Views.Pages
{
    /// <summary>
    /// Página para visualizar y gestionar operaciones del sistema
    /// </summary>
    public sealed partial class OperacionesPage : Page
    {
        public OperacionesViewModel ViewModel { get; }
        private readonly INotificacionService _notificacionService;
        private readonly ICargoService _cargoService;
        private readonly ICargoImageService _cargoImageService;
        private readonly IOperacionImageService _operacionImageService;
        // Bloquea handlers de filtros que se disparan durante InitializeComponent()
        private bool _isNavigating = true;

        public OperacionesPage()
        {
            // Resolver el ViewModel desde DI
            ViewModel = AppServices.Get<OperacionesViewModel>();

            // Resolver el servicio de notificaciones desde DI
            _notificacionService = AppServices.Get<INotificacionService>();
            
            // Resolver el servicio de cargos desde DI
            _cargoService = AppServices.Get<ICargoService>();

            // Resolver el servicio de imágenes de cargo desde DI
            _cargoImageService = AppServices.Get<ICargoImageService>();

            // Resolver el servicio de imágenes de operación desde DI
            _operacionImageService = AppServices.Get<IOperacionImageService>();
            
            this.InitializeComponent();
            ButtonClickLogger.Attach(this, AppServices.Get<ILoggingService>(), nameof(OperacionesPage));
            
            // Establecer el DataContext para los bindings
            this.DataContext = ViewModel;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            _isNavigating = true;
            await ViewModel.InitializeAsync();
            await ViewModel.LoadOperacionesAsync(null /* preload eliminado: TotalMonto viene del backend */);
            _isNavigating = false;
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.ApplyFiltersAsync(null /* preload eliminado: TotalMonto viene del backend */);
        }

        private async void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            ClienteASB.Text = string.Empty;
            EquipoASB.Text = string.Empty;
            AreaASB.Text = string.Empty;
            await ViewModel.ClearFiltersAsync(null /* preload eliminado: TotalMonto viene del backend */);
        }

        private async void PaginaAnterior_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.GoToPreviousPageAsync();
        }

        private async void PaginaSiguiente_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.GoToNextPageAsync();
        }

        private void ToggleFiltros_Click(object sender, RoutedEventArgs e)
        {
            Filtros.Visibility = Filtros.Visibility == Visibility.Visible
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        // --- Handlers AutoSuggestBox Cliente ---
        private void ClienteASB_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
                ViewModel.ActualizarSugerenciasCliente(sender.Text);
        }

        private async void ClienteASB_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            var texto = args.ChosenSuggestion as string ?? args.QueryText;
            ViewModel.AplicarFiltroCliente(texto);
            await ViewModel.ApplyFiltersAsync(null /* preload eliminado: TotalMonto viene del backend */);
        }

        // --- Handlers AutoSuggestBox Equipo ---
        private void EquipoASB_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
                ViewModel.ActualizarSugerenciasEquipo(sender.Text);
        }

        private async void EquipoASB_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            var texto = args.ChosenSuggestion as string ?? args.QueryText;
            ViewModel.AplicarFiltroEquipo(texto);
            await ViewModel.ApplyFiltersAsync(null /* preload eliminado: TotalMonto viene del backend */);
        }

        // --- Handlers AutoSuggestBox Área ---
        private void AreaASB_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
                ViewModel.ActualizarSugerenciasArea(sender.Text);
        }

        private async void AreaASB_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            AreaDto? area = null;
            if (args.ChosenSuggestion is AreaDto chosen)
                area = chosen;
            else if (!string.IsNullOrWhiteSpace(args.QueryText))
                area = ViewModel.Areas.FirstOrDefault(a => a.Nombre.Equals(args.QueryText, StringComparison.OrdinalIgnoreCase));
            ViewModel.AplicarFiltroArea(area);
            await ViewModel.ApplyFiltersAsync(null /* preload eliminado: TotalMonto viene del backend */);
        }

        // --- Handlers filtros que disparan carga inmediata ---
        private async void IdTipoComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ViewModel == null || _isNavigating) return;
            await ViewModel.ApplyFiltersAsync(null /* preload eliminado: TotalMonto viene del backend */);
        }

        private async void FechaInicialPicker_DateChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
        {
            if (ViewModel == null || _isNavigating) return;
            await ViewModel.ApplyFiltersAsync(null /* preload eliminado: TotalMonto viene del backend */);
        }

        private async void FechaFinalPicker_DateChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
        {
            if (ViewModel == null || _isNavigating) return;
            await ViewModel.ApplyFiltersAsync(null /* preload eliminado: TotalMonto viene del backend */);
        }

        private async void NotaTextBox_EnterInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            args.Handled = true;
            await ViewModel.ApplyFiltersAsync(null /* preload eliminado: TotalMonto viene del backend */);
        }

        private async void ToggleExpandButton_Click(object sender, RoutedEventArgs e)
        {
            // Get the OperacionDto from the sender's Tag property
            if (sender is FrameworkElement element && element.Tag is Models.OperacionDto operacion)
            {
                operacion.Expand = !operacion.Expand;
                
                // Load cargos and image indicators when expanding
                if (operacion.Expand && operacion.IdOperacion.HasValue)
                {
                    await LoadCargosForOperacionAsync(operacion);
                    if (!operacion.ImagesLoaded)
                        await RefreshImageIndicatorsAsync(operacion);
                    ViewModel.RefreshPdfPaths(operacion);
                }
            }
        }

        /// <summary>
        /// (Eliminado) El preload masivo de cargos se removió. El TotalMonto
        /// viene pre-calculado por el backend (fn_operaciones_gestionar) y los
        /// cargos se cargan a demanda al abrir el visor de cada operación.
        /// </summary>
        private static Func<List<Models.OperacionDto>, Task>? CargosPreloadCallbackEliminado() => null;

        private async Task LoadCargosForOperacionAsync(Models.OperacionDto operacion)
        {
            if (!operacion.IdOperacion.HasValue)
                return;

            try
            {
                // Only load cargos if they haven't been loaded yet and not currently loading
                if (!operacion.CargosLoaded && !operacion.IsLoadingCargos)
                {
                    operacion.IsLoadingCargos = true;
                    
                    var query = new CargoEditDto
                    {
                        IdOperacion = operacion.IdOperacion.Value
                    };

                    var cargos = await _cargoService.GetCargosAsync(query);

                    // OperacionDto ahora maneja internamente CollectionChanged + PropertyChanged
                    // para mantener TotalMonto cacheado, sin memory leaks. No es necesario
                    // suscribir handlers aquí.
                    operacion.Cargos.Clear();
                    foreach (var cargo in cargos)
                    {
                        operacion.Cargos.Add(cargo);
                        // Carga de imágenes en background con manejo de errores
                        _ = LoadImagesForCargoSafeAsync(cargo);
                    }

                    operacion.CargosLoaded = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cargar cargos: {ex.GetType().Name} - {ex.Message}");
                await _notificacionService.MostrarAsync("Error al cargar cargos", "No se pudieron cargar los cargos de la operación. Por favor, intente nuevamente.");
            }
            finally
            {
                operacion.IsLoadingCargos = false;
            }
        }

        /// <summary>
        /// Safe wrapper for LoadImagesForCargoAsync that catches and logs any unhandled exceptions
        /// </summary>
        private async Task LoadImagesForCargoSafeAsync(Models.CargoDto cargo)
        {
            try
            {
                await LoadImagesForCargoAsync(cargo);
            }
            catch (Exception ex)
            {
                // Log the exception but don't throw - this is a fire-and-forget operation
                System.Diagnostics.Debug.WriteLine($"Error inesperado al cargar imágenes para cargo {cargo?.IdCargo}: {ex.GetType().Name} - {ex.Message}");
            }
        }

        /// <summary>
        /// Carga las imágenes para un cargo específico desde el almacenamiento local
        /// </summary>
        private async Task LoadImagesForCargoAsync(Models.CargoDto cargo)
        {
            if (cargo.IdCargo <= 0 || !cargo.IdOperacion.HasValue || cargo.IdOperacion.Value <= 0 || cargo.ImagesLoaded || cargo.IsLoadingImages)
                return;

            try
            {
                cargo.IsLoadingImages = true;

                var images = await _cargoImageService.GetImagesAsync(cargo.IdOperacion.Value, cargo.IdCargo);

                cargo.Images.Clear();
                foreach (var image in images)
                {
                    cargo.Images.Add(image);
                }

                cargo.NotifyImagesChanged();
                cargo.ImagesLoaded = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cargar imágenes del cargo {cargo.IdCargo}: {ex.GetType().Name} - {ex.Message}");
                // No mostrar error al usuario, simplemente no se cargan las imágenes
            }
            finally
            {
                cargo.IsLoadingImages = false;
            }
        }

        /// <summary>
        /// Actualiza los indicadores de imágenes para una operación
        /// </summary>
        private async Task RefreshImageIndicatorsAsync(Models.OperacionDto operacion)
        {
            if (!operacion.IdOperacion.HasValue)
                return;

            try
            {
                // Paralelizar las 4 llamadas (antes eran secuenciales: latencia ~4x mayor).
                var idOp = operacion.IdOperacion.Value;
                var prefacturasTask   = _operacionImageService.GetPrefacturasAsync(idOp);
                var hojasServicioTask = _operacionImageService.GetHojasServicioAsync(idOp);
                var ordenesCompraTask = _operacionImageService.GetOrdenComprasAsync(idOp);
                var hasFacturaTask    = _operacionImageService.HasFacturaAsync(idOp);
                await Task.WhenAll(prefacturasTask, hojasServicioTask, ordenesCompraTask, hasFacturaTask);

                var prefacturas   = prefacturasTask.Result;
                var hojasServicio = hojasServicioTask.Result;
                var ordenesCompra = ordenesCompraTask.Result;
                var hasFactura    = hasFacturaTask.Result;

                operacion.HasPrefactura = prefacturas.Count > 0;
                operacion.HasHojaServicio = hojasServicio.Count > 0;
                operacion.HasOrdenCompra = ordenesCompra.Count > 0;
                operacion.HasFactura = hasFactura;

                // Limpiar y repoblar las colecciones existentes en vez de reemplazar
                // para que ItemsRepeater detecte los cambios vía CollectionChanged.
                ReplaceItems(operacion.ImagenesPrefactura, prefacturas);
                ReplaceItems(operacion.ImagenesHojaServicio, hojasServicio);
                ReplaceItems(operacion.ImagenesOrdenCompra, ordenesCompra);

                operacion.ImagesLoaded = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al actualizar indicadores de imágenes: {ex.GetType().Name} - {ex.Message}");
            }
        }

        private void AbrirVisorButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement element || element.Tag is not Models.OperacionDto operacion)
                return;
            OperacionVisorNavigator.Navigate(operacion);
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
    }
}
