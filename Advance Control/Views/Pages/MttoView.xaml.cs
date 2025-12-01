using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Extensions.DependencyInjection;
using Advance_Control.ViewModels;
using Advance_Control.Services.Notificacion;

namespace Advance_Control.Views
{
    /// <summary>
    /// Página para visualizar y gestionar operaciones de mantenimiento
    /// </summary>
    public sealed partial class MttoView : Page
    {
        public MttoViewModel ViewModel { get; }
        private readonly INotificacionService _notificacionService;

        public MttoView()
        {
            // Resolver el ViewModel desde DI
            ViewModel = ((App)Application.Current).Host.Services.GetRequiredService<MttoViewModel>();

            // Resolver el servicio de notificaciones desde DI
            _notificacionService = ((App)Application.Current).Host.Services.GetRequiredService<INotificacionService>();

            this.InitializeComponent();
            
            // Establecer el DataContext para los bindings
            this.DataContext = ViewModel;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            
            // Cargar los mantenimientos cuando se navega a esta página
            await ViewModel.LoadMantenimientosAsync();
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.LoadMantenimientosAsync();
        }

        private async void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.ClearFiltersAsync();
        }

        /// <summary>
        /// Validates that a NumberBox has a positive value greater than zero
        /// </summary>
        /// <param name="value">The value to validate</param>
        /// <returns>True if the value is valid (non-NaN and greater than zero)</returns>
        private static bool IsValidPositiveNumber(double value)
        {
            return !double.IsNaN(value) && value > 0;
        }

