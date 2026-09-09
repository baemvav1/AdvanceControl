using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Advance_Control.ViewModels;
using Advance_Control.Views.Dialogs;
using Advance_Control.Services.RelacionesInmueble;
using Advance_Control.Services.Notificacion;
using Advance_Control.Services.Logging;
using Advance_Control.Utilities;
using Advance_Control.Services.Ubicaciones;
using Advance_Control.Services.Activity;
using Advance_Control.Views.Pages;

namespace Advance_Control.Views.Pages
{
    /// <summary>
    /// Página para visualizar y gestionar inmuebles
    /// </summary>
    public sealed partial class InmueblesPage : Page
    {
        public InmueblesViewModel ViewModel { get; }
        private readonly IRelacionInmuebleService _relacionService;
        private readonly INotificacionService _notificacionService;
        private readonly IUbicacionService _ubicacionService;
        private readonly IActivityService _activityService;

        public InmueblesPage()
        {
            ViewModel = AppServices.Get<InmueblesViewModel>();
            _relacionService = AppServices.Get<IRelacionInmuebleService>();
            _notificacionService = AppServices.Get<INotificacionService>();
            _ubicacionService = AppServices.Get<IUbicacionService>();
            _activityService = AppServices.Get<IActivityService>();

            this.InitializeComponent();
            ButtonClickLogger.Attach(this, AppServices.Get<ILoggingService>(), nameof(InmueblesPage));

            this.DataContext = ViewModel;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            await ViewModel.InitializeAsync();
            await ViewModel.LoadInmueblesAsync();
        }

        private async void FiltroTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key != global::Windows.System.VirtualKey.Enter)
            {
                return;
            }

