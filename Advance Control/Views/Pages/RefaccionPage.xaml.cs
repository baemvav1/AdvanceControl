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
using Advance_Control.Services.Logging;
using Advance_Control.Utilities;
using Advance_Control.Services.RelacionesRefaccionEquipo;
using Advance_Control.Services.Activity;
using Advance_Control.Models;
using Advance_Control.Views.Dialogs;

namespace Advance_Control.Views.Pages
{
    /// <summary>
    /// Página para visualizar y gestionar refacciones
    /// </summary>
    public sealed partial class RefaccionPage : Page
    {
        public RefaccionesViewModel ViewModel { get; }
        private readonly INotificacionService _notificacionService;
        private readonly IRelacionRefaccionEquipoService _relacionRefaccionEquipoService;
        private readonly IActivityService _activityService;

        public RefaccionPage()
        {
            // Resolver el ViewModel desde DI
            ViewModel = AppServices.Get<RefaccionesViewModel>();
            
            // Resolver el servicio de notificaciones desde DI
            _notificacionService = AppServices.Get<INotificacionService>();
            
            // Resolver el servicio de relaciones refacción-equipo desde DI
            _relacionRefaccionEquipoService = AppServices.Get<IRelacionRefaccionEquipoService>();
            
            // Resolver el servicio de equipos desde DI

            // Resolver el servicio de actividades desde DI
            _activityService = AppServices.Get<IActivityService>();
            
            this.InitializeComponent();
            ButtonClickLogger.Attach(this, AppServices.Get<ILoggingService>(), nameof(RefaccionPage));

            // Establecer el DataContext para los bindings
            this.DataContext = ViewModel;

            AutoSuggestHelper.Conectar(MarcaAutoSuggestBox, () => ViewModel.ValoresMarca, () => _ = ViewModel.LoadRefaccionesAsync());
            AutoSuggestHelper.Conectar(SerieAutoSuggestBox, () => ViewModel.ValoresSerie, () => _ = ViewModel.LoadRefaccionesAsync());
            AutoSuggestHelper.Conectar(DescripcionAutoSuggestBox, () => ViewModel.ValoresDescripcion, () => _ = ViewModel.LoadRefaccionesAsync());
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            
            // Cargar las refacciones cuando se navega a esta página
            await ViewModel.LoadRefaccionesAsync();
        }

        private async void FiltroTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key != global::Windows.System.VirtualKey.Enter)
            {
                return;
            }

