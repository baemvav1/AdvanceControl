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
using Advance_Control.Services.RelacionesProveedorRefaccion;
using Advance_Control.Services.Refacciones;
using Advance_Control.Models;

namespace Advance_Control.Views
{
    /// <summary>
    /// Página para visualizar y gestionar proveedores del sistema
    /// </summary>
    public sealed partial class ProveedoresView : Page
    {
        public ProveedoresViewModel ViewModel { get; }
        private readonly INotificacionService _notificacionService;
        private readonly IRelacionProveedorRefaccionService _relacionProveedorRefaccionService;
        private readonly IRefaccionService _refaccionService;

        public ProveedoresView()
        {
            // Resolver el ViewModel desde DI
            ViewModel = ((App)Application.Current).Host.Services.GetRequiredService<ProveedoresViewModel>();
            
            // Resolver el servicio de notificaciones desde DI
            _notificacionService = ((App)Application.Current).Host.Services.GetRequiredService<INotificacionService>();
            
            // Resolver el servicio de relaciones proveedor-refacción desde DI
            _relacionProveedorRefaccionService = ((App)Application.Current).Host.Services.GetRequiredService<IRelacionProveedorRefaccionService>();
            
            // Resolver el servicio de refacciones desde DI
            _refaccionService = ((App)Application.Current).Host.Services.GetRequiredService<IRefaccionService>();
            
            this.InitializeComponent();
            
            // Establecer el DataContext para los bindings
            this.DataContext = ViewModel;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            
            // Cargar los proveedores cuando se navega a esta página
            await ViewModel.LoadProveedoresAsync();
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.LoadProveedoresAsync();
        }

        private async void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.ClearFiltersAsync();
        }

        private async void HeadGrid_Tapped(object sender, TappedRoutedEventArgs e)
        {
            // Get the ProveedorDto from the sender's Tag property
            if (sender is FrameworkElement element && element.Tag is Models.ProveedorDto proveedor)
            {
                proveedor.Expand = !proveedor.Expand;
                
                // Load relaciones refaccion when expanding if not already loaded
                if (proveedor.Expand && !proveedor.RelacionesRefaccionLoaded)
                {
                    await LoadRelacionesRefaccionForProveedorAsync(proveedor);
                }
            }
        }

        private async void ToggleExpandButton_Click(object sender, RoutedEventArgs e)
        {
            // Get the ProveedorDto from the sender's Tag property
            if (sender is FrameworkElement element && element.Tag is Models.ProveedorDto proveedor)
            {
                proveedor.Expand = !proveedor.Expand;
                
                // Load relaciones refaccion when expanding if not already loaded
                if (proveedor.Expand && !proveedor.RelacionesRefaccionLoaded)
                {
                    await LoadRelacionesRefaccionForProveedorAsync(proveedor);
                }
            }
        }

        private async System.Threading.Tasks.Task LoadRelacionesRefaccionForProveedorAsync(ProveedorDto proveedor)
        {
            if (proveedor.IsLoadingRelacionesRefaccion)
                return;

            try
            {
                proveedor.IsLoadingRelacionesRefaccion = true;
                
                var relaciones = await _relacionProveedorRefaccionService.GetRelacionesAsync(proveedor.IdProveedor, 0);
                
                proveedor.RelacionesRefaccion.Clear();
                foreach (var relacion in relaciones)
                {
                    proveedor.RelacionesRefaccion.Add(relacion);
                }
                
                proveedor.RelacionesRefaccionLoaded = true;
                proveedor.NotifyNoRelacionesRefaccionMessageChanged();
            }
            catch (Exception)
            {
                // Log error silently - the UI will show empty list
                proveedor.RelacionesRefaccionLoaded = true;
                proveedor.NotifyNoRelacionesRefaccionMessageChanged();
            }
            finally
            {
                proveedor.IsLoadingRelacionesRefaccion = false;
            }
        }

