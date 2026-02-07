using Advance_Control.Models;
using Advance_Control.Services.Cargos;
using Advance_Control.Services.LocalStorage;
using Advance_Control.Services.Notificacion;
using Advance_Control.Services.UserInfo;
using Advance_Control.Utilities;
using Advance_Control.ViewModels;
using Advance_Control.Views.Equipos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using System;
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
        private readonly IUserInfoService _userInfoService;
        private readonly ICargoImageService _cargoImageService;

        /// <summary>
        /// Currency formatter for the NumberBox
        /// </summary>
        public INumberFormatter2 CurrencyFormatter { get; }

        public OperacionesView()
        {
            // Resolver el ViewModel desde DI
            ViewModel = ((App)Application.Current).Host.Services.GetRequiredService<OperacionesViewModel>();

            // Resolver el servicio de notificaciones desde DI
            _notificacionService = ((App)Application.Current).Host.Services.GetRequiredService<INotificacionService>();
            
            // Resolver el servicio de cargos desde DI
            _cargoService = ((App)Application.Current).Host.Services.GetRequiredService<ICargoService>();

            // Resolver el servicio de información de usuario desde DI
            _userInfoService = ((App)Application.Current).Host.Services.GetRequiredService<IUserInfoService>();

            // Resolver el servicio de imágenes de cargo desde DI
            _cargoImageService = ((App)Application.Current).Host.Services.GetRequiredService<ICargoImageService>();

            // Initialize currency formatter for Mexican Pesos
            var currencyFormatter = new CurrencyFormatter("MXN");
            currencyFormatter.FractionDigits = 2;
            CurrencyFormatter = currencyFormatter;
            
            this.InitializeComponent();
            
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

        private async void HeadGrid_Tapped(object sender, TappedRoutedEventArgs e)
        {
            // Get the OperacionDto from the sender's Tag property
            if (sender is FrameworkElement element && element.Tag is Models.OperacionDto operacion)
            {
                operacion.Expand = !operacion.Expand;
                
                // Load cargos when expanding
                if (operacion.Expand && operacion.IdOperacion.HasValue)
                {
                    await LoadCargosForOperacionAsync(operacion);
                }
            }
        }

        private async void ToggleExpandButton_Click(object sender, RoutedEventArgs e)
        {
            // Get the OperacionDto from the sender's Tag property
            if (sender is FrameworkElement element && element.Tag is Models.OperacionDto operacion)
            {
                operacion.Expand = !operacion.Expand;
                
                // Load cargos when expanding
                if (operacion.Expand && operacion.IdOperacion.HasValue)
                {
                    await LoadCargosForOperacionAsync(operacion);
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
                            {
                                // Notify bindings to update
                                operacion.OnPropertyChanged(nameof(operacion.TotalMonto));
                            }
                        };

                        // Load images for this cargo asynchronously with error handling
                        _ = LoadImagesForCargoSafeAsync(cargo);
                    }

                    // Subscribe to collection changes to update total when items are added/removed
                    operacion.Cargos.CollectionChanged += (s, e) =>
                    {
                        if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
                        {
                            foreach (CargoDto cargo in e.NewItems)
                            {
                                cargo.PropertyChanged += (sender, args) =>
                                {
                                    if (args.PropertyName == nameof(CargoDto.Monto))
                                    {
                                        operacion.OnPropertyChanged(nameof(operacion.TotalMonto));
                                    }
                                };
                            }
                        }
                        // Notify bindings to update total
                        operacion.OnPropertyChanged(nameof(operacion.TotalMonto));
                    };
                    
                    // Notify that TotalMonto should be recalculated after loading cargos
                    operacion.OnPropertyChanged(nameof(operacion.TotalMonto));
                    
                    operacion.CargosLoaded = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cargar cargos: {ex.GetType().Name} - {ex.Message}");
                await _notificacionService.MostrarNotificacionAsync(
                    titulo: "Error al cargar cargos",
                    nota: "No se pudieron cargar los cargos de la operación. Por favor, intente nuevamente.",
                    fechaHoraInicio: DateTime.Now);
            }
            finally
            {
                operacion.IsLoadingCargos = false;
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
                        await _notificacionService.MostrarNotificacionAsync(
                            titulo: "Operación eliminada",
                            nota: "La operación se ha eliminado correctamente.",
                            fechaHoraInicio: DateTime.Now);
                    }
                    else
                    {
                        await _notificacionService.MostrarNotificacionAsync(
                            titulo: "Error",
                            nota: "No se pudo eliminar la operación. Por favor, intente nuevamente.",
                            fechaHoraInicio: DateTime.Now);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error al eliminar operación: {ex.GetType().Name} - {ex.Message}");
                    await _notificacionService.MostrarNotificacionAsync(
                        titulo: "Error",
                        nota: "Ocurrió un error al eliminar la operación. Por favor, intente nuevamente.",
                        fechaHoraInicio: DateTime.Now);
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

        private async void AddCargoButton_Click(object sender, RoutedEventArgs e)
        {
            // Obtener la operación desde el Tag del botón
            if (sender is not FrameworkElement element || element.Tag is not Models.OperacionDto operacion)
                return;

            if (!operacion.IdOperacion.HasValue)
                return;
            var userInfo = await _userInfoService.GetUserInfoAsync();
            // Crear el UserControl para agregar cargo, pasando idProveedor registrado en el contacto que realiza la operacion
            var agregarCargoControl = new Equipos.AgregarCargoUserControl(operacion.IdOperacion.Value, userInfo?.IdProveedor);

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
                        // Recargar los cargos para obtener los datos completos desde la API
                        operacion.CargosLoaded = false;
                        await LoadCargosForOperacionAsync(operacion);

                        await _notificacionService.MostrarNotificacionAsync(
                            titulo: "Cargo creado",
                            nota: $"El cargo con ID {newCargo.IdCargo} se ha creado correctamente.",
                            fechaHoraInicio: DateTime.Now);
                    }
                    else
                    {
                        await _notificacionService.MostrarNotificacionAsync(
                            titulo: "Error",
                            nota: "No se pudo crear el cargo. Por favor, intente nuevamente.",
                            fechaHoraInicio: DateTime.Now);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error al crear cargo: {ex.GetType().Name} - {ex.Message}");
                    await _notificacionService.MostrarNotificacionAsync(
                        titulo: "Error",
                        nota: "Ocurrió un error al crear el cargo. Por favor, intente nuevamente.",
                        fechaHoraInicio: DateTime.Now);
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
                        // Encontrar la operación que contiene este cargo y eliminarlo de la colección
                        foreach (var operacion in ViewModel.Operaciones)
                        {
                            var cargoToRemove = operacion.Cargos.FirstOrDefault(c => c.IdCargo == cargo.IdCargo);
                            if (cargoToRemove != null)
                            {
                                operacion.Cargos.Remove(cargoToRemove);
                                break;
                            }
                        }

                        await _notificacionService.MostrarNotificacionAsync(
                            titulo: "Cargo eliminado",
                            nota: "El cargo se ha eliminado correctamente.",
                            fechaHoraInicio: DateTime.Now);
                    }
                    else
                    {
                        await _notificacionService.MostrarNotificacionAsync(
                            titulo: "Error",
                            nota: "No se pudo eliminar el cargo. Por favor, intente nuevamente.",
                            fechaHoraInicio: DateTime.Now);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error al eliminar cargo: {ex.GetType().Name} - {ex.Message}");
                    await _notificacionService.MostrarNotificacionAsync(
                        titulo: "Error",
                        nota: "Ocurrió un error al eliminar el cargo. Por favor, intente nuevamente.",
                        fechaHoraInicio: DateTime.Now);
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
                    
                    await _notificacionService.MostrarNotificacionAsync(
                        titulo: "Información",
                        nota: "Para cargos de tipo Servicio, la cantidad siempre es 1.",
                        fechaHoraInicio: DateTime.Now);
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
                    // Exit edit mode after successful save
                    cargo.IsEditing = false;

                    // Notify that TotalMonto should be recalculated
                    foreach (var operacion in ViewModel.Operaciones)
                    {
                        if (operacion.Cargos.Contains(cargo))
                        {
                            operacion.OnPropertyChanged(nameof(operacion.TotalMonto));
                            break;
                        }
                    }

                    await _notificacionService.MostrarNotificacionAsync(
                        titulo: "Cargo actualizado",
                        nota: "El cargo se ha actualizado correctamente.",
                        fechaHoraInicio: DateTime.Now);
                }
                else
                {
                    await _notificacionService.MostrarNotificacionAsync(
                        titulo: "Error",
                        nota: "No se pudo actualizar el cargo. Por favor, intente nuevamente.",
                        fechaHoraInicio: DateTime.Now);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al actualizar cargo: {ex.GetType().Name} - {ex.Message}");
                await _notificacionService.MostrarNotificacionAsync(
                    titulo: "Error",
                    nota: "Ocurrió un error al actualizar el cargo. Por favor, intente nuevamente.",
                    fechaHoraInicio: DateTime.Now);
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
                await _notificacionService.MostrarNotificacionAsync(
                    titulo: "Error",
                    nota: "No se puede mostrar la refacción porque el cargo no tiene un ID válido.",
                    fechaHoraInicio: DateTime.Now);
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
                    await _notificacionService.MostrarNotificacionAsync(
                        titulo: "Error",
                        nota: "No se pudo obtener la información del cargo desde el servidor.",
                        fechaHoraInicio: DateTime.Now);
                    return;
                }
                
                System.Diagnostics.Debug.WriteLine($"Cargo obtenido del API: IdCargo={cargoActualizado.IdCargo}, IdRelacionCargo={cargoActualizado.IdRelacionCargo}");

                // Verificar que el cargo actualizado tenga un idRelacionCargo
                if (!cargoActualizado.IdRelacionCargo.HasValue)
                {
                    System.Diagnostics.Debug.WriteLine($"IdRelacionCargo es null para cargo {cargoActualizado.IdCargo} incluso después de consultar el API");
                    await _notificacionService.MostrarNotificacionAsync(
                        titulo: "Error",
                        nota: "No se puede mostrar la refacción porque el cargo no tiene una relación válida. El campo IdRelacionCargo está vacío en el servidor.",
                        fechaHoraInicio: DateTime.Now);
                    return;
                }

                // Crear el UserControl para visualizar la refacción usando el ID de la relación del cargo actualizado
                var viewerControl = new Equipos.RefaccionesViewerUserControl(cargoActualizado.IdRelacionCargo.Value);

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
                await _notificacionService.MostrarNotificacionAsync(
                    titulo: "Error",
                    nota: "No se pudo cargar la información de la refacción. Por favor, intente nuevamente.",
                    fechaHoraInicio: DateTime.Now);
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
                await _notificacionService.MostrarNotificacionAsync(
                    titulo: "No hay cargos",
                    nota: "No se puede generar una cotización porque no hay cargos asociados a esta operación.",
                    fechaHoraInicio: DateTime.Now);
                return;
            }

            try
            {
                // Generar la cotización
                var filePath = await ViewModel.GenerateQuoteAsync(operacion);

                if (!string.IsNullOrEmpty(filePath))
                {
                    // Mostrar diálogo de éxito con opción de abrir el archivo
                    var dialog = new ContentDialog
                    {
                        Title = "Cotización generada",
                        Content = $"La cotización se ha generado exitosamente en:\n\n{filePath}\n\n¿Desea abrir el archivo?",
                        PrimaryButtonText = "Abrir",
                        CloseButtonText = "Cerrar",
                        DefaultButton = ContentDialogButton.Primary,
                        XamlRoot = this.XamlRoot
                    };

                    var result = await dialog.ShowAsync();

                    if (result == ContentDialogResult.Primary)
                    {
                        // Abrir el archivo PDF con la aplicación predeterminada
                        var file = await Windows.Storage.StorageFile.GetFileFromPathAsync(filePath);
                        await Windows.System.Launcher.LaunchFileAsync(file);
                    }

                    await _notificacionService.MostrarNotificacionAsync(
                        titulo: "Cotización generada",
                        nota: "La cotización PDF se ha generado correctamente.",
                        fechaHoraInicio: DateTime.Now);
                }
                else
                {
                    await _notificacionService.MostrarNotificacionAsync(
                        titulo: "Error",
                        nota: "No se pudo generar la cotización. Por favor, intente nuevamente.",
                        fechaHoraInicio: DateTime.Now);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al generar cotización: {ex.GetType().Name} - {ex.Message}");
                await _notificacionService.MostrarNotificacionAsync(
                    titulo: "Error",
                    nota: "Ocurrió un error al generar la cotización. Por favor, intente nuevamente.",
                    fechaHoraInicio: DateTime.Now);
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
                await _notificacionService.MostrarNotificacionAsync(
                    titulo: "No hay cargos",
                    nota: "No se puede generar un reporte porque no hay cargos asociados a esta operación.",
                    fechaHoraInicio: DateTime.Now);
                return;
            }

            try
            {
                // Generar el reporte
                var filePath = await ViewModel.GenerateReporteAsync(operacion);

                if (!string.IsNullOrEmpty(filePath))
                {
                    // Mostrar diálogo de éxito con opción de abrir el archivo
                    var dialog = new ContentDialog
                    {
                        Title = "Reporte generado",
                        Content = $"El reporte se ha generado exitosamente en:\n\n{filePath}\n\n¿Desea abrir el archivo?",
                        PrimaryButtonText = "Abrir",
                        CloseButtonText = "Cerrar",
                        DefaultButton = ContentDialogButton.Primary,
                        XamlRoot = this.XamlRoot
                    };

                    var result = await dialog.ShowAsync();

                    if (result == ContentDialogResult.Primary)
                    {
                        // Abrir el archivo PDF con la aplicación predeterminada
                        var file = await Windows.Storage.StorageFile.GetFileFromPathAsync(filePath);
                        await Windows.System.Launcher.LaunchFileAsync(file);
                    }

                    await _notificacionService.MostrarNotificacionAsync(
                        titulo: "Reporte generado",
                        nota: "El reporte PDF se ha generado correctamente.",
                        fechaHoraInicio: DateTime.Now);
                }
                else
                {
                    await _notificacionService.MostrarNotificacionAsync(
                        titulo: "Error",
                        nota: "No se pudo generar el reporte. Por favor, intente nuevamente.",
                        fechaHoraInicio: DateTime.Now);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al generar reporte: {ex.GetType().Name} - {ex.Message}");
                await _notificacionService.MostrarNotificacionAsync(
                    titulo: "Error",
                    nota: "Ocurrió un error al generar el reporte. Por favor, intente nuevamente.",
                    fechaHoraInicio: DateTime.Now);
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

            if (cargo.IdCargo <= 0)
            {
                await _notificacionService.MostrarNotificacionAsync(
                    titulo: "Error",
                    nota: "El cargo no tiene un ID válido para cargar imágenes.",
                    fechaHoraInicio: DateTime.Now);
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
                var result = await _cargoImageService.UploadImageAsync(cargo.IdCargo, stream, contentType);

                if (result != null)
                {
                    // Agregar la imagen a la colección del cargo
                    cargo.Images.Add(result);
                    cargo.NotifyImagesChanged();

                    await _notificacionService.MostrarNotificacionAsync(
                        titulo: "Imagen cargada",
                        nota: $"La imagen {result.FileName} se ha guardado correctamente.",
                        fechaHoraInicio: DateTime.Now);
                }
                else
                {
                    await _notificacionService.MostrarNotificacionAsync(
                        titulo: "Error",
                        nota: "No se pudo guardar la imagen. Por favor, intente nuevamente.",
                        fechaHoraInicio: DateTime.Now);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cargar imagen: {ex.GetType().Name} - {ex.Message}");
                await _notificacionService.MostrarNotificacionAsync(
                    titulo: "Error",
                    nota: "Ocurrió un error al cargar la imagen. Por favor, intente nuevamente.",
                    fechaHoraInicio: DateTime.Now);
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
                await _notificacionService.MostrarNotificacionAsync(
                    titulo: "Error",
                    nota: "La imagen no tiene un nombre de archivo válido.",
                    fechaHoraInicio: DateTime.Now);
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
                    var success = await _cargoImageService.DeleteImageAsync(image.FileName);

                    if (success)
                    {
                        // Use IdCargo from image to find the parent cargo more efficiently
                        var targetCargo = ViewModel.Operaciones
                            .SelectMany(op => op.Cargos)
                            .FirstOrDefault(c => c.IdCargo == image.IdCargo);

                        if (targetCargo != null)
                        {
                            var imageToRemove = targetCargo.Images.FirstOrDefault(i => i.FileName == image.FileName);
                            if (imageToRemove != null)
                            {
                                targetCargo.Images.Remove(imageToRemove);
                                targetCargo.NotifyImagesChanged();
                            }
                        }

                        await _notificacionService.MostrarNotificacionAsync(
                            titulo: "Imagen eliminada",
                            nota: "La imagen se ha eliminado correctamente.",
                            fechaHoraInicio: DateTime.Now);
                    }
                    else
                    {
                        await _notificacionService.MostrarNotificacionAsync(
                            titulo: "Error",
                            nota: "No se pudo eliminar la imagen. Por favor, intente nuevamente.",
                            fechaHoraInicio: DateTime.Now);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error al eliminar imagen: {ex.GetType().Name} - {ex.Message}");
                    await _notificacionService.MostrarNotificacionAsync(
                        titulo: "Error",
                        nota: "Ocurrió un error al eliminar la imagen. Por favor, intente nuevamente.",
                        fechaHoraInicio: DateTime.Now);
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
            if (cargo.IdCargo <= 0 || cargo.ImagesLoaded || cargo.IsLoadingImages)
                return;

            try
            {
                cargo.IsLoadingImages = true;

                var images = await _cargoImageService.GetImagesAsync(cargo.IdCargo);

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
    }
}
