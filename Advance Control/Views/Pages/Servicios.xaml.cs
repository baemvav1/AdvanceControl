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
    /// Página para visualizar y gestionar servicios
    /// </summary>
    public sealed partial class Servicios : Page
    {
        public ServiciosViewModel ViewModel { get; }
        private readonly INotificacionService _notificacionService;

        public Servicios()
        {
            // Resolver el ViewModel desde DI
            ViewModel = ((App)Application.Current).Host.Services.GetRequiredService<ServiciosViewModel>();
            
            // Resolver el servicio de notificaciones desde DI
            _notificacionService = ((App)Application.Current).Host.Services.GetRequiredService<INotificacionService>();
            
            this.InitializeComponent();
            
            // Establecer el DataContext para los bindings
            this.DataContext = ViewModel;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            
            // Cargar los servicios cuando se navega a esta página
            await ViewModel.LoadServiciosAsync();
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.LoadServiciosAsync();
        }

        private async void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.ClearFiltersAsync();
        }

        private async void NuevoButton_Click(object sender, RoutedEventArgs e)
        {
            // Crear los campos del formulario
            var conceptoTextBox = new TextBox
            {
                PlaceholderText = "Ingrese el concepto",
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

            var costoTextBox = new TextBox
            {
                PlaceholderText = "Ingrese el costo",
                InputScope = new InputScope { Names = { new InputScopeName(InputScopeNameValue.Number) } },
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
                    new TextBlock { Text = "Concepto:" },
                    conceptoTextBox,
                    new TextBlock { Text = "Descripción:" },
                    descripcionTextBox,
                    new TextBlock { Text = "Costo:" },
                    costoTextBox,
                    estatusCheckBox
                }
            };

            var dialog = new ContentDialog
            {
                Title = "Nuevo Servicio",
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
                    // Validar campos obligatorios
                    if (string.IsNullOrWhiteSpace(conceptoTextBox.Text))
                    {
                        await _notificacionService.MostrarNotificacionAsync(
                            titulo: "Campo requerido",
                            mensaje: "El concepto es obligatorio.",
                            tipo: TipoNotificacion.Error
                        );
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(descripcionTextBox.Text))
                    {
                        await _notificacionService.MostrarNotificacionAsync(
                            titulo: "Campo requerido",
                            mensaje: "La descripción es obligatoria.",
                            tipo: TipoNotificacion.Error
                        );
                        return;
                    }

                    var costo = ParseCosto(costoTextBox.Text);

                    var success = await ViewModel.CreateServicioAsync(
                        conceptoTextBox.Text,
                        descripcionTextBox.Text,
                        costo,
                        estatusCheckBox.IsChecked ?? true
                    );

                    if (success)
                    {
                        await _notificacionService.MostrarNotificacionAsync(
                            titulo: "Servicio creado",
                            mensaje: "El servicio se creó exitosamente.",
                            tipo: TipoNotificacion.Success
                        );
                    }
                    else
                    {
                        await _notificacionService.MostrarNotificacionAsync(
                            titulo: "Error",
                            mensaje: "No se pudo crear el servicio.",
                            tipo: TipoNotificacion.Error
                        );
                    }
                }
                catch (Exception ex)
                {
                    await _notificacionService.MostrarNotificacionAsync(
                        titulo: "Error",
                        mensaje: $"Error al crear servicio: {ex.Message}",
                        tipo: TipoNotificacion.Error
                    );
                }
            }
        }

        private void HeadGrid_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (sender is Grid grid && grid.Tag is ServicioDto servicio)
            {
                servicio.Expand = !servicio.Expand;
            }
        }

        private void ToggleExpandButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is ServicioDto servicio)
            {
                servicio.Expand = !servicio.Expand;
            }
        }

        private async void EditarButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is ServicioDto servicio)
            {
                // Crear los campos del formulario con los valores actuales
                var conceptoTextBox = new TextBox
                {
                    Text = servicio.Concepto,
                    PlaceholderText = "Ingrese el concepto",
                    Margin = new Thickness(0, 0, 0, 8)
                };

                var descripcionTextBox = new TextBox
                {
                    Text = servicio.Descripcion,
                    PlaceholderText = "Ingrese la descripción",
                    AcceptsReturn = true,
                    TextWrapping = TextWrapping.Wrap,
                    MinHeight = 100,
                    MaxHeight = 200,
                    Margin = new Thickness(0, 0, 0, 8)
                };

                var costoTextBox = new TextBox
                {
                    Text = servicio.Costo?.ToString(CultureInfo.InvariantCulture),
                    PlaceholderText = "Ingrese el costo",
                    InputScope = new InputScope { Names = { new InputScopeName(InputScopeNameValue.Number) } },
                    Margin = new Thickness(0, 0, 0, 8)
                };

                var dialogContent = new StackPanel
                {
                    Spacing = 8,
                    Children =
                    {
                        new TextBlock { Text = "Concepto:" },
                        conceptoTextBox,
                        new TextBlock { Text = "Descripción:" },
                        descripcionTextBox,
                        new TextBlock { Text = "Costo:" },
                        costoTextBox
                    }
                };

                var dialog = new ContentDialog
                {
                    Title = "Editar Servicio",
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

                        var updateData = new ServicioQueryDto
                        {
                            Concepto = string.IsNullOrWhiteSpace(conceptoTextBox.Text) ? null : conceptoTextBox.Text,
                            Descripcion = string.IsNullOrWhiteSpace(descripcionTextBox.Text) ? null : descripcionTextBox.Text,
                            Costo = costo
                        };

                        var success = await ViewModel.UpdateServicioAsync(servicio.IdServicio, updateData);

                        if (success)
                        {
                            await _notificacionService.MostrarNotificacionAsync(
                                titulo: "Servicio actualizado",
                                mensaje: "El servicio se actualizó exitosamente.",
                                tipo: TipoNotificacion.Success
                            );
                        }
                        else
                        {
                            await _notificacionService.MostrarNotificacionAsync(
                                titulo: "Error",
                                mensaje: "No se pudo actualizar el servicio.",
                                tipo: TipoNotificacion.Error
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        await _notificacionService.MostrarNotificacionAsync(
                            titulo: "Error",
                            mensaje: $"Error al actualizar servicio: {ex.Message}",
                            tipo: TipoNotificacion.Error
                        );
                    }
                }
            }
        }

        private async void EliminarButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is ServicioDto servicio)
            {
                // Mostrar confirmación
                var confirmDialog = new ContentDialog
                {
                    Title = "Confirmar eliminación",
                    Content = $"¿Está seguro de que desea eliminar el servicio '{servicio.Concepto}'?",
                    PrimaryButtonText = "Sí, eliminar",
                    CloseButtonText = "Cancelar",
                    DefaultButton = ContentDialogButton.Close,
                    XamlRoot = this.XamlRoot
                };

                var result = await confirmDialog.ShowAsync();

                if (result == ContentDialogResult.Primary)
                {
                    try
                    {
                        var success = await ViewModel.DeleteServicioAsync(servicio.IdServicio);

                        if (success)
                        {
                            await _notificacionService.MostrarNotificacionAsync(
                                titulo: "Servicio eliminado",
                                mensaje: "El servicio se eliminó exitosamente.",
                                tipo: TipoNotificacion.Success
                            );
                        }
                        else
                        {
                            await _notificacionService.MostrarNotificacionAsync(
                                titulo: "Error",
                                mensaje: "No se pudo eliminar el servicio.",
                                tipo: TipoNotificacion.Error
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        await _notificacionService.MostrarNotificacionAsync(
                            titulo: "Error",
                            mensaje: $"Error al eliminar servicio: {ex.Message}",
                            tipo: TipoNotificacion.Error
                        );
                    }
                }
            }
        }

        private static double ParseCosto(string? costoText)
        {
            if (string.IsNullOrWhiteSpace(costoText))
            {
                throw new ArgumentException("El costo es obligatorio.");
            }

            if (!double.TryParse(costoText, NumberStyles.Any, CultureInfo.InvariantCulture, out var costo))
            {
                throw new ArgumentException("El costo debe ser un número válido.");
            }

            if (costo < 0)
            {
                throw new ArgumentException("El costo no puede ser negativo.");
            }

            return costo;
        }
    }
}

