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
    /// Página para visualizar y gestionar entidades
    /// </summary>
    public sealed partial class EntidadesView : Page
    {
        public EntidadesViewModel ViewModel { get; }
        private readonly INotificacionService _notificacionService;
        private readonly ILoggingService _loggingService;

        public EntidadesView()
        {
            // Resolver el ViewModel desde DI
            ViewModel = ((App)Application.Current).Host.Services.GetRequiredService<EntidadesViewModel>();
            
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
            
            // Cargar las entidades cuando se navega a esta página
            await ViewModel.LoadEntidadesAsync();
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.LoadEntidadesAsync();
        }

        private async void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.ClearFiltersAsync();
        }

        private async void NuevoButton_Click(object sender, RoutedEventArgs e)
        {
            // Crear los campos del formulario
            var nombreComercialTextBox = new TextBox
            {
                PlaceholderText = "Nombre comercial (requerido)",
                Margin = new Thickness(0, 0, 0, 8)
            };

            var razonSocialTextBox = new TextBox
            {
                PlaceholderText = "Razón social (requerido)",
                Margin = new Thickness(0, 0, 0, 8)
            };

            var rfcTextBox = new TextBox
            {
                PlaceholderText = "RFC (opcional)",
                MaxLength = 13,
                Margin = new Thickness(0, 0, 0, 8)
            };

            var calleTextBox = new TextBox
            {
                PlaceholderText = "Calle (opcional)",
                Margin = new Thickness(0, 0, 0, 8)
            };

            var numExtTextBox = new TextBox
            {
                PlaceholderText = "Número exterior (opcional)",
                Margin = new Thickness(0, 0, 0, 8)
            };

            var numIntTextBox = new TextBox
            {
                PlaceholderText = "Número interior (opcional)",
                Margin = new Thickness(0, 0, 0, 8)
            };

            var coloniaTextBox = new TextBox
            {
                PlaceholderText = "Colonia (opcional)",
                Margin = new Thickness(0, 0, 0, 8)
            };

            var cpTextBox = new TextBox
            {
                PlaceholderText = "Código postal (opcional)",
                MaxLength = 5,
                Margin = new Thickness(0, 0, 0, 8)
            };

            var ciudadTextBox = new TextBox
            {
                PlaceholderText = "Ciudad (opcional)",
                Margin = new Thickness(0, 0, 0, 8)
            };

            var estadoTextBox = new TextBox
            {
                PlaceholderText = "Estado (opcional)",
                Margin = new Thickness(0, 0, 0, 8)
            };

            var paisTextBox = new TextBox
            {
                PlaceholderText = "País (opcional)",
                Margin = new Thickness(0, 0, 0, 8)
            };

            var apoderadoTextBox = new TextBox
            {
                PlaceholderText = "Apoderado (opcional)",
                Margin = new Thickness(0, 0, 0, 8)
            };

            var dialogContent = new ScrollViewer
            {
                Content = new StackPanel
                {
                    Spacing = 8,
                    Children =
                    {
                        new TextBlock { Text = "Nombre Comercial:", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold },
                        nombreComercialTextBox,
                        new TextBlock { Text = "Razón Social:", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold },
                        razonSocialTextBox,
                        new TextBlock { Text = "RFC:" },
                        rfcTextBox,
                        new TextBlock { Text = "Calle:" },
                        calleTextBox,
                        new TextBlock { Text = "Número Exterior:" },
                        numExtTextBox,
                        new TextBlock { Text = "Número Interior:" },
                        numIntTextBox,
                        new TextBlock { Text = "Colonia:" },
                        coloniaTextBox,
                        new TextBlock { Text = "Código Postal:" },
                        cpTextBox,
                        new TextBlock { Text = "Ciudad:" },
                        ciudadTextBox,
                        new TextBlock { Text = "Estado:" },
                        estadoTextBox,
                        new TextBlock { Text = "País:" },
                        paisTextBox,
                        new TextBlock { Text = "Apoderado:" },
                        apoderadoTextBox
                    }
                },
                MaxHeight = 500
            };

            var dialog = new ContentDialog
            {
                Title = "Nueva Entidad",
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
                if (string.IsNullOrWhiteSpace(nombreComercialTextBox.Text))
                {
                    await _notificacionService.MostrarNotificacionAsync(
                        titulo: "Validación",
                        nota: "El nombre comercial es obligatorio",
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

                try
                {
                    var success = await ViewModel.CreateEntidadAsync(
                        nombreComercial: nombreComercialTextBox.Text.Trim(),
                        razonSocial: razonSocialTextBox.Text.Trim(),
                        rfc: string.IsNullOrWhiteSpace(rfcTextBox.Text) ? null : rfcTextBox.Text.Trim(),
                        cp: string.IsNullOrWhiteSpace(cpTextBox.Text) ? null : cpTextBox.Text.Trim(),
                        estado: string.IsNullOrWhiteSpace(estadoTextBox.Text) ? null : estadoTextBox.Text.Trim(),
                        ciudad: string.IsNullOrWhiteSpace(ciudadTextBox.Text) ? null : ciudadTextBox.Text.Trim(),
                        pais: string.IsNullOrWhiteSpace(paisTextBox.Text) ? null : paisTextBox.Text.Trim(),
                        calle: string.IsNullOrWhiteSpace(calleTextBox.Text) ? null : calleTextBox.Text.Trim(),
                        numExt: string.IsNullOrWhiteSpace(numExtTextBox.Text) ? null : numExtTextBox.Text.Trim(),
                        numInt: string.IsNullOrWhiteSpace(numIntTextBox.Text) ? null : numIntTextBox.Text.Trim(),
                        colonia: string.IsNullOrWhiteSpace(coloniaTextBox.Text) ? null : coloniaTextBox.Text.Trim(),
                        apoderado: string.IsNullOrWhiteSpace(apoderadoTextBox.Text) ? null : apoderadoTextBox.Text.Trim()
                    );

                    if (success)
                    {
                        await _notificacionService.MostrarNotificacionAsync(
                            titulo: "Entidad creada",
                            nota: $"Entidad \"{nombreComercialTextBox.Text.Trim()}\" creada correctamente",
                            fechaHoraInicio: DateTime.Now);
                    }
                    else
                    {
                        await _notificacionService.MostrarNotificacionAsync(
                            titulo: "Error",
                            nota: "No se pudo crear la entidad. Verifique los datos e intente nuevamente.",
                            fechaHoraInicio: DateTime.Now);
                    }
                }
                catch (Exception ex)
                {
                    await _loggingService.LogErrorAsync("Error al crear entidad desde la UI", ex, "EntidadesView", "NuevoButton_Click");
                    
                    await _notificacionService.MostrarNotificacionAsync(
                        titulo: "Error",
                        nota: "Ocurrió un error al crear la entidad. Por favor, intente nuevamente.",
                        fechaHoraInicio: DateTime.Now);
                }
            }
        }

        private void HeadGrid_Tapped(object sender, TappedRoutedEventArgs e)
        {
            // Get the EntidadDto from the sender's Tag property
            if (sender is FrameworkElement element && element.Tag is Models.EntidadDto entidad)
            {
                entidad.Expand = !entidad.Expand;
            }
        }

        private void ToggleExpandButton_Click(object sender, RoutedEventArgs e)
        {
            // Get the EntidadDto from the sender's Tag property
            if (sender is FrameworkElement element && element.Tag is Models.EntidadDto entidad)
            {
                entidad.Expand = !entidad.Expand;
            }
        }
    }
}
