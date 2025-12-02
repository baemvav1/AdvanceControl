using System;
using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Extensions.DependencyInjection;
using Advance_Control.ViewModels;
using Advance_Control.Services.Notificacion;
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

        public RefaaccionView()
        {
            // Resolver el ViewModel desde DI
            ViewModel = ((App)Application.Current).Host.Services.GetRequiredService<RefaccionesViewModel>();
            
            // Resolver el servicio de notificaciones desde DI
            _notificacionService = ((App)Application.Current).Host.Services.GetRequiredService<INotificacionService>();
            
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

        private void HeadGrid_Tapped(object sender, TappedRoutedEventArgs e)
        {
            // Get the RefaccionDto from the sender's Tag property
            if (sender is FrameworkElement element && element.Tag is RefaccionDto refaccion)
            {
                refaccion.Expand = !refaccion.Expand;
            }
        }

        private void ToggleExpandButton_Click(object sender, RoutedEventArgs e)
        {
            // Get the RefaccionDto from the sender's Tag property
            if (sender is FrameworkElement element && element.Tag is RefaccionDto refaccion)
            {
                refaccion.Expand = !refaccion.Expand;
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
    }
}
