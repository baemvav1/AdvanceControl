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
using Microsoft.UI.Xaml.Navigation;
using System;
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
            
            await ViewModel.LoadOperacionesAsync();
            await PreloadCargosAsync();
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.LoadOperacionesAsync();
            await PreloadCargosAsync();
        }

        private async void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.ClearFiltersAsync();
            await PreloadCargosAsync();
        }

        private void ToggleFiltros_Click(object sender, RoutedEventArgs e)
        {
            Filtros.Visibility = Filtros.Visibility == Visibility.Visible
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        private void ReporteGeneral_Click(object sender, RoutedEventArgs e)
        {
            // Funcionalidad pendiente de implementar
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
                    if (!operacion.TieneCheck)
                        await ViewModel.LoadCheckAsync(operacion);
                    ViewModel.RefreshPdfPaths(operacion);
                }
            }
        }

        /// <summary>
        /// Precarga los cargos de todas las operaciones visibles para mostrar el total correcto.
        /// </summary>
        private async Task PreloadCargosAsync()
        {
            if (ViewModel?.Operaciones == null || ViewModel.Operaciones.Count == 0) return;
            var tasks = ViewModel.Operaciones
                .Where(op => op.IdOperacion.HasValue && !op.CargosLoaded)
                .Select(op => LoadCargosForOperacionAsync(op));
            await Task.WhenAll(tasks);
        }

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

        private async void SelectClienteButton_Click(object sender, RoutedEventArgs e)
        {
            // Crear el UserControl para seleccionar cliente
            var seleccionarClienteControl = new SeleccionarClienteUserControl();

            // Crear el diálogo
            var dialog = new ContentDialog
            {
                Title = "Seleccionar Cliente",
                Content = seleccionarClienteControl,
                PrimaryButtonText = "Aceptar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary && seleccionarClienteControl.HasSelection)
            {
                var selectedCliente = seleccionarClienteControl.SelectedCliente;
                if (selectedCliente != null)
                {
                    ViewModel.IdClienteFilter = selectedCliente.IdCliente;
                    ViewModel.SelectedClienteText = $"{selectedCliente.RazonSocial} (ID: {selectedCliente.IdCliente})";
                }
            }
        }

        private async void SelectEquipoButton_Click(object sender, RoutedEventArgs e)
        {
            // Crear el UserControl para seleccionar equipo
            var seleccionarEquipoControl = new SeleccionarEquipoUserControl();

            // Crear el diálogo
            var dialog = new ContentDialog
            {
                Title = "Seleccionar Equipo",
                Content = seleccionarEquipoControl,
                PrimaryButtonText = "Aceptar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary && seleccionarEquipoControl.HasSelection)
            {
                var selectedEquipo = seleccionarEquipoControl.SelectedEquipo;
                if (selectedEquipo != null)
                {
                    ViewModel.IdEquipoFilter = selectedEquipo.IdEquipo;
                    ViewModel.SelectedEquipoText = $"{selectedEquipo.Marca} - {selectedEquipo.Identificador} (ID: {selectedEquipo.IdEquipo})";
                }
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

                operacion.ImagenesPrefactura = new System.Collections.ObjectModel.ObservableCollection<Models.OperacionImageDto>(prefacturas);
                operacion.ImagenesHojaServicio = new System.Collections.ObjectModel.ObservableCollection<Models.OperacionImageDto>(hojasServicio);
                operacion.ImagenesOrdenCompra = new System.Collections.ObjectModel.ObservableCollection<Models.OperacionImageDto>(ordenesCompra);

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
    }
}
