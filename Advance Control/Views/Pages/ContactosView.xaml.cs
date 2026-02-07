using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Extensions.DependencyInjection;
using Advance_Control.ViewModels;
using Advance_Control.Services.Notificacion;
using Advance_Control.Services.Logging;

namespace Advance_Control.Views.Pages
{
    /// <summary>
    /// Página para visualizar y gestionar contactos
    /// </summary>
    public sealed partial class ContactosView : Page
    {
        public ContactosViewModel ViewModel { get; }
        private readonly INotificacionService _notificacionService;
        private readonly ILoggingService _loggingService;

        public ContactosView()
        {
            // Resolver el ViewModel desde DI
            ViewModel = ((App)Application.Current).Host.Services.GetRequiredService<ContactosViewModel>();
            
            // Resolver el servicio de notificaciones desde DI
            _notificacionService = ((App)Application.Current).Host.Services.GetRequiredService<INotificacionService>();
            
            // Resolver el servicio de logging desde DI
            _loggingService = ((App)Application.Current).Host.Services.GetRequiredService<ILoggingService>();
            
            this.InitializeComponent();
            
            // Establecer el DataContext para los bindings
            this.DataContext = ViewModel;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            
            // Cargar los contactos cuando se navega a esta página
            await ViewModel.LoadContactosAsync();
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.LoadContactosAsync();
        }

        private async void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.ClearFiltersAsync();
        }

