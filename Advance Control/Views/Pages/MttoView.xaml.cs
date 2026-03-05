using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Extensions.DependencyInjection;
using Advance_Control.ViewModels;
using Advance_Control.Services.Notificacion;
using Advance_Control.Services.Logging;
using Advance_Control.Utilities;
using Advance_Control.Services.UserInfo;
using Advance_Control.Services.Contactos;
using Advance_Control.Services.Activity;
using Advance_Control.Views.Dialogs;
using Advance_Control.Models;

namespace Advance_Control.Views
{
    /// <summary>
    /// Página para visualizar y gestionar operaciones de mantenimiento
    /// </summary>
    public sealed partial class MttoView : Page
    {
        public MttoViewModel ViewModel { get; }
        private readonly INotificacionService _notificacionService;
        private readonly IUserInfoService _userInfoService;
        private readonly IContactoService _contactoService;
        private readonly IActivityService _activityService;

        public MttoView()
        {
            // Resolver el ViewModel desde DI
            ViewModel = AppServices.Get<MttoViewModel>();

            // Resolver el servicio de notificaciones desde DI
            _notificacionService = AppServices.Get<INotificacionService>();

            // Resolver el servicio de información de usuario desde DI
            _userInfoService = AppServices.Get<IUserInfoService>();

            // Resolver el servicio de contactos desde DI
            _contactoService = AppServices.Get<IContactoService>();

            // Resolver el servicio de actividades desde DI
            _activityService = AppServices.Get<IActivityService>();

            this.InitializeComponent();
            ButtonClickLogger.Attach(this, AppServices.Get<ILoggingService>(), nameof(MttoView));
            
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
                    await _notificacionService.MostrarAsync("Error de validación", "Debe seleccionar un tipo de mantenimiento.");
                    return;
                }

                if (!nuevoMantenimientoControl.IdEquipo.HasValue)
                {
                    await _notificacionService.MostrarAsync("Error de validación", "Debe seleccionar un equipo.");
                    return;
                }

