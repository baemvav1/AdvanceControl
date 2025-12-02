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
using Advance_Control.Services.RelacionesRefaccionEquipo;
using Advance_Control.Services.Equipos;
using Advance_Control.Models;

namespace Advance_Control.Views
{
    /// <summary>
    /// Página para visualizar y gestionar refacciones
    /// </summary>
    public sealed partial class RefaaccionView : Page
    {
        public RefaccionesViewModel ViewModel { get; }
        private readonly INotificacionService _notificacionService;
        private readonly IRelacionRefaccionEquipoService _relacionRefaccionEquipoService;
        private readonly IEquipoService _equipoService;

        public RefaaccionView()
        {
            // Resolver el ViewModel desde DI
            ViewModel = ((App)Application.Current).Host.Services.GetRequiredService<RefaccionesViewModel>();
            
            // Resolver el servicio de notificaciones desde DI
            _notificacionService = ((App)Application.Current).Host.Services.GetRequiredService<INotificacionService>();
            
            // Resolver el servicio de relaciones refacción-equipo desde DI
            _relacionRefaccionEquipoService = ((App)Application.Current).Host.Services.GetRequiredService<IRelacionRefaccionEquipoService>();
            
            // Resolver el servicio de equipos desde DI
            _equipoService = ((App)Application.Current).Host.Services.GetRequiredService<IEquipoService>();
            
            this.InitializeComponent();
            
            // Establecer el DataContext para los bindings
            this.DataContext = ViewModel;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            
            // Cargar las refacciones cuando se navega a esta página
            await ViewModel.LoadRefaccionesAsync();
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.LoadRefaccionesAsync();
        }

        private async void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.ClearFiltersAsync();
        }

        private async void NuevoButton_Click(object sender, RoutedEventArgs e)
        {
            // Crear los campos del formulario
            var marcaTextBox = new TextBox
            {
                PlaceholderText = "Ingrese la marca",
                Margin = new Thickness(0, 0, 0, 8)
            };

            var serieTextBox = new TextBox
            {
                PlaceholderText = "Ingrese la serie",
                Margin = new Thickness(0, 0, 0, 8)
            };

            var costoTextBox = new TextBox
            {
                PlaceholderText = "Ingrese el costo",
                InputScope = new InputScope { Names = { new InputScopeName(InputScopeNameValue.Number) } },
                Margin = new Thickness(0, 0, 0, 8)
            };

            var descripcionTextBox = new TextBox
            {
                PlaceholderText = "Ingrese la descripción",
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                MinHeight = 100,
                MaxHeight = 200,
                Margin = new Thickness(0, 0, 0, 8)
            };

            var estatusCheckBox = new CheckBox
            {
                Content = "Activo",
                IsChecked = true
            };

            var dialogContent = new StackPanel
            {
                Spacing = 8,
                Children =
                {
                    new TextBlock { Text = "Marca:" },
                    marcaTextBox,
                    new TextBlock { Text = "Serie:" },
                    serieTextBox,
                    new TextBlock { Text = "Costo:" },
                    costoTextBox,
                    new TextBlock { Text = "Descripción:" },
                    descripcionTextBox,
                    estatusCheckBox
                }
            };

            var dialog = new ContentDialog
            {
                Title = "Nueva Refacción",
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
                    var costo = ParseCosto(costoTextBox.Text);

                    var success = await ViewModel.CreateRefaccionAsync(
                        string.IsNullOrWhiteSpace(marcaTextBox.Text) ? null : marcaTextBox.Text,
                        string.IsNullOrWhiteSpace(serieTextBox.Text) ? null : serieTextBox.Text,
                        costo,
                        string.IsNullOrWhiteSpace(descripcionTextBox.Text) ? null : descripcionTextBox.Text,
                        estatusCheckBox.IsChecked ?? true
                    );

                    if (success)
                    {
                        await _notificacionService.MostrarNotificacionAsync(
                            titulo: "Refacción creada",
                            nota: "Refacción creada correctamente",
                            fechaHoraInicio: DateTime.Now);
                    }
                    else
                    {
                        await _notificacionService.MostrarNotificacionAsync(
                            titulo: "Error",
                            nota: "No se pudo crear la refacción. Por favor, intente nuevamente.",
                            fechaHoraInicio: DateTime.Now);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error al crear refacción: {ex.GetType().Name} - {ex.Message}");
                    
                    await _notificacionService.MostrarNotificacionAsync(
                        titulo: "Error",
                        nota: "Ocurrió un error al crear la refacción. Por favor, intente nuevamente.",
                        fechaHoraInicio: DateTime.Now);
                }
            }
        }

