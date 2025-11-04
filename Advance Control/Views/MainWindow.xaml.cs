using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Advance_Control.Services.OnlineCheck;
using Advance_Control.Services.Logging;
using Advance_Control.Navigation;
using Advance_Control.Views;

namespace Advance_Control
{
    public sealed partial class MainWindow : Window
    {
        private readonly IOnlineCheck _onlineCheck;
        private readonly ILoggingService _logger;
        private readonly INavigationService _navigationService;

        // Constructor adaptado para que DI inyecte IOnlineCheck y INavigationService.
        public MainWindow(IOnlineCheck onlineCheck, ILoggingService logger, INavigationService navigationService)
        {
            this.InitializeComponent();
            _onlineCheck = onlineCheck ?? throw new ArgumentNullException(nameof(onlineCheck));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));

            // Inicializar el servicio de navegación con el Frame
            _navigationService.Initialize(contentFrame);

            // Configurar las rutas para cada página
            _navigationService.Configure<OperacionesView>("Operaciones");
            _navigationService.Configure<AcesoriaView>("Asesoria");
            _navigationService.Configure<MttoView>("Mantenimiento");
            _navigationService.Configure<ClientesView>("Clientes");

            // Suscribirse al evento ItemInvoked del NavigationView
            nvSample.ItemInvoked += NavigationView_ItemInvoked;

            // Suscribirse al evento BackRequested del NavigationView
            nvSample.BackRequested += NavigationView_BackRequested;

            // Navegar a la página inicial
            _navigationService.Navigate("Operaciones");

            // Actualizar el estado del botón de retroceso
            UpdateBackButtonState();
        }

        private void NavigationView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.InvokedItemContainer is NavigationViewItem item)
            {
                var tag = item.Tag?.ToString();
                if (!string.IsNullOrEmpty(tag))
                {
                    _navigationService.Navigate(tag);
                    UpdateBackButtonState();
                }
            }
        }

        private void NavigationView_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
        {
            if (_navigationService.CanGoBack)
            {
                _navigationService.GoBack();
                UpdateBackButtonState();
            }
        }

        private void UpdateBackButtonState()
        {
            nvSample.IsBackEnabled = _navigationService.CanGoBack;
        }

        // Handler del botón que usa _onlineCheck sin bloquear el hilo de UI.
        /*private async void CheckButton_Click(object sender, RoutedEventArgs e)
        {
            CheckButton.IsEnabled = false;
            StatusTextBlock.Text = "Comprobando...";

            try
            {
                var result = await _onlineCheck.CheckAsync();

                if (result.IsOnline)
                {
                    StatusTextBlock.Text = "API ONLINE";
                }
                else if (result.StatusCode.HasValue)
                {
                    StatusTextBlock.Text = $"API returned status {result.StatusCode}: {result.ErrorMessage}";
                }
                else
                {
                    StatusTextBlock.Text = $"Network error: {result.ErrorMessage}";
                }
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al verificar conectividad desde UI", ex, "MainWindow", "CheckButton_Click");
                // Captura cualquier excepción inesperada y mostrar un mensaje útil.
                StatusTextBlock.Text = $"Error inesperado: {ex.Message}";
            }
            finally
            {
                CheckButton.IsEnabled = true;
            }
        }*/
    }
}
