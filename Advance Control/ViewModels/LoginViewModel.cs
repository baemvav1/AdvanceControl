using System;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Advance_Control.Services.Auth;
using Advance_Control.Services.Logging;
using Advance_Control.Services.Notificacion;

namespace Advance_Control.ViewModels
{
    /// <summary>
    /// ViewModel para la vista de inicio de sesión.
    /// Gestiona las credenciales del usuario y el comando de inicio de sesión.
    /// </summary>
    public class LoginViewModel : ViewModelBase
    {
        private readonly IAuthService _authService;
        private readonly ILoggingService _logger;
        private readonly INotificacionService _notificacionService;
        
        private string _user = string.Empty;
        private string _password = string.Empty;
        private bool _isLoading;
        private string _errorMessage = string.Empty;
        private bool _loginSuccessful;

        public LoginViewModel(IAuthService authService, ILoggingService logger, INotificacionService notificacionService)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _notificacionService = notificacionService ?? throw new ArgumentNullException(nameof(notificacionService));
            
            // Inicializar el comando de login
            LoginCommand = new RelayCommand(ExecuteLogin, CanExecuteLogin);
        }

        /// <summary>
        /// Nombre de usuario
        /// </summary>
        public string User
        {
            get => _user;
            set
            {
                if (SetProperty(ref _user, value))
                {
                    // Notificar cambio en CanLogin cuando cambia el usuario
                    OnPropertyChanged(nameof(CanLogin));
                    // Actualizar el estado del comando
                    (LoginCommand as RelayCommand)?.NotifyCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// Contraseña del usuario
        /// </summary>
        public string Password
        {
            get => _password;
            set
            {
                if (SetProperty(ref _password, value))
                {
                    // Notificar cambio en CanLogin cuando cambia la contraseña
                    OnPropertyChanged(nameof(CanLogin));
                    // Actualizar el estado del comando
                    (LoginCommand as RelayCommand)?.NotifyCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// Indica si la operación de inicio de sesión está en curso
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (SetProperty(ref _isLoading, value))
                {
                    // Actualizar el estado del comando cuando cambia IsLoading
                    (LoginCommand as RelayCommand)?.NotifyCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// Mensaje de error para mostrar al usuario
        /// </summary>
        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                if (SetProperty(ref _errorMessage, value))
                {
                    // Notificar cuando cambia el estado de error
                    OnPropertyChanged(nameof(HasError));
                }
            }
        }

        /// <summary>
        /// Indica si hay un mensaje de error activo
        /// </summary>
        public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

        /// <summary>
        /// Indica si se puede realizar el login (validación básica)
        /// </summary>
        public bool CanLogin => !string.IsNullOrWhiteSpace(User) && 
                                !string.IsNullOrWhiteSpace(Password) && 
                                !IsLoading;

        /// <summary>
        /// Indica si el login fue exitoso
        /// </summary>
        public bool LoginSuccessful
        {
            get => _loginSuccessful;
            private set => SetProperty(ref _loginSuccessful, value);
        }

        /// <summary>
        /// Comando para ejecutar el inicio de sesión
        /// </summary>
        public ICommand LoginCommand { get; }

        /// <summary>
        /// Valida que las credenciales cumplan con los requisitos mínimos
        /// </summary>
        /// <returns>True si las credenciales son válidas, false en caso contrario</returns>
        private bool ValidateCredentials()
        {
            ErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(User))
            {
                ErrorMessage = "El nombre de usuario es requerido.";
                return false;
            }

            if (User.Length < 3)
            {
                ErrorMessage = "El nombre de usuario debe tener al menos 3 caracteres.";
                return false;
            }

            if (User.Length > 150)
            {
                ErrorMessage = "El nombre de usuario no puede tener más de 150 caracteres.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "La contraseña es requerida.";
                return false;
            }

            if (Password.Length < 8)
            {
                ErrorMessage = "La contraseña debe tener al menos 8 caracteres.";
                return false;
            }

            if (Password.Length > 100)
            {
                ErrorMessage = "La contraseña no puede tener más de 100 caracteres.";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Verifica si el comando de login puede ejecutarse
        /// </summary>
        /// <returns>True si puede ejecutarse, false en caso contrario</returns>
        private bool CanExecuteLogin()
        {
            return CanLogin;
        }

        /// <summary>
        /// Ejecuta el proceso de inicio de sesión
        /// </summary>
        private async void ExecuteLogin()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;
                LoginSuccessful = false;

                // Validar credenciales
                if (!ValidateCredentials())
                {
                    return;
                }

                // Llamar al servicio de autenticación
                var success = await _authService.AuthenticateAsync(User, Password);
                
                if (success)
                {
                    LoginSuccessful = true;
                    await _logger.LogInformationAsync($"Usuario autenticado exitosamente: {User}", "LoginViewModel", "ExecuteLogin");
                    
                    // Mostrar notificación de bienvenida
                    await _notificacionService.MostrarNotificacionAsync(
                        titulo: "Bienvenido",
                        nota: $"Usuario {User} ha iniciado sesión exitosamente",
                        fechaHoraInicio: DateTime.Now);
                }
                else
                {
                    ErrorMessage = "Usuario o contraseña incorrectos.";
                    await _logger.LogWarningAsync($"Intento de login fallido para usuario: {User}", "LoginViewModel", "ExecuteLogin");
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al iniciar sesión: {ex.Message}";
                await _logger.LogErrorAsync($"Error al intentar autenticar usuario: {User}", ex, "LoginViewModel", "ExecuteLogin");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Limpia los datos del formulario
        /// </summary>
        public void ClearForm()
        {
            User = string.Empty;
            Password = string.Empty;
            ErrorMessage = string.Empty;
        }
    }
}
