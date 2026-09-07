using Advance_Control.Models;
using Advance_Control.Services.Activity;
using Advance_Control.Services.Notificacion;
using Advance_Control.Services.Logging;
using Advance_Control.Utilities;
using Advance_Control.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Globalization;

namespace Advance_Control.Views.Pages
{
    /// <summary>
    /// Catálogo de Productos (materiales/insumos suministrados al cliente final).
    /// Calcado de ServiciosPage: mismo patrón de tarjetas expandibles y diálogos de
    /// alta/edición armados en code-behind, con el agregado de costo directo + % de
    /// utilidad → costo final (calculado en vivo mientras se captura).
    /// </summary>
    public sealed partial class ProductosPage : Page
    {
        public ProductosViewModel ViewModel { get; }
        private readonly INotificacionService _notificacionService;
        private readonly IActivityService _activityService;

        public ProductosPage()
        {
            ViewModel = AppServices.Get<ProductosViewModel>();
            _notificacionService = AppServices.Get<INotificacionService>();
            _activityService = AppServices.Get<IActivityService>();

            this.InitializeComponent();
            ButtonClickLogger.Attach(this, AppServices.Get<ILoggingService>(), nameof(ProductosPage));

            this.DataContext = ViewModel;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            await ViewModel.LoadProductosAsync();
        }

        private async void FiltroTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key != global::Windows.System.VirtualKey.Enter)
            {
                return;
            }

