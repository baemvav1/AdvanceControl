using Advance_Control.Models;
using Advance_Control.Services.Activity;
using Advance_Control.Services.Cargos;
using Advance_Control.Services.Contactos;
using Advance_Control.Services.ImageViewer;
using Advance_Control.Services.LocalStorage;
using Advance_Control.Services.Notificacion;
using Advance_Control.Services.Session;
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
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Globalization.NumberFormatting;
using Windows.Storage.Pickers;

namespace Advance_Control.Views
{
    /// <summary>
    /// Página para visualizar y gestionar operaciones del sistema
    /// </summary>
    public sealed partial class OperacionesView : Page
    {
        public OperacionesViewModel ViewModel { get; }
        private readonly INotificacionService _notificacionService;
        private readonly ICargoService _cargoService;
        private readonly IUserSessionService _userSessionService;
        private readonly ICargoImageService _cargoImageService;
        private readonly IOperacionImageService _operacionImageService;
        private readonly IImageViewerService _imageViewerService;
        private readonly IActivityService _activityService;
        private readonly IContactoService _contactoService;

        /// <summary>
        /// Currency formatter for the NumberBox
        /// </summary>
        public INumberFormatter2 CurrencyFormatter { get; }

        public OperacionesView()
        {
            // Resolver el ViewModel desde DI
            ViewModel = AppServices.Get<OperacionesViewModel>();

            // Resolver el servicio de notificaciones desde DI
            _notificacionService = AppServices.Get<INotificacionService>();
            
            // Resolver el servicio de cargos desde DI
            _cargoService = AppServices.Get<ICargoService>();

            // Resolver el servicio de sesión de usuario desde DI
            _userSessionService = AppServices.Get<IUserSessionService>();

            // Resolver el servicio de imágenes de cargo desde DI
            _cargoImageService = AppServices.Get<ICargoImageService>();

            // Resolver el servicio de imágenes de operación desde DI
            _operacionImageService = AppServices.Get<IOperacionImageService>();

            // Resolver el servicio de visor de imágenes desde DI
            _imageViewerService = AppServices.Get<IImageViewerService>();

            // Resolver el servicio de actividades desde DI
            _activityService = AppServices.Get<IActivityService>();

            // Resolver el servicio de contactos desde DI
            _contactoService = AppServices.Get<IContactoService>();

            // Initialize currency formatter for Mexican Pesos
            var currencyFormatter = new CurrencyFormatter("MXN");
            currencyFormatter.FractionDigits = 2;
            CurrencyFormatter = currencyFormatter;
            
            this.InitializeComponent();
            ButtonClickLogger.Attach(this, AppServices.Get<ILoggingService>(), nameof(OperacionesView));
            
            // Establecer el DataContext para los bindings
            this.DataContext = ViewModel;
        }

        /// <summary>
        /// Calculates the total sum of all Monto values in the Cargos collection
        /// </summary>
        public double CalculateTotalMonto(ObservableCollection<CargoDto> cargos)
        {
            if (cargos == null || cargos.Count == 0)
                return 0.0;

            return cargos.Sum(c => c.Monto ?? 0.0);
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            
            // Cargar las operaciones cuando se navega a esta página
            await ViewModel.LoadOperacionesAsync();
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.LoadOperacionesAsync();
        }

        private async void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.ClearFiltersAsync();
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

        private void EnsureCollectionChangedSubscribed(Models.OperacionDto operacion)
        {
            // Usar un campo de respaldo por operación para evitar suscripciones duplicadas
            operacion.Cargos.CollectionChanged -= OnCargosCollectionChanged;
            operacion.Cargos.CollectionChanged += OnCargosCollectionChanged;

            void OnCargosCollectionChanged(object? s, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
            {
                if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add && e.NewItems != null)
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
            }
        }