        private async void HeadGrid_Tapped(object sender, TappedRoutedEventArgs e)
        {
            // Get the RefaccionDto from the sender's Tag property
            if (sender is FrameworkElement element && element.Tag is RefaccionDto refaccion)
            {
                refaccion.Expand = !refaccion.Expand;
                
                // Load relaciones equipo when expanding if not already loaded
                if (refaccion.Expand && !refaccion.RelacionesEquipoLoaded)
                {
                    await LoadRelacionesEquipoForRefaccionAsync(refaccion);
                }
            }
        }

        private async void ToggleExpandButton_Click(object sender, RoutedEventArgs e)
        {
            // Get the RefaccionDto from the sender's Tag property
            if (sender is FrameworkElement element && element.Tag is RefaccionDto refaccion)
            {
                refaccion.Expand = !refaccion.Expand;
                
                // Load relaciones equipo when expanding if not already loaded
                if (refaccion.Expand && !refaccion.RelacionesEquipoLoaded)
                {
                    await LoadRelacionesEquipoForRefaccionAsync(refaccion);
                }
            }
        }

        private async System.Threading.Tasks.Task LoadRelacionesEquipoForRefaccionAsync(RefaccionDto refaccion)
        {
            if (refaccion.IsLoadingRelacionesEquipo)
                return;

            try
            {
                refaccion.IsLoadingRelacionesEquipo = true;
                
                var relaciones = await _relacionRefaccionEquipoService.GetRelacionesAsync(refaccion.IdRefaccion, 0);
                
                refaccion.RelacionesEquipo.Clear();
                foreach (var relacion in relaciones)
                {
                    refaccion.RelacionesEquipo.Add(relacion);
                }
                
                refaccion.RelacionesEquipoLoaded = true;
                refaccion.NotifyNoRelacionesEquipoMessageChanged();
            }
            catch (Exception)
            {
                // Log error silently - the UI will show empty list
                refaccion.RelacionesEquipoLoaded = true;
                refaccion.NotifyNoRelacionesEquipoMessageChanged();
            }
            finally
            {
                refaccion.IsLoadingRelacionesEquipo = false;
            }
        }

        private async void DeleteRefaccionButton_Click(object sender, RoutedEventArgs e)
        {
            // Obtener la refacción desde el Tag del botón
            if (sender is not FrameworkElement element || element.Tag is not RefaccionDto refaccion)
                return;

            // Mostrar diálogo de confirmación
            var dialog = new ContentDialog
            {
                Title = "Confirmar eliminación",
                Content = $"¿Está seguro de que desea eliminar la refacción \"{refaccion.Marca} - {refaccion.Serie}\"?",
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
                    var success = await ViewModel.DeleteRefaccionAsync(refaccion.IdRefaccion);

                    if (success)
                    {
                        await _notificacionService.MostrarNotificacionAsync(
                            titulo: "Refacción eliminada",
                            nota: "Refacción eliminada correctamente",
                            fechaHoraInicio: DateTime.Now);
                    }
                    else
                    {
                        await _notificacionService.MostrarNotificacionAsync(
                            titulo: "Error",
                            nota: "No se pudo eliminar la refacción. Por favor, intente nuevamente.",
                            fechaHoraInicio: DateTime.Now);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error al eliminar refacción: {ex.GetType().Name} - {ex.Message}");
                    
                    await _notificacionService.MostrarNotificacionAsync(
                        titulo: "Error",
                        nota: "Ocurrió un error al eliminar la refacción. Por favor, intente nuevamente.",
                        fechaHoraInicio: DateTime.Now);
                }
            }
        }

