using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Extensions.DependencyInjection;
using Advance_Control.ViewModels;
using Advance_Control.Services.Notificacion;
using Advance_Control.Services.RelacionOperacionProveedorRefaccion;
using Advance_Control.Services.RelacionesProveedorRefaccion;
using Advance_Control.Services.Proveedores;
using Advance_Control.Views.Equipos;
using Advance_Control.Models;

namespace Advance_Control.Views
{
    /// <summary>
    /// Página para visualizar y gestionar operaciones del sistema
    /// </summary>
    public sealed partial class OperacionesView : Page
    {
        public OperacionesViewModel ViewModel { get; }
        private readonly INotificacionService _notificacionService;
        private readonly IRelacionOperacionProveedorRefaccionService _relacionOperacionProveedorRefaccionService;
        private readonly IRelacionProveedorRefaccionService _relacionProveedorRefaccionService;
        private readonly IProveedorService _proveedorService;

        public OperacionesView()
        {
            // Resolver el ViewModel desde DI
            ViewModel = ((App)Application.Current).Host.Services.GetRequiredService<OperacionesViewModel>();

            // Resolver el servicio de notificaciones desde DI
            _notificacionService = ((App)Application.Current).Host.Services.GetRequiredService<INotificacionService>();

            // Resolver el servicio de relaciones operación-proveedor-refacción desde DI
            _relacionOperacionProveedorRefaccionService = ((App)Application.Current).Host.Services.GetRequiredService<IRelacionOperacionProveedorRefaccionService>();

            // Resolver el servicio de relaciones proveedor-refacción desde DI
            _relacionProveedorRefaccionService = ((App)Application.Current).Host.Services.GetRequiredService<IRelacionProveedorRefaccionService>();

            // Resolver el servicio de proveedores desde DI
            _proveedorService = ((App)Application.Current).Host.Services.GetRequiredService<IProveedorService>();
            
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

                // Load relaciones refaccion when expanding if not already loaded
                if (operacion.Expand && !operacion.RelacionesRefaccionLoaded)
                {
                    await LoadRelacionesRefaccionForOperacionAsync(operacion);
                }
            }
        }

