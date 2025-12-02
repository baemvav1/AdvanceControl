using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Extensions.DependencyInjection;
using Advance_Control.ViewModels;
using Advance_Control.Views.Equipos;
using Advance_Control.Services.Relaciones;
using Advance_Control.Services.Notificacion;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Advance_Control.Views
{
    /// <summary>
    /// Página para visualizar y gestionar equipos
    /// </summary>
    public sealed partial class EquiposView : Page
    {
        public EquiposViewModel ViewModel { get; }
        private readonly IRelacionService _relacionService;
        private readonly INotificacionService _notificacionService;

        public EquiposView()
        {
            // Resolver el ViewModel desde DI
            ViewModel = ((App)Application.Current).Host.Services.GetRequiredService<EquiposViewModel>();
            
            // Resolver el servicio de relaciones desde DI
            _relacionService = ((App)Application.Current).Host.Services.GetRequiredService<IRelacionService>();
            
            // Resolver el servicio de notificaciones desde DI
            _notificacionService = ((App)Application.Current).Host.Services.GetRequiredService<INotificacionService>();
            
            this.InitializeComponent();
            
            // Establecer el DataContext para los bindings
            this.DataContext = ViewModel;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            
            // Cargar los equipos cuando se navega a esta página
            await ViewModel.LoadEquiposAsync();
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.LoadEquiposAsync();
        }

        private async void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.ClearFiltersAsync();
        }

        private async void NuevoButton_Click(object sender, RoutedEventArgs e)
        {
            // Resolver el ViewModel desde DI
            var nuevoEquipoViewModel = ((App)Application.Current).Host.Services.GetRequiredService<NuevoEquipoViewModel>();
            var nuevoEquipoView = new NuevoEquipoView(nuevoEquipoViewModel);
            
            // Crear el diálogo similar a como lo hace el Login
            var dialog = new ContentDialog
            {
                Title = "Nuevo Equipo",
                Content = nuevoEquipoView,
                XamlRoot = this.XamlRoot
            };

            // Configurar el cierre del diálogo desde el NuevoEquipoView
            nuevoEquipoView.CloseDialogAction = () => 
            {
                try
                {
                    var dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
                    if (dispatcherQueue != null)
                    {
                        _ = dispatcherQueue.TryEnqueue(() =>
                        {
                            try
                            {
                                dialog.Hide();
                            }
                            catch
                            {
                                // El diálogo ya puede estar cerrado
                            }
                        });
                    }
                    else
                    {
                        dialog.Hide();
                    }
                }
                catch
                {
                    // El diálogo ya puede estar cerrado
                }
            };

            await dialog.ShowAsync();

            // Si se guardó exitosamente, crear el equipo en el servidor
            if (nuevoEquipoView.SaveSuccessful && nuevoEquipoViewModel.Creado.HasValue)
            {
                await ViewModel.CreateEquipoAsync(
                    nuevoEquipoViewModel.Marca,
                    nuevoEquipoViewModel.Creado.Value,
                    string.IsNullOrWhiteSpace(nuevoEquipoViewModel.Descripcion) ? null : nuevoEquipoViewModel.Descripcion,
                    nuevoEquipoViewModel.Identificador,
                    nuevoEquipoViewModel.Estatus
                );
            }
        }

        private async void HeadGrid_Tapped(object sender, TappedRoutedEventArgs e)
        {
            // Get the EquipoDto from the sender's Tag property
            if (sender is FrameworkElement element && element.Tag is Models.EquipoDto equipo)
            {
                equipo.Expand = !equipo.Expand;
                
                // Load relaciones when expanding if not already loaded
                if (equipo.Expand && !equipo.RelacionesLoaded && !string.IsNullOrWhiteSpace(equipo.Identificador))
                {
                    await LoadRelacionesForEquipoAsync(equipo);
                }
            }
        }

        private async void ToggleExpandButton_Click(object sender, RoutedEventArgs e)
        {
            // Get the EquipoDto from the sender's Tag property
            if (sender is FrameworkElement element && element.Tag is Models.EquipoDto equipo)
            {
                equipo.Expand = !equipo.Expand;
                
                // Load relaciones when expanding if not already loaded
                if (equipo.Expand && !equipo.RelacionesLoaded && !string.IsNullOrWhiteSpace(equipo.Identificador))
                {
                    await LoadRelacionesForEquipoAsync(equipo);
                }
            }
        }

        private async System.Threading.Tasks.Task LoadRelacionesForEquipoAsync(Models.EquipoDto equipo)
        {
            if (equipo.IsLoadingRelaciones || string.IsNullOrWhiteSpace(equipo.Identificador))
                return;

            try
            {
                equipo.IsLoadingRelaciones = true;
                
                var relaciones = await _relacionService.GetRelacionesAsync(equipo.Identificador, 0);
                
                equipo.Relaciones.Clear();
                foreach (var relacion in relaciones)
                {
                    equipo.Relaciones.Add(relacion);
                }
                
                equipo.RelacionesLoaded = true;
                equipo.NotifyNoRelacionesMessageChanged();
            }
            catch (Exception)
            {
                // Log error silently - the UI will show empty list
                equipo.RelacionesLoaded = true;
                equipo.NotifyNoRelacionesMessageChanged();
            }
            finally
            {
                equipo.IsLoadingRelaciones = false;
            }
        }

        private async void DeleteRelacionButton_Click(object sender, RoutedEventArgs e)
        {
            // Obtener la relación desde el Tag del botón
            if (sender is not FrameworkElement element || element.Tag is not Models.RelacionClienteDto relacion)
                return;

            // Buscar el equipo que contiene esta relación
            var equipo = ViewModel.Equipos.FirstOrDefault(eq => eq.Relaciones.Contains(relacion));
            if (equipo == null || string.IsNullOrWhiteSpace(equipo.Identificador))
                return;

            // Mostrar diálogo de confirmación usando ContentDialog directamente
            var dialog = new ContentDialog
            {
                Title = "Confirmar eliminación",
                Content = $"¿Está seguro de que desea eliminar la relación con el cliente \"{relacion.RazonSocial}\"?",
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
                    var success = await _relacionService.DeleteRelacionAsync(equipo.Identificador, relacion.IdCliente);

                    if (success)
                    {
                        // Eliminar la relación de la colección local
                        equipo.Relaciones.Remove(relacion);
                        equipo.NotifyNoRelacionesMessageChanged();
                        
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
                    // Log exception details for debugging (exception is logged by RelacionService)
                    System.Diagnostics.Debug.WriteLine($"Error al eliminar relación: {ex.GetType().Name} - {ex.Message}");
                    
                    // Mostrar notificación de error
                    await _notificacionService.MostrarNotificacionAsync(
                        titulo: "Error",
                        nota: "Ocurrió un error al eliminar la relación. Por favor, intente nuevamente.",
                        fechaHoraInicio: DateTime.Now);
                }
            }
        }

        private async void EditRelacionButton_Click(object sender, RoutedEventArgs e)
        {
            // Obtener la relación desde el Tag del botón
            if (sender is not FrameworkElement element || element.Tag is not Models.RelacionClienteDto relacion)
                return;

            // Buscar el equipo que contiene esta relación
            var equipo = ViewModel.Equipos.FirstOrDefault(eq => eq.Relaciones.Contains(relacion));
            if (equipo == null || string.IsNullOrWhiteSpace(equipo.Identificador))
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
                        Text = $"Cliente: {relacion.RazonSocial}",
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
                    var success = await _relacionService.UpdateNotaAsync(equipo.Identificador, relacion.IdCliente, nuevaNota);

                    if (success)
                    {
                        // Actualizar la nota en el objeto local
                        relacion.Nota = nuevaNota;
                        
                        // Recargar las relaciones para actualizar la UI (necesario porque x:Bind es OneTime por defecto)
                        equipo.RelacionesLoaded = false;
                        await LoadRelacionesForEquipoAsync(equipo);
                        
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

        private async System.Threading.Tasks.Task ShowErrorDialogAsync(string message)
        {
            var errorDialog = new ContentDialog
            {
                Title = "Error",
                Content = message,
                CloseButtonText = "Aceptar",
                XamlRoot = this.XamlRoot
            };
            await errorDialog.ShowAsync();
        }

        private async void NuevaRelacion_Click(object sender, RoutedEventArgs e)
        {
            // Obtener el equipo desde el Tag del botón
            if (sender is not FrameworkElement element || element.Tag is not Models.EquipoDto equipo)
                return;

            if (string.IsNullOrWhiteSpace(equipo.Identificador))
                return;

            // Crear el UserControl para seleccionar cliente
            var seleccionarClienteControl = new SeleccionarClienteUserControl();

            // Crear el diálogo
            var dialog = new ContentDialog
            {
                Title = "Nueva Relación",
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
                var nota = seleccionarClienteControl.Nota;

                if (selectedCliente == null)
                    return;

                try
                {
                    // Llamar al servicio para crear la relación
                    var success = await _relacionService.CreateRelacionAsync(
                        equipo.Identificador,
                        selectedCliente.IdCliente,
                        nota);

                    if (success)
                    {
                        // Recargar las relaciones para actualizar la UI
                        equipo.RelacionesLoaded = false;
                        await LoadRelacionesForEquipoAsync(equipo);

                        // Mostrar notificación de éxito
                        await _notificacionService.MostrarNotificacionAsync(
                            titulo: "Relación creada",
                            nota: $"Relación con el cliente \"{selectedCliente.RazonSocial}\" creada correctamente",
                            fechaHoraInicio: DateTime.Now);
                    }
                    else
                    {
                        // Mostrar notificación de error - la relación puede que ya exista
                        await _notificacionService.MostrarNotificacionAsync(
                            titulo: "Error",
                            nota: "No se pudo crear la relación. Es posible que ya exista una relación con este cliente.",
                            fechaHoraInicio: DateTime.Now);
                    }
                }
                catch (Exception ex)
                {
                    // Log exception details for debugging
                    System.Diagnostics.Debug.WriteLine($"Error al crear relación: {ex.GetType().Name} - {ex.Message}");

                    // Mostrar notificación de error
                    await _notificacionService.MostrarNotificacionAsync(
                        titulo: "Error",
                        nota: "Ocurrió un error al crear la relación. Por favor, intente nuevamente.",
                        fechaHoraInicio: DateTime.Now);
                }
            }
        }

    }
}

