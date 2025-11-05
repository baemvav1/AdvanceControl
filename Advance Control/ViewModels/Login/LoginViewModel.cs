using System;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Advance_Control.Services.Auth;
using Advance_Control.Services.Logging;

namespace Advance_Control.ViewModels.Login
{
    public class LoginViewModel : ViewModelBase
    {
        private readonly IAuthService _authService;
        private readonly ILoggingService _logger;

        private string _username = string.Empty;
        private string _password = string.Empty;
        private bool _isLoading;
        private string _errorMessage = string.Empty;
        private bool _loginResult;

        public LoginViewModel(IAuthService authService, ILoggingService logger)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            LoginCommand = new AsyncRelayCommand(LoginAsync, () => !IsLoading);
        }

        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (SetProperty(ref _isLoading, value))
                {
                    (LoginCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
                }
            }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        /// <summary>
        /// Gets the boolean result of the login operation
        /// </summary>
        public bool LoginResult
        {
            get => _loginResult;
            private set => SetProperty(ref _loginResult, value);
        }

        public ICommand LoginCommand { get; }

        private async Task LoginAsync()
        {
            ErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(Username))
            {
                ErrorMessage = "Por favor ingrese un nombre de usuario";
                return;
            }

            if (string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Por favor ingrese una contraseña";
                return;
            }

            try
            {
                IsLoading = true;
                var success = await _authService.AuthenticateAsync(Username, Password);
                
                LoginResult = success;

                if (!success)
                {
                    ErrorMessage = "Usuario o contraseña incorrectos";
                    await _logger.LogWarningAsync($"Intento de login fallido para usuario: {Username}", "LoginViewModel", "LoginAsync");
                }
                else
                {
                    await _logger.LogInformationAsync($"Login exitoso para usuario: {Username}", "LoginViewModel", "LoginAsync");
                }
            }
            catch (Exception ex)
            {
                LoginResult = false;
                ErrorMessage = "Error al intentar iniciar sesión. Por favor intente nuevamente.";
                await _logger.LogErrorAsync($"Error en login para usuario: {Username}", ex, "LoginViewModel", "LoginAsync");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