        private async void EditRefaccionButton_Click(object sender, RoutedEventArgs e)
        {
            // Obtener la refacción desde el Tag del botón
            if (sender is not FrameworkElement element || element.Tag is not RefaccionDto refaccion)
                return;

            // Crear los campos del formulario con los valores actuales
            var marcaTextBox = new TextBox
            {
                Text = refaccion.Marca ?? string.Empty,
                PlaceholderText = "Ingrese la marca",
                Margin = new Thickness(0, 0, 0, 8)
            };

            var serieTextBox = new TextBox
            {
                Text = refaccion.Serie ?? string.Empty,
                PlaceholderText = "Ingrese la serie",
                Margin = new Thickness(0, 0, 0, 8)
            };

            var costoTextBox = new TextBox
            {
                Text = refaccion.Costo?.ToString() ?? string.Empty,
                PlaceholderText = "Ingrese el costo",
                InputScope = new InputScope { Names = { new InputScopeName(InputScopeNameValue.Number) } },
                Margin = new Thickness(0, 0, 0, 8)
            };

            var descripcionTextBox = new TextBox
            {
                Text = refaccion.Descripcion ?? string.Empty,
                PlaceholderText = "Ingrese la descripción",
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                MinHeight = 100,
                MaxHeight = 200,
                Margin = new Thickness(0, 0, 0, 8)
            };

            var estatusCheckBox = new CheckBox
            {
                Content = "Activo",
                IsChecked = refaccion.Estatus ?? true
            };

            var dialogContent = new StackPanel
            {
                Spacing = 8,
                Children =
                {
                    new TextBlock { Text = "Marca:" },
                    marcaTextBox,
                    new TextBlock { Text = "Serie:" },
                    serieTextBox,
                    new TextBlock { Text = "Costo:" },
                    costoTextBox,
                    new TextBlock { Text = "Descripción:" },
                    descripcionTextBox,
                    estatusCheckBox
                }
            };

            var dialog = new ContentDialog
            {
                Title = "Editar Refacción",
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
                    var costo = ParseCosto(costoTextBox.Text);

                    var updateData = new RefaccionQueryDto
                    {
                        IdRefaccion = refaccion.IdRefaccion,
                        Marca = string.IsNullOrWhiteSpace(marcaTextBox.Text) ? null : marcaTextBox.Text,
                        Serie = string.IsNullOrWhiteSpace(serieTextBox.Text) ? null : serieTextBox.Text,
                        Costo = costo,
                        Descripcion = string.IsNullOrWhiteSpace(descripcionTextBox.Text) ? null : descripcionTextBox.Text,
                        Estatus = estatusCheckBox.IsChecked ?? true
                    };

                    var success = await ViewModel.UpdateRefaccionAsync(refaccion.IdRefaccion, updateData);

                    if (success)
                    {
                        await _notificacionService.MostrarNotificacionAsync(
                            titulo: "Refacción actualizada",
                            nota: "Refacción actualizada correctamente",
                            fechaHoraInicio: DateTime.Now);
                    }
                    else
                    {
                        await _notificacionService.MostrarNotificacionAsync(
                            titulo: "Error",
                            nota: "No se pudo actualizar la refacción. Por favor, intente nuevamente.",
                            fechaHoraInicio: DateTime.Now);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error al actualizar refacción: {ex.GetType().Name} - {ex.Message}");
                    
                    await _notificacionService.MostrarNotificacionAsync(
                        titulo: "Error",
                        nota: "Ocurrió un error al actualizar la refacción. Por favor, intente nuevamente.",
                        fechaHoraInicio: DateTime.Now);
                }
            }
        }

