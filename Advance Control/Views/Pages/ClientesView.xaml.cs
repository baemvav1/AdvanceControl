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

        public ClientesView()
        {
            // Resolver el ViewModel desde DI
            ViewModel = ((App)Application.Current).Host.Services.GetRequiredService<CustomersViewModel>();
            
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
            // Crear los campos del formulario
            var rfcTextBox = new TextBox
            {
                PlaceholderText = "RFC del cliente (requerido)",
                MaxLength = 13,
                Margin = new Thickness(0, 0, 0, 8)
            };

            var razonSocialTextBox = new TextBox
            {
                PlaceholderText = "Razón social (requerido)",
                Margin = new Thickness(0, 0, 0, 8)
            };

            var nombreComercialTextBox = new TextBox
            {
                PlaceholderText = "Nombre comercial (requerido)",
                Margin = new Thickness(0, 0, 0, 8)
            };

            var regimenFiscalTextBox = new TextBox
            {
                PlaceholderText = "Régimen fiscal (opcional)",
                Margin = new Thickness(0, 0, 0, 8)
            };

            var usoCfdiTextBox = new TextBox
            {
                PlaceholderText = "Uso CFDI (opcional)",
                Margin = new Thickness(0, 0, 0, 8)
            };

            var diasCreditoNumberBox = new NumberBox
            {
                PlaceholderText = "Días de crédito",
                Minimum = 0,
                SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline,
                Margin = new Thickness(0, 0, 0, 8)
            };

            var limiteCreditoNumberBox = new NumberBox
            {
                PlaceholderText = "Límite de crédito",
                Minimum = 0,
                SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline,
                Margin = new Thickness(0, 0, 0, 8)
            };

            var prioridadNumberBox = new NumberBox
            {
                PlaceholderText = "Prioridad (0-10)",
                Minimum = 0,
                Maximum = 10,
                SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline,
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

            var estatusCheckBox = new CheckBox
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
                Title = "Nuevo Cliente",
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

                try
                {
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

                    var success = await ViewModel.CreateClienteAsync(
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
                            titulo: "Cliente creado",
                            nota: $"Cliente \"{nombreComercialTextBox.Text.Trim()}\" creado correctamente",
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

        private void HeadGrid_Tapped(object sender, TappedRoutedEventArgs e)
        {
            // Get the CustomerDto from the sender's Tag property
            if (sender is FrameworkElement element && element.Tag is Models.CustomerDto customer)
            {
                customer.Expand = !customer.Expand;
            }
        }

        private void ToggleExpandButton_Click(object sender, RoutedEventArgs e)
        {
            // Get the CustomerDto from the sender's Tag property
            if (sender is FrameworkElement element && element.Tag is Models.CustomerDto customer)
            {
                customer.Expand = !customer.Expand;
            }
        }
    }
}