                if (!nuevoMantenimientoControl.IdCliente.HasValue)
                {
                    await _notificacionService.MostrarAsync("Error de validación", "Debe seleccionar un cliente.");
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
                        await _notificacionService.MostrarAsync("Mantenimiento creado", "El mantenimiento se ha creado correctamente.");
                    }
                    else
                    {
                        await _notificacionService.MostrarAsync("Error", "No se pudo crear el mantenimiento. Por favor, intente nuevamente.");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error al crear mantenimiento: {ex.GetType().Name} - {ex.Message}");
                    await _notificacionService.MostrarAsync("Error", "Ocurrió un error al crear el mantenimiento. Por favor, intente nuevamente.");
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
                        await _notificacionService.MostrarAsync("Mantenimiento eliminado", "El mantenimiento se ha eliminado correctamente.");
                    }
                    else
                    {
                        await _notificacionService.MostrarAsync("Error", "No se pudo eliminar el mantenimiento. Por favor, intente nuevamente.");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error al eliminar mantenimiento: {ex.GetType().Name} - {ex.Message}");
                    await _notificacionService.MostrarAsync("Error", "Ocurrió un error al eliminar el mantenimiento. Por favor, intente nuevamente.");
                }
            }
        }

        private async void AtenderMantenimientoButton_Click(object sender, RoutedEventArgs e)
        {
            // Obtener el mantenimiento desde el Tag del botón
            if (sender is not FrameworkElement element || element.Tag is not Models.MantenimientoDto mantenimiento)
                return;

            if (!mantenimiento.IdMantenimiento.HasValue)
                return;

            try
            {
                // Obtener la información del usuario autenticado
                var userInfo = await _userInfoService.GetUserInfoAsync();

                if (userInfo == null)
                {
                    await _notificacionService.MostrarAsync("Error", "No se pudo obtener la información del usuario autenticado.");
                    return;
                }

                // Mostrar diálogo de confirmación
                var tipoMantenimiento = mantenimiento.TipoMantenimiento ?? "sin tipo especificado";
                var dialog = new ContentDialog
                {
                    Title = "Confirmar atención",
                    Content = $"¿Está seguro de que desea marcar como atendido el mantenimiento #{mantenimiento.IdMantenimiento} ({tipoMantenimiento})?",
                    PrimaryButtonText = "Atender",
                    CloseButtonText = "Cancelar",
                    DefaultButton = ContentDialogButton.Primary,
                    XamlRoot = this.XamlRoot
                };

                var result = await dialog.ShowAsync();

                if (result == ContentDialogResult.Primary)
                {
                    var success = await ViewModel.UpdateAtendidoAsync(mantenimiento.IdMantenimiento.Value, userInfo.CredencialId);

                    if (success)
                    {
                        _activityService.Registrar("Mantenimiento", "Mant. atendido");
                        await _notificacionService.MostrarAsync("Mantenimiento atendido", "El mantenimiento se ha marcado como atendido correctamente.");
                    }
                    else
                    {
                        await _notificacionService.MostrarAsync("Error", "No se pudo marcar el mantenimiento como atendido. Por favor, intente nuevamente.");
                    }
                }
            }
            catch (Exception)
            {
                // Error is already logged in ViewModel and Service layers
                await _notificacionService.MostrarAsync("Error", "Ocurrió un error al atender el mantenimiento. Por favor, intente nuevamente.");
            }
        }

        private async void AtenderComoButton_Click(object sender, RoutedEventArgs e)
        {
            // Obtener el mantenimiento desde el Tag del botón
            if (sender is not FrameworkElement element || element.Tag is not Models.MantenimientoDto mantenimiento)
                return;

            if (!mantenimiento.IdMantenimiento.HasValue)
                return;

            try
            {
                // Obtener todos los contactos disponibles
                var contactos = await _contactoService.GetContactosAsync(new ContactoQueryDto());

                if (contactos == null || contactos.Count == 0)
                {
                    await _notificacionService.MostrarAsync("Sin contactos", "No hay contactos disponibles para atender el mantenimiento.");
                    return;
                }

                // Crear ListView para seleccionar contacto
                var contactoListView = new ListView
                {
                    SelectionMode = ListViewSelectionMode.Single,
                    MaxHeight = 400
                };

                foreach (var contacto in contactos)
                {
                    var itemContent = new StackPanel
                    {
                        Spacing = 2,
                        Children =
                        {
                            new TextBlock
                            {
                                Text = contacto.NombreCompleto,
                                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
                            },
                            new TextBlock
                            {
                                Text = contacto.Cargo ?? "Sin cargo",
                                FontSize = 12,
                                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray)
                            },
                            new TextBlock
                            {
                                Text = !string.IsNullOrWhiteSpace(contacto.Correo) ? contacto.Correo :
                                       !string.IsNullOrWhiteSpace(contacto.Telefono) ? contacto.Telefono : "",
                                FontSize = 11,
                                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.DimGray),
                                Visibility = (!string.IsNullOrWhiteSpace(contacto.Correo) || !string.IsNullOrWhiteSpace(contacto.Telefono))
                                    ? Visibility.Visible : Visibility.Collapsed
                            }
                        }
                    };

                    contactoListView.Items.Add(new ListViewItem { Content = itemContent, Tag = contacto });
                }

                var dialogContent = new StackPanel
                {
                    Spacing = 8,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = $"Seleccione un contacto para atender el mantenimiento #{mantenimiento.IdMantenimiento}:",
                            TextWrapping = TextWrapping.Wrap,
                            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
                        },
                        contactoListView
                    }
                };

                var dialog = new ContentDialog
                {
                    Title = "Atender Como Contacto",
                    Content = dialogContent,
                    PrimaryButtonText = "Atender",
                    CloseButtonText = "Cancelar",
                    DefaultButton = ContentDialogButton.Primary,
                    XamlRoot = this.XamlRoot
                };

                var result = await dialog.ShowAsync();

                if (result == ContentDialogResult.Primary && contactoListView.SelectedItem is ListViewItem selectedItem 
                    && selectedItem.Tag is ContactoDto selectedContacto)
                {
                    // Atender el mantenimiento con el contacto seleccionado
                    var tipoMantenimiento = mantenimiento.TipoMantenimiento ?? "sin tipo especificado";
                    var confirmDialog = new ContentDialog
                    {
                        Title = "Confirmar atención",
                        Content = $"¿Está seguro de que desea marcar como atendido el mantenimiento #{mantenimiento.IdMantenimiento} ({tipoMantenimiento}) por el contacto \"{selectedContacto.NombreCompleto}\"?",
                        PrimaryButtonText = "Atender",
                        CloseButtonText = "Cancelar",
                        DefaultButton = ContentDialogButton.Primary,
                        XamlRoot = this.XamlRoot
                    };

                    var confirmResult = await confirmDialog.ShowAsync();

                    if (confirmResult == ContentDialogResult.Primary)
                    {
                        var success = await ViewModel.UpdateAtendidoAsync(
                            mantenimiento.IdMantenimiento.Value,
                            Convert.ToInt32(selectedContacto.ContactoId) // Conversión segura de long a int
                        );

                        if (success)
                        {
                            _activityService.Registrar("Mantenimiento", "Atendido como tipo");
                            await _notificacionService.MostrarAsync("Mantenimiento atendido", $"El mantenimiento se ha marcado como atendido por {selectedContacto.NombreCompleto}.");
                        }
                        else
                        {
                            await _notificacionService.MostrarAsync("Error", "No se pudo marcar el mantenimiento como atendido. Por favor, intente nuevamente.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al atender mantenimiento como contacto: {ex.GetType().Name} - {ex.Message}");
                await _notificacionService.MostrarAsync("Error", "Ocurrió un error al atender el mantenimiento. Por favor, intente nuevamente.");
            }
        }
    }
}