            e.Handled = true;
            await ViewModel.LoadRefaccionesAsync();
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
                        string.IsNullOrWhiteSpace(serieTextBox.Text) ? "N/D" : serieTextBox.Text,
                        costo,
                        string.IsNullOrWhiteSpace(descripcionTextBox.Text) ? null : descripcionTextBox.Text,
                        estatusCheckBox.IsChecked ?? true
                    );

                    if (success)
                    {
                        await _notificacionService.MostrarAsync("Refacción creada", "Refacción creada correctamente");
                    }
                    else
                    {
                        await _notificacionService.MostrarAsync("Error", "No se pudo crear la refacción. Por favor, intente nuevamente.");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error al crear refacción: {ex.GetType().Name} - {ex.Message}");
                    
                    await _notificacionService.MostrarAsync("Error", "Ocurrió un error al crear la refacción. Por favor, intente nuevamente.");
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
                        await _notificacionService.MostrarAsync("Refacción eliminada", "Refacción eliminada correctamente");
                    }
                    else
                    {
                        await _notificacionService.MostrarAsync("Error", "No se pudo eliminar la refacción. Por favor, intente nuevamente.");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error al eliminar refacción: {ex.GetType().Name} - {ex.Message}");
                    
                    await _notificacionService.MostrarAsync("Error", "Ocurrió un error al eliminar la refacción. Por favor, intente nuevamente.");
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
                        Serie = string.IsNullOrWhiteSpace(serieTextBox.Text) ? "N/D" : serieTextBox.Text,
                        Costo = costo,
                        Descripcion = string.IsNullOrWhiteSpace(descripcionTextBox.Text) ? null : descripcionTextBox.Text,
                        Estatus = estatusCheckBox.IsChecked ?? true
                    };

                    var success = await ViewModel.UpdateRefaccionAsync(refaccion.IdRefaccion, updateData);

                    if (success)
                    {
                        await _notificacionService.MostrarAsync("Refacción actualizada", "Refacción actualizada correctamente");
                    }
                    else
                    {
                        await _notificacionService.MostrarAsync("Error", "No se pudo actualizar la refacción. Por favor, intente nuevamente.");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error al actualizar refacción: {ex.GetType().Name} - {ex.Message}");
                    
                    await _notificacionService.MostrarAsync("Error", "Ocurrió un error al actualizar la refacción. Por favor, intente nuevamente.");
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

            // El equipo se elige con el buscador de equipos (modal sobre este diálogo)
            EquipoDto? selectedEquipo = null;

            var equipoElegidoText = new TextBlock
            {
                Text = "Ningún equipo seleccionado",
                FontStyle = global::Windows.UI.Text.FontStyle.Italic,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 12)
            };

            var buscarEquipoButton = new Button
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Content = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 8,
                    Children = { new SymbolIcon(Symbol.Find), new TextBlock { Text = "Buscar equipo..." } }
                }
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
                    new TextBlock { Text = "Equipo:" },
                    buscarEquipoButton,
                    equipoElegidoText,
                    new TextBlock { Text = "Nota:" },
                    notaTextBox
                }
            };

            buscarEquipoButton.Click += async (s, args) =>
            {
                var elegido = await BuscadorEquiposDialog.MostrarAsync(this.XamlRoot);
                if (elegido is null)
                    return;

                selectedEquipo = new EquipoDto
                {
                    IdEquipo = elegido.IdEquipo,
                    Identificador = elegido.Identificador,
                    Marca = elegido.Marca,
                    Descripcion = elegido.Descripcion,
                    IdUbicacion = elegido.IdUbicacion
                };
                equipoElegidoText.Text = $"{elegido.Identificador} · {elegido.MarcaTexto} · {elegido.ClientesTexto}";
                equipoElegidoText.FontStyle = global::Windows.UI.Text.FontStyle.Normal;
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

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                try
                {
                    if (selectedEquipo == null)
                    {
                        await _notificacionService.MostrarAsync("Error", "Debe seleccionar un equipo de la lista.");
                        return;
                    }

                    var idEquipo = selectedEquipo.IdEquipo;
                    var nota = string.IsNullOrWhiteSpace(notaTextBox.Text) ? null : notaTextBox.Text;

                    var success = await _relacionRefaccionEquipoService.CreateRelacionAsync(refaccion.IdRefaccion, idEquipo, nota);

                    if (success)
                    {
                        _activityService.Registrar("Refacciones", "Equipo asociado");
                        // Recargar las relaciones para actualizar la UI
                        refaccion.RelacionesEquipoLoaded = false;
                        await LoadRelacionesEquipoForRefaccionAsync(refaccion);

                        await _notificacionService.MostrarAsync("Relación creada", $"Relación con el equipo '{selectedEquipo.Marca} - {selectedEquipo.Identificador}' creada correctamente");
                    }
                    else
                    {
                        // Mostrar notificación de error - la relación puede que ya exista
                        await _notificacionService.MostrarAsync("Error", "No se pudo crear la relación. Es posible que ya exista una relación con este equipo.");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error al crear relación equipo: {ex.GetType().Name} - {ex.Message}");

                    await _notificacionService.MostrarAsync("Error", "Ocurrió un error al crear la relación. Por favor, intente nuevamente.");
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
                        _activityService.Registrar("Refacciones", "Relación modificada");
                        // Actualizar la nota en el objeto local
                        relacion.Nota = nuevaNota;

                        // Recargar las relaciones para actualizar la UI (necesario porque x:Bind es OneTime por defecto)
                        refaccion.RelacionesEquipoLoaded = false;
                        await LoadRelacionesEquipoForRefaccionAsync(refaccion);

                        // Mostrar notificación de éxito
                        await _notificacionService.MostrarAsync("Nota actualizada", "Nota actualizada correctamente");
                    }
                    else
                    {
                        // Mostrar notificación de error
                        await _notificacionService.MostrarAsync("Error", "No se pudo actualizar la nota. Por favor, intente nuevamente.");
                    }
                }
                catch (Exception ex)
                {
                    // Log exception details for debugging
                    System.Diagnostics.Debug.WriteLine($"Error al actualizar nota: {ex.GetType().Name} - {ex.Message}");

                    // Mostrar notificación de error
                    await _notificacionService.MostrarAsync("Error", "Ocurrió un error al actualizar la nota. Por favor, intente nuevamente.");
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
                        _activityService.Registrar("Refacciones", "Relación eliminada");
                        // Eliminar la relación de la colección local
                        refaccion.RelacionesEquipo.Remove(relacion);
                        refaccion.NotifyNoRelacionesEquipoMessageChanged();

                        // Mostrar notificación de éxito
                        await _notificacionService.MostrarAsync("Relación eliminada", "Relación eliminada correctamente");
                    }
                    else
                    {
                        // Mostrar notificación de error
                        await _notificacionService.MostrarAsync("Error", "No se pudo eliminar la relación. Por favor, intente nuevamente.");
                    }
                }
                catch (Exception ex)
                {
                    // Log exception details for debugging
                    System.Diagnostics.Debug.WriteLine($"Error al eliminar relación: {ex.GetType().Name} - {ex.Message}");

                    // Mostrar notificación de error
                    await _notificacionService.MostrarAsync("Error", "Ocurrió un error al eliminar la relación. Por favor, intente nuevamente.");
                }
            }
        }
    }
}
