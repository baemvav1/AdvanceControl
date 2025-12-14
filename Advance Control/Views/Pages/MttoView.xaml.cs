using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Extensions.DependencyInjection;
using Advance_Control.ViewModels;
using Advance_Control.Services.Notificacion;
using Advance_Control.Views.Equipos;

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

        private async void NuevoButton_Click(object sender, RoutedEventArgs e)
        {
            // Crear el UserControl para el nuevo mantenimiento
            var nuevoMantenimientoControl = new NuevoMantenimientoUserControl();

            var dialog = new ContentDialog
            {
                Title = "Nuevo Mantenimiento",
                Content = nuevoMantenimientoControl,
                PrimaryButtonText = "Crear",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                // Validar campos obligatorios
                if (!nuevoMantenimientoControl.IdTipoMantenimiento.HasValue)
                {
                    await _notificacionService.MostrarNotificacionAsync(
                        titulo: "Error de validación",
                        nota: "Debe seleccionar un tipo de mantenimiento.",
                        fechaHoraInicio: DateTime.Now);
                    return;
                }

                if (!nuevoMantenimientoControl.IdEquipo.HasValue)
                {
                    await _notificacionService.MostrarNotificacionAsync(
                        titulo: "Error de validación",
                        nota: "Debe seleccionar un equipo.",
                        fechaHoraInicio: DateTime.Now);
                    return;
                }

                if (!nuevoMantenimientoControl.IdCliente.HasValue)
                {
                    await _notificacionService.MostrarNotificacionAsync(
                        titulo: "Error de validación",
                        nota: "Debe seleccionar un cliente.",
                        fechaHoraInicio: DateTime.Now);
                    return;
                }

                try
                {
                    var success = await ViewModel.CreateMantenimientoAsync(
                        nuevoMantenimientoControl.IdTipoMantenimiento.Value,
                        nuevoMantenimientoControl.IdCliente.Value,
                        nuevoMantenimientoControl.IdEquipo.Value,
                        nuevoMantenimientoControl.Nota
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