            e.Handled = true;
            await ViewModel.LoadInmueblesAsync();
        }

        private async void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.ClearFiltersAsync();
        }

        private async void NuevoButton_Click(object sender, RoutedEventArgs e)
        {
            var nuevoInmuebleViewModel = AppServices.Get<NuevoInmuebleViewModel>();
            var nuevoInmuebleView = new NuevoInmuebleUserControl(nuevoInmuebleViewModel);

            var dialog = new ContentDialog
            {
                Title = "Nuevo Inmueble",
                Content = nuevoInmuebleView,
                XamlRoot = this.XamlRoot
            };

            nuevoInmuebleView.CloseDialogAction = () =>
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
            if (sender is FrameworkElement element && element.Tag is Models.InmuebleDto inmueble)
            {
                inmueble.Expand = !inmueble.Expand;

                if (inmueble.Expand && !inmueble.RelacionesLoaded && !string.IsNullOrWhiteSpace(inmueble.Identificador))
                {
                    await LoadRelacionesForInmuebleAsync(inmueble);
                }

                if (inmueble.Expand && inmueble.HasUbicacion && inmueble.Ubicacion == null)
                {
                    await LoadUbicacionForInmuebleAsync(inmueble);
                }
            }
        }

        private async void ToggleExpandButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is Models.InmuebleDto inmueble)
            {
                inmueble.Expand = !inmueble.Expand;

                if (inmueble.Expand && !inmueble.RelacionesLoaded && !string.IsNullOrWhiteSpace(inmueble.Identificador))
                {
                    await LoadRelacionesForInmuebleAsync(inmueble);
                }

                if (inmueble.Expand && inmueble.HasUbicacion && inmueble.Ubicacion == null)
                {
                    await LoadUbicacionForInmuebleAsync(inmueble);
                }
            }
        }

        private async Task LoadRelacionesForInmuebleAsync(Models.InmuebleDto inmueble)
        {
            if (inmueble.IsLoadingRelaciones || string.IsNullOrWhiteSpace(inmueble.Identificador))
                return;

            try
            {
                inmueble.IsLoadingRelaciones = true;

                var relaciones = await _relacionService.GetRelacionesAsync(inmueble.Identificador, 0);

                inmueble.Relaciones.Clear();
                foreach (var relacion in relaciones)
                {
                    inmueble.Relaciones.Add(relacion);
                }

                inmueble.RelacionesLoaded = true;
                inmueble.NotifyNoRelacionesMessageChanged();
            }
            catch (Exception)
            {
                inmueble.RelacionesLoaded = true;
                inmueble.NotifyNoRelacionesMessageChanged();
            }
            finally
            {
                inmueble.IsLoadingRelaciones = false;
            }
        }

        private async void DeleteRelacionButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement element || element.Tag is not Models.RelacionClienteDto relacion)
                return;

            var inmueble = ViewModel.Inmuebles.FirstOrDefault(im => im.Relaciones.Contains(relacion));
            if (inmueble == null || string.IsNullOrWhiteSpace(inmueble.Identificador))
                return;

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
                    var success = await _relacionService.DeleteRelacionAsync(inmueble.Identificador, relacion.IdCliente);

                    if (success)
                    {
                        _activityService.Registrar("Inmuebles", "Relación eliminada");
                        inmueble.Relaciones.Remove(relacion);
                        inmueble.NotifyNoRelacionesMessageChanged();

                        await _notificacionService.MostrarAsync("Relación eliminada", "Relación eliminada correctamente");
                    }
                    else
                    {
                        await _notificacionService.MostrarAsync("Error", "No se pudo eliminar la relación. Por favor, intente nuevamente.");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error al eliminar relación: {ex.GetType().Name} - {ex.Message}");
                    await _notificacionService.MostrarAsync("Error", "Ocurrió un error al eliminar la relación. Por favor, intente nuevamente.");
                }
            }
        }

        private async void EditRelacionButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement element || element.Tag is not Models.RelacionClienteDto relacion)
                return;

            var inmueble = ViewModel.Inmuebles.FirstOrDefault(im => im.Relaciones.Contains(relacion));
            if (inmueble == null || string.IsNullOrWhiteSpace(inmueble.Identificador))
                return;

            var notaTextBox = new TextBox
            {
                Text = relacion.Nota ?? string.Empty,
                PlaceholderText = "Ingrese una nota...",
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                MinHeight = 100,
                MaxHeight = 200
            };

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

                    var success = await _relacionService.UpdateNotaAsync(inmueble.Identificador, relacion.IdCliente, nuevaNota);

                    if (success)
                    {
                        _activityService.Registrar("Inmuebles", "Relación modificada");
                        relacion.Nota = nuevaNota;

                        inmueble.RelacionesLoaded = false;
                        await LoadRelacionesForInmuebleAsync(inmueble);

                        await _notificacionService.MostrarAsync("Nota actualizada", "Nota actualizada correctamente");
                    }
                    else
                    {
                        await _notificacionService.MostrarAsync("Error", "No se pudo actualizar la nota. Por favor, intente nuevamente.");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error al actualizar nota: {ex.GetType().Name} - {ex.Message}");
                    await _notificacionService.MostrarAsync("Error", "Ocurrió un error al actualizar la nota. Por favor, intente nuevamente.");
                }
            }
        }

        private async void NuevaRelacion_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement element || element.Tag is not Models.InmuebleDto inmueble)
                return;

            if (string.IsNullOrWhiteSpace(inmueble.Identificador))
                return;

            var seleccionarClienteControl = new SeleccionarClienteUserControl();

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
                    var success = await _relacionService.CreateRelacionAsync(
                        inmueble.Identificador,
                        selectedCliente.IdCliente,
                        nota);

                    if (success)
                    {
                        _activityService.Registrar("Inmuebles", "Relación creada");
                        inmueble.RelacionesLoaded = false;
                        await LoadRelacionesForInmuebleAsync(inmueble);

                        await _notificacionService.MostrarAsync("Relación creada", $"Relación con el cliente \"{selectedCliente.RazonSocial}\" creada correctamente");
                    }
                    else
                    {
                        await _notificacionService.MostrarAsync("Error", "No se pudo crear la relación. Es posible que ya exista una relación con este cliente.");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error al crear relación: {ex.GetType().Name} - {ex.Message}");
                    await _notificacionService.MostrarAsync("Error", "Ocurrió un error al crear la relación. Por favor, intente nuevamente.");
                }
            }
        }

        private async Task LoadUbicacionForInmuebleAsync(Models.InmuebleDto inmueble)
        {
            if (inmueble.IsLoadingUbicacion || !inmueble.IdUbicacion.HasValue || inmueble.IdUbicacion.Value <= 0)
                return;

            try
            {
                inmueble.IsLoadingUbicacion = true;

                var ubicacion = await _ubicacionService.GetUbicacionByIdAsync(inmueble.IdUbicacion.Value);

                if (ubicacion != null)
                {
                    inmueble.Ubicacion = ubicacion;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cargar ubicación: {ex.GetType().Name} - {ex.Message}");
            }
            finally
            {
                inmueble.IsLoadingUbicacion = false;
            }
        }

        private async void AgregarUbicacionButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement element || element.Tag is not Models.InmuebleDto inmueble)
                return;

            await MostrarDialogoSeleccionUbicacionAsync(inmueble, "Ubicación asignada");
        }

        private async void CrearUbicacionButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement element || element.Tag is not Models.InmuebleDto inmueble)
                return;

            try
            {
                var ubicacionWindow = new Views.Windows.UbicacionWindow();
                ubicacionWindow.Activate();

                var tcs = new TaskCompletionSource<bool>();
                ubicacionWindow.Closed += (_, _) => tcs.TrySetResult(true);
                await tcs.Task;

                if (ubicacionWindow.UbicacionCreada != null)
                {
                    var creada = ubicacionWindow.UbicacionCreada;

                    var updateData = new Models.InmuebleQueryDto
                    {
                        IdUbicacion = creada.IdUbicacion
                    };

                    var success = await ViewModel.UpdateInmuebleAsync(inmueble.IdInmueble, updateData);

                    if (success)
                    {
                        _activityService.Registrar("Inmuebles", "Ubicación creada y asignada");
                        inmueble.IdUbicacion = creada.IdUbicacion;
                        inmueble.Ubicacion = creada;
                        await _notificacionService.MostrarAsync("Ubicación asignada",
                            $"Ubicación \"{creada.Nombre}\" creada y asignada correctamente");
                    }
                    else
                    {
                        await _notificacionService.MostrarAsync("Error",
                            "La ubicación se creó pero no se pudo asignar al inmueble.");
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
            if (sender is not FrameworkElement element || element.Tag is not Models.InmuebleDto inmueble)
                return;

            await MostrarDialogoSeleccionUbicacionAsync(inmueble, "Ubicación modificada");
        }

        private async Task MostrarDialogoSeleccionUbicacionAsync(Models.InmuebleDto inmueble, string actividadTitulo = "Ubicación asignada")
        {
            var seleccionarUbicacionControl = new SeleccionarUbicacionUserControl();

            try
            {
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

                if (selectedUbicacion.IdArea == null)
                {
                    await _notificacionService.MostrarAsync(
                        "Ubicación sin área",
                        "La ubicación seleccionada no pertenece a ningún área definida. Por favor seleccione una ubicación dentro de un área.");
                    return;
                }

                try
                {
                    var updateData = new Models.InmuebleQueryDto
                    {
                        IdUbicacion = selectedUbicacion.IdUbicacion
                    };

                    var success = await ViewModel.UpdateInmuebleAsync(inmueble.IdInmueble, updateData);

                    if (success)
                    {
                        _activityService.Registrar("Inmuebles", actividadTitulo);
                        inmueble.IdUbicacion = selectedUbicacion.IdUbicacion;
                        inmueble.Ubicacion = selectedUbicacion;

                        await _notificacionService.MostrarAsync("Ubicación actualizada", $"Ubicación \"{selectedUbicacion.Nombre}\" asignada correctamente");
                    }
                    else
                    {
                        await _notificacionService.MostrarAsync("Error", "No se pudo actualizar la ubicación. Por favor, intente nuevamente.");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error al actualizar ubicación: {ex.GetType().Name} - {ex.Message}");

                    var errorMessage = "Ocurrió un error al actualizar la ubicación. ";
                    if (ex is System.Net.Http.HttpRequestException)
                    {
                        errorMessage += "Por favor, verifique su conexión a internet e intente nuevamente.";
                    }
                    else
                    {
                        errorMessage += "Por favor, intente nuevamente o contacte al soporte técnico.";
                    }

                    await _notificacionService.MostrarAsync("Error", errorMessage);
                }
            }
        }

        /// <summary>
        /// Maneja el evento click del botón "Ver en Mapa".
        /// Navega a la página de Ubicaciones con el ID de ubicación del inmueble seleccionado.
        /// </summary>
        private void VerEnMapaButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement element || element.Tag is not Models.InmuebleDto inmueble)
                return;

            if (inmueble.Ubicacion == null || !inmueble.IdUbicacion.HasValue)
                return;

            Frame.Navigate(typeof(UbicacionesPage), inmueble.IdUbicacion.Value);
        }

        private void ToggleFiltros_Click(object sender, RoutedEventArgs e)
        {
            Filtros.Visibility = Filtros.Visibility == Visibility.Visible
                ? Visibility.Collapsed
                : Visibility.Visible;
        }
    }
}