            e.Handled = true;
            await ViewModel.LoadProductosAsync();
        }

        private void ToggleFiltros_Click(object sender, RoutedEventArgs e)
        {
            Filtros.Visibility = Filtros.Visibility == Visibility.Visible
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        private async void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.ClearFiltersAsync();
        }

        private async void NuevoButton_Click(object sender, RoutedEventArgs e)
        {
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

            var costoDirectoTextBox = new TextBox
            {
                PlaceholderText = "Nuestro costo, antes de IVA",
                InputScope = new InputScope { Names = { new InputScopeName(InputScopeNameValue.Number) } },
                Margin = new Thickness(0, 0, 0, 8)
            };

            var porcentajeTextBox = new TextBox
            {
                Text = "0",
                PlaceholderText = "% de utilidad sobre el costo directo",
                InputScope = new InputScope { Names = { new InputScopeName(InputScopeNameValue.Number) } },
                Margin = new Thickness(0, 0, 0, 8)
            };

            var costoFinalTextBlock = new TextBlock { FontWeight = Microsoft.UI.Text.FontWeights.SemiBold };

            void ActualizarCostoFinal()
            {
                costoFinalTextBlock.Text = TryCalcularCostoFinal(costoDirectoTextBox.Text, porcentajeTextBox.Text, out var final)
                    ? $"Costo final al cliente: ${final:0.00}"
                    : "Costo final al cliente: -";
            }

            costoDirectoTextBox.TextChanged += (_, _) => ActualizarCostoFinal();
            porcentajeTextBox.TextChanged += (_, _) => ActualizarCostoFinal();
            ActualizarCostoFinal();

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
                    new TextBlock { Text = "Costo directo (nuestro costo, antes de IVA):" },
                    costoDirectoTextBox,
                    new TextBlock { Text = "% de utilidad:" },
                    porcentajeTextBox,
                    costoFinalTextBlock,
                    estatusCheckBox
                }
            };

            var dialog = new ContentDialog
            {
                Title = "Nuevo Producto",
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
                    if (string.IsNullOrWhiteSpace(conceptoTextBox.Text))
                    {
                        await _notificacionService.MostrarAsync("Campo requerido", "El concepto es obligatorio.");
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(descripcionTextBox.Text))
                    {
                        await _notificacionService.MostrarAsync("Campo requerido", "La descripción es obligatoria.");
                        return;
                    }

                    var costoDirecto = ParseCosto(costoDirectoTextBox.Text, "El costo directo");
                    var porcentaje = ParsePorcentaje(porcentajeTextBox.Text);

                    var success = await ViewModel.CreateProductoAsync(
                        conceptoTextBox.Text,
                        descripcionTextBox.Text,
                        costoDirecto,
                        porcentaje,
                        estatusCheckBox.IsChecked ?? true
                    );

                    if (success)
                    {
                        await _notificacionService.MostrarAsync("Producto creado", "El producto se creó exitosamente.");
                    }
                    else
                    {
                        await _notificacionService.MostrarAsync("Error", "No se pudo crear el producto.");
                    }
                }
                catch (Exception ex)
                {
                    await _notificacionService.MostrarAsync("Error", $"Error al crear producto: {ex.Message}");
                }
            }
        }

        private void HeadGrid_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (sender is Grid grid && grid.Tag is ProductoDto producto)
            {
                producto.Expand = !producto.Expand;
            }
        }

        private void ToggleExpandButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is ProductoDto producto)
            {
                producto.Expand = !producto.Expand;
            }
        }

        private async void EditarButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is ProductoDto producto)
            {
                var conceptoTextBox = new TextBox
                {
                    Text = producto.Concepto,
                    PlaceholderText = "Ingrese el concepto",
                    Margin = new Thickness(0, 0, 0, 8)
                };

                var descripcionTextBox = new TextBox
                {
                    Text = producto.Descripcion,
                    PlaceholderText = "Ingrese la descripción",
                    AcceptsReturn = true,
                    TextWrapping = TextWrapping.Wrap,
                    MinHeight = 100,
                    MaxHeight = 200,
                    Margin = new Thickness(0, 0, 0, 8)
                };

                var costoDirectoTextBox = new TextBox
                {
                    Text = producto.CostoDirecto?.ToString(CultureInfo.InvariantCulture),
                    PlaceholderText = "Nuestro costo, antes de IVA",
                    InputScope = new InputScope { Names = { new InputScopeName(InputScopeNameValue.Number) } },
                    Margin = new Thickness(0, 0, 0, 8)
                };

                var porcentajeTextBox = new TextBox
                {
                    Text = producto.PorcentajeUtilidad?.ToString(CultureInfo.InvariantCulture) ?? "0",
                    PlaceholderText = "% de utilidad sobre el costo directo",
                    InputScope = new InputScope { Names = { new InputScopeName(InputScopeNameValue.Number) } },
                    Margin = new Thickness(0, 0, 0, 8)
                };

                var costoFinalTextBlock = new TextBlock { FontWeight = Microsoft.UI.Text.FontWeights.SemiBold };

                void ActualizarCostoFinal()
                {
                    costoFinalTextBlock.Text = TryCalcularCostoFinal(costoDirectoTextBox.Text, porcentajeTextBox.Text, out var final)
                        ? $"Costo final al cliente: ${final:0.00}"
                        : "Costo final al cliente: -";
                }

                costoDirectoTextBox.TextChanged += (_, _) => ActualizarCostoFinal();
                porcentajeTextBox.TextChanged += (_, _) => ActualizarCostoFinal();
                ActualizarCostoFinal();

                var estatusCheckBox = new CheckBox
                {
                    Content = "Activo",
                    IsChecked = producto.Estatus ?? true
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
                        new TextBlock { Text = "Costo directo (nuestro costo, antes de IVA):" },
                        costoDirectoTextBox,
                        new TextBlock { Text = "% de utilidad:" },
                        porcentajeTextBox,
                        costoFinalTextBlock,
                        estatusCheckBox
                    }
                };

                var dialog = new ContentDialog
                {
                    Title = "Editar Producto",
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
                        if (string.IsNullOrWhiteSpace(conceptoTextBox.Text))
                        {
                            await _notificacionService.MostrarAsync("Campo requerido", "El concepto es obligatorio.");
                            return;
                        }

                        if (string.IsNullOrWhiteSpace(descripcionTextBox.Text))
                        {
                            await _notificacionService.MostrarAsync("Campo requerido", "La descripción es obligatoria.");
                            return;
                        }

                        var costoDirecto = ParseCosto(costoDirectoTextBox.Text, "El costo directo");
                        var porcentaje = ParsePorcentaje(porcentajeTextBox.Text);

                        var updateData = new ProductoQueryDto
                        {
                            Concepto = conceptoTextBox.Text,
                            Descripcion = descripcionTextBox.Text,
                            CostoDirecto = costoDirecto,
                            PorcentajeUtilidad = porcentaje,
                            Estatus = estatusCheckBox.IsChecked ?? true
                        };

                        var success = await ViewModel.UpdateProductoAsync(producto.IdProducto, updateData);

                        if (success)
                        {
                            _activityService.Registrar("Productos", "Producto modificado");
                            await _notificacionService.MostrarAsync("Producto actualizado", "El producto se actualizó exitosamente.");
                        }
                        else
                        {
                            await _notificacionService.MostrarAsync("Error", "No se pudo actualizar el producto.");
                        }
                    }
                    catch (Exception ex)
                    {
                        await _notificacionService.MostrarAsync("Error", $"Error al actualizar producto: {ex.Message}");
                    }
                }
            }
        }

        private async void EliminarButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is ProductoDto producto)
            {
                var confirmDialog = new ContentDialog
                {
                    Title = "Confirmar eliminación",
                    Content = $"¿Está seguro de que desea eliminar el producto '{producto.Concepto}'?",
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
                        var success = await ViewModel.DeleteProductoAsync(producto.IdProducto);

                        if (success)
                        {
                            _activityService.Registrar("Productos", "Producto eliminado");
                            await _notificacionService.MostrarAsync("Producto eliminado", "El producto se eliminó exitosamente.");
                        }
                        else
                        {
                            await _notificacionService.MostrarAsync("Error", "No se pudo eliminar el producto.");
                        }
                    }
                    catch (Exception ex)
                    {
                        await _notificacionService.MostrarAsync("Error", $"Error al eliminar producto: {ex.Message}");
                    }
                }
            }
        }

        private static double ParseCosto(string? costoText, string nombreCampo)
        {
            if (string.IsNullOrWhiteSpace(costoText))
            {
                throw new ArgumentException($"{nombreCampo} es obligatorio.");
            }

            if (!double.TryParse(costoText, NumberStyles.Any, CultureInfo.InvariantCulture, out var costo))
            {
                throw new ArgumentException($"{nombreCampo} debe ser un número válido.");
            }

            if (costo < 0)
            {
                throw new ArgumentException($"{nombreCampo} no puede ser negativo.");
            }

            return costo;
        }

        private static double ParsePorcentaje(string? porcentajeText)
        {
            if (string.IsNullOrWhiteSpace(porcentajeText))
            {
                return 0;
            }

            if (!double.TryParse(porcentajeText, NumberStyles.Any, CultureInfo.InvariantCulture, out var porcentaje))
            {
                throw new ArgumentException("El % de utilidad debe ser un número válido.");
            }

            if (porcentaje < 0)
            {
                throw new ArgumentException("El % de utilidad no puede ser negativo.");
            }

            return porcentaje;
        }

        private static bool TryCalcularCostoFinal(string? costoDirectoText, string? porcentajeText, out double costoFinal)
        {
            costoFinal = 0;

            if (!double.TryParse(costoDirectoText, NumberStyles.Any, CultureInfo.InvariantCulture, out var costoDirecto))
            {
                return false;
            }

            double.TryParse(porcentajeText, NumberStyles.Any, CultureInfo.InvariantCulture, out var porcentaje);

            costoFinal = costoDirecto * (1 + porcentaje / 100.0);
            return true;
        }
    }
}
