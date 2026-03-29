using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using global::Windows.Foundation;
using global::Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Extensions.DependencyInjection;
using Advance_Control.ViewModels;
using Advance_Control.Views.Dialogs;
using Advance_Control.Services.Relaciones;
using Advance_Control.Services.Notificacion;
using Advance_Control.Services.Logging;
using Advance_Control.Utilities;
using Advance_Control.Services.Ubicaciones;
using Advance_Control.Services.Activity;
using Advance_Control.Views.Pages;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Advance_Control.Views.Pages
{
    /// <summary>
    /// Página para visualizar y gestionar equipos
    /// </summary>
    public sealed partial class EquiposPage : Page
    {
        public EquiposViewModel ViewModel { get; }
        private readonly IRelacionService _relacionService;
        private readonly INotificacionService _notificacionService;
        private readonly IUbicacionService _ubicacionService;
        private readonly IActivityService _activityService;

        public EquiposPage()
        {
            // Resolver el ViewModel desde DI
            ViewModel = AppServices.Get<EquiposViewModel>();
            
            // Resolver el servicio de relaciones desde DI
            _relacionService = AppServices.Get<IRelacionService>();
            
            // Resolver el servicio de notificaciones desde DI
            _notificacionService = AppServices.Get<INotificacionService>();
            
            // Resolver el servicio de ubicaciones desde DI
            _ubicacionService = AppServices.Get<IUbicacionService>();

            // Resolver el servicio de actividades desde DI
            _activityService = AppServices.Get<IActivityService>();
            
            this.InitializeComponent();
            ButtonClickLogger.Attach(this, AppServices.Get<ILoggingService>(), nameof(EquiposPage));
            
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
            var nuevoEquipoViewModel = AppServices.Get<NuevoEquipoViewModel>();
            var nuevoEquipoView = new NuevoEquipoUserControl(nuevoEquipoViewModel);
            
            // Crear el diálogo similar a como lo hace el Login
            var dialog = new ContentDialog
            {
                Title = "Nuevo Equipo",
                Content = nuevoEquipoView,
                XamlRoot = this.XamlRoot
            };

            // Configurar el cierre del diálogo desde el NuevoEquipoUserControl
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

                // Load ubicacion when expanding if not already loaded
                if (equipo.Expand && equipo.HasUbicacion && equipo.Ubicacion == null)
                {
                    await LoadUbicacionForEquipoAsync(equipo);
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

                // Load ubicacion when expanding if not already loaded
                if (equipo.Expand && equipo.HasUbicacion && equipo.Ubicacion == null)
                {
                    await LoadUbicacionForEquipoAsync(equipo);
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
                        _activityService.Registrar("Equipos", "Relación eliminada");
                        // Eliminar la relación de la colección local
                        equipo.Relaciones.Remove(relacion);
                        equipo.NotifyNoRelacionesMessageChanged();
                        
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
                    // Log exception details for debugging (exception is logged by RelacionService)
                    System.Diagnostics.Debug.WriteLine($"Error al eliminar relación: {ex.GetType().Name} - {ex.Message}");
                    
                    // Mostrar notificación de error
                    await _notificacionService.MostrarAsync("Error", "Ocurrió un error al eliminar la relación. Por favor, intente nuevamente.");
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
                        _activityService.Registrar("Equipos", "Relación modificada");
                        // Actualizar la nota en el objeto local
                        relacion.Nota = nuevaNota;
                        
                        // Recargar las relaciones para actualizar la UI (necesario porque x:Bind es OneTime por defecto)
                        equipo.RelacionesLoaded = false;
                        await LoadRelacionesForEquipoAsync(equipo);
                        
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
                        _activityService.Registrar("Equipos", "Relación creada");
                        // Recargar las relaciones para actualizar la UI
                        equipo.RelacionesLoaded = false;
                        await LoadRelacionesForEquipoAsync(equipo);

                        // Mostrar notificación de éxito
                        await _notificacionService.MostrarAsync("Relación creada", $"Relación con el cliente \"{selectedCliente.RazonSocial}\" creada correctamente");
                    }
                    else
                    {
                        // Mostrar notificación de error - la relación puede que ya exista
                        await _notificacionService.MostrarAsync("Error", "No se pudo crear la relación. Es posible que ya exista una relación con este cliente.");
                    }
                }
                catch (Exception ex)
                {
                    // Log exception details for debugging
                    System.Diagnostics.Debug.WriteLine($"Error al crear relación: {ex.GetType().Name} - {ex.Message}");

                    // Mostrar notificación de error
                    await _notificacionService.MostrarAsync("Error", "Ocurrió un error al crear la relación. Por favor, intente nuevamente.");
                }
            }
        }

        private async System.Threading.Tasks.Task LoadUbicacionForEquipoAsync(Models.EquipoDto equipo)
        {
            if (equipo.IsLoadingUbicacion || !equipo.IdUbicacion.HasValue || equipo.IdUbicacion.Value <= 0)
                return;

            try
            {
                equipo.IsLoadingUbicacion = true;
                
                var ubicacion = await _ubicacionService.GetUbicacionByIdAsync(equipo.IdUbicacion.Value);
                
                if (ubicacion != null)
                {
                    equipo.Ubicacion = ubicacion;
                }
            }
            catch (Exception ex)
            {
                // Log error silently - the UI will continue to show "sin ubicacion"
                System.Diagnostics.Debug.WriteLine($"Error al cargar ubicación: {ex.GetType().Name} - {ex.Message}");
            }
            finally
            {
                equipo.IsLoadingUbicacion = false;
            }
        }

        private async void AgregarUbicacionButton_Click(object sender, RoutedEventArgs e)
        {
            // Obtener el equipo desde el Tag del botón
            if (sender is not FrameworkElement element || element.Tag is not Models.EquipoDto equipo)
                return;

            await MostrarDialogoSeleccionUbicacionAsync(equipo, "Ubicación asignada");
        }

        private async void CrearUbicacionButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement element || element.Tag is not Models.EquipoDto equipo)
                return;

            try
            {
                var ubicacionWindow = new Views.Windows.UbicacionWindow();
                ubicacionWindow.Activate();

                // Esperar a que se cierre la ventana
                var tcs = new TaskCompletionSource<bool>();
                ubicacionWindow.Closed += (_, _) => tcs.TrySetResult(true);
                await tcs.Task;

                if (ubicacionWindow.UbicacionCreada != null)
                {
                    var creada = ubicacionWindow.UbicacionCreada;

                    // Asignar la ubicación al equipo
                    var updateData = new Models.EquipoQueryDto
                    {
                        IdUbicacion = creada.IdUbicacion
                    };

                    var success = await ViewModel.UpdateEquipoAsync(equipo.IdEquipo, updateData);

                    if (success)
                    {
                        _activityService.Registrar("Equipos", "Ubicación creada y asignada");
                        equipo.IdUbicacion = creada.IdUbicacion;
                        equipo.Ubicacion = creada;
                        await _notificacionService.MostrarAsync("Ubicación asignada",
                            $"Ubicación \"{creada.Nombre}\" creada y asignada correctamente");
                    }
                    else
                    {
                        await _notificacionService.MostrarAsync("Error",
                            "La ubicación se creó pero no se pudo asignar al equipo.");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en CrearUbicacionButton_Click: {ex.Message}");
                await _notificacionService.MostrarAsync("Error",
                    "Ocurrió un error al crear la ubicación.");
            }
        }

        private async void EditarUbicacionButton_Click(object sender, RoutedEventArgs e)
        {
            // Obtener el equipo desde el Tag del botón
            if (sender is not FrameworkElement element || element.Tag is not Models.EquipoDto equipo)
                return;

            await MostrarDialogoSeleccionUbicacionAsync(equipo, "Ubicación modificada");
        }

        private async System.Threading.Tasks.Task MostrarDialogoSeleccionUbicacionAsync(Models.EquipoDto equipo, string actividadTitulo = "Ubicación asignada")
        {
            // Crear el UserControl para seleccionar ubicación
            var seleccionarUbicacionControl = new SeleccionarUbicacionUserControl();

            try
            {
                // Cargar las ubicaciones
                seleccionarUbicacionControl.IsLoading = true;
                var ubicaciones = await _ubicacionService.GetUbicacionesAsync();
                
                seleccionarUbicacionControl.Ubicaciones.Clear();
                foreach (var ubicacion in ubicaciones)
                {
                    seleccionarUbicacionControl.Ubicaciones.Add(ubicacion);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cargar ubicaciones: {ex.GetType().Name} - {ex.Message}");
                await _notificacionService.MostrarAsync("Error", "No se pudieron cargar las ubicaciones. Por favor, verifique su conexión e intente nuevamente.");
                return;
            }
            finally
            {
                seleccionarUbicacionControl.IsLoading = false;
            }

            // Crear el diálogo
            var dialog = new ContentDialog
            {
                Title = "Seleccionar Ubicación",
                Content = seleccionarUbicacionControl,
                PrimaryButtonText = "Guardar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary && seleccionarUbicacionControl.SelectedUbicacion != null)
            {
                var selectedUbicacion = seleccionarUbicacionControl.SelectedUbicacion;

                // Validar que la ubicación pertenece a un área definida
                if (selectedUbicacion.IdArea == null)
                {
                    await _notificacionService.MostrarAsync(
                        "Ubicación sin área",
                        "La ubicación seleccionada no pertenece a ningún área definida. Por favor seleccione una ubicación dentro de un área.");
                    return;
                }

                try
                {
                    // Actualizar el equipo con el ID de ubicación seleccionado
                    var updateData = new Models.EquipoQueryDto
                    {
                        IdUbicacion = selectedUbicacion.IdUbicacion
                    };

                    var success = await ViewModel.UpdateEquipoAsync(equipo.IdEquipo, updateData);

                    if (success)
                    {
                        _activityService.Registrar("Equipos", actividadTitulo);
                        // Actualizar el equipo local con la nueva ubicación
                        equipo.IdUbicacion = selectedUbicacion.IdUbicacion;
                        equipo.Ubicacion = selectedUbicacion;

                        // Mostrar notificación de éxito
                        await _notificacionService.MostrarAsync("Ubicación actualizada", $"Ubicación \"{selectedUbicacion.Nombre}\" asignada correctamente");
                    }
                    else
                    {
                        // Mostrar notificación de error
                        await _notificacionService.MostrarAsync("Error", "No se pudo actualizar la ubicación. Por favor, intente nuevamente.");
                    }
                }
                catch (Exception ex)
                {
                    // Log exception details for debugging
                    System.Diagnostics.Debug.WriteLine($"Error al actualizar ubicación: {ex.GetType().Name} - {ex.Message}");

                    // Provide more specific error message
                    var errorMessage = "Ocurrió un error al actualizar la ubicación. ";
                    if (ex is System.Net.Http.HttpRequestException)
                    {
                        errorMessage += "Por favor, verifique su conexión a internet e intente nuevamente.";
                    }
                    else
                    {
                        errorMessage += "Por favor, intente nuevamente o contacte al soporte técnico.";
                    }

                    // Mostrar notificación de error
                    await _notificacionService.MostrarAsync("Error", errorMessage);
                }
            }
        }

        /// <summary>
        /// Maneja el evento click del botón "Ver en Mapa".
        /// Navega a la página de Ubicaciones con el ID de ubicación del equipo seleccionado.
        /// </summary>
        private void VerEnMapaButton_Click(object sender, RoutedEventArgs e)
        {
            // Obtener el equipo desde el Tag del botón
            if (sender is not FrameworkElement element || element.Tag is not Models.EquipoDto equipo)
                return;

            // Verificar que hay una ubicación asignada
            if (equipo.Ubicacion == null || !equipo.IdUbicacion.HasValue)
                return;

            // Navegar a la página de Ubicaciones pasando el ID de ubicación
            Frame.Navigate(typeof(UbicacionesPage), equipo.IdUbicacion.Value);
        }

    }
}