        private async void NuevaRelacionRefaccion_Click(object sender, RoutedEventArgs e)
        {
            // Obtener el proveedor desde el Tag del botón
            if (sender is not FrameworkElement element || element.Tag is not ProveedorDto proveedor)
                return;

            // Variables para almacenar la refacción seleccionada y la lista de refacciones
            RefaccionDto? selectedRefaccion = null;
            List<RefaccionDto> allRefacciones = new List<RefaccionDto>();
            List<RefaccionDto> filteredRefacciones = new List<RefaccionDto>();

            // Crear los campos de búsqueda
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

            // Crear el ListView para mostrar refacciones
            var refaccionesListView = new ListView
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
                    new TextBlock { Text = "Buscar Refacción:" },
                    searchPanel,
                    new TextBlock { Text = "Seleccionar Refacción:" },
                    loadingRing,
                    refaccionesListView,
                    new TextBlock { Text = "Precio:" },
                    precioTextBox,
                    new TextBlock { Text = "Nota:" },
                    notaTextBox
                }
            };

            // Función para actualizar la lista de refacciones filtradas en el ListView
            void UpdateRefaccionesListView()
            {
                refaccionesListView.Items.Clear();
                foreach (var refaccion in filteredRefacciones)
                {
                    var itemPanel = new StackPanel
                    {
                        Orientation = Orientation.Vertical,
                        Padding = new Thickness(8, 4, 8, 4)
                    };

                    var marcaText = new TextBlock
                    {
                        Text = refaccion.Marca ?? "(Sin marca)",
                        FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                        FontSize = 14
                    };

                    var serieText = new TextBlock
                    {
                        Text = refaccion.Serie ?? "(Sin serie)",
                        Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray),
                        FontSize = 12
                    };

                    var idText = new TextBlock
                    {
                        Text = $"ID: {refaccion.IdRefaccion} | Costo: ${refaccion.Costo}",
                        Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.DimGray),
                        FontSize = 11
                    };

                    itemPanel.Children.Add(marcaText);
                    itemPanel.Children.Add(serieText);
                    itemPanel.Children.Add(idText);

                    var listViewItem = new ListViewItem
                    {
                        Content = itemPanel,
                        Tag = refaccion
                    };

                    refaccionesListView.Items.Add(listViewItem);
                }
            }

            // Función para buscar refacciones con filtros
            async System.Threading.Tasks.Task SearchRefaccionesAsync()
            {
                loadingRing.IsActive = true;
                loadingRing.Visibility = Visibility.Visible;
                refaccionesListView.Visibility = Visibility.Collapsed;

                try
                {
                    var query = new RefaccionQueryDto
                    {
                        Marca = string.IsNullOrWhiteSpace(marcaFilterTextBox.Text) ? null : marcaFilterTextBox.Text,
                        Serie = string.IsNullOrWhiteSpace(serieFilterTextBox.Text) ? null : serieFilterTextBox.Text
                    };

                    allRefacciones = await _refaccionService.GetRefaccionesAsync(query);
                    filteredRefacciones = allRefacciones;
                    UpdateRefaccionesListView();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error al cargar refacciones: {ex.GetType().Name} - {ex.Message}");
                    filteredRefacciones = new List<RefaccionDto>();
                    UpdateRefaccionesListView();
                }
                finally
                {
                    loadingRing.IsActive = false;
                    loadingRing.Visibility = Visibility.Collapsed;
                    refaccionesListView.Visibility = Visibility.Visible;
                }
            }

            // Función para iniciar la carga de refacciones de forma segura
            async void LoadRefaccionesInitiallyAsync()
            {
                try
                {
                    await SearchRefaccionesAsync();
                }
                catch
                {
                    // Exceptions are already handled in SearchRefaccionesAsync
                }
            }

            // Manejar el evento de clic en el botón de búsqueda
            searchButton.Click += async (s, args) =>
            {
                await SearchRefaccionesAsync();
            };

            // Manejar la selección de refacción en el ListView
            refaccionesListView.SelectionChanged += (s, args) =>
            {
                if (refaccionesListView.SelectedItem is ListViewItem item && item.Tag is RefaccionDto refaccion)
                {
                    selectedRefaccion = refaccion;
                }
                else
                {
                    selectedRefaccion = null;
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

            // Cargar refacciones inicialmente
            LoadRefaccionesInitiallyAsync();

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                try
                {
                    if (selectedRefaccion == null)
                    {
                        await _notificacionService.MostrarNotificacionAsync(
                            titulo: "Error",
                            nota: "Debe seleccionar una refacción de la lista.",
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

                    var idRefaccion = selectedRefaccion.IdRefaccion;
                    var nota = string.IsNullOrWhiteSpace(notaTextBox.Text) ? null : notaTextBox.Text;

                    var success = await _relacionProveedorRefaccionService.CreateRelacionAsync(proveedor.IdProveedor, idRefaccion, precio.Value, nota);

                    if (success)
                    {
                        // Recargar las relaciones para actualizar la UI
                        proveedor.RelacionesRefaccionLoaded = false;
                        await LoadRelacionesRefaccionForProveedorAsync(proveedor);

                        await _notificacionService.MostrarNotificacionAsync(
                            titulo: "Relación creada",
                            nota: $"Relación con la refacción '{selectedRefaccion.Marca} - {selectedRefaccion.Serie}' creada correctamente",
                            fechaHoraInicio: DateTime.Now);
                    }
                    else
                    {
                        await _notificacionService.MostrarNotificacionAsync(
                            titulo: "Error",
                            nota: "No se pudo crear la relación. Es posible que ya exista una relación con esta refacción.",
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
            if (sender is not FrameworkElement element || element.Tag is not RelacionProveedorRefaccionDto relacion)
                return;

            // Buscar el proveedor que contiene esta relación
            var proveedor = ViewModel.Proveedores.FirstOrDefault(p => p.RelacionesRefaccion.Contains(relacion));
            if (proveedor == null)
                return;

            // Crear los campos para editar precio y nota
            var precioTextBox = new TextBox
            {
                Text = relacion.Precio?.ToString() ?? string.Empty,
                PlaceholderText = "Ingrese el precio",
                InputScope = new InputScope { Names = { new InputScopeName(InputScopeNameValue.Number) } },
                Margin = new Thickness(0, 0, 0, 8)
            };

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
                        Text = "Precio:",
                        Margin = new Thickness(0, 8, 0, 4)
                    },
                    precioTextBox,
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
                    var nuevoPrecio = ParsePrecio(precioTextBox.Text);
                    var nuevaNota = notaTextBox.Text;

                    bool precioUpdated = false;
                    bool notaUpdated = false;

                    // Actualizar precio si cambió y es válido
                    if (nuevoPrecio != null && nuevoPrecio > 0 && nuevoPrecio != relacion.Precio)
                    {
                        precioUpdated = await _relacionProveedorRefaccionService.UpdatePrecioAsync(relacion.IdRelacionProveedor, nuevoPrecio.Value);
                    }

                    // Actualizar nota si cambió
                    if (nuevaNota != relacion.Nota)
                    {
                        notaUpdated = await _relacionProveedorRefaccionService.UpdateNotaAsync(relacion.IdRelacionProveedor, nuevaNota);
                    }

                    if (precioUpdated || notaUpdated)
                    {
                        // Recargar las relaciones para actualizar la UI
                        proveedor.RelacionesRefaccionLoaded = false;
                        await LoadRelacionesRefaccionForProveedorAsync(proveedor);

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
            if (sender is not FrameworkElement element || element.Tag is not RelacionProveedorRefaccionDto relacion)
                return;

            // Buscar el proveedor que contiene esta relación
            var proveedor = ViewModel.Proveedores.FirstOrDefault(p => p.RelacionesRefaccion.Contains(relacion));
            if (proveedor == null)
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
                    var success = await _relacionProveedorRefaccionService.DeleteRelacionAsync(relacion.IdRelacionProveedor);

                    if (success)
                    {
                        // Eliminar la relación de la colección local
                        proveedor.RelacionesRefaccion.Remove(relacion);
                        proveedor.NotifyNoRelacionesRefaccionMessageChanged();

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
        private static double? ParsePrecio(string precioText)
        {
            if (string.IsNullOrWhiteSpace(precioText))
            {
                return null;
            }

            if (double.TryParse(precioText, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedPrecio))
            {
                return parsedPrecio;
            }

            // Also try parsing with current culture as fallback for user-friendly input
            if (double.TryParse(precioText, NumberStyles.Any, CultureInfo.CurrentCulture, out parsedPrecio))
            {
                return parsedPrecio;
            }

            return null;
        }
    }
}