        private async void ToggleExpandButton_Click(object sender, RoutedEventArgs e)
        {
            // Get the OperacionDto from the sender's Tag property
            if (sender is FrameworkElement element && element.Tag is Models.OperacionDto operacion)
            {
                operacion.Expand = !operacion.Expand;

                // Load relaciones refaccion when expanding if not already loaded
                if (operacion.Expand && !operacion.RelacionesRefaccionLoaded)
                {
                    await LoadRelacionesRefaccionForOperacionAsync(operacion);
                }
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

        private async System.Threading.Tasks.Task LoadRelacionesRefaccionForOperacionAsync(OperacionDto operacion)
        {
            if (operacion.IsLoadingRelacionesRefaccion || !operacion.IdOperacion.HasValue)
                return;

            try
            {
                operacion.IsLoadingRelacionesRefaccion = true;
                
                var relaciones = await _relacionOperacionProveedorRefaccionService.GetRelacionesAsync(operacion.IdOperacion.Value);
                
                operacion.RelacionesRefaccion.Clear();
                foreach (var relacion in relaciones)
                {
                    operacion.RelacionesRefaccion.Add(relacion);
                }
                
                operacion.RelacionesRefaccionLoaded = true;
                operacion.NotifyNoRelacionesRefaccionMessageChanged();
            }
            catch (Exception)
            {
                // Log error silently - the UI will show empty list
                operacion.RelacionesRefaccionLoaded = true;
                operacion.NotifyNoRelacionesRefaccionMessageChanged();
            }
            finally
            {
                operacion.IsLoadingRelacionesRefaccion = false;
            }
        }

        private async void NuevaRelacionRefaccion_Click(object sender, RoutedEventArgs e)
        {
            // Obtener la operación desde el Tag del botón
            if (sender is not FrameworkElement element || element.Tag is not OperacionDto operacion)
                return;

            if (!operacion.IdOperacion.HasValue)
                return;

            // Variables para almacenar el proveedor-refacción seleccionado y la lista de relaciones proveedor-refacción
            RelacionProveedorRefaccionDto? selectedProveedorRefaccion = null;
            List<ProveedorDto> allProveedores = new List<ProveedorDto>();
            List<RelacionProveedorRefaccionDto> allRelacionesProveedorRefaccion = new List<RelacionProveedorRefaccionDto>();
            List<RelacionProveedorRefaccionDto> filteredRelacionesProveedorRefaccion = new List<RelacionProveedorRefaccionDto>();

            // Crear los campos de búsqueda
            var proveedorComboBox = new ComboBox
            {
                PlaceholderText = "Seleccionar proveedor",
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(0, 0, 0, 8)
            };

            var marcaFilterTextBox = new TextBox
            {
                PlaceholderText = "Buscar por marca",
                Margin = new Thickness(0, 0, 4, 0)
            };

            var serieFilterTextBox = new TextBox
            {
                PlaceholderText = "Buscar por serie",
                Margin = new Thickness(4, 0, 0, 0)
            };

            var searchButton = new Button
            {
                Content = "Buscar",
                Margin = new Thickness(8, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Bottom
            };

            var searchPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 12),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            searchPanel.Children.Add(marcaFilterTextBox);
            searchPanel.Children.Add(serieFilterTextBox);
            searchPanel.Children.Add(searchButton);

            // Crear el ListView para mostrar proveedor-refacciones
            var proveedorRefaccionListView = new ListView
            {
                Height = 200,
                SelectionMode = ListViewSelectionMode.Single,
                Margin = new Thickness(0, 0, 0, 12)
            };

            // Crear el ProgressRing para la carga
            var loadingRing = new ProgressRing
            {
                Width = 30,
                Height = 30,
                IsActive = true,
                Margin = new Thickness(0, 20, 0, 20)
            };

            var precioTextBox = new TextBox
            {
                PlaceholderText = "Ingrese el precio",
                InputScope = new InputScope { Names = { new InputScopeName(InputScopeNameValue.Number) } },
                Margin = new Thickness(0, 0, 0, 8)
            };

            var notaTextBox = new TextBox
            {
                PlaceholderText = "Ingrese una nota (opcional)",
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                MinHeight = 80,
                MaxHeight = 150,
                Margin = new Thickness(0, 0, 0, 8)
            };

            var dialogContent = new StackPanel
            {
                Spacing = 8,
                MinWidth = 400,
                Children =
                {
                    new TextBlock { Text = "Seleccionar Proveedor:" },
                    proveedorComboBox,
                    new TextBlock { Text = "Buscar Refacción:" },
                    searchPanel,
                    new TextBlock { Text = "Seleccionar Proveedor-Refacción:" },
                    loadingRing,
                    proveedorRefaccionListView,
                    new TextBlock { Text = "Precio:" },
                    precioTextBox,
                    new TextBlock { Text = "Nota:" },
                    notaTextBox
                }
            };

            // Función para actualizar la lista de proveedor-refacciones filtradas en el ListView
            void UpdateProveedorRefaccionListView()
            {
                proveedorRefaccionListView.Items.Clear();
                foreach (var proveedorRefaccion in filteredRelacionesProveedorRefaccion)
                {
                    var itemPanel = new StackPanel
                    {
                        Orientation = Orientation.Vertical,
                        Padding = new Thickness(8, 4, 8, 4)
                    };

                    var marcaText = new TextBlock
                    {
                        Text = proveedorRefaccion.Marca ?? "(Sin marca)",
                        FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                        FontSize = 14
                    };

                    var serieText = new TextBlock
                    {
                        Text = proveedorRefaccion.Serie ?? "(Sin serie)",
                        Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray),
                        FontSize = 12
                    };

                    var precioText = new TextBlock
                    {
                        Text = $"ID Relación: {proveedorRefaccion.IdRelacionProveedor} | Precio: ${proveedorRefaccion.Precio}",
                        Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.DimGray),
                        FontSize = 11
                    };

                    itemPanel.Children.Add(marcaText);
                    itemPanel.Children.Add(serieText);
                    itemPanel.Children.Add(precioText);

                    var listViewItem = new ListViewItem
                    {
                        Content = itemPanel,
                        Tag = proveedorRefaccion
                    };

                    proveedorRefaccionListView.Items.Add(listViewItem);
                }
            }

            // Función para buscar proveedor-refacciones con filtros
            async System.Threading.Tasks.Task SearchProveedorRefaccionesAsync()
            {
                loadingRing.IsActive = true;
                loadingRing.Visibility = Visibility.Visible;
                proveedorRefaccionListView.Visibility = Visibility.Collapsed;

                try
                {
                    var selectedProveedorItem = proveedorComboBox.SelectedItem as ComboBoxItem;
                    if (selectedProveedorItem == null || selectedProveedorItem.Tag == null)
                    {
                        filteredRelacionesProveedorRefaccion = new List<RelacionProveedorRefaccionDto>();
                        UpdateProveedorRefaccionListView();
                        return;
                    }

                    var selectedProveedor = selectedProveedorItem.Tag as ProveedorDto;
                    if (selectedProveedor == null)
                    {
                        filteredRelacionesProveedorRefaccion = new List<RelacionProveedorRefaccionDto>();
                        UpdateProveedorRefaccionListView();
                        return;
                    }

                    // Obtener todas las relaciones del proveedor seleccionado
                    allRelacionesProveedorRefaccion = await _relacionProveedorRefaccionService.GetRelacionesAsync(selectedProveedor.IdProveedor, 0);

                    // Filtrar por marca y serie si es necesario
                    filteredRelacionesProveedorRefaccion = allRelacionesProveedorRefaccion;
                    if (!string.IsNullOrWhiteSpace(marcaFilterTextBox.Text))
                    {
                        filteredRelacionesProveedorRefaccion = filteredRelacionesProveedorRefaccion
                            .Where(r => r.Marca != null && r.Marca.Contains(marcaFilterTextBox.Text, StringComparison.OrdinalIgnoreCase))
                            .ToList();
                    }
                    if (!string.IsNullOrWhiteSpace(serieFilterTextBox.Text))
                    {
                        filteredRelacionesProveedorRefaccion = filteredRelacionesProveedorRefaccion
                            .Where(r => r.Serie != null && r.Serie.Contains(serieFilterTextBox.Text, StringComparison.OrdinalIgnoreCase))
                            .ToList();
                    }

                    UpdateProveedorRefaccionListView();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error al cargar proveedor-refacciones: {ex.GetType().Name} - {ex.Message}");
                    filteredRelacionesProveedorRefaccion = new List<RelacionProveedorRefaccionDto>();
                    UpdateProveedorRefaccionListView();
                }
                finally
                {
                    loadingRing.IsActive = false;
                    loadingRing.Visibility = Visibility.Collapsed;
                    proveedorRefaccionListView.Visibility = Visibility.Visible;
                }
            }

            // Función para cargar proveedores inicialmente
            async System.Threading.Tasks.Task LoadProveedoresInitiallyAsync()
            {
                try
                {
                    proveedorComboBox.IsEnabled = false;
                    var query = new ProveedorQueryDto();
                    allProveedores = await _proveedorService.GetProveedoresAsync(query);

                    proveedorComboBox.Items.Clear();
                    foreach (var proveedor in allProveedores)
                    {
                        var item = new ComboBoxItem
                        {
                            Content = $"{proveedor.RazonSocial} ({proveedor.IdProveedor})",
                            Tag = proveedor
                        };
                        proveedorComboBox.Items.Add(item);
                    }

                    proveedorComboBox.IsEnabled = true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error al cargar proveedores: {ex.GetType().Name} - {ex.Message}");
                    proveedorComboBox.IsEnabled = true;
                }
            }

            // Manejar el cambio de selección del proveedor
            proveedorComboBox.SelectionChanged += async (s, args) =>
            {
                await SearchProveedorRefaccionesAsync();
            };

            // Manejar el evento de clic en el botón de búsqueda
            searchButton.Click += async (s, args) =>
            {
                await SearchProveedorRefaccionesAsync();
            };

            // Manejar la selección de proveedor-refacción en el ListView
            proveedorRefaccionListView.SelectionChanged += (s, args) =>
            {
                if (proveedorRefaccionListView.SelectedItem is ListViewItem item && item.Tag is RelacionProveedorRefaccionDto proveedorRefaccion)
                {
                    selectedProveedorRefaccion = proveedorRefaccion;
                }
                else
                {
                    selectedProveedorRefaccion = null;
                }
            };

            var dialog = new ContentDialog
            {
                Title = "Nueva Relación con Refacción",
                Content = dialogContent,
                PrimaryButtonText = "Guardar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            // Cargar proveedores inicialmente
            LoadProveedoresInitiallyAsync();

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                try
                {
                    if (selectedProveedorRefaccion == null || !selectedProveedorRefaccion.IdRelacionProveedor.HasValue)
                    {
                        await _notificacionService.MostrarNotificacionAsync(
                            titulo: "Error",
                            nota: "Debe seleccionar una proveedor-refacción de la lista.",
                            fechaHoraInicio: DateTime.Now);
                        return;
                    }

                    var precio = ParsePrecio(precioTextBox.Text);
                    if (precio == null || precio <= 0)
                    {
                        await _notificacionService.MostrarNotificacionAsync(
                            titulo: "Error",
                            nota: "Debe ingresar un precio válido mayor que 0.",
                            fechaHoraInicio: DateTime.Now);
                        return;
                    }

                    var idProveedorRefaccion = selectedProveedorRefaccion.IdRelacionProveedor.Value;
                    var nota = string.IsNullOrWhiteSpace(notaTextBox.Text) ? null : notaTextBox.Text;

                    var success = await _relacionOperacionProveedorRefaccionService.CreateRelacionAsync(operacion.IdOperacion.Value, idProveedorRefaccion, precio.Value, nota);

                    if (success)
                    {
                        // Recargar las relaciones para actualizar la UI
                        operacion.RelacionesRefaccionLoaded = false;
                        await LoadRelacionesRefaccionForOperacionAsync(operacion);

                        await _notificacionService.MostrarNotificacionAsync(
                            titulo: "Relación creada",
                            nota: $"Relación con la refacción '{selectedProveedorRefaccion.Marca} - {selectedProveedorRefaccion.Serie}' creada correctamente",
                            fechaHoraInicio: DateTime.Now);
                    }
                    else
                    {
                        await _notificacionService.MostrarNotificacionAsync(
                            titulo: "Error",
                            nota: "No se pudo crear la relación. Es posible que ya exista una relación con esta proveedor-refacción.",
                            fechaHoraInicio: DateTime.Now);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error al crear relación refacción: {ex.GetType().Name} - {ex.Message}");

                    await _notificacionService.MostrarNotificacionAsync(
                        titulo: "Error",
                        nota: "Ocurrió un error al crear la relación. Por favor, intente nuevamente.",
                        fechaHoraInicio: DateTime.Now);
                }
            }
        }

        private async void EditRelacionRefaccionButton_Click(object sender, RoutedEventArgs e)
        {
            // Obtener la relación desde el Tag del botón
            if (sender is not FrameworkElement element || element.Tag is not RelacionOperacionProveedorRefaccionDto relacion)
                return;

            if (!relacion.IdRelacionOperacionProveedorRefaccion.HasValue)
                return;

            // Buscar la operación que contiene esta relación
            var operacion = ViewModel.Operaciones.FirstOrDefault(o => o.RelacionesRefaccion.Contains(relacion));
            if (operacion == null)
                return;

            // Crear los campos para editar nota
            var notaTextBox = new TextBox
            {
                Text = relacion.Nota ?? string.Empty,
                PlaceholderText = "Ingrese una nota...",
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                MinHeight = 100,
                MaxHeight = 200
            };

            // Crear el contenido del diálogo
            var dialogContent = new StackPanel
            {
                Spacing = 8,
                Children =
                {
                    new TextBlock
                    {
                        Text = $"Refacción: {relacion.Marca} - {relacion.Serie}",
                        FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
                    },
                    new TextBlock
                    {
                        Text = "Nota:",
                        Margin = new Thickness(0, 8, 0, 4)
                    },
                    notaTextBox
                }
            };

            // Mostrar diálogo para editar
            var dialog = new ContentDialog
            {
                Title = "Editar Relación",
                Content = dialogContent,
                PrimaryButtonText = "Guardar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                try
                {
                    var nuevaNota = notaTextBox.Text;

                    bool notaUpdated = false;

                    // Actualizar nota si cambió
                    if (nuevaNota != relacion.Nota)
                    {
                        notaUpdated = await _relacionOperacionProveedorRefaccionService.UpdateNotaAsync(relacion.IdRelacionOperacionProveedorRefaccion.Value, nuevaNota);
                    }

                    if (notaUpdated)
                    {
                        // Recargar las relaciones para actualizar la UI
                        operacion.RelacionesRefaccionLoaded = false;
                        await LoadRelacionesRefaccionForOperacionAsync(operacion);

                        await _notificacionService.MostrarNotificacionAsync(
                            titulo: "Relación actualizada",
                            nota: "Relación actualizada correctamente",
                            fechaHoraInicio: DateTime.Now);
                    }
                    else
                    {
                        await _notificacionService.MostrarNotificacionAsync(
                            titulo: "Sin cambios",
                            nota: "No se realizaron cambios en la relación.",
                            fechaHoraInicio: DateTime.Now);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error al actualizar relación: {ex.GetType().Name} - {ex.Message}");

                    await _notificacionService.MostrarNotificacionAsync(
                        titulo: "Error",
                        nota: "Ocurrió un error al actualizar la relación. Por favor, intente nuevamente.",
                        fechaHoraInicio: DateTime.Now);
                }
            }
        }

        private async void DeleteRelacionRefaccionButton_Click(object sender, RoutedEventArgs e)
        {
            // Obtener la relación desde el Tag del botón
            if (sender is not FrameworkElement element || element.Tag is not RelacionOperacionProveedorRefaccionDto relacion)
                return;

            if (!relacion.IdRelacionOperacionProveedorRefaccion.HasValue)
                return;

            // Buscar la operación que contiene esta relación
            var operacion = ViewModel.Operaciones.FirstOrDefault(o => o.RelacionesRefaccion.Contains(relacion));
            if (operacion == null)
                return;

            // Mostrar diálogo de confirmación
            var dialog = new ContentDialog
            {
                Title = "Confirmar eliminación",
                Content = $"¿Está seguro de que desea eliminar la relación con la refacción \"{relacion.Marca} - {relacion.Serie}\"?",
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
                    var success = await _relacionOperacionProveedorRefaccionService.DeleteRelacionAsync(relacion.IdRelacionOperacionProveedorRefaccion.Value);

                    if (success)
                    {
                        // Eliminar la relación de la colección local
                        operacion.RelacionesRefaccion.Remove(relacion);
                        operacion.NotifyNoRelacionesRefaccionMessageChanged();

                        await _notificacionService.MostrarNotificacionAsync(
                            titulo: "Relación eliminada",
                            nota: "Relación eliminada correctamente",
                            fechaHoraInicio: DateTime.Now);
                    }
                    else
                    {
                        await _notificacionService.MostrarNotificacionAsync(
                            titulo: "Error",
                            nota: "No se pudo eliminar la relación. Por favor, intente nuevamente.",
                            fechaHoraInicio: DateTime.Now);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error al eliminar relación: {ex.GetType().Name} - {ex.Message}");

                    await _notificacionService.MostrarNotificacionAsync(
                        titulo: "Error",
                        nota: "Ocurrió un error al eliminar la relación. Por favor, intente nuevamente.",
                        fechaHoraInicio: DateTime.Now);
                }
            }
        }

        /// <summary>
        /// Parses a price string value using invariant culture for consistent number parsing.
        /// </summary>
        /// <param name="precioText">The price text to parse</param>
        /// <returns>The parsed price value, or null if the text is empty or invalid</returns>
        private static float? ParsePrecio(string precioText)
        {
            if (string.IsNullOrWhiteSpace(precioText))
            {
                return null;
            }

            if (float.TryParse(precioText, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedPrecio))
            {
                return parsedPrecio;
            }

            // Also try parsing with current culture as fallback for user-friendly input
            if (float.TryParse(precioText, NumberStyles.Any, CultureInfo.CurrentCulture, out parsedPrecio))
            {
                return parsedPrecio;
            }

            return null;
        }
    }
}