        private async void EditOperacionButton_Click(object sender, RoutedEventArgs e)
        {
            // Obtener la operación desde el Tag del botón
            if (sender is not FrameworkElement element || element.Tag is not Models.OperacionDto operacion)
                return;

            if (!operacion.IdOperacion.HasValue)
                return;

            try
            {
                var montoNumberBox = new NumberBox
                {
                    Value = (double)operacion.Monto,
                    PlaceholderText = "Monto de la operación",
                    Minimum = 0,
                    SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline,
                    Margin = new Thickness(0, 0, 0, 8)
                };

                var dialogContent = new StackPanel
                {
                    Spacing = 8,
                    Children =
                    {
                        new TextBlock { Text = "Monto:", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold },
                        montoNumberBox
                    }
                };

                var dialog = new ContentDialog
                {
                    Title = "Editar Operación",
                    Content = dialogContent,
                    PrimaryButtonText = "Guardar",
                    CloseButtonText = "Cancelar",
                    DefaultButton = ContentDialogButton.Primary,
                    XamlRoot = this.XamlRoot
                };

                var result = await dialog.ShowAsync();

                if (result == ContentDialogResult.Primary)
                {
                    if (double.IsNaN(montoNumberBox.Value))
                    {
                        await _notificacionService.MostrarAsync("Validación", "El monto es obligatorio");
                        return;
                    }

                    var monto = Math.Round(Convert.ToDecimal(montoNumberBox.Value), 2);

                    var success = await ViewModel.UpdateOperacionAsync(operacion.IdOperacion.Value, monto: monto);
                    if (success)
                    {
                        operacion.Monto = monto;
                        await _notificacionService.MostrarAsync("Operación actualizada", $"El monto se actualizó a {monto:C2}");
                    }
                    else
                    {
                        await _notificacionService.MostrarAsync("Error", "No se pudo actualizar el monto. Por favor, intente nuevamente.");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al editar operación: {ex.GetType().Name} - {ex.Message}");
                await _notificacionService.MostrarAsync("Error", "Ocurrió un error al editar la operación. Por favor, intente nuevamente.");
            }
        }

        private async void DeleteOperacionButton_Click(object sender, RoutedEventArgs e)
        {
            // Obtener la operación desde el Tag del botón
            if (sender is not FrameworkElement element || element.Tag is not Models.OperacionDto operacion)
                return;

            if (!operacion.IdOperacion.HasValue)
                return;

            // Mostrar diálogo de confirmación
            var identificador = operacion.Identificador ?? "sin identificador";
            var dialog = new ContentDialog
            {
                Title = "Confirmar eliminación",
                Content = $"¿Está seguro de que desea eliminar la operación del equipo {identificador}?",
                PrimaryButtonText = "Eliminar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                try
                {
                    var success = await ViewModel.DeleteOperacionAsync(operacion.IdOperacion.Value);

                    if (success)
                    {
                        await _notificacionService.MostrarAsync("Operación eliminada", "La operación se ha eliminado correctamente.");
                    }
                    else
                    {
                        await _notificacionService.MostrarAsync("Error", "No se pudo eliminar la operación. Por favor, intente nuevamente.");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error al eliminar operación: {ex.GetType().Name} - {ex.Message}");
                    await _notificacionService.MostrarAsync("Error", "Ocurrió un error al eliminar la operación. Por favor, intente nuevamente.");
                }
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
        /// Recalcula el TotalMonto de la operación y lo persiste en el servidor via UpdateOperacionAsync.
        /// Se llama después de agregar, editar o eliminar cargos.
        /// </summary>
        private async Task ActualizarMontoEnServidorAsync(Models.OperacionDto operacion)
        {
            if (operacion?.IdOperacion.HasValue != true) return;
            operacion.OnPropertyChanged(nameof(operacion.TotalMonto));
            var nuevoMonto = (decimal)operacion.TotalMonto;
            await ViewModel.UpdateOperacionAsync(operacion.IdOperacion.Value, monto: nuevoMonto);
            operacion.Monto = nuevoMonto;
        }

        private async void AddCargoButton_Click(object sender, RoutedEventArgs e)
        {
            // Obtener la operación desde el Tag del botón
            if (sender is not FrameworkElement element || element.Tag is not Models.OperacionDto operacion)
                return;

            if (!operacion.IdOperacion.HasValue)
                return;
            // Usar IdProveedor de la sesión de usuario (cargada en el login, sin llamadas adicionales al API)
            var agregarCargoControl = new Dialogs.AgregarCargoUserControl(operacion.IdOperacion.Value, _userSessionService.IdProveedor > 0 ? _userSessionService.IdProveedor : (int?)null);

            var dialog = new ContentDialog
            {
                Title = "Agregar Cargo",
                Content = agregarCargoControl,
                PrimaryButtonText = "Agregar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            // Set up validation before closing
            dialog.PrimaryButtonClick += (d, args) =>
            {
                if (!agregarCargoControl.IsValid)
                {
                    args.Cancel = true;
                }
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                try
                {
                    // Obtener el DTO con los datos del cargo
                    var cargoEditDto = agregarCargoControl.GetCargoEditDto();

                    // Crear el cargo usando el servicio
                    var newCargo = await _cargoService.CreateCargoAsync(cargoEditDto);

                    if (newCargo != null)
                    {
                        _activityService.Registrar("Operaciones", "Cargo agregado");
                        // Invalidar PDFs existentes porque los cargos cambiaron
                        ViewModel.DeleteOperacionPdfs(operacion.IdOperacion.Value, "*");
                        operacion.CotizacionPdfPath = null;
                        operacion.ReportePdfPath = null;

                        // Recargar los cargos para obtener los datos completos desde la API
                        operacion.CargosLoaded = false;
                        await LoadCargosForOperacionAsync(operacion);

                        // Actualizar el monto de la operación con la suma de los cargos
                        if (operacion.TotalMonto > 0)
                        {
                            await ViewModel.UpdateOperacionAsync(operacion.IdOperacion.Value, monto: (decimal)operacion.TotalMonto);
                            operacion.Monto = (decimal)operacion.TotalMonto;
                        }

                        await _notificacionService.MostrarAsync("Cargo creado", $"El cargo con ID {newCargo.IdCargo} se ha creado correctamente.");
                    }
                    else
                    {
                        await _notificacionService.MostrarAsync("Error", "No se pudo crear el cargo. Por favor, intente nuevamente.");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error al crear cargo: {ex.GetType().Name} - {ex.Message}");
                    await _notificacionService.MostrarAsync("Error", "Ocurrió un error al crear el cargo. Por favor, intente nuevamente.");
                }
            }
        }

        private async void DeleteCargoButton_Click(object sender, RoutedEventArgs e)
        {
            // Obtener el cargo desde el Tag del botón
            if (sender is not FrameworkElement element || element.Tag is not Models.CargoDto cargo)
                return;

            if (cargo.IdCargo <= 0)
                return;

            // Mostrar diálogo de confirmación
            var dialog = new ContentDialog
            {
                Title = "Confirmar eliminación",
                Content = $"¿Está seguro de que desea eliminar el cargo con ID {cargo.IdCargo}?",
                PrimaryButtonText = "Eliminar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                try
                {
                    var success = await _cargoService.DeleteCargoAsync(cargo.IdCargo);

                    if (success)
                    {
                        _activityService.Registrar("Operaciones", "Cargo eliminado");
                        // Invalidar PDFs existentes porque los cargos cambiaron
                        var parentOp = ViewModel.Operaciones.FirstOrDefault(o => o.Cargos.Any(c => c.IdCargo == cargo.IdCargo));
                        if (parentOp?.IdOperacion.HasValue == true)
                        {
                            ViewModel.DeleteOperacionPdfs(parentOp.IdOperacion.Value, "*");
                            parentOp.CotizacionPdfPath = null;
                            parentOp.ReportePdfPath = null;
                        }

                        // Encontrar la operación que contiene este cargo y eliminarlo de la colección
                        foreach (var operacion in ViewModel.Operaciones)
                        {
                            var cargoToRemove = operacion.Cargos.FirstOrDefault(c => c.IdCargo == cargo.IdCargo);
                            if (cargoToRemove != null)
                            {
                                operacion.Cargos.Remove(cargoToRemove);
                                await ActualizarMontoEnServidorAsync(operacion);
                                break;
                            }
                        }

                        await _notificacionService.MostrarAsync("Cargo eliminado", "El cargo se ha eliminado correctamente.");
                    }
                    else
                    {
                        await _notificacionService.MostrarAsync("Error", "No se pudo eliminar el cargo. Por favor, intente nuevamente.");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error al eliminar cargo: {ex.GetType().Name} - {ex.Message}");
                    await _notificacionService.MostrarAsync("Error", "Ocurrió un error al eliminar el cargo. Por favor, intente nuevamente.");
                }
            }
        }

        private void EditCargoButton_Click(object sender, RoutedEventArgs e)
        {
            // Obtener el cargo desde el Tag del botón
            if (sender is not FrameworkElement element || element.Tag is not Models.CargoDto cargo)
                return;

            // Toggle edit mode
            cargo.IsEditing = !cargo.IsEditing;
        }

        private void CargoRow_Tapped(object sender, TappedRoutedEventArgs e)
        {
            // Obtener el cargo desde el DataContext del StackPanel
            if (sender is not FrameworkElement element || element.DataContext is not Models.CargoDto cargo)
                return;

            // Solo toggle si el cargo tiene imágenes
            if (cargo.HasImages)
            {
                cargo.IsGalleryExpanded = !cargo.IsGalleryExpanded;
            }
        }

        private void ExpandGalleryButton_Click(object sender, RoutedEventArgs e)
        {
            // Obtener el cargo desde el Tag del botón
            if (sender is not Button button || button.Tag is not Models.CargoDto cargo)
                return;

            // Toggle gallery expansion
            cargo.IsGalleryExpanded = !cargo.IsGalleryExpanded;
        }
        
        private async void CargoField_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key != Windows.System.VirtualKey.Enter)
                return;

            // Obtener el TextBox
            if (sender is not TextBox textBox)
                return;

            // Obtener el cargo desde el Tag del TextBox
            if (textBox.Tag is not Models.CargoDto cargo)
                return;

            if (cargo.IdCargo <= 0)
                return;

            try
            {
                // Handle Cantidad changes - if cargo type is Servicio, cantidad must be 1
                if (cargo.TipoCargo == "Servicio" && cargo.Cantidad != 1)
                {
                    cargo.Cantidad = 1;
                    
                    await _notificacionService.MostrarAsync("Información", "Para cargos de tipo Servicio, la cantidad siempre es 1.");
                }

                // Actualizar el cargo
                var query = new CargoEditDto
                {
                    IdCargo = cargo.IdCargo,
                    IdTipoCargo = cargo.IdTipoCargo,
                    IdRelacionCargo = cargo.IdRelacionCargo,
                    Monto = cargo.Monto,
                    Nota = cargo.Nota,
                    Cantidad = cargo.Cantidad,
                    Unitario = cargo.Unitario
                };

                var success = await _cargoService.UpdateCargoAsync(query);

                if (success)
                {
                    _activityService.Registrar("Operaciones", "Cargo modificado");
                    // Exit edit mode after successful save
                    cargo.IsEditing = false;

                    // Invalidar PDFs existentes porque los cargos cambiaron
                    foreach (var operacion in ViewModel.Operaciones)
                    {
                        if (operacion.Cargos.Contains(cargo) && operacion.IdOperacion.HasValue)
                        {
                            ViewModel.DeleteOperacionPdfs(operacion.IdOperacion.Value, "*");
                            operacion.CotizacionPdfPath = null;
                            operacion.ReportePdfPath = null;
                            break;
                        }
                    }

                    // Recalcular y persistir el monto de la operación
                    foreach (var operacion in ViewModel.Operaciones)
                    {
                        if (operacion.Cargos.Contains(cargo))
                        {
                            await ActualizarMontoEnServidorAsync(operacion);
                            break;
                        }
                    }

                    await _notificacionService.MostrarAsync("Cargo actualizado", "El cargo se ha actualizado correctamente.");
                }
                else
                {
                    await _notificacionService.MostrarAsync("Error", "No se pudo actualizar el cargo. Por favor, intente nuevamente.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al actualizar cargo: {ex.GetType().Name} - {ex.Message}");
                await _notificacionService.MostrarAsync("Error", "Ocurrió un error al actualizar el cargo. Por favor, intente nuevamente.");
            }
        }

        private async void ViewRefaccionFromCargoButton_Click(object sender, RoutedEventArgs e)
        {
            // Obtener el cargo desde el Tag del botón
            if (sender is not FrameworkElement element || element.Tag is not Models.CargoDto cargo)
                return;

            // Log para debugging
            System.Diagnostics.Debug.WriteLine($"ViewRefaccionFromCargoButton_Click: IdCargo={cargo.IdCargo}, TipoCargo={cargo.TipoCargo}, IdRelacionCargo={cargo.IdRelacionCargo}");

            // Verificar que el cargo sea de tipo Refaccion
            if (cargo.TipoCargo != "Refaccion")
            {
                System.Diagnostics.Debug.WriteLine($"Cargo no es de tipo Refaccion: {cargo.TipoCargo}");
                return;
            }

            // Validar que tengamos un IdCargo válido para consultar
            if (cargo.IdCargo <= 0)
            {
                System.Diagnostics.Debug.WriteLine($"IdCargo inválido: {cargo.IdCargo}");
                await _notificacionService.MostrarAsync("Error", "No se puede mostrar la refacción porque el cargo no tiene un ID válido.");
                return;
            }

            try
            {
                // Obtener el cargo actualizado desde el API usando su ID
                // Esto asegura que tengamos la información más reciente incluyendo IdRelacionCargo
                System.Diagnostics.Debug.WriteLine($"Consultando cargo {cargo.IdCargo} desde API...");
                
                var query = new CargoEditDto
                {
                    IdCargo = cargo.IdCargo
                };

                var cargos = await _cargoService.GetCargosAsync(query);
                var cargoActualizado = cargos?.FirstOrDefault();
                
                if (cargoActualizado == null)
                {
                    System.Diagnostics.Debug.WriteLine($"No se encontró el cargo {cargo.IdCargo} en el API");
                    await _notificacionService.MostrarAsync("Error", "No se pudo obtener la información del cargo desde el servidor.");
                    return;
                }
                
                System.Diagnostics.Debug.WriteLine($"Cargo obtenido del API: IdCargo={cargoActualizado.IdCargo}, IdRelacionCargo={cargoActualizado.IdRelacionCargo}");

                // Verificar que el cargo actualizado tenga un idRelacionCargo
                if (!cargoActualizado.IdRelacionCargo.HasValue)
                {
                    System.Diagnostics.Debug.WriteLine($"IdRelacionCargo es null para cargo {cargoActualizado.IdCargo} incluso después de consultar el API");
                    await _notificacionService.MostrarAsync("Error", "No se puede mostrar la refacción porque el cargo no tiene una relación válida. El campo IdRelacionCargo está vacío en el servidor.");
                    return;
                }

                // Crear el UserControl para visualizar la refacción usando el ID de la relación del cargo actualizado
                var viewerControl = new Dialogs.RefaccionesViewerUserControl(cargoActualizado.IdRelacionCargo.Value);

                // Crear el diálogo
                var dialog = new ContentDialog
                {
                    Title = "Detalles de la Refacción",
                    Content = viewerControl,
                    CloseButtonText = "Cerrar",
                    DefaultButton = ContentDialogButton.Close,
                    XamlRoot = this.XamlRoot
                };

                await dialog.ShowAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al mostrar detalles de refacción: {ex.GetType().Name} - {ex.Message}");
                await _notificacionService.MostrarAsync("Error", "No se pudo cargar la información de la refacción. Por favor, intente nuevamente.");
            }
        }

        private async void GenerarCotizacionButton_Click(object sender, RoutedEventArgs e)
        {
            // Obtener la operación desde el Tag del botón
            if (sender is not FrameworkElement element || element.Tag is not Models.OperacionDto operacion)
                return;

            if (!operacion.IdOperacion.HasValue)
                return;

            // Verificar que la operación tenga cargos cargados
            if (operacion.Cargos == null || operacion.Cargos.Count == 0)
            {
                await _notificacionService.MostrarAsync("No hay cargos", "No se puede generar una cotización porque no hay cargos asociados a esta operación.");
                return;
            }

            try
            {
                // Si ya existe una cotización, eliminarla para regenerar
                if (!string.IsNullOrEmpty(ViewModel.FindExistingPdf(operacion.IdOperacion.Value, "Cotizacion")))
                    ViewModel.DeleteOperacionPdfs(operacion.IdOperacion.Value, "Cotizacion");

                // Seleccionar contacto "Dirigido a:" si la operación tiene cliente
                string? dirigidoA = null;
                ContactoDto? contactoParaCorreo = null;
                List<ContactoDto> contactosCliente = [];
                if (operacion.IdCliente.HasValue && operacion.IdCliente.Value > 0)
                {
                    try
                    {
                        var contactos = await _contactoService.GetContactosAsync(
                            new Models.ContactoQueryDto { IdCliente = operacion.IdCliente.Value });

                        if (contactos != null && contactos.Count > 0)
                        {
                            contactosCliente = contactos;
                            var listView = new ListView
                            {
                                ItemsSource = contactos,
                                DisplayMemberPath = "NombreCompleto",
                                SelectionMode = ListViewSelectionMode.Single,
                                MaxHeight = 300
                            };

                            var selectDialog = new ContentDialog
                            {
                                Title = "¿A quién va dirigida la cotización?",
                                Content = new ScrollViewer { Content = listView, MaxHeight = 320 },
                                PrimaryButtonText = "Seleccionar",
                                SecondaryButtonText = "Omitir",
                                DefaultButton = ContentDialogButton.Primary,
                                XamlRoot = this.XamlRoot
                            };

                            var selectResult = await selectDialog.ShowAsync();

                            if (selectResult == ContentDialogResult.Primary && listView.SelectedItem is Models.ContactoDto contactoSeleccionado)
                            {
                                contactoParaCorreo = contactoSeleccionado;
                                dirigidoA = string.Join(" ", new[] { contactoSeleccionado.Tratamiento, contactoSeleccionado.Nombre, contactoSeleccionado.Apellido }
                                    .Where(s => !string.IsNullOrWhiteSpace(s)));
                            }
                        }
                    }
                    catch
                    {
                        // Si falla la obtención de contactos, continuar sin "Dirigido a:"
                    }
                }

                // Generar la cotización
                var filePath = await ViewModel.GenerateQuoteAsync(operacion, dirigidoA);

                if (!string.IsNullOrEmpty(filePath))
                {
                    _activityService.Registrar("Operaciones", "Cotización generada");
                    operacion.CotizacionPdfPath = filePath;

                    // Marcar cotización como generada
                    await ViewModel.UpdateCheckAsync(operacion, "cotizacionGenerada");

                    // Mostrar visor de PDF con opciones de envío por correo
                    var visor = new CotizacionVisorDialog(
                        filePath,
                        contactoParaCorreo,
                        contactosCliente,
                        operacion.RazonSocial ?? string.Empty,
                        this.XamlRoot);

                    var visorResult = await visor.ShowAsync();
                    visor.NotificarResultado(visorResult);

                    if (visor.Resultado == CotizacionVisorResultado.EnviarCorreo)
                    {
                        var emailDialog = new EnviarCotizacionDialog(
                            filePath,
                            contactoParaCorreo,
                            contactosCliente,
                            operacion.RazonSocial ?? string.Empty,
                            this.XamlRoot,
                            idOperacion: operacion.IdOperacion);

                        var emailResult = await emailDialog.ShowAsync();
                        if (emailResult == ContentDialogResult.Primary)
                        {
                            await ViewModel.UpdateCheckAsync(operacion, "cotizacionEnviada");
                            await _notificacionService.MostrarAsync("Correo enviado", "La cotización fue enviada correctamente por correo.");
                        }
                    }
                    else if (visor.Resultado == CotizacionVisorResultado.AbrirExterno)
                    {
                        var file = await Windows.Storage.StorageFile.GetFileFromPathAsync(filePath);
                        await Windows.System.Launcher.LaunchFileAsync(file);
                    }
                }
                else
                {
                    await _notificacionService.MostrarAsync("Error", "No se pudo generar la cotización. Por favor, intente nuevamente.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al generar cotización: {ex.GetType().Name} - {ex.Message}");
                await _notificacionService.MostrarAsync("Error", "Ocurrió un error al generar la cotización. Por favor, intente nuevamente.");
            }
        }

        private async void GenerarReporteButton_Click(object sender, RoutedEventArgs e)
        {
            // Obtener la operación desde el Tag del botón
            if (sender is not FrameworkElement element || element.Tag is not Models.OperacionDto operacion)
                return;

            if (!operacion.IdOperacion.HasValue)
                return;

            // Verificar que la operación tenga cargos cargados
            if (operacion.Cargos == null || operacion.Cargos.Count == 0)
            {
                await _notificacionService.MostrarAsync("No hay cargos", "No se puede generar un reporte porque no hay cargos asociados a esta operación.");
                return;
            }

            try
            {
                // Si ya existe un reporte, eliminarlo para regenerar
                if (!string.IsNullOrEmpty(ViewModel.FindExistingPdf(operacion.IdOperacion.Value, "Reporte")))
                    ViewModel.DeleteOperacionPdfs(operacion.IdOperacion.Value, "Reporte");

                // Seleccionar contacto "Dirigido a:" si la operación tiene cliente
                string? dirigidoAReporte = null;
                ContactoDto? contactoReporte = null;
                List<ContactoDto> contactosReporte = [];
                if (operacion.IdCliente.HasValue && operacion.IdCliente.Value > 0)
                {
                    try
                    {
                        var contactos = await _contactoService.GetContactosAsync(
                            new Models.ContactoQueryDto { IdCliente = operacion.IdCliente.Value });

                        if (contactos != null && contactos.Count > 0)
                        {
                            contactosReporte = contactos;
                            var listView = new ListView
                            {
                                ItemsSource = contactos,
                                DisplayMemberPath = "NombreCompleto",
                                SelectionMode = ListViewSelectionMode.Single,
                                MaxHeight = 300
                            };

                            var selectDialog = new ContentDialog
                            {
                                Title = "¿A quién va dirigido el reporte?",
                                Content = new ScrollViewer { Content = listView, MaxHeight = 320 },
                                PrimaryButtonText = "Seleccionar",
                                SecondaryButtonText = "Omitir",
                                DefaultButton = ContentDialogButton.Primary,
                                XamlRoot = this.XamlRoot
                            };

                            var selectResult = await selectDialog.ShowAsync();

                            if (selectResult == ContentDialogResult.Primary && listView.SelectedItem is Models.ContactoDto contactoSeleccionado)
                            {
                                contactoReporte = contactoSeleccionado;
                                dirigidoAReporte = string.Join(" ", new[] { contactoSeleccionado.Tratamiento, contactoSeleccionado.Nombre, contactoSeleccionado.Apellido }
                                    .Where(s => !string.IsNullOrWhiteSpace(s)));
                            }
                        }
                    }
                    catch
                    {
                        // Si falla la obtención de contactos, continuar sin "Dirigido a:"
                    }
                }

                // Generar el reporte
                var filePath = await ViewModel.GenerateReporteAsync(operacion, dirigidoAReporte);

                if (!string.IsNullOrEmpty(filePath))
                {
                    _activityService.Registrar("Operaciones", "Reporte generado");
                    operacion.ReportePdfPath = filePath;

                    // Marcar reporte como generado en el check
                    await ViewModel.UpdateCheckAsync(operacion, "reporteGenerado");

                    // Mostrar visor de PDF con opciones de envío por correo
                    var visor = new CotizacionVisorDialog(
                        filePath,
                        contactoReporte,
                        contactosReporte,
                        operacion.RazonSocial ?? string.Empty,
                        this.XamlRoot,
                        tipo: "Reporte");

                    var visorResult = await visor.ShowAsync();
                    visor.NotificarResultado(visorResult);

                    if (visor.Resultado == CotizacionVisorResultado.EnviarCorreo)
                    {
                        var emailDialog = new EnviarCotizacionDialog(
                            filePath,
                            contactoReporte,
                            contactosReporte,
                            operacion.RazonSocial ?? string.Empty,
                            this.XamlRoot,
                            tipo: "Reporte",
                            idOperacion: operacion.IdOperacion);

                        var emailResult = await emailDialog.ShowAsync();
                        if (emailResult == ContentDialogResult.Primary)
                        {
                            await ViewModel.UpdateCheckAsync(operacion, "reporteEnviado");
                            await _notificacionService.MostrarAsync("Correo enviado", "El reporte fue enviado correctamente por correo.");
                        }
                    }
                    else if (visor.Resultado == CotizacionVisorResultado.AbrirExterno)
                    {
                        var file = await Windows.Storage.StorageFile.GetFileFromPathAsync(filePath);
                        await Windows.System.Launcher.LaunchFileAsync(file);
                    }
                }
                else
                {
                    var errMsg = !string.IsNullOrWhiteSpace(ViewModel.ErrorMessage)
                        ? ViewModel.ErrorMessage
                        : "No se pudo generar el reporte. Por favor, intente nuevamente.";
                    await _notificacionService.MostrarAsync("Error al generar reporte", errMsg);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al generar reporte: {ex.GetType().Name} - {ex.Message}");
                await _notificacionService.MostrarAsync("Error", "Ocurrió un error al generar el reporte. Por favor, intente nuevamente.");
            }
        }

        /// <summary>
        /// Abre el PDF de cotización existente para la operación.
        /// </summary>
        private async void AbrirCotizacionPdfButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement element || element.Tag is not Models.OperacionDto operacion)
                return;

            var path = operacion.CotizacionPdfPath;
            if (string.IsNullOrEmpty(path)) return;

            try
            {
                var file = await Windows.Storage.StorageFile.GetFileFromPathAsync(path);
                await Windows.System.Launcher.LaunchFileAsync(file);
            }
            catch
            {
                // El archivo puede haber sido eliminado externamente; refrescar estado
                operacion.CotizacionPdfPath = null;
            }
        }

        /// <summary>
        /// Abre el PDF de reporte existente para la operación.
        /// </summary>
        private async void AbrirReportePdfButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement element || element.Tag is not Models.OperacionDto operacion)
                return;

            var path = operacion.ReportePdfPath;
            if (string.IsNullOrEmpty(path)) return;

            try
            {
                var file = await Windows.Storage.StorageFile.GetFileFromPathAsync(path);
                await Windows.System.Launcher.LaunchFileAsync(file);
            }
            catch
            {
                // El archivo puede haber sido eliminado externamente; refrescar estado
                operacion.ReportePdfPath = null;
            }
        }

        /// <summary>
        /// Maneja el clic en el botón de cargar imagen para un cargo
        /// </summary>
        private async void UploadCargoImageButton_Click(object sender, RoutedEventArgs e)
        {
            // Obtener el cargo desde el Tag del botón
            if (sender is not FrameworkElement element || element.Tag is not Models.CargoDto cargo)
                return;

            if (cargo.IdCargo <= 0 || !cargo.IdOperacion.HasValue || cargo.IdOperacion.Value <= 0)
            {
                await _notificacionService.MostrarAsync("Error", "El cargo no tiene un ID válido para cargar imágenes.");
                return;
            }

            try
            {
                // Crear el selector de archivos
                var picker = new FileOpenPicker();
                
                // Obtener el HWND de la ventana principal para inicializar el picker
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

                // Configurar tipos de archivo permitidos
                picker.ViewMode = PickerViewMode.Thumbnail;
                picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
                picker.FileTypeFilter.Add(".jpg");
                picker.FileTypeFilter.Add(".jpeg");
                picker.FileTypeFilter.Add(".png");
                picker.FileTypeFilter.Add(".gif");
                picker.FileTypeFilter.Add(".bmp");

                // Mostrar el selector
                var file = await picker.PickSingleFileAsync();

                if (file == null)
                {
                    // Usuario canceló la selección
                    return;
                }

                // Mostrar indicador de carga
                cargo.IsLoadingImages = true;

                // Leer el archivo como stream
                using var stream = await file.OpenStreamForReadAsync();
                
                // Determinar el tipo de contenido basado en la extensión
                var contentType = GetContentTypeFromExtension(file.FileType);

                // Guardar la imagen localmente
                var result = await _cargoImageService.UploadImageAsync(cargo.IdOperacion.Value, cargo.IdCargo, stream, contentType);

                if (result != null)
                {
                    _activityService.Registrar("Operaciones", "Imagen cargada en cargo");
                    // Agregar la imagen a la colección del cargo
                    cargo.Images.Add(result);
                    cargo.NotifyImagesChanged();

                    await _notificacionService.MostrarAsync("Imagen cargada", $"La imagen {result.FileName} se ha guardado correctamente.");
                }
                else
                {
                    await _notificacionService.MostrarAsync("Error", "No se pudo guardar la imagen. Por favor, intente nuevamente.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cargar imagen: {ex.GetType().Name} - {ex.Message}");
                await _notificacionService.MostrarAsync("Error", "Ocurrió un error al cargar la imagen. Por favor, intente nuevamente.");
            }
            finally
            {
                cargo.IsLoadingImages = false;
            }
        }

        /// <summary>
        /// Maneja el clic en el botón de eliminar imagen de un cargo
        /// </summary>
        private async void DeleteCargoImageButton_Click(object sender, RoutedEventArgs e)
        {
            // Obtener la imagen desde el Tag del botón
            if (sender is not FrameworkElement element || element.Tag is not Models.CargoImageDto image)
                return;

            if (string.IsNullOrEmpty(image.FileName))
            {
                await _notificacionService.MostrarAsync("Error", "La imagen no tiene un nombre de archivo válido.");
                return;
            }

            // Mostrar diálogo de confirmación
            var dialog = new ContentDialog
            {
                Title = "Confirmar eliminación",
                Content = $"¿Está seguro de que desea eliminar la imagen {image.FileName}?",
                PrimaryButtonText = "Eliminar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            var dialogResult = await dialog.ShowAsync();

            if (dialogResult == ContentDialogResult.Primary)
            {
                try
                {
                    // Find the parent cargo to get idOperacion
                    var targetCargo = ViewModel.Operaciones
                        .SelectMany(op => op.Cargos)
                        .FirstOrDefault(c => c.IdCargo == image.IdCargo);

                    if (targetCargo == null || !targetCargo.IdOperacion.HasValue || targetCargo.IdOperacion.Value <= 0)
                    {
                        await _notificacionService.MostrarAsync("Error", "No se pudo determinar la operación del cargo.");
                        return;
                    }

                    var success = await _cargoImageService.DeleteImageAsync(targetCargo.IdOperacion.Value, image.FileName);

                    if (success)
                    {
                        _activityService.Registrar("Operaciones", "Imagen eliminada");
                        var imageToRemove = targetCargo.Images.FirstOrDefault(i => i.FileName == image.FileName);
                        if (imageToRemove != null)
                        {
                            targetCargo.Images.Remove(imageToRemove);
                            targetCargo.NotifyImagesChanged();
                        }

                        await _notificacionService.MostrarAsync("Imagen eliminada", "La imagen se ha eliminado correctamente.");
                    }
                    else
                    {
                        await _notificacionService.MostrarAsync("Error", "No se pudo eliminar la imagen. Por favor, intente nuevamente.");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error al eliminar imagen: {ex.GetType().Name} - {ex.Message}");
                    await _notificacionService.MostrarAsync("Error", "Ocurrió un error al eliminar la imagen. Por favor, intente nuevamente.");
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
        /// Obtiene el tipo de contenido basado en la extensión del archivo
        /// </summary>
        private static string GetContentTypeFromExtension(string extension)
        {
            return ImageContentTypeHelper.GetContentTypeFromExtension(extension);
        }

        /// <summary>
        /// Obtiene todos los cargos seleccionados de una operación
        /// </summary>
        private System.Collections.Generic.List<Models.CargoDto> GetSelectedCargos(Models.OperacionDto operacion)
        {
            return operacion.Cargos.Where(c => c.IsSelected).ToList();
        }

        /// <summary>
        /// Maneja el evento Checked del checkbox de cargo para implementar selección única.
        /// Cuando se selecciona un cargo, se deseleccionan y colapsan todos los demás cargos de la misma operación.
        /// </summary>
        private void CargoCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            // Obtener el cargo desde el Tag del CheckBox
            if (sender is not CheckBox checkBox || checkBox.Tag is not Models.CargoDto selectedCargo)
                return;

            // Usar IdOperacion del cargo para encontrar la operación padre de forma más eficiente
            var operacion = selectedCargo.IdOperacion.HasValue
                ? ViewModel.Operaciones.FirstOrDefault(op => op.IdOperacion == selectedCargo.IdOperacion.Value)
                : ViewModel.Operaciones.FirstOrDefault(op => op.Cargos.Contains(selectedCargo));

            if (operacion != null)
            {
                // Deseleccionar y colapsar todos los demás cargos de esta operación
                foreach (var cargo in operacion.Cargos)
                {
                    if (cargo != selectedCargo)
                    {
                        cargo.IsSelected = false;
                        cargo.IsGalleryExpanded = false;
                    }
                }
            }
        }

        /// <summary>
        /// Maneja el clic en el botón de cargar imagen para el cargo seleccionado
        /// </summary>
        private async void UploadSelectedCargoImageButton_Click(object sender, RoutedEventArgs e)
        {
            // Obtener la operación desde el Tag del botón
            if (sender is not FrameworkElement element || element.Tag is not Models.OperacionDto operacion)
                return;

            var selectedCargos = GetSelectedCargos(operacion);
            if (selectedCargos.Count == 0)
            {
                await _notificacionService.MostrarAsync("Sin selección", "Por favor, seleccione un cargo para cargar una imagen.");
                return;
            }

            // Usar solo el primer cargo seleccionado
            var selectedCargo = selectedCargos[0];
            if (selectedCargos.Count > 1)
            {
                await _notificacionService.MostrarAsync("Múltiples selecciones", "Se procesará solo el primer cargo seleccionado.");
            }

            if (selectedCargo.IdCargo <= 0 || !selectedCargo.IdOperacion.HasValue || selectedCargo.IdOperacion.Value <= 0)
            {
                await _notificacionService.MostrarAsync("Error", "El cargo no tiene un ID válido para cargar imágenes.");
                return;
            }

            try
            {
                // Crear el selector de archivos
                var picker = new FileOpenPicker();
                
                // Obtener el HWND de la ventana principal para inicializar el picker
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

                // Configurar tipos de archivo permitidos
                picker.ViewMode = PickerViewMode.Thumbnail;
                picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
                picker.FileTypeFilter.Add(".jpg");
                picker.FileTypeFilter.Add(".jpeg");
                picker.FileTypeFilter.Add(".png");
                picker.FileTypeFilter.Add(".gif");
                picker.FileTypeFilter.Add(".bmp");

                // Mostrar el selector
                var file = await picker.PickSingleFileAsync();

                if (file == null)
                {
                    // Usuario canceló la selección
                    return;
                }

                // Mostrar indicador de carga
                selectedCargo.IsLoadingImages = true;

                // Leer el archivo como stream
                using var stream = await file.OpenStreamForReadAsync();
                
                // Determinar el tipo de contenido basado en la extensión
                var contentType = GetContentTypeFromExtension(file.FileType);

                // Guardar la imagen localmente
                var result = await _cargoImageService.UploadImageAsync(selectedCargo.IdOperacion.Value, selectedCargo.IdCargo, stream, contentType);

                if (result != null)
                {
                    // Agregar la imagen a la colección del cargo
                    selectedCargo.Images.Add(result);
                    selectedCargo.NotifyImagesChanged();

                    await _notificacionService.MostrarAsync("Imagen cargada", $"La imagen {result.FileName} se ha guardado correctamente.");
                }
                else
                {
                    await _notificacionService.MostrarAsync("Error", "No se pudo guardar la imagen. Por favor, intente nuevamente.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cargar imagen: {ex.GetType().Name} - {ex.Message}");
                await _notificacionService.MostrarAsync("Error", "Ocurrió un error al cargar la imagen. Por favor, intente nuevamente.");
            }
            finally
            {
                selectedCargo.IsLoadingImages = false;
            }
        }

        /// <summary>
        /// Maneja el clic en el botón de editar para el cargo seleccionado
        /// </summary>
        private async void EditSelectedCargoButton_Click(object sender, RoutedEventArgs e)
        {
            // Obtener la operación desde el Tag del botón
            if (sender is not FrameworkElement element || element.Tag is not Models.OperacionDto operacion)
                return;

            var selectedCargos = GetSelectedCargos(operacion);
            if (selectedCargos.Count == 0)
            {
                await _notificacionService.MostrarAsync("Sin selección", "Por favor, seleccione un cargo para editar.");
                return;
            }

            // Usar solo el primer cargo seleccionado
            var selectedCargo = selectedCargos[0];
            if (selectedCargos.Count > 1)
            {
                await _notificacionService.MostrarAsync("Múltiples selecciones", "Se procesará solo el primer cargo seleccionado.");
            }

            // Toggle edit mode
            selectedCargo.IsEditing = !selectedCargo.IsEditing;
        }

        /// <summary>
        /// Maneja el clic en el botón de eliminar para los cargos seleccionados
        /// </summary>
        private async void DeleteSelectedCargosButton_Click(object sender, RoutedEventArgs e)
        {
            // Obtener la operación desde el Tag del botón
            if (sender is not FrameworkElement element || element.Tag is not Models.OperacionDto operacion)
                return;

            var selectedCargos = GetSelectedCargos(operacion);
            if (selectedCargos.Count == 0)
            {
                await _notificacionService.MostrarAsync("Sin selección", "Por favor, seleccione al menos un cargo para eliminar.");
                return;
            }

            // Mostrar diálogo de confirmación
            var mensaje = selectedCargos.Count == 1 
                ? $"¿Está seguro de que desea eliminar el cargo con ID {selectedCargos[0].IdCargo}?"
                : $"¿Está seguro de que desea eliminar {selectedCargos.Count} cargos seleccionados?";
            
            var dialog = new ContentDialog
            {
                Title = "Confirmar eliminación",
                Content = mensaje,
                PrimaryButtonText = "Eliminar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                var eliminados = 0;
                var errores = 0;

                foreach (var cargo in selectedCargos)
                {
                    try
                    {
                        if (cargo.IdCargo <= 0)
                            continue;

                        var success = await _cargoService.DeleteCargoAsync(cargo.IdCargo);

                        if (success)
                        {
                            operacion.Cargos.Remove(cargo);
                            eliminados++;
                        }
                        else
                        {
                            errores++;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error al eliminar cargo {cargo.IdCargo}: {ex.GetType().Name} - {ex.Message}");
                        errores++;
                    }
                }

                if (eliminados > 0 && errores == 0)
                {
                    await ActualizarMontoEnServidorAsync(operacion);
                    await _notificacionService.MostrarAsync("Cargos eliminados", eliminados == 1 
                            ? "El cargo se ha eliminado correctamente."
                            : $"Se han eliminado {eliminados} cargos correctamente.");
                }
                else if (eliminados > 0 && errores > 0)
                {
                    await ActualizarMontoEnServidorAsync(operacion);
                    await _notificacionService.MostrarAsync("Eliminación parcial", $"Se eliminaron {eliminados} cargos, pero hubo {errores} errores.");
                }
                else
                {
                    await _notificacionService.MostrarAsync("Error", "No se pudieron eliminar los cargos. Por favor, intente nuevamente.");
                }
            }
        }

        /// <summary>
        /// Maneja el clic en el botón de ver refacción para el cargo seleccionado
        /// </summary>
        private async void ViewSelectedRefaccionButton_Click(object sender, RoutedEventArgs e)
        {
            // Obtener la operación desde el Tag del botón
            if (sender is not FrameworkElement element || element.Tag is not Models.OperacionDto operacion)
                return;

            var selectedCargos = GetSelectedCargos(operacion);
            if (selectedCargos.Count == 0)
            {
                await _notificacionService.MostrarAsync("Sin selección", "Por favor, seleccione un cargo para ver los detalles de la refacción.");
                return;
            }

            // Usar solo el primer cargo seleccionado
            var selectedCargo = selectedCargos[0];
            if (selectedCargos.Count > 1)
            {
                await _notificacionService.MostrarAsync("Múltiples selecciones", "Se procesará solo el primer cargo seleccionado.");
            }

            // Verificar que el cargo sea de tipo Refaccion
            if (selectedCargo.TipoCargo != "Refaccion")
            {
                await _notificacionService.MostrarAsync("Tipo incorrecto", "Solo se pueden ver detalles de refacciones para cargos de tipo 'Refaccion'.");
                return;
            }

            // Validar que tengamos un IdCargo válido para consultar
            if (selectedCargo.IdCargo <= 0)
            {
                await _notificacionService.MostrarAsync("Error", "No se puede mostrar la refacción porque el cargo no tiene un ID válido.");
                return;
            }

            try
            {
                // Obtener el cargo actualizado desde el API usando su ID
                var query = new CargoEditDto
                {
                    IdCargo = selectedCargo.IdCargo
                };

                var cargos = await _cargoService.GetCargosAsync(query);
                var cargoActualizado = cargos?.FirstOrDefault();
                
                if (cargoActualizado == null)
                {
                    await _notificacionService.MostrarAsync("Error", "No se pudo obtener la información del cargo desde el servidor.");
                    return;
                }

                // Verificar que el cargo actualizado tenga un idRelacionCargo
                if (!cargoActualizado.IdRelacionCargo.HasValue)
                {
                    await _notificacionService.MostrarAsync("Error", "No se puede mostrar la refacción porque el cargo no tiene una relación válida.");
                    return;
                }

                // Crear el UserControl para visualizar la refacción
                var viewerControl = new Dialogs.RefaccionesViewerUserControl(cargoActualizado.IdRelacionCargo.Value);

                // Crear el diálogo
                var dialog = new ContentDialog
                {
                    Title = "Detalles de la Refacción",
                    Content = viewerControl,
                    CloseButtonText = "Cerrar",
                    DefaultButton = ContentDialogButton.Close,
                    XamlRoot = this.XamlRoot
                };

                await dialog.ShowAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al mostrar detalles de refacción: {ex.GetType().Name} - {ex.Message}");
                await _notificacionService.MostrarAsync("Error", "No se pudo cargar la información de la refacción. Por favor, intente nuevamente.");
            }
        }

        /// <summary>
        /// Maneja el clic en el botón de expandir galería para el cargo seleccionado
        /// </summary>
        private async void ExpandSelectedGalleryButton_Click(object sender, RoutedEventArgs e)
        {
            // Obtener la operación desde el Tag del botón
            if (sender is not FrameworkElement element || element.Tag is not Models.OperacionDto operacion)
                return;

            var selectedCargos = GetSelectedCargos(operacion);
            if (selectedCargos.Count == 0)
            {
                await _notificacionService.MostrarAsync("Sin selección", "Por favor, seleccione un cargo para ver las imágenes.");
                return;
            }

            // Usar solo el primer cargo seleccionado
            var selectedCargo = selectedCargos[0];
            if (selectedCargos.Count > 1)
            {
                await _notificacionService.MostrarAsync("Múltiples selecciones", "Se procesará solo el primer cargo seleccionado.");
            }

            if (!selectedCargo.HasImages)
            {
                await _notificacionService.MostrarAsync("Sin imágenes", "El cargo seleccionado no tiene imágenes.");
                return;
            }

            // Toggle gallery expansion
            selectedCargo.IsGalleryExpanded = !selectedCargo.IsGalleryExpanded;
        }

        /// <summary>
        /// Helper method to upload an operation image (Prefactura or Hoja Servicio)
        /// </summary>
        private async Task UploadOperacionImageAsync(Models.OperacionDto operacion, string imageType)
        {
            if (!operacion.IdOperacion.HasValue || operacion.IdOperacion.Value <= 0)
            {
                await _notificacionService.MostrarAsync("Error", "La operación no tiene un ID válido para cargar imágenes.");
                return;
            }

            try
            {
                // Crear el selector de archivos
                var picker = new FileOpenPicker();
                
                // Obtener el HWND de la ventana principal para inicializar el picker
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

                // Configurar tipos de archivo permitidos
                picker.ViewMode = PickerViewMode.Thumbnail;
                picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
                picker.FileTypeFilter.Add(".jpg");
                picker.FileTypeFilter.Add(".jpeg");
                picker.FileTypeFilter.Add(".png");
                picker.FileTypeFilter.Add(".gif");
                picker.FileTypeFilter.Add(".bmp");

                // Mostrar el selector
                var file = await picker.PickSingleFileAsync();

                if (file == null)
                {
                    // Usuario canceló la selección
                    return;
                }

                // Leer el archivo como stream
                using var stream = await file.OpenStreamForReadAsync();
                
                // Determinar el tipo de contenido basado en la extensión
                var contentType = GetContentTypeFromExtension(file.FileType);

                // Guardar la imagen según el tipo
                Models.OperacionImageDto? result = null;
                if (imageType == "Prefactura")
                {
                    result = await _operacionImageService.UploadPrefacturaAsync(operacion.IdOperacion.Value, stream, contentType);
                }
                else if (imageType == "HojaServicio")
                {
                    result = await _operacionImageService.UploadHojaServicioAsync(operacion.IdOperacion.Value, stream, contentType);
                }
                else if (imageType == "OrdenCompra")
                {
                    result = await _operacionImageService.UploadOrdenCompraAsync(operacion.IdOperacion.Value, stream, contentType);
                }

                if (result != null)
                {
                    // Resetear flag para forzar recarga de indicadores
                    operacion.ImagesLoaded = false;
                    // Update indicator on the operacion model
                    await RefreshImageIndicatorsAsync(operacion);

                    var actividadTitulo = imageType switch
                    {
                        "Prefactura"   => "Prefactura cargada",
                        "HojaServicio" => "Hoja servicio cargada",
                        "OrdenCompra"  => "Orden compra cargada",
                        _              => $"{imageType} cargada"
                    };
                    _activityService.Registrar("Operaciones", actividadTitulo);

                    // Actualizar checkOperacion
                    var campoCheok = imageType switch
                    {
                        "Prefactura"   => "prefacturaCargada",
                        "HojaServicio" => "hojaServicioCargada",
                        "OrdenCompra"  => "ordenCompraCargada",
                        _              => null
                    };
                    if (campoCheok != null)
                        await ViewModel.UpdateCheckAsync(operacion, campoCheok);

                    await _notificacionService.MostrarAsync($"{imageType} cargada", $"La {imageType.ToLower()} {result.FileName} se ha guardado correctamente.");
                }
                else
                {
                    await _notificacionService.MostrarAsync("Error", $"No se pudo guardar la {imageType.ToLower()}. Por favor, intente nuevamente.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cargar {imageType.ToLower()}: {ex.GetType().Name} - {ex.Message}");
                await _notificacionService.MostrarAsync("Error", $"Ocurrió un error al cargar la {imageType.ToLower()}. Por favor, intente nuevamente.");
            }
        }

        /// <summary>
        /// Maneja el clic en el botón de cargar prefactura
        /// </summary>
        private async void UploadPrefacturaButton_Click(object sender, RoutedEventArgs e)
        {
            // Obtener la operación desde el Tag del botón
            if (sender is not FrameworkElement element || element.Tag is not Models.OperacionDto operacion)
                return;

            await UploadOperacionImageAsync(operacion, "Prefactura");
        }

        /// <summary>
        /// Maneja el clic en el botón de cargar hoja de servicio
        /// </summary>
        private async void UploadHojaServicioButton_Click(object sender, RoutedEventArgs e)
        {
            // Obtener la operación desde el Tag del botón
            if (sender is not FrameworkElement element || element.Tag is not Models.OperacionDto operacion)
                return;

            await UploadOperacionImageAsync(operacion, "HojaServicio");
        }

        /// <summary>
        /// Maneja el clic en el botón de cargar orden de compra
        /// </summary>
        private async void UploadOrdenCompraButton_Click(object sender, RoutedEventArgs e)
        {
            // Obtener la operación desde el Tag del botón
            if (sender is not FrameworkElement element || element.Tag is not Models.OperacionDto operacion)
                return;

            await UploadOperacionImageAsync(operacion, "OrdenCompra");
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

        /// <summary>
        /// Maneja el clic en una imagen de operación para abrirla en el visor
        /// </summary>
        private async void ViewOperacionImageButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement element || element.Tag is not Models.OperacionImageDto image)
                return;

            if (string.IsNullOrEmpty(image.Url))
                return;

            await _imageViewerService.ShowImageAsync(image.Url, image.Tipo);
        }

        /// <summary>
        /// Maneja el clic en el botón de eliminar imagen de una operación (Prefactura, Hoja de Servicio u Orden de Compra)
        /// </summary>
        private async void DeleteOperacionImageButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement element || element.Tag is not Models.OperacionImageDto image)
                return;

            if (string.IsNullOrEmpty(image.FileName))
            {
                await _notificacionService.MostrarAsync("Error", "La imagen no tiene un nombre de archivo válido.");
                return;
            }

            var dialog = new ContentDialog
            {
                Title = "Confirmar eliminación",
                Content = $"¿Está seguro de que desea eliminar la imagen {image.FileName}?",
                PrimaryButtonText = "Eliminar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            var dialogResult = await dialog.ShowAsync();

            if (dialogResult == ContentDialogResult.Primary)
            {
                try
                {
                    var operacion = ViewModel.Operaciones
                        .FirstOrDefault(op => op.IdOperacion.HasValue && op.IdOperacion.Value == image.IdOperacion);

                    if (operacion == null)
                    {
                        await _notificacionService.MostrarAsync("Error", "No se pudo determinar la operación de la imagen.");
                        return;
                    }

                    var success = await _operacionImageService.DeleteImageAsync(image.IdOperacion, image.FileName);

                    if (success)
                    {
                        _activityService.Registrar("Operaciones", "Doc. op. eliminado");
                        operacion.ImagesLoaded = false;
                        await RefreshImageIndicatorsAsync(operacion);

                        await _notificacionService.MostrarAsync("Imagen eliminada", "La imagen se ha eliminado correctamente.");
                    }
                    else
                    {
                        await _notificacionService.MostrarAsync("Error", "No se pudo eliminar la imagen. Por favor, intente nuevamente.");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error al eliminar imagen de operación: {ex.GetType().Name} - {ex.Message}");
                    await _notificacionService.MostrarAsync("Error", "Ocurrió un error al eliminar la imagen. Por favor, intente nuevamente.");
                }
            }
        }
        /// <summary>
        /// Maneja el clic en el botón de cargar factura PDF (finaliza la operación)
        /// </summary>
        private async void UploadFacturaButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement element || element.Tag is not Models.OperacionDto operacion)
                return;

            if (!operacion.IdOperacion.HasValue || operacion.IdOperacion.Value <= 0)
                return;

            try
            {
                var picker = new FileOpenPicker();
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

                picker.ViewMode = PickerViewMode.List;
                picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                picker.FileTypeFilter.Add(".pdf");

                var file = await picker.PickSingleFileAsync();
                if (file == null)
                    return;

                using var stream = await file.OpenStreamForReadAsync();

                var result = await _operacionImageService.UploadFacturaAsync(operacion.IdOperacion.Value, stream);

                if (result != null)
                {
                    _activityService.Registrar("Operaciones", "Factura cargada");
                    await ViewModel.UpdateCheckAsync(operacion, "facturaCargada");

                    // Finalizar la operación con la fecha de hoy
                    var fechaFinal = DateTime.Today;
                    var updated = await ViewModel.UpdateOperacionAsync(operacion.IdOperacion.Value, fechaFinal: fechaFinal);

                    if (updated)
                    {
                        operacion.FechaFinal = fechaFinal;
                    }

                    operacion.HasFactura = true;

                    await _notificacionService.MostrarAsync("Factura cargada", "La factura PDF se guardó correctamente y la operación fue finalizada.");
                }
                else
                {
                    await _notificacionService.MostrarAsync("Error", "No se pudo guardar la factura. Por favor, intente nuevamente.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cargar factura: {ex.GetType().Name} - {ex.Message}");
                await _notificacionService.MostrarAsync("Error", "Ocurrió un error al cargar la factura. Por favor, intente nuevamente.");
            }
        }

        /// <summary>
        /// Abre la factura PDF con el visor predeterminado del sistema
        /// </summary>
        private async void VerFacturaButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement element || element.Tag is not Models.OperacionDto operacion)
                return;

            if (!operacion.IdOperacion.HasValue)
                return;

            try
            {
                var factura = await _operacionImageService.GetFacturaAsync(operacion.IdOperacion.Value);
                if (factura == null || string.IsNullOrEmpty(factura.Url))
                {
                    await _notificacionService.MostrarAsync("Error", "No se encontró el archivo de factura.");
                    return;
                }

                var file = await Windows.Storage.StorageFile.GetFileFromPathAsync(factura.Url);
                await Windows.System.Launcher.LaunchFileAsync(file);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al abrir factura: {ex.GetType().Name} - {ex.Message}");
                await _notificacionService.MostrarAsync("Error", "No se pudo abrir el archivo de factura.");
            }
        }

        /// <summary>
        /// Reabre una operación limpiando su fechaFinal
        /// </summary>
        private async void ReabrirOperacionButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement element || element.Tag is not Models.OperacionDto operacion)
                return;

            if (!operacion.IdOperacion.HasValue)
                return;

            var dialog = new ContentDialog
            {
                Title = "Reabrir operación",
                Content = "¿Está seguro de que desea reabrir esta operación? Se eliminará la fecha de finalización.",
                PrimaryButtonText = "Reabrir",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary)
                return;

            try
            {
                var success = await ViewModel.ReopenOperacionAsync(operacion.IdOperacion.Value);

                if (success)
                {
                    _activityService.Registrar("Operaciones", "Operación reabierta");
                    operacion.FechaFinal = null;

                    await _notificacionService.MostrarAsync("Operación reabierta", "La operación fue reabierta correctamente.");
                }
                else
                {
                    await _notificacionService.MostrarAsync("Error", "No se pudo reabrir la operación. Por favor, intente nuevamente.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al reabrir operación: {ex.GetType().Name} - {ex.Message}");
                await _notificacionService.MostrarAsync("Error", "Ocurrió un error al reabrir la operación.");
            }
        }
    }
}
