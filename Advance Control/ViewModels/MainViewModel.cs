using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Advance_Control.Navigation;
using Advance_Control.Services.OnlineCheck;
using Advance_Control.Services.Logging;
using Advance_Control.Services.Auth;
using Advance_Control.Services.Dialog;
using Microsoft.UI.Xaml.Controls;
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

        // Prepare for future login implementation
        public async Task<bool> LoginAsync(string username, string password)
        {
            try
            {
                var success = await _authService.AuthenticateAsync(username, password);
                if (success)
                {
                    IsAuthenticated = true;
                    await _logger.LogInformationAsync($"Usuario autenticado exitosamente: {username}", "MainViewModel", "LoginAsync");
                }
                return success;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error al intentar autenticar usuario: {username}", ex, "MainViewModel", "LoginAsync");
                return false;
            }
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
        /// Shows the login dialog and returns the boolean result
        /// </summary>
        public async Task<bool> ShowLoginDialogAsync()
        {
            try
            {
                // Resolve LoginViewModel from DI container
                var loginViewModel = _serviceProvider.GetRequiredService<ViewModels.Login.LoginViewModel>();
                var loginView = new Views.Login.LoginView(loginViewModel);

                // Show the dialog and get the result
                var result = await _dialogService.ShowDialogAsync<bool>(
                    content: loginView,
                    title: "Iniciar Sesión",
                    primaryButtonText: "Iniciar Sesión",
                    secondaryButtonText: "Cancelar");

                // Update authentication state if login was successful
                if (result.IsConfirmed && result.Result)
                {
                    IsAuthenticated = true;
                    await _logger.LogInformationAsync("Usuario autenticado mediante diálogo de login", "MainViewModel", "ShowLoginDialogAsync");
                }

                return result.IsConfirmed && result.Result;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error al mostrar diálogo de login", ex, "MainViewModel", "ShowLoginDialogAsync");
                return false;
            }
        }
    }
}