        private async void NuevoButton_Click(object sender, RoutedEventArgs e)
        {
            // Crear el contenido del diálogo para nuevo mantenimiento
            var idTipoMantenimientoBox = new NumberBox
            {
                Header = "ID Tipo Mantenimiento",
                PlaceholderText = "Ingrese el ID del tipo",
                Minimum = 1,
                SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact
            };

            var idClienteBox = new NumberBox
            {
                Header = "ID Cliente",
                PlaceholderText = "Ingrese el ID del cliente",
                Minimum = 1,
                SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact
            };

            var idEquipoBox = new NumberBox
            {
                Header = "ID Equipo",
                PlaceholderText = "Ingrese el ID del equipo",
                Minimum = 1,
                SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact
            };

            var costoBox = new NumberBox
            {
                Header = "Costo",
                PlaceholderText = "Ingrese el costo",
                Minimum = 0.01,
                SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact
            };

            var notaBox = new TextBox
            {
                Header = "Nota (opcional)",
                PlaceholderText = "Ingrese una nota...",
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                MinHeight = 60,
                MaxHeight = 120
            };

            var dialogContent = new StackPanel
            {
                Spacing = 12,
                Children =
                {
                    idTipoMantenimientoBox,
                    idClienteBox,
                    idEquipoBox,
                    costoBox,
                    notaBox
                }
            };

            var dialog = new ContentDialog
            {
                Title = "Nuevo Mantenimiento",
                Content = dialogContent,
                PrimaryButtonText = "Crear",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                // Validar campos obligatorios usando el método auxiliar
                if (!IsValidPositiveNumber(idTipoMantenimientoBox.Value))
                {
                    await _notificacionService.MostrarNotificacionAsync(
                        titulo: "Error de validación",
                        nota: "El ID del tipo de mantenimiento es obligatorio y debe ser mayor que 0.",
                        fechaHoraInicio: DateTime.Now);
                    return;
                }

                if (!IsValidPositiveNumber(idClienteBox.Value))
                {
                    await _notificacionService.MostrarNotificacionAsync(
                        titulo: "Error de validación",
                        nota: "El ID del cliente es obligatorio y debe ser mayor que 0.",
                        fechaHoraInicio: DateTime.Now);
                    return;
                }

                if (!IsValidPositiveNumber(idEquipoBox.Value))
                {
                    await _notificacionService.MostrarNotificacionAsync(
                        titulo: "Error de validación",
                        nota: "El ID del equipo es obligatorio y debe ser mayor que 0.",
                        fechaHoraInicio: DateTime.Now);
                    return;
                }

                if (!IsValidPositiveNumber(costoBox.Value))
                {
                    await _notificacionService.MostrarNotificacionAsync(
                        titulo: "Error de validación",
                        nota: "El costo es obligatorio y debe ser mayor que 0.",
                        fechaHoraInicio: DateTime.Now);
                    return;
                }

                try
                {
                    var success = await ViewModel.CreateMantenimientoAsync(
                        (int)idTipoMantenimientoBox.Value,
                        (int)idClienteBox.Value,
                        (int)idEquipoBox.Value,
                        costoBox.Value,
                        string.IsNullOrWhiteSpace(notaBox.Text) ? null : notaBox.Text
                    );

                    if (success)
                    {
                        await _notificacionService.MostrarNotificacionAsync(
                            titulo: "Mantenimiento creado",
                            nota: "El mantenimiento se ha creado correctamente.",
                            fechaHoraInicio: DateTime.Now);
                    }
                    else
                    {
                        await _notificacionService.MostrarNotificacionAsync(
                            titulo: "Error",
                            nota: "No se pudo crear el mantenimiento. Por favor, intente nuevamente.",
                            fechaHoraInicio: DateTime.Now);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error al crear mantenimiento: {ex.GetType().Name} - {ex.Message}");
                    await _notificacionService.MostrarNotificacionAsync(
                        titulo: "Error",
                        nota: "Ocurrió un error al crear el mantenimiento. Por favor, intente nuevamente.",
                        fechaHoraInicio: DateTime.Now);
                }
            }
        }

        private void HeadGrid_Tapped(object sender, TappedRoutedEventArgs e)
        {
            // Get the MantenimientoDto from the sender's Tag property
            if (sender is FrameworkElement element && element.Tag is Models.MantenimientoDto mantenimiento)
            {
                mantenimiento.Expand = !mantenimiento.Expand;
            }
        }

        private void ToggleExpandButton_Click(object sender, RoutedEventArgs e)
        {
            // Get the MantenimientoDto from the sender's Tag property
            if (sender is FrameworkElement element && element.Tag is Models.MantenimientoDto mantenimiento)
            {
                mantenimiento.Expand = !mantenimiento.Expand;
            }
        }

        private async void DeleteMantenimientoButton_Click(object sender, RoutedEventArgs e)
        {
            // Obtener el mantenimiento desde el Tag del botón
            if (sender is not FrameworkElement element || element.Tag is not Models.MantenimientoDto mantenimiento)
                return;

            if (!mantenimiento.IdMantenimiento.HasValue)
                return;

            // Mostrar diálogo de confirmación
            var dialog = new ContentDialog
            {
                Title = "Confirmar eliminación",
                Content = $"¿Está seguro de que desea eliminar el mantenimiento #{mantenimiento.IdMantenimiento} ({mantenimiento.TipoMantenimiento})?",
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
                    var success = await ViewModel.DeleteMantenimientoAsync(mantenimiento.IdMantenimiento.Value);

                    if (success)
                    {
                        await _notificacionService.MostrarNotificacionAsync(
                            titulo: "Mantenimiento eliminado",
                            nota: "El mantenimiento se ha eliminado correctamente.",
                            fechaHoraInicio: DateTime.Now);
                    }
                    else
                    {
                        await _notificacionService.MostrarNotificacionAsync(
                            titulo: "Error",
                            nota: "No se pudo eliminar el mantenimiento. Por favor, intente nuevamente.",
                            fechaHoraInicio: DateTime.Now);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error al eliminar mantenimiento: {ex.GetType().Name} - {ex.Message}");
                    await _notificacionService.MostrarNotificacionAsync(
                        titulo: "Error",
                        nota: "Ocurrió un error al eliminar el mantenimiento. Por favor, intente nuevamente.",
                        fechaHoraInicio: DateTime.Now);
                }
            }
        }
    }
}