        private async void NuevoButton_Click(object sender, RoutedEventArgs e)
        {
            // Crear los campos del formulario
            var nombreTextBox = new TextBox
            {
                PlaceholderText = "Nombre del contacto (requerido)",
                Margin = new Thickness(0, 0, 0, 8)
            };

            var apellidoTextBox = new TextBox
            {
                PlaceholderText = "Apellido del contacto",
                Margin = new Thickness(0, 0, 0, 8)
            };

            var correoTextBox = new TextBox
            {
                PlaceholderText = "Correo electrónico",
                Margin = new Thickness(0, 0, 0, 8)
            };

            var telefonoTextBox = new TextBox
            {
                PlaceholderText = "Teléfono",
                Margin = new Thickness(0, 0, 0, 8)
            };

            var departamentoTextBox = new TextBox
            {
                PlaceholderText = "Departamento",
                Margin = new Thickness(0, 0, 0, 8)
            };

            var cargoTextBox = new TextBox
            {
                PlaceholderText = "Cargo",
                Margin = new Thickness(0, 0, 0, 8)
            };

            var codigoInternoTextBox = new TextBox
            {
                PlaceholderText = "Código interno",
                Margin = new Thickness(0, 0, 0, 8)
            };

            var notasTextBox = new TextBox
            {
                PlaceholderText = "Notas adicionales (opcional)",
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                MinHeight = 80,
                MaxHeight = 150,
                Margin = new Thickness(0, 0, 0, 8)
            };

            var idClienteNumberBox = new NumberBox
            {
                PlaceholderText = "ID del cliente asociado",
                Minimum = 0,
                SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline,
                Margin = new Thickness(0, 0, 0, 8)
            };

            var idProveedorNumberBox = new NumberBox
            {
                PlaceholderText = "ID del proveedor asociado",
                Minimum = 0,
                SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline,
                Margin = new Thickness(0, 0, 0, 8)
            };

            var activoCheckBox = new CheckBox
            {
                Content = "Activo",
                IsChecked = true
            };

            var dialogContent = new ScrollViewer
            {
                Content = new StackPanel
                {
                    Spacing = 8,
                    Children =
                    {
                        new TextBlock { Text = "Nombre:", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold },
                        nombreTextBox,
                        new TextBlock { Text = "Apellido:" },
                        apellidoTextBox,
                        new TextBlock { Text = "Correo electrónico:" },
                        correoTextBox,
                        new TextBlock { Text = "Teléfono:" },
                        telefonoTextBox,
                        new TextBlock { Text = "Departamento:" },
                        departamentoTextBox,
                        new TextBlock { Text = "Cargo:" },
                        cargoTextBox,
                        new TextBlock { Text = "Código Interno:" },
                        codigoInternoTextBox,
                        new TextBlock { Text = "ID Cliente:" },
                        idClienteNumberBox,
                        new TextBlock { Text = "ID Proveedor:" },
                        idProveedorNumberBox,
                        new TextBlock { Text = "Notas:" },
                        notasTextBox,
                        activoCheckBox
                    }
                },
                MaxHeight = 500
            };

            var dialog = new ContentDialog
            {
                Title = "Nuevo Contacto",
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
                if (string.IsNullOrWhiteSpace(nombreTextBox.Text))
                {
                    await _notificacionService.MostrarNotificacionAsync(
                        titulo: "Validación",
                        nota: "El nombre es obligatorio",
                        fechaHoraInicio: DateTime.Now);
                    return;
                }

                try
                {
                    // Convertir valores de NumberBox de manera segura
                    int? idCliente = null;
                    if (!double.IsNaN(idClienteNumberBox.Value))
                    {
                        idCliente = Convert.ToInt32(Math.Round(idClienteNumberBox.Value));
                    }

                    int? idProveedor = null;
                    if (!double.IsNaN(idProveedorNumberBox.Value))
                    {
                        idProveedor = Convert.ToInt32(Math.Round(idProveedorNumberBox.Value));
                    }

                    var success = await ViewModel.CreateContactoAsync(
                        nombre: nombreTextBox.Text.Trim(),
                        apellido: string.IsNullOrWhiteSpace(apellidoTextBox.Text) ? null : apellidoTextBox.Text.Trim(),
                        correo: string.IsNullOrWhiteSpace(correoTextBox.Text) ? null : correoTextBox.Text.Trim(),
                        telefono: string.IsNullOrWhiteSpace(telefonoTextBox.Text) ? null : telefonoTextBox.Text.Trim(),
                        departamento: string.IsNullOrWhiteSpace(departamentoTextBox.Text) ? null : departamentoTextBox.Text.Trim(),
                        cargo: string.IsNullOrWhiteSpace(cargoTextBox.Text) ? null : cargoTextBox.Text.Trim(),
                        codigoInterno: string.IsNullOrWhiteSpace(codigoInternoTextBox.Text) ? null : codigoInternoTextBox.Text.Trim(),
                        notas: string.IsNullOrWhiteSpace(notasTextBox.Text) ? null : notasTextBox.Text.Trim(),
                        idCliente: idCliente,
                        idProveedor: idProveedor,
                        activo: activoCheckBox.IsChecked ?? true
                    );

                    if (success)
                    {
                        await _notificacionService.MostrarNotificacionAsync(
                            titulo: "Contacto creado",
                            nota: $"Contacto \"{nombreTextBox.Text.Trim()}\" creado correctamente",
                            fechaHoraInicio: DateTime.Now);
                    }
                    else
                    {
                        await _notificacionService.MostrarNotificacionAsync(
                            titulo: "Error",
                            nota: "No se pudo crear el contacto. Verifique los datos e intente nuevamente.",
                            fechaHoraInicio: DateTime.Now);
                    }
                }
                catch (Exception ex)
                {
                    await _loggingService.LogErrorAsync("Error al crear contacto desde la UI", ex, "ContactosView", "NuevoButton_Click");
                    
                    await _notificacionService.MostrarNotificacionAsync(
                        titulo: "Error",
                        nota: "Ocurrió un error al crear el contacto. Por favor, intente nuevamente.",
                        fechaHoraInicio: DateTime.Now);
                }
            }
        }

        private void HeadGrid_Tapped(object sender, TappedRoutedEventArgs e)
        {
            ToggleContactoExpand(sender);
        }

        private void ToggleExpandButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleContactoExpand(sender);
        }

        private void ToggleContactoExpand(object sender)
        {
            // Get the ContactoDto from the sender's Tag property and toggle expand state
            if (sender is FrameworkElement element && element.Tag is Models.ContactoDto contacto)
            {
                contacto.Expand = !contacto.Expand;
            }
        }
    }
}