        /// <summary>
        /// Parses a cost string value using invariant culture for consistent number parsing.
        /// </summary>
        /// <param name="costoText">The cost text to parse</param>
        /// <returns>The parsed cost value, or null if the text is empty or invalid</returns>
        private static double? ParseCosto(string costoText)
        {
            if (string.IsNullOrWhiteSpace(costoText))
            {
                return null;
            }

            if (double.TryParse(costoText, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedCosto))
            {
                return parsedCosto;
            }

            // Also try parsing with current culture as fallback for user-friendly input
            if (double.TryParse(costoText, NumberStyles.Any, CultureInfo.CurrentCulture, out parsedCosto))
            {
                return parsedCosto;
            }

            return null;
        }

        private async void NuevaRelacionEquipo_Click(object sender, RoutedEventArgs e)
        {
            // Obtener la refacción desde el Tag del botón
            if (sender is not FrameworkElement element || element.Tag is not RefaccionDto refaccion)
                return;

            // Variables para almacenar el equipo seleccionado y la lista de equipos
            EquipoDto? selectedEquipo = null;
            List<EquipoDto> allEquipos = new List<EquipoDto>();
            List<EquipoDto> filteredEquipos = new List<EquipoDto>();

            // Crear los campos de búsqueda
            var marcaFilterTextBox = new TextBox
            {
                PlaceholderText = "Buscar por marca",
                Margin = new Thickness(0, 0, 4, 0)
            };

            var identificadorFilterTextBox = new TextBox
            {
                PlaceholderText = "Buscar por identificador",
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
            searchPanel.Children.Add(identificadorFilterTextBox);
            searchPanel.Children.Add(searchButton);

            // Crear el ListView para mostrar equipos
            var equiposListView = new ListView
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
                    new TextBlock { Text = "Buscar Equipo:" },
                    searchPanel,
                    new TextBlock { Text = "Seleccionar Equipo:" },
                    loadingRing,
                    equiposListView,
                    new TextBlock { Text = "Nota:" },
                    notaTextBox
                }
            };

            // Función para actualizar la lista de equipos filtrados en el ListView
            void UpdateEquiposListView()
            {
                equiposListView.Items.Clear();
                foreach (var equipo in filteredEquipos)
                {
                    var itemPanel = new StackPanel
                    {
                        Orientation = Orientation.Vertical,
                        Padding = new Thickness(8, 4, 8, 4)
                    };

                    var marcaText = new TextBlock
                    {
                        Text = equipo.Marca ?? "(Sin marca)",
                        FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                        FontSize = 14
                    };

                    var identifierText = new TextBlock
                    {
                        Text = equipo.Identificador ?? "(Sin identificador)",
                        Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray),
                        FontSize = 12
                    };

                    var idText = new TextBlock
                    {
                        Text = $"ID: {equipo.IdEquipo}",
                        Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.DimGray),
                        FontSize = 11
                    };

                    itemPanel.Children.Add(marcaText);
                    itemPanel.Children.Add(identifierText);
                    itemPanel.Children.Add(idText);

                    var listViewItem = new ListViewItem
                    {
                        Content = itemPanel,
                        Tag = equipo
                    };

