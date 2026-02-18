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
using Advance_Control.Services.Notificacion;
using Advance_Control.Services.Logging;
using Advance_Control.Services.Contactos;
using Advance_Control.Models;
using Advance_Control.Views.Dialogs;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Advance_Control.Views
{
    /// <summary>
    /// Página para visualizar y gestionar clientes
    /// </summary>
    public sealed partial class ClientesView : Page
    {
        public CustomersViewModel ViewModel { get; }
        private readonly INotificacionService _notificacionService;
        private readonly ILoggingService _loggingService;
        private readonly IContactoService _contactoService;

        public ClientesView()
        {
            // Resolver el ViewModel desde DI
            ViewModel = ((App)Application.Current).Host.Services.GetRequiredService<CustomersViewModel>();
            
            // Resolver el servicio de notificaciones desde DI
            _notificacionService = ((App)Application.Current).Host.Services.GetRequiredService<INotificacionService>();
            
            // Resolver el servicio de logging desde DI
            _loggingService = ((App)Application.Current).Host.Services.GetRequiredService<ILoggingService>();
            
            // Resolver el servicio de contactos desde DI
            _contactoService = ((App)Application.Current).Host.Services.GetRequiredService<IContactoService>();
            
            this.InitializeComponent();
            
            // Establecer el DataContext para los bindings
            this.DataContext = ViewModel;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            
            // Cargar los clientes cuando se navega a esta página
            await ViewModel.LoadClientesAsync();
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.LoadClientesAsync();
        }

        private async void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.ClearFiltersAsync();
        }

        private async void NuevoButton_Click(object sender, RoutedEventArgs e)
        {
            // Crear el UserControl para el nuevo cliente
            var nuevoClienteControl = new NuevoClienteUserControl();

            var dialog = new ContentDialog
            {
                Title = "Nuevo Cliente",
                Content = nuevoClienteControl,
                PrimaryButtonText = "Guardar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                // Validar campos requeridos
                if (!nuevoClienteControl.IsValid)
                {
                    await _notificacionService.MostrarNotificacionAsync(
                        titulo: "Validación",
                        nota: "Por favor complete todos los campos obligatorios (RFC, Razón Social y Nombre Comercial)",
                        fechaHoraInicio: DateTime.Now);
                    return;
                }

                try
                {
                    var success = await ViewModel.CreateClienteAsync(
                        rfc: nuevoClienteControl.Rfc,
                        razonSocial: nuevoClienteControl.RazonSocial,
                        nombreComercial: nuevoClienteControl.NombreComercial,
                        regimenFiscal: nuevoClienteControl.RegimenFiscal,
                        usoCfdi: nuevoClienteControl.UsoCfdi,
                        diasCredito: nuevoClienteControl.DiasCredito,
                        limiteCredito: nuevoClienteControl.LimiteCredito,
                        prioridad: nuevoClienteControl.Prioridad,
                        notas: nuevoClienteControl.Notas,
                        estatus: nuevoClienteControl.Estatus
                    );

                    if (success)
                    {
                        await _notificacionService.MostrarNotificacionAsync(
                            titulo: "Cliente creado",
                            nota: $"Cliente \"{nuevoClienteControl.NombreComercial}\" creado correctamente",
                            fechaHoraInicio: DateTime.Now);
                    }
                    else
                    {
                        await _notificacionService.MostrarNotificacionAsync(
                            titulo: "Error",
                            nota: "No se pudo crear el cliente. Verifique los datos e intente nuevamente.",
                            fechaHoraInicio: DateTime.Now);
                    }
                }
                catch (Exception ex)
                {
                    await _loggingService.LogErrorAsync("Error al crear cliente desde la UI", ex, "ClientesView", "NuevoButton_Click");
                    
                    await _notificacionService.MostrarNotificacionAsync(
                        titulo: "Error",
                        nota: "Ocurrió un error al crear el cliente. Por favor, intente nuevamente.",
                        fechaHoraInicio: DateTime.Now);
                }
            }
        }

        private async void HeadGrid_Tapped(object sender, TappedRoutedEventArgs e)
        {
            // Get the CustomerDto from the sender's Tag property
            if (sender is FrameworkElement element && element.Tag is Models.CustomerDto customer)
            {
                customer.Expand = !customer.Expand;
                
                // Load contactos when expanding if not already loaded
                if (customer.Expand && !customer.ContactosLoaded)
                {
                    await LoadContactosForClienteAsync(customer);
                }
            }
        }

        private async void ToggleExpandButton_Click(object sender, RoutedEventArgs e)
        {
            // Get the CustomerDto from the sender's Tag property
            if (sender is FrameworkElement element && element.Tag is Models.CustomerDto customer)
            {
                customer.Expand = !customer.Expand;
                
                // Load contactos when expanding if not already loaded
                if (customer.Expand && !customer.ContactosLoaded)
                {
                    await LoadContactosForClienteAsync(customer);
                }
            }
        }

        private async System.Threading.Tasks.Task LoadContactosForClienteAsync(Models.CustomerDto cliente)
        {
            if (cliente.IsLoadingContactos)
                return;

            try
            {
                cliente.IsLoadingContactos = true;
                
                var contactoQuery = new ContactoQueryDto { IdCliente = cliente.IdCliente };
                var contactos = await _contactoService.GetContactosAsync(contactoQuery);
                
                cliente.Contactos.Clear();
                foreach (var contacto in contactos)
                {
                    cliente.Contactos.Add(contacto);
                }
                
                cliente.ContactosLoaded = true;
                cliente.NotifyNoContactosMessageChanged();
            }
            catch (Exception ex)
            {
                // Log error - the UI will show empty list
                await _loggingService.LogErrorAsync("Error al cargar contactos del cliente", ex, "ClientesView", "LoadContactosForClienteAsync");
                cliente.ContactosLoaded = true;
                cliente.NotifyNoContactosMessageChanged();
            }
            finally
            {
                cliente.IsLoadingContactos = false;
            }
        }

        private async void NuevoContacto_Click(object sender, RoutedEventArgs e)
        {
            // Obtener el cliente desde el Tag del botón
            if (sender is not FrameworkElement element || element.Tag is not Models.CustomerDto cliente)
                return;

            try
            {
                // Obtener contactos sin cliente asignado (IdCliente = 0 o null)
                var contactosSinCliente = await _contactoService.GetContactosAsync(new ContactoQueryDto { IdCliente = 0 });
                
                if (contactosSinCliente == null || contactosSinCliente.Count == 0)
                {
                    await _notificacionService.MostrarNotificacionAsync(
                        titulo: "Sin contactos disponibles",
                        nota: "No hay contactos sin cliente asignado disponibles para agregar.",
                        fechaHoraInicio: DateTime.Now);
                    return;
                }

                // Crear ListView para seleccionar contacto
                var contactoListView = new ListView
                {
                    SelectionMode = ListViewSelectionMode.Single,
                    MaxHeight = 300
                };

                foreach (var contacto in contactosSinCliente)
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
                                Foreground = new SolidColorBrush(Microsoft.UI.Colors.Gray)
                            },
                            new TextBlock
                            {
                                Text = !string.IsNullOrWhiteSpace(contacto.Correo) ? contacto.Correo :
                                       !string.IsNullOrWhiteSpace(contacto.Telefono) ? contacto.Telefono : "",
                                FontSize = 11,
                                Foreground = new SolidColorBrush(Microsoft.UI.Colors.DimGray),
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
                            Text = $"Seleccione un contacto para asignar al cliente \"{cliente.NombreComercial}\":",
                            TextWrapping = TextWrapping.Wrap
                        },
                        contactoListView
                    }
                };

                var dialog = new ContentDialog
                {
                    Title = "Agregar Contacto",
                    Content = dialogContent,
                    PrimaryButtonText = "Agregar",
                    CloseButtonText = "Cancelar",
                    DefaultButton = ContentDialogButton.Primary,
                    XamlRoot = this.XamlRoot
                };

                var result = await dialog.ShowAsync();

                if (result == ContentDialogResult.Primary && contactoListView.SelectedItem is ListViewItem selectedItem 
                    && selectedItem.Tag is ContactoDto selectedContacto)
                {
                    // Actualizar el idCliente del contacto
                    var updateDto = new ContactoEditDto
                    {
                        ContactoId = selectedContacto.ContactoId,
                        IdCliente = cliente.IdCliente,
                        // Mantener los demás campos
                        Nombre = selectedContacto.Nombre,
                        Apellido = selectedContacto.Apellido,
                        Correo = selectedContacto.Correo,
                        Telefono = selectedContacto.Telefono,
                        Departamento = selectedContacto.Departamento,
                        CodigoInterno = selectedContacto.CodigoInterno,
                        Activo = selectedContacto.Activo,
                        Notas = selectedContacto.Notas,
                        IdProveedor = selectedContacto.IdProveedor,
                        Cargo = selectedContacto.Cargo
                    };

                    var updateResult = await _contactoService.UpdateContactoAsync(updateDto);

                    if (updateResult.Success)
                    {
                        // Recargar contactos del cliente
                        cliente.ContactosLoaded = false;
                        await LoadContactosForClienteAsync(cliente);

                        await _notificacionService.MostrarNotificacionAsync(
                            titulo: "Contacto agregado",
                            nota: $"Contacto \"{selectedContacto.NombreCompleto}\" agregado correctamente",
                            fechaHoraInicio: DateTime.Now);
                    }
                    else
                    {
                        await _notificacionService.MostrarNotificacionAsync(
                            titulo: "Error",
                            nota: "No se pudo agregar el contacto. Por favor, intente nuevamente.",
                            fechaHoraInicio: DateTime.Now);
                    }
                }
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Error al agregar contacto", ex, "ClientesView", "NuevoContacto_Click");
                await _notificacionService.MostrarNotificacionAsync(
                    titulo: "Error",
                    nota: "Ocurrió un error al agregar el contacto. Por favor, intente nuevamente.",
                    fechaHoraInicio: DateTime.Now);
            }
        }

        private async void DeleteContactoButton_Click(object sender, RoutedEventArgs e)
        {
            // Obtener el contacto desde el Tag del botón
            if (sender is not FrameworkElement element || element.Tag is not ContactoDto contacto)
                return;

            // Buscar el cliente que contiene este contacto
            var cliente = ViewModel.Customers.FirstOrDefault(c => c.Contactos.Contains(contacto));
            if (cliente == null)
                return;

            // Mostrar diálogo de confirmación
            var dialog = new ContentDialog
            {
                Title = "Confirmar eliminación",
                Content = $"¿Está seguro de que desea quitar el contacto \"{contacto.NombreCompleto}\" de este cliente?",
                PrimaryButtonText = "Quitar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                try
                {
                    // Actualizar el idCliente del contacto a 0 (sin cliente)
                    var updateDto = new ContactoEditDto
                    {
                        ContactoId = contacto.ContactoId,
                        IdCliente = 0,
                        // Mantener los demás campos
                        Nombre = contacto.Nombre,
                        Apellido = contacto.Apellido,
                        Correo = contacto.Correo,
                        Telefono = contacto.Telefono,
                        Departamento = contacto.Departamento,
                        CodigoInterno = contacto.CodigoInterno,
                        Activo = contacto.Activo,
                        Notas = contacto.Notas,
                        IdProveedor = contacto.IdProveedor,
                        Cargo = contacto.Cargo
                    };

                    var updateResult = await _contactoService.UpdateContactoAsync(updateDto);

                    if (updateResult.Success)
                    {
                        // Eliminar el contacto de la colección local
                        cliente.Contactos.Remove(contacto);
                        cliente.NotifyNoContactosMessageChanged();

                        await _notificacionService.MostrarNotificacionAsync(
                            titulo: "Contacto quitado",
                            nota: "Contacto quitado del cliente correctamente",
                            fechaHoraInicio: DateTime.Now);
                    }
                    else
                    {
                        await _notificacionService.MostrarNotificacionAsync(
                            titulo: "Error",
                            nota: "No se pudo quitar el contacto. Por favor, intente nuevamente.",
                            fechaHoraInicio: DateTime.Now);
                    }
                }
                catch (Exception ex)
                {
                    await _loggingService.LogErrorAsync("Error al quitar contacto del cliente", ex, "ClientesView", "DeleteContactoButton_Click");
                    await _notificacionService.MostrarNotificacionAsync(
                        titulo: "Error",
                        nota: "Ocurrió un error al quitar el contacto. Por favor, intente nuevamente.",
                        fechaHoraInicio: DateTime.Now);
                }
            }
        }

        private async void EditClienteButton_Click(object sender, RoutedEventArgs e)
        {
            // Obtener el cliente desde el Tag del botón
            if (sender is not FrameworkElement element || element.Tag is not Models.CustomerDto cliente)
                return;

            try
            {
                // Crear los campos del formulario con los valores actuales
                var rfcTextBox = new TextBox
                {
                    Text = cliente.Rfc ?? "",
                    PlaceholderText = "RFC del cliente (requerido)",
                    MaxLength = 13,
                    Margin = new Thickness(0, 0, 0, 8)
                };

                var razonSocialTextBox = new TextBox
                {
                    Text = cliente.RazonSocial ?? "",
                    PlaceholderText = "Razón social (requerido)",
                    Margin = new Thickness(0, 0, 0, 8)
                };

                var nombreComercialTextBox = new TextBox
                {
                    Text = cliente.NombreComercial ?? "",
                    PlaceholderText = "Nombre comercial (requerido)",
                    Margin = new Thickness(0, 0, 0, 8)
                };

                var regimenFiscalTextBox = new TextBox
                {
                    Text = cliente.RegimenFiscal ?? "",
                    PlaceholderText = "Régimen fiscal (opcional)",
                    Margin = new Thickness(0, 0, 0, 8)
                };

                var usoCfdiTextBox = new TextBox
                {
                    Text = cliente.UsoCfdi ?? "",
                    PlaceholderText = "Uso CFDI (opcional)",
                    Margin = new Thickness(0, 0, 0, 8)
                };

                var diasCreditoNumberBox = new NumberBox
                {
                    Value = cliente.DiasCredito.HasValue ? cliente.DiasCredito.Value : double.NaN,
                    PlaceholderText = "Días de crédito",
                    Minimum = 0,
                    SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline,
                    Margin = new Thickness(0, 0, 0, 8)
                };

                var limiteCreditoNumberBox = new NumberBox
                {
                    Value = cliente.LimiteCredito.HasValue ? (double)cliente.LimiteCredito.Value : double.NaN,
                    PlaceholderText = "Límite de crédito",
                    Minimum = 0,
                    SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline,
                    Margin = new Thickness(0, 0, 0, 8)
                };

                var prioridadNumberBox = new NumberBox
                {
                    Value = cliente.Prioridad != 0 ? cliente.Prioridad : double.NaN,
                    PlaceholderText = "Prioridad (0-10)",
                    Minimum = 0,
                    Maximum = 10,
                    SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline,
                    Margin = new Thickness(0, 0, 0, 8)
                };

                var notasTextBox = new TextBox
                {
                    Text = cliente.Notas ?? "",
                    PlaceholderText = "Notas adicionales (opcional)",
                    AcceptsReturn = true,
                    TextWrapping = TextWrapping.Wrap,
                    MinHeight = 80,
                    MaxHeight = 150,
                    Margin = new Thickness(0, 0, 0, 8)
                };

                var estatusCheckBox = new CheckBox
                {
                    Content = "Activo",
                    IsChecked = cliente.Estatus
                };

                var dialogContent = new ScrollViewer
                {
                    Content = new StackPanel
                    {
                        Spacing = 8,
                        Children =
                        {
                            new TextBlock { Text = "RFC:", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold },
                            rfcTextBox,
                            new TextBlock { Text = "Razón Social:", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold },
                            razonSocialTextBox,
                            new TextBlock { Text = "Nombre Comercial:", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold },
                            nombreComercialTextBox,
                            new TextBlock { Text = "Régimen Fiscal:" },
                            regimenFiscalTextBox,
                            new TextBlock { Text = "Uso CFDI:" },
                            usoCfdiTextBox,
                            new TextBlock { Text = "Días de Crédito:" },
                            diasCreditoNumberBox,
                            new TextBlock { Text = "Límite de Crédito:" },
                            limiteCreditoNumberBox,
                            new TextBlock { Text = "Prioridad:" },
                            prioridadNumberBox,
                            new TextBlock { Text = "Notas:" },
                            notasTextBox,
                            estatusCheckBox
                        }
                    },
                    MaxHeight = 500
                };

                var dialog = new ContentDialog
                {
                    Title = "Editar Cliente",
                    Content = dialogContent,
                    PrimaryButtonText = "Guardar",
                    CloseButtonText = "Cancelar",
                    DefaultButton = ContentDialogButton.Primary,
                    XamlRoot = this.XamlRoot
                };

                var result = await dialog.ShowAsync();

                if (result == ContentDialogResult.Primary)
                {
                    // Validar campos requeridos
                    if (string.IsNullOrWhiteSpace(rfcTextBox.Text))
                    {
                        await _notificacionService.MostrarNotificacionAsync(
                            titulo: "Validación",
                            nota: "El RFC es obligatorio",
                            fechaHoraInicio: DateTime.Now);
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(razonSocialTextBox.Text))
                    {
                        await _notificacionService.MostrarNotificacionAsync(
                            titulo: "Validación",
                            nota: "La razón social es obligatoria",
                            fechaHoraInicio: DateTime.Now);
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(nombreComercialTextBox.Text))
                    {
                        await _notificacionService.MostrarNotificacionAsync(
                            titulo: "Validación",
                            nota: "El nombre comercial es obligatorio",
                            fechaHoraInicio: DateTime.Now);
                        return;
                    }

                    // Convertir valores de NumberBox de manera segura
                    int? diasCredito = null;
                    if (!double.IsNaN(diasCreditoNumberBox.Value))
                    {
                        diasCredito = Convert.ToInt32(Math.Round(diasCreditoNumberBox.Value));
                    }

                    decimal? limiteCredito = null;
                    if (!double.IsNaN(limiteCreditoNumberBox.Value))
                    {
                        limiteCredito = Convert.ToDecimal(limiteCreditoNumberBox.Value);
                    }

                    int? prioridad = null;
                    if (!double.IsNaN(prioridadNumberBox.Value))
                    {
                        prioridad = Convert.ToInt32(Math.Round(prioridadNumberBox.Value));
                    }

                    var success = await ViewModel.UpdateClienteAsync(
                        idCliente: cliente.IdCliente,
                        rfc: rfcTextBox.Text.Trim(),
                        razonSocial: razonSocialTextBox.Text.Trim(),
                        nombreComercial: nombreComercialTextBox.Text.Trim(),
                        regimenFiscal: string.IsNullOrWhiteSpace(regimenFiscalTextBox.Text) ? null : regimenFiscalTextBox.Text.Trim(),
                        usoCfdi: string.IsNullOrWhiteSpace(usoCfdiTextBox.Text) ? null : usoCfdiTextBox.Text.Trim(),
                        diasCredito: diasCredito,
                        limiteCredito: limiteCredito,
                        prioridad: prioridad,
                        notas: string.IsNullOrWhiteSpace(notasTextBox.Text) ? null : notasTextBox.Text.Trim(),
                        estatus: estatusCheckBox.IsChecked ?? true
                    );

                    if (success)
                    {
                        await _notificacionService.MostrarNotificacionAsync(
                            titulo: "Cliente actualizado",
                            nota: $"Cliente \"{nombreComercialTextBox.Text.Trim()}\" actualizado correctamente",
                            fechaHoraInicio: DateTime.Now);
                    }
                    else
                    {
                        await _notificacionService.MostrarNotificacionAsync(
                            titulo: "Error",
                            nota: "No se pudo actualizar el cliente. Verifique los datos e intente nuevamente.",
                            fechaHoraInicio: DateTime.Now);
                    }
                }
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Error al editar cliente desde la UI", ex, "ClientesView", "EditClienteButton_Click");
                
                await _notificacionService.MostrarNotificacionAsync(
                    titulo: "Error",
                    nota: "Ocurrió un error al editar el cliente. Por favor, intente nuevamente.",
                    fechaHoraInicio: DateTime.Now);
            }
        }

        private async void DeleteClienteButton_Click(object sender, RoutedEventArgs e)
        {
            // Obtener el cliente desde el Tag del botón
            if (sender is not FrameworkElement element || element.Tag is not Models.CustomerDto cliente)
                return;

            // Mostrar diálogo de confirmación
            var dialog = new ContentDialog
            {
                Title = "Confirmar eliminación",
                Content = $"¿Está seguro de que desea eliminar el cliente \"{cliente.NombreComercial}\" (RFC: {cliente.Rfc})?",
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
                    var success = await ViewModel.DeleteClienteAsync(cliente.IdCliente);

                    if (success)
                    {
                        await _notificacionService.MostrarNotificacionAsync(
                            titulo: "Cliente eliminado",
                            nota: "Cliente eliminado correctamente",
                            fechaHoraInicio: DateTime.Now);
                    }
                    else
                    {
                        await _notificacionService.MostrarNotificacionAsync(
                            titulo: "Error",
                            nota: "No se pudo eliminar el cliente. Por favor, intente nuevamente.",
                            fechaHoraInicio: DateTime.Now);
                    }
                }
                catch (Exception ex)
                {
                    await _loggingService.LogErrorAsync("Error al eliminar cliente desde la UI", ex, "ClientesView", "DeleteClienteButton_Click");
                    await _notificacionService.MostrarNotificacionAsync(
                        titulo: "Error",
                        nota: "Ocurrió un error al eliminar el cliente. Por favor, intente nuevamente.",
                        fechaHoraInicio: DateTime.Now);
                }
            }
        }
    }
}
