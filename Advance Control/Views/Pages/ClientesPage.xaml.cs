using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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
using Advance_Control.Services.Notificacion;
using Advance_Control.Services.Logging;
using Advance_Control.Services.Contactos;
using Advance_Control.Services.Clientes;
using Advance_Control.Services.Activity;
using Advance_Control.Services.SatCatalogo;
using Advance_Control.Services.Suscripciones;
using Advance_Control.Models;
using Advance_Control.Views.Dialogs;
using Advance_Control.Utilities;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Advance_Control.Views.Pages
{
    /// <summary>
    /// Página para visualizar y gestionar clientes
    /// </summary>
    public sealed partial class ClientesPage : Page
    {
        public CustomersViewModel ViewModel { get; }
        private readonly INotificacionService _notificacionService;
        private readonly ILoggingService _loggingService;
        private readonly IContactoService _contactoService;
        private readonly IActivityService _activityService;
        private readonly IContratoSuscripcionService _contratoSuscripcionService;

        public ClientesPage()
        {
            // Resolver el ViewModel desde DI
            ViewModel = AppServices.Get<CustomersViewModel>();
            
            // Resolver el servicio de notificaciones desde DI
            _notificacionService = AppServices.Get<INotificacionService>();
            
            // Resolver el servicio de logging desde DI
            _loggingService = AppServices.Get<ILoggingService>();
            
            // Resolver el servicio de contactos desde DI
            _contactoService = AppServices.Get<IContactoService>();

            // Resolver el servicio de actividades desde DI
            _activityService = AppServices.Get<IActivityService>();

            // Resolver el servicio de contratos de suscripción desde DI
            _contratoSuscripcionService = AppServices.Get<IContratoSuscripcionService>();
            
            this.InitializeComponent();
            ButtonClickLogger.Attach(this, _loggingService, nameof(ClientesPage));

            // Establecer el DataContext para los bindings
            this.DataContext = ViewModel;

            AutoSuggestHelper.Conectar(BusquedaAutoSuggestBox, () => ViewModel.ValoresRazonSocial, () => _ = ViewModel.LoadClientesAsync());
            AutoSuggestHelper.Conectar(RfcAutoSuggestBox, () => ViewModel.ValoresRfc, () => _ = ViewModel.LoadClientesAsync());
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            
            // Cargar los clientes cuando se navega a esta página
            await ViewModel.LoadClientesAsync();
        }

        private async void FiltroTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key != global::Windows.System.VirtualKey.Enter)
            {
                return;
            }

            e.Handled = true;
            await ViewModel.LoadClientesAsync();
        }

        private async void SincronizarClientesButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ViewModel.IsLoading = true;

                var clienteService = AppServices.Get<IClienteService>();
                var resultado = await clienteService.ImportarClientesDesdeFacturasAsync();

                await ViewModel.LoadClientesAsync();

                var dialog = new ContentDialog
                {
                    Title = "Sincronización de clientes",
                    Content = resultado.Cantidad > 0
                        ? $"Se importaron {resultado.Cantidad} cliente(s) nuevo(s) desde facturas."
                        : "No se encontraron clientes nuevos para importar desde facturas.",
                    CloseButtonText = "Aceptar",
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();
            }
            catch (Exception ex)
            {
                ViewModel.ErrorMessage = $"No se pudo sincronizar clientes desde facturas: {ex.Message}";
            }
            finally
            {
                ViewModel.IsLoading = false;
            }
        }

        private void ToggleFiltros_Click(object sender, RoutedEventArgs e)
        {
            Filtros.Visibility = Filtros.Visibility == Visibility.Collapsed
                ? Visibility.Visible
                : Visibility.Collapsed;
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
                    await _notificacionService.MostrarAsync("Validación", "Por favor complete todos los campos obligatorios (RFC, Razón Social y Nombre Comercial)");
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
                        estatus: nuevoClienteControl.Estatus,
                        codigoPostal: nuevoClienteControl.CodigoPostal
                    );

                    if (success)
                    {
                        await _notificacionService.MostrarAsync("Cliente creado", $"Cliente \"{nuevoClienteControl.NombreComercial}\" creado correctamente");
                    }
                    else
                    {
                        await _notificacionService.MostrarAsync("Error", "No se pudo crear el cliente. Verifique los datos e intente nuevamente.");
                    }
                }
                catch (Exception ex)
                {
                    await _loggingService.LogErrorAsync("Error al crear cliente desde la UI", ex, "ClientesPage", "NuevoButton_Click");
                    
                    await _notificacionService.MostrarAsync("Error", "Ocurrió un error al crear el cliente. Por favor, intente nuevamente.");
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

                // Load suscripcion when expanding if not already loaded
                if (customer.Expand && !customer.SuscripcionCargada)
                {
                    await LoadSuscripcionForClienteAsync(customer);
                }
            }
        }

        private async System.Threading.Tasks.Task LoadSuscripcionForClienteAsync(Models.CustomerDto cliente)
        {
            if (cliente.IsLoadingSuscripcion)
                return;

            try
            {
                cliente.IsLoadingSuscripcion = true;

                var contratos = await _contratoSuscripcionService.GetContratosAsync(cliente.IdCliente);
                cliente.Suscripcion = contratos.OrderByDescending(c => c.Id).FirstOrDefault();
                cliente.SuscripcionCargada = true;
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Error al cargar la suscripción del cliente", ex, "ClientesPage", "LoadSuscripcionForClienteAsync");
                cliente.SuscripcionCargada = true;
            }
            finally
            {
                cliente.IsLoadingSuscripcion = false;
            }
        }

        private async void GenerarContratoButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement element || element.Tag is not Models.CustomerDto cliente)
                return;

            var seleccionarNivelControl = new SeleccionarNivelSuscripcionUserControl();
            var seleccionarDialog = new ContentDialog
            {
                Title = "Generar contrato de suscripción",
                Content = seleccionarNivelControl,
                PrimaryButtonText = "Continuar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            var seleccionarResult = await seleccionarDialog.ShowAsync();
            if (seleccionarResult != ContentDialogResult.Primary || string.IsNullOrEmpty(seleccionarNivelControl.NivelSeleccionado))
                return;

            var generarControl = new GenerarContratoUserControl(seleccionarNivelControl.NivelSeleccionado, cliente.IdCliente, cliente.RazonSocial, null);
            var generarDialog = new ContentDialog
            {
                Title = $"Contrato {seleccionarNivelControl.NivelSeleccionado} — {cliente.RazonSocial}",
                Content = generarControl,
                CloseButtonText = "Cerrar",
                XamlRoot = this.XamlRoot
            };
            generarControl.CloseDialogAction = () => generarDialog.Hide();

            await generarDialog.ShowAsync();

            if (generarControl.GeneradoExitosamente)
            {
                cliente.SuscripcionCargada = false;
                await LoadSuscripcionForClienteAsync(cliente);
                await _notificacionService.MostrarAsync("Contrato generado", "El contrato de suscripción se generó y guardó correctamente.");
            }
        }

        private async void CargarDocumentoFirmadoButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement element || element.Tag is not Models.CustomerDto cliente || cliente.Suscripcion == null)
                return;

            var picker = new global::Windows.Storage.Pickers.FileOpenPicker();
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
            picker.FileTypeFilter.Add(".pdf");
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".png");

            var file = await picker.PickSingleFileAsync();
            if (file == null) return;

            try
            {
                var contratoDocumentoService = AppServices.Get<Services.LocalStorage.IContratoDocumentoService>();
                var contentType = file.FileType.ToLowerInvariant() switch
                {
                    ".pdf" => "application/pdf",
                    ".png" => "image/png",
                    _ => "image/jpeg"
                };

                await using var stream = await file.OpenStreamForReadAsync();
                var url = await contratoDocumentoService.SubirFirmadoAsync(cliente.Suscripcion.Id, stream, contentType);

                if (string.IsNullOrWhiteSpace(url))
                {
                    await _notificacionService.MostrarAsync("Error", "No se pudo subir el documento firmado.");
                    return;
                }

                var marcado = await _contratoSuscripcionService.MarcarFirmadoAsync(cliente.Suscripcion.Id, url);
                if (marcado)
                {
                    cliente.SuscripcionCargada = false;
                    await LoadSuscripcionForClienteAsync(cliente);
                    await _notificacionService.MostrarAsync("Documento cargado", "El documento firmado se cargó correctamente y el contrato quedó marcado como firmado.");
                }
                else
                {
                    await _notificacionService.MostrarAsync("Error", "El documento se subió pero no se pudo marcar el contrato como firmado.");
                }
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Error al cargar documento firmado", ex, "ClientesPage", "CargarDocumentoFirmadoButton_Click");
                await _notificacionService.MostrarAsync("Error", "Ocurrió un error al cargar el documento firmado.");
            }
        }

        private async void VerPdfGeneradoButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is Models.CustomerDto cliente && cliente.Suscripcion?.PdfGeneradoUrl != null)
            {
                await AbrirUrlAsync(cliente.Suscripcion.PdfGeneradoUrl);
            }
        }

        private async void VerDocumentoFirmadoButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is Models.CustomerDto cliente && cliente.Suscripcion?.PdfFirmadoUrl != null)
            {
                await AbrirUrlAsync(cliente.Suscripcion.PdfFirmadoUrl);
            }
        }

        private void GenerarFacturaIgualaButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement element || element.Tag is not Models.CustomerDto cliente || cliente.Suscripcion == null)
                return;

            var periodo = DateTime.Now.ToString("yyyy-MM");
            var ventana = new Views.Windows.TimbrarIgualaWindow(cliente.Suscripcion, cliente, periodo);
            ventana.Activate();
        }

        private async System.Threading.Tasks.Task AbrirUrlAsync(string url)
        {
            try
            {
                if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
                {
                    await global::Windows.System.Launcher.LaunchUriAsync(uri);
                }
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"No se pudo abrir la URL {url}", ex, "ClientesPage", "AbrirUrlAsync");
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
                await _loggingService.LogErrorAsync("Error al cargar contactos del cliente", ex, "ClientesPage", "LoadContactosForClienteAsync");
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
                // Un contacto puede estar vinculado a varias empresas: se listan
                // todos los contactos y se excluyen los que YA están vinculados
                // a este cliente en particular (relacion_contacto_cliente).
                var todosLosContactos = await _contactoService.GetContactosAsync(new ContactoQueryDto());
                var idsYaVinculados = cliente.Contactos.Select(c => c.ContactoId).ToHashSet();
                var contactosDisponibles = todosLosContactos
                    .Where(c => !idsYaVinculados.Contains(c.ContactoId))
                    .ToList();

                if (contactosDisponibles.Count == 0)
                {
                    await _notificacionService.MostrarAsync("Sin contactos disponibles", "No hay más contactos disponibles para vincular a este cliente.");
                    return;
                }

                // Crear ListView para seleccionar contacto
                var contactoListView = new ListView
                {
                    SelectionMode = ListViewSelectionMode.Single,
                    MaxHeight = 300
                };

                foreach (var contacto in contactosDisponibles)
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
                    // Vincula el contacto a este cliente sin afectar sus demás
                    // vínculos (relacion_contacto_cliente es muchos-a-muchos).
                    var updateResult = await _contactoService.VincularClienteAsync(selectedContacto.ContactoId, cliente.IdCliente);

                    if (updateResult.Success)
                    {
                        _activityService.Registrar("Clientes", "Contacto agregado");
                        // Recargar contactos del cliente
                        cliente.ContactosLoaded = false;
                        await LoadContactosForClienteAsync(cliente);

                        await _notificacionService.MostrarAsync("Contacto agregado", $"Contacto \"{selectedContacto.NombreCompleto}\" agregado correctamente");
                    }
                    else
                    {
                        await _notificacionService.MostrarAsync("Error", "No se pudo agregar el contacto. Por favor, intente nuevamente.");
                    }
                }
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Error al agregar contacto", ex, "ClientesPage", "NuevoContacto_Click");
                await _notificacionService.MostrarAsync("Error", "Ocurrió un error al agregar el contacto. Por favor, intente nuevamente.");
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
                    // Desvincula el contacto SOLO de este cliente; sigue
                    // apareciendo en cualquier otra empresa a la que esté
                    // vinculado.
                    var updateResult = await _contactoService.DesvincularClienteAsync(contacto.ContactoId, cliente.IdCliente);

                    if (updateResult.Success)
                    {
                        _activityService.Registrar("Clientes", "Contacto eliminado");
                        // Eliminar el contacto de la colección local
                        cliente.Contactos.Remove(contacto);
                        cliente.NotifyNoContactosMessageChanged();

                        await _notificacionService.MostrarAsync("Contacto quitado", "Contacto quitado del cliente correctamente");
                    }
                    else
                    {
                        await _notificacionService.MostrarAsync("Error", "No se pudo quitar el contacto. Por favor, intente nuevamente.");
                    }
                }
                catch (Exception ex)
                {
                    await _loggingService.LogErrorAsync("Error al quitar contacto del cliente", ex, "ClientesPage", "DeleteContactoButton_Click");
                    await _notificacionService.MostrarAsync("Error", "Ocurrió un error al quitar el contacto. Por favor, intente nuevamente.");
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

                var satCatalogoService = AppServices.Get<ISatCatalogoService>();

                var regimenFiscalAutoSuggestBox = new AutoSuggestBox
                {
                    PlaceholderText = "Buscar régimen fiscal (opcional)",
                    Margin = new Thickness(0, 0, 0, 8)
                };
                var regimenes = await satCatalogoService.ListarRegimenFiscalAsync();
                SatCatalogoAutoSuggestHelper.Configurar(regimenFiscalAutoSuggestBox, regimenes, cliente.RegimenFiscal);

                var usoCfdiAutoSuggestBox = new AutoSuggestBox
                {
                    PlaceholderText = "Buscar uso de CFDI (opcional)",
                    Margin = new Thickness(0, 0, 0, 8)
                };
                var usos = await satCatalogoService.ListarUsoCfdiAsync();
                SatCatalogoAutoSuggestHelper.Configurar(usoCfdiAutoSuggestBox, usos, cliente.UsoCfdi);

                var codigoPostalTextBox = new TextBox
                {
                    Text = cliente.CodigoPostal ?? "",
                    PlaceholderText = "Código postal del domicilio fiscal (opcional)",
                    MaxLength = 5,
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
                            regimenFiscalAutoSuggestBox,
                            new TextBlock { Text = "Uso CFDI:" },
                            usoCfdiAutoSuggestBox,
                            new TextBlock { Text = "Código Postal:" },
                            codigoPostalTextBox,
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
                        await _notificacionService.MostrarAsync("Validación", "El RFC es obligatorio");
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(razonSocialTextBox.Text))
                    {
                        await _notificacionService.MostrarAsync("Validación", "La razón social es obligatoria");
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(nombreComercialTextBox.Text))
                    {
                        await _notificacionService.MostrarAsync("Validación", "El nombre comercial es obligatorio");
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
                        regimenFiscal: string.IsNullOrWhiteSpace(SatCatalogoAutoSuggestHelper.ExtraerClave(regimenFiscalAutoSuggestBox.Text)) ? null : SatCatalogoAutoSuggestHelper.ExtraerClave(regimenFiscalAutoSuggestBox.Text),
                        usoCfdi: string.IsNullOrWhiteSpace(SatCatalogoAutoSuggestHelper.ExtraerClave(usoCfdiAutoSuggestBox.Text)) ? null : SatCatalogoAutoSuggestHelper.ExtraerClave(usoCfdiAutoSuggestBox.Text),
                        diasCredito: diasCredito,
                        limiteCredito: limiteCredito,
                        prioridad: prioridad,
                        notas: string.IsNullOrWhiteSpace(notasTextBox.Text) ? null : notasTextBox.Text.Trim(),
                        estatus: estatusCheckBox.IsChecked ?? true,
                        codigoPostal: string.IsNullOrWhiteSpace(codigoPostalTextBox.Text) ? null : codigoPostalTextBox.Text.Trim()
                    );

                    if (success)
                    {
                        await _notificacionService.MostrarAsync("Cliente actualizado", $"Cliente \"{nombreComercialTextBox.Text.Trim()}\" actualizado correctamente");
                    }
                    else
                    {
                        await _notificacionService.MostrarAsync("Error", "No se pudo actualizar el cliente. Verifique los datos e intente nuevamente.");
                    }
                }
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Error al editar cliente desde la UI", ex, "ClientesPage", "EditClienteButton_Click");
                
                await _notificacionService.MostrarAsync("Error", "Ocurrió un error al editar el cliente. Por favor, intente nuevamente.");
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
                        await _notificacionService.MostrarAsync("Cliente eliminado", "Cliente eliminado correctamente");
                    }
                    else
                    {
                        await _notificacionService.MostrarAsync("Error", "No se pudo eliminar el cliente. Por favor, intente nuevamente.");
                    }
                }
                catch (Exception ex)
                {
                    await _loggingService.LogErrorAsync("Error al eliminar cliente desde la UI", ex, "ClientesPage", "DeleteClienteButton_Click");
                    await _notificacionService.MostrarAsync("Error", "Ocurrió un error al eliminar el cliente. Por favor, intente nuevamente.");
                }
            }
        }
    }
}