                    equiposListView.Items.Add(listViewItem);
                }
            }

            // Función para buscar equipos con filtros
            async System.Threading.Tasks.Task SearchEquiposAsync()
            {
                loadingRing.IsActive = true;
                loadingRing.Visibility = Visibility.Visible;
                equiposListView.Visibility = Visibility.Collapsed;

                try
                {
                    var query = new EquipoQueryDto
                    {
                        Marca = string.IsNullOrWhiteSpace(marcaFilterTextBox.Text) ? null : marcaFilterTextBox.Text,
                        Identificador = string.IsNullOrWhiteSpace(identificadorFilterTextBox.Text) ? null : identificadorFilterTextBox.Text
                    };

                    allEquipos = await _equipoService.GetEquiposAsync(query);
                    filteredEquipos = allEquipos;
                    UpdateEquiposListView();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error al cargar equipos: {ex.GetType().Name} - {ex.Message}");
                    filteredEquipos = new List<EquipoDto>();
                    UpdateEquiposListView();
                    // Errors are handled silently - the UI will show an empty list
                    // This is consistent with existing error handling patterns in this codebase
                }
                finally
                {
                    loadingRing.IsActive = false;
                    loadingRing.Visibility = Visibility.Collapsed;
                    equiposListView.Visibility = Visibility.Visible;
                }
            }

            // Función para iniciar la carga de equipos de forma segura
            // No retorna hasta completar o fallar, sin propagar excepciones
            async void LoadEquiposInitiallyAsync()
            {
                try
                {
                    await SearchEquiposAsync();
                }
                catch
                {
                    // Exceptions are already handled in SearchEquiposAsync
                    // This wrapper ensures any unexpected exceptions don't crash the app
                }
            }

            // Manejar el evento de clic en el botón de búsqueda
            searchButton.Click += async (s, args) =>
            {
                await SearchEquiposAsync();
            };

            // Manejar la selección de equipo en el ListView
            equiposListView.SelectionChanged += (s, args) =>
            {
                if (equiposListView.SelectedItem is ListViewItem item && item.Tag is EquipoDto equipo)
                {
                    selectedEquipo = equipo;
                }
                else
                {
                    selectedEquipo = null;
                }
            };

            var dialog = new ContentDialog
            {
                Title = "Nueva Relación con Equipo",
                Content = dialogContent,
                PrimaryButtonText = "Guardar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            // Cargar equipos inicialmente usando el wrapper async void
            LoadEquiposInitiallyAsync();

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                try
                {
                    if (selectedEquipo == null)
                    {
                        await _notificacionService.MostrarNotificacionAsync(
                            titulo: "Error",
                            nota: "Debe seleccionar un equipo de la lista.",
                            fechaHoraInicio: DateTime.Now);
                        return;
                    }

                    var idEquipo = selectedEquipo.IdEquipo;
                    var nota = string.IsNullOrWhiteSpace(notaTextBox.Text) ? null : notaTextBox.Text;

                    var success = await _relacionRefaccionEquipoService.CreateRelacionAsync(refaccion.IdRefaccion, idEquipo, nota);

                    if (success)
                    {
                        // Recargar las relaciones para actualizar la UI
                        refaccion.RelacionesEquipoLoaded = false;
                        await LoadRelacionesEquipoForRefaccionAsync(refaccion);

                        await _notificacionService.MostrarNotificacionAsync(
                            titulo: "Relación creada",
                            nota: $"Relación con el equipo '{selectedEquipo.Marca} - {selectedEquipo.Identificador}' creada correctamente",
                            fechaHoraInicio: DateTime.Now);
                    }
                    else
                    {
                        // Mostrar notificación de error - la relación puede que ya exista
                        await _notificacionService.MostrarNotificacionAsync(
                            titulo: "Error",
                            nota: "No se pudo crear la relación. Es posible que ya exista una relación con este equipo.",
                            fechaHoraInicio: DateTime.Now);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error al crear relación equipo: {ex.GetType().Name} - {ex.Message}");

                    await _notificacionService.MostrarNotificacionAsync(
                        titulo: "Error",
                        nota: "Ocurrió un error al crear la relación. Por favor, intente nuevamente.",
                        fechaHoraInicio: DateTime.Now);
                }
            }
        }

        private async void EditRelacionEquipoButton_Click(object sender, RoutedEventArgs e)
        {
            // Obtener la relación desde el Tag del botón
            if (sender is not FrameworkElement element || element.Tag is not RelacionEquipoDto relacion)
                return;

            // Buscar la refacción que contiene esta relación
            var refaccion = ViewModel.Refacciones.FirstOrDefault(r => r.RelacionesEquipo.Contains(relacion));
            if (refaccion == null)
                return;

            // Crear el TextBox para editar la nota
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
                        Text = $"Equipo: {relacion.Marca} - {relacion.Identificador}",
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

            // Mostrar diálogo para editar la nota
            var dialog = new ContentDialog
            {
                Title = "Editar Nota",
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

                    // Llamar al servicio para actualizar la nota
                    var success = await _relacionRefaccionEquipoService.UpdateNotaAsync(relacion.IdRelacionRefaccion, nuevaNota);

                    if (success)
                    {
                        // Actualizar la nota en el objeto local
                        relacion.Nota = nuevaNota;

                        // Recargar las relaciones para actualizar la UI (necesario porque x:Bind es OneTime por defecto)
                        refaccion.RelacionesEquipoLoaded = false;
                        await LoadRelacionesEquipoForRefaccionAsync(refaccion);

                        // Mostrar notificación de éxito
                        await _notificacionService.MostrarNotificacionAsync(
                            titulo: "Nota actualizada",
                            nota: "Nota actualizada correctamente",
                            fechaHoraInicio: DateTime.Now);
                    }
                    else
                    {
                        // Mostrar notificación de error
                        await _notificacionService.MostrarNotificacionAsync(
                            titulo: "Error",
                            nota: "No se pudo actualizar la nota. Por favor, intente nuevamente.",
                            fechaHoraInicio: DateTime.Now);
                    }
                }
                catch (Exception ex)
                {
                    // Log exception details for debugging
                    System.Diagnostics.Debug.WriteLine($"Error al actualizar nota: {ex.GetType().Name} - {ex.Message}");

                    // Mostrar notificación de error
                    await _notificacionService.MostrarNotificacionAsync(
                        titulo: "Error",
                        nota: "Ocurrió un error al actualizar la nota. Por favor, intente nuevamente.",
                        fechaHoraInicio: DateTime.Now);
                }
            }
        }

        private async void DeleteRelacionEquipoButton_Click(object sender, RoutedEventArgs e)
        {
            // Obtener la relación desde el Tag del botón
            if (sender is not FrameworkElement element || element.Tag is not RelacionEquipoDto relacion)
                return;

            // Buscar la refacción que contiene esta relación
            var refaccion = ViewModel.Refacciones.FirstOrDefault(r => r.RelacionesEquipo.Contains(relacion));
            if (refaccion == null)
                return;

            // Mostrar diálogo de confirmación
            var dialog = new ContentDialog
            {
                Title = "Confirmar eliminación",
                Content = $"¿Está seguro de que desea eliminar la relación con el equipo \"{relacion.Marca} - {relacion.Identificador}\"?",
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
                    // Llamar al servicio para eliminar la relación
                    var success = await _relacionRefaccionEquipoService.DeleteRelacionAsync(relacion.IdRelacionRefaccion);

                    if (success)
                    {
                        // Eliminar la relación de la colección local
                        refaccion.RelacionesEquipo.Remove(relacion);
                        refaccion.NotifyNoRelacionesEquipoMessageChanged();

                        // Mostrar notificación de éxito
                        await _notificacionService.MostrarNotificacionAsync(
                            titulo: "Relación eliminada",
                            nota: "Relación eliminada correctamente",
                            fechaHoraInicio: DateTime.Now);
                    }
                    else
                    {
                        // Mostrar notificación de error
                        await _notificacionService.MostrarNotificacionAsync(
                            titulo: "Error",
                            nota: "No se pudo eliminar la relación. Por favor, intente nuevamente.",
                            fechaHoraInicio: DateTime.Now);
                    }
                }
                catch (Exception ex)
                {
                    // Log exception details for debugging
                    System.Diagnostics.Debug.WriteLine($"Error al eliminar relación: {ex.GetType().Name} - {ex.Message}");

                    // Mostrar notificación de error
                    await _notificacionService.MostrarNotificacionAsync(
                        titulo: "Error",
                        nota: "Ocurrió un error al eliminar la relación. Por favor, intente nuevamente.",
                        fechaHoraInicio: DateTime.Now);
                }
            }
        }
    }
}
