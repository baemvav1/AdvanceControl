using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Advance_Control.Navigation;
using Advance_Control.Services.OnlineCheck;
using Advance_Control.Services.Logging;
using Advance_Control.Services.Auth;
using Microsoft.UI.Xaml.Controls;
using Advance_Control.Services.Dialog;
using Advance_Control.Views.Login;
using Microsoft.Extensions.DependencyInjection;

namespace Advance_Control.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly INavigationService _navigationService;
        private readonly IOnlineCheck _onlineCheck;
        private readonly ILoggingService _logger;
        private readonly IAuthService _authService;
        private readonly IDialogService _dialogService;
        private readonly IServiceProvider _serviceProvider;

        private string _title = "Advance Control";
        private bool _isAuthenticated;
        private bool _isBackEnabled;

        public MainViewModel(
            INavigationService navigationService,
            IOnlineCheck onlineCheck,
            ILoggingService logger,
            IAuthService authService,
            IDialogService dialogService,
            IServiceProvider serviceProvider)
        {
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _onlineCheck = onlineCheck ?? throw new ArgumentNullException(nameof(onlineCheck));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

            // Initialize authentication state
            _isAuthenticated = _authService.IsAuthenticated;
        }

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public bool IsAuthenticated
        {
            get => _isAuthenticated;
            set => SetProperty(ref _isAuthenticated, value);
        }

        public bool IsBackEnabled
        {
            get => _isBackEnabled;
            set => SetProperty(ref _isBackEnabled, value);
        }

        public INavigationService NavigationService => _navigationService;

        public void InitializeNavigation(Frame contentFrame)
        {
            if (contentFrame == null)
                throw new ArgumentNullException(nameof(contentFrame));

            // Initialize the navigation service with the Frame
            _navigationService.Initialize(contentFrame);

            // Configure routes for each page
            _navigationService.Configure<Views.OperacionesView>("Operaciones");
            _navigationService.Configure<Views.AcesoriaView>("Asesoria");
            _navigationService.Configure<Views.MttoView>("Mantenimiento");
            _navigationService.Configure<Views.ClientesView>("Clientes");

            // Subscribe to Frame navigation events
            contentFrame.Navigated += OnFrameNavigated;

            // Navigate to initial page
            _navigationService.Navigate("Operaciones");

            // Update back button state
            UpdateBackButtonState();
        }

        public void OnNavigationItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.InvokedItemContainer is NavigationViewItem item)
            {
                var tag = item.Tag?.ToString();
                if (!string.IsNullOrEmpty(tag))
                {
                    _navigationService.Navigate(tag);
                }
            }
        }

        public void OnBackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
        {
            if (_navigationService.CanGoBack)
            {
                _navigationService.GoBack();
            }
        }

        private void OnFrameNavigated(object sender, Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            // Update back button state when navigation occurs
            UpdateBackButtonState();
        }

        private void UpdateBackButtonState()
        {
            IsBackEnabled = _navigationService.CanGoBack;
        }

        public async Task LogoutAsync()
        {
            try
            {
                await _authService.ClearTokenAsync();
                IsAuthenticated = false;
                await _logger.LogInformationAsync("Usuario cerró sesión", "MainViewModel", "LogoutAsync");
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error al cerrar sesión", ex, "MainViewModel", "LogoutAsync");
            }
        }

        public async Task<bool> CheckOnlineStatusAsync()
        {
            try
            {
                var result = await _onlineCheck.CheckAsync();
                return result.IsOnline;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error al verificar estado online", ex, "MainViewModel", "CheckOnlineStatusAsync");
                return false;
            }
        }
        /// <summary>
        /// Muestra el diálogo de inicio de sesión
        /// </summary>
        /// <returns>True si el usuario completó el login exitosamente, false si canceló</returns>
        public async Task<bool> ShowLoginDialogAsync()
        {
            // La autenticación ahora ocurre completamente dentro de LoginView/LoginViewModel
            // MainViewModel solo muestra el diálogo y verifica el resultado
            var loginViewModel = _serviceProvider.GetRequiredService<LoginViewModel>();
            var loginView = new LoginView(loginViewModel);
            
            var dialog = new Microsoft.UI.Xaml.Controls.ContentDialog
            {
                Title = "Iniciar Sesión",
                Content = loginView,
                XamlRoot = GetXamlRoot()
                // No configurar botones del dialog - usar los botones internos de LoginView
            };

            // Configurar el cierre del diálogo desde el LoginView
            loginView.CloseDialogAction = () => dialog.Hide();

            // Manejar el cierre automático cuando el login sea exitoso
            loginViewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(LoginViewModel.LoginSuccessful) && loginViewModel.LoginSuccessful)
                {
                    // Actualizar el estado de autenticación en MainViewModel
                    IsAuthenticated = true;
                }
            };

            var result = await dialog.ShowAsync();
            
            // Retornar true si el login fue exitoso, false si fue cancelado
            return loginViewModel.LoginSuccessful;
        }

        /// <summary>
        /// Obtiene el XamlRoot necesario para mostrar diálogos
        /// </summary>
        private Microsoft.UI.Xaml.XamlRoot GetXamlRoot()
        {
            if (App.MainWindow?.Content is Microsoft.UI.Xaml.FrameworkElement rootElement)
            {
                return rootElement.XamlRoot;
            }

            throw new InvalidOperationException(
                "No se pudo obtener el XamlRoot. Asegúrese de que existe una ventana activa con contenido.");
        }
    }
}
