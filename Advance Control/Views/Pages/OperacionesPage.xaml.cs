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
            
            await ViewModel.InitializeAsync();
            await ViewModel.LoadOperacionesAsync(CargosPreloadCallback());
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.LoadOperacionesAsync(CargosPreloadCallback());
        }

        private async void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            ClienteASB.Text = string.Empty;
            EquipoASB.Text = string.Empty;
            AreaASB.Text = string.Empty;
            await ViewModel.ClearFiltersAsync(CargosPreloadCallback());
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
            await ViewModel.LoadOperacionesAsync(CargosPreloadCallback());
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
            await ViewModel.LoadOperacionesAsync(CargosPreloadCallback());
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
            await ViewModel.LoadOperacionesAsync(CargosPreloadCallback());
        }

        // --- Handlers filtros que disparan carga inmediata ---
        private async void IdTipoComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ViewModel == null) return;
            await ViewModel.LoadOperacionesAsync(CargosPreloadCallback());
        }

        private async void FechaInicialPicker_DateChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
        {
            if (ViewModel == null) return;
            await ViewModel.LoadOperacionesAsync(CargosPreloadCallback());
        }

        private async void FechaFinalPicker_DateChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
        {
            if (ViewModel == null) return;
            await ViewModel.LoadOperacionesAsync(CargosPreloadCallback());
        }

        private async void NotaTextBox_EnterInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            args.Handled = true;
            await ViewModel.LoadOperacionesAsync(CargosPreloadCallback());
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
        /// Devuelve un callback que precarga los cargos de una lista de operaciones antes
        /// de que sean visibles en la UI, evitando el parpadeo de TotalMonto = 0.
        /// </summary>
        private Func<List<Models.OperacionDto>, Task> CargosPreloadCallback() =>
            async staged =>
            {
                var tasks = staged
                    .Where(op => op.IdOperacion.HasValue && !op.CargosLoaded)
                    .Select(op => LoadCargosForOperacionAsync(op));
                await Task.WhenAll(tasks);
            };

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
                    
                    operacion.Cargos.Clear();
                    foreach (var cargo in cargos)
                    {
                        operacion.Cargos.Add(cargo);
                        // Subscribe to PropertyChanged to update total when Monto changes
                        cargo.PropertyChanged += (s, e) =>
                        {
                            if (e.PropertyName == nameof(CargoDto.Monto))
                                operacion.OnPropertyChanged(nameof(operacion.TotalMonto));
                        };

                        // Load images for this cargo asynchronously with error handling
                        _ = LoadImagesForCargoSafeAsync(cargo);
                    }
                    
                    // Notify that TotalMonto should be recalculated after loading cargos
                    operacion.OnPropertyChanged(nameof(operacion.TotalMonto));
                    
                    operacion.CargosLoaded = true;
                }

                // Suscribir CollectionChanged solo una vez por operación para evitar acumulación de handlers
                if (!operacion.CollectionChangedSubscribed)
                {
                    operacion.Cargos.CollectionChanged += (s, e) =>
                    {
                        if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
                        {
                            foreach (CargoDto cargo in e.NewItems)
                            {
                                cargo.PropertyChanged += (sender, args) =>
                                {
                                    if (args.PropertyName == nameof(CargoDto.Monto))
                                        operacion.OnPropertyChanged(nameof(operacion.TotalMonto));
                                };
                            }
                        }
                        operacion.OnPropertyChanged(nameof(operacion.TotalMonto));
                    };
                    operacion.CollectionChangedSubscribed = true;
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
                var prefacturas = await _operacionImageService.GetPrefacturasAsync(operacion.IdOperacion.Value);
                var hojasServicio = await _operacionImageService.GetHojasServicioAsync(operacion.IdOperacion.Value);
                var ordenesCompra = await _operacionImageService.GetOrdenComprasAsync(operacion.IdOperacion.Value);
                var hasFactura = await _operacionImageService.HasFacturaAsync(operacion.IdOperacion.Value);

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
            var visor = new Views.Windows.OperacionVisorWindow(operacion);
            visor.Activate();
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
