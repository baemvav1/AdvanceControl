using System;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Extensions.DependencyInjection;
using Advance_Control.ViewModels;
using Advance_Control.Services.Notificacion;
using Advance_Control.Services.Cargos;
using Advance_Control.Views.Equipos;
using Advance_Control.Models;
using CommunityToolkit.WinUI.UI.Controls;

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

        public OperacionesView()
        {
            // Resolver el ViewModel desde DI
            ViewModel = ((App)Application.Current).Host.Services.GetRequiredService<OperacionesViewModel>();

            // Resolver el servicio de notificaciones desde DI
            _notificacionService = ((App)Application.Current).Host.Services.GetRequiredService<INotificacionService>();
            
            // Resolver el servicio de cargos desde DI
            _cargoService = ((App)Application.Current).Host.Services.GetRequiredService<ICargoService>();
            
            this.InitializeComponent();
            
            // Establecer el DataContext para los bindings
            this.DataContext = ViewModel;
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

        private async System.Threading.Tasks.Task LoadCargosForOperacionAsync(Models.OperacionDto operacion)
        {
            if (!operacion.IdOperacion.HasValue)
                return;

            try
            {
                // Only load cargos if they haven't been loaded yet
                if (!operacion.CargosLoaded)
                {
                    var query = new CargoEditDto
                    {
                        IdRelacionCargo = operacion.IdOperacion.Value
                    };

                    var cargos = await _cargoService.GetCargosAsync(query);
                    
                    operacion.Cargos.Clear();
                    foreach (var cargo in cargos)
                    {
                        operacion.Cargos.Add(cargo);
                    }
                    
                    operacion.CargosLoaded = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cargar cargos: {ex.GetType().Name} - {ex.Message}");
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

            // Crear los campos del formulario
            var idTipoCargoNumberBox = new NumberBox
            {
                PlaceholderText = "ID Tipo Cargo (requerido)",
                Minimum = 0,
                SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline,
                Margin = new Thickness(0, 0, 0, 8)
            };

            var idRelacionCargoNumberBox = new NumberBox
            {
                PlaceholderText = "ID Relación Cargo (requerido)",
                Minimum = 0,
                SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline,
                Margin = new Thickness(0, 0, 0, 8)
            };

            var montoNumberBox = new NumberBox
            {
                PlaceholderText = "Monto (requerido)",
                Minimum = 0,
                SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline,
                Margin = new Thickness(0, 0, 0, 8)
            };

            var notaTextBox = new TextBox
            {
                PlaceholderText = "Nota (opcional)",
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                MinHeight = 60,
                MaxHeight = 120,
                Margin = new Thickness(0, 0, 0, 8)
            };

            var dialogContent = new ScrollViewer
            {
                Content = new StackPanel
                {
                    Spacing = 8,
                    Children =
                    {
                        new TextBlock { Text = "ID Tipo Cargo:", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold },
                        idTipoCargoNumberBox,
                        new TextBlock { Text = "ID Relación Cargo:", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold },
                        idRelacionCargoNumberBox,
                        new TextBlock { Text = "Monto:", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold },
                        montoNumberBox,
                        new TextBlock { Text = "Nota:" },
                        notaTextBox
                    }
                },
                MaxHeight = 400
            };

            var dialog = new ContentDialog
            {
                Title = "Agregar Cargo",
                Content = dialogContent,
                PrimaryButtonText = "Agregar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                // Validar campos requeridos
                if (double.IsNaN(idTipoCargoNumberBox.Value))
                {
                    await _notificacionService.MostrarNotificacionAsync(
                        titulo: "Validación",
                        nota: "El ID Tipo Cargo es obligatorio",
                        fechaHoraInicio: DateTime.Now);
                    return;
                }

                if (double.IsNaN(idRelacionCargoNumberBox.Value))
                {
                    await _notificacionService.MostrarNotificacionAsync(
                        titulo: "Validación",
                        nota: "El ID Relación Cargo es obligatorio",
                        fechaHoraInicio: DateTime.Now);
                    return;
                }

                if (double.IsNaN(montoNumberBox.Value))
                {
                    await _notificacionService.MostrarNotificacionAsync(
                        titulo: "Validación",
                        nota: "El Monto es obligatorio",
                        fechaHoraInicio: DateTime.Now);
                    return;
                }

                try
                {
                    // Crear el cargo
                    var query = new CargoEditDto
                    {
                        IdTipoCargo = Convert.ToInt32(Math.Round(idTipoCargoNumberBox.Value)),
                        IdOperacion = operacion.IdOperacion.Value,
                        IdRelacionCargo = Convert.ToInt32(Math.Round(idRelacionCargoNumberBox.Value)),
                        Monto = montoNumberBox.Value,
                        Nota = string.IsNullOrWhiteSpace(notaTextBox.Text) ? null : notaTextBox.Text.Trim()
                    };

                    var newCargo = await _cargoService.CreateCargoAsync(query);

                    if (newCargo != null)
                    {
                        operacion.Cargos.Add(newCargo);
                        await _notificacionService.MostrarNotificacionAsync(
                            titulo: "Cargo agregado",
                            nota: "El cargo se ha agregado correctamente.",
                            fechaHoraInicio: DateTime.Now);
                    }
                    else
                    {
                        await _notificacionService.MostrarNotificacionAsync(
                            titulo: "Error",
                            nota: "No se pudo agregar el cargo. Por favor, intente nuevamente.",
                            fechaHoraInicio: DateTime.Now);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error al agregar cargo: {ex.GetType().Name} - {ex.Message}");
                    await _notificacionService.MostrarNotificacionAsync(
                        titulo: "Error",
                        nota: "Ocurrió un error al agregar el cargo. Por favor, intente nuevamente.",
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

        private async void CargosDataGrid_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key != Windows.System.VirtualKey.Enter)
                return;

            // Obtener el DataGrid
            if (sender is not DataGrid dataGrid)
                return;

            // Obtener la operación desde el Tag del DataGrid
            if (dataGrid.Tag is not Models.OperacionDto operacion)
                return;

            // Obtener el cargo seleccionado
            if (dataGrid.SelectedItem is not Models.CargoDto cargo)
                return;

            if (cargo.IdCargo <= 0)
                return;

            try
            {
                // Actualizar el cargo
                var query = new CargoEditDto
                {
                    IdCargo = cargo.IdCargo,
                    IdTipoCargo = cargo.IdTipoCargo,
                    IdRelacionCargo = cargo.IdRelacionCargo,
                    Monto = cargo.Monto,
                    Nota = cargo.Nota
                };

                var success = await _cargoService.UpdateCargoAsync(query);

                if (success)
                {
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
    }
}
