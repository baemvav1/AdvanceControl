using System;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Advance_Control.Services.Auth;
using Advance_Control.Services.Logging;
using Advance_Control.Services.Notificacion;
using Advance_Control.Services.Security;
using Advance_Control.Services.Session;
using Advance_Control.Services.Activity;

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
        private readonly ISecureStorage _secureStorage;
        private readonly IUserSessionService _userSessionService;
        private readonly IActivityService _activityService;
        
        private string _user = string.Empty;
        private string _password = string.Empty;
        private bool _isLoading;
        private string _errorMessage = string.Empty;
        private bool _loginSuccessful;
        private bool _isAuthenticated;
        private bool _rememberMe;
        
        private const string Key_RememberMe = "login.remember_me";
        private const string Key_SavedUsername = "login.saved_username";

        public LoginViewModel(IAuthService authService, ILoggingService logger, INotificacionService notificacionService, ISecureStorage secureStorage, IUserSessionService userSessionService, IActivityService activityService)
        {
            _authService         = authService         ?? throw new ArgumentNullException(nameof(authService));
            _logger              = logger              ?? throw new ArgumentNullException(nameof(logger));
            _notificacionService = notificacionService ?? throw new ArgumentNullException(nameof(notificacionService));
            _secureStorage       = secureStorage       ?? throw new ArgumentNullException(nameof(secureStorage));
            _userSessionService  = userSessionService  ?? throw new ArgumentNullException(nameof(userSessionService));
            _activityService     = activityService     ?? throw new ArgumentNullException(nameof(activityService));
            
            // Inicializar el comando de login y logout
            LoginCommand = new AsyncRelayCommand(ExecuteLoginAsync, CanExecuteLogin);
            LogoutCommand = new AsyncRelayCommand(ExecuteLogoutAsync, CanExecuteLogout);
            
            // Verificar si el usuario ya está autenticado
            _isAuthenticated = _authService.IsAuthenticated;
            
            // Cargar credenciales guardadas si existe la configuración de recordar
            _ = LoadSavedCredentialsAsync();
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
                    (LoginCommand as IRelayCommand)?.NotifyCanExecuteChanged();
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
                    (LoginCommand as IRelayCommand)?.NotifyCanExecuteChanged();
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
        /// Indica si el usuario ya está autenticado
        /// </summary>
        public bool IsAuthenticated
        {
            get => _isAuthenticated;
            set
            {
                if (SetProperty(ref _isAuthenticated, value))
                {
                    (LogoutCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
                    OnPropertyChanged(nameof(IsNotAuthenticated));
                }
            }
        }

        /// <summary>
        /// Indica si el usuario NO está autenticado (inverso de IsAuthenticated para bindings)
        /// </summary>
        public bool IsNotAuthenticated => !IsAuthenticated;

        /// <summary>
        /// Indica si se deben recordar las credenciales del usuario
        /// </summary>
        public bool RememberMe
        {
            get => _rememberMe;
            set
            {
                if (SetProperty(ref _rememberMe, value))
                {
                    _ = SaveRememberMePreferenceAsync(value);
                }
            }
        }

        /// <summary>
        /// Comando para ejecutar el inicio de sesión
        /// </summary>
        public ICommand LoginCommand { get; }

        /// <summary>
        /// Comando para ejecutar el cierre de sesión
        /// </summary>
        public ICommand LogoutCommand { get; }

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

            if (Password.Length < 4)
            {
                ErrorMessage = "La contraseña debe tener al menos 4 caracteres.";
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
        private async Task ExecuteLoginAsync()
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
                    IsAuthenticated = true;
                    LoginSuccessful = true;
                    await _activityService.CrearActividadAsync("Sesion", $"Inicio de sesión: {User}");
                    await _logger.LogInformationAsync($"Usuario autenticado exitosamente: {User}", "LoginViewModel", "ExecuteLogin");
                    
                    // Guardar credenciales si RememberMe está habilitado
                    if (RememberMe)
                    {
                        await SaveCredentialsAsync();
                    }
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

        /// <summary>
        /// Verifica si el comando de logout puede ejecutarse
        /// </summary>
        /// <returns>True si puede ejecutarse, false en caso contrario</returns>
        private bool CanExecuteLogout()
        {
            return IsAuthenticated && !IsLoading;
        }

        /// <summary>
        /// Ejecuta el proceso de cierre de sesión
        /// </summary>
        private async Task ExecuteLogoutAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                // Llamar al servicio de autenticación para cerrar sesión
                var success = await _authService.LogoutAsync();
                
                if (success)
                {
                    _userSessionService.Clear();
                    IsAuthenticated = false;
                    LoginSuccessful = false;
                    await _logger.LogInformationAsync($"Usuario cerró sesión exitosamente: {User}", "LoginViewModel", "ExecuteLogoutAsync");
                    
                    // Mostrar notificación de cierre de sesión
                    await _notificacionService.MostrarNotificacionAsync(
                        titulo: "Sesión Cerrada",
                        nota: "Ha cerrado sesión exitosamente",
                        fechaHoraInicio: DateTime.Now);
                    
                    // Limpiar el formulario y credenciales guardadas
                    ClearForm();
                    await ClearSavedCredentialsAsync();
                }
                else
                {
                    ErrorMessage = "Error al cerrar sesión. Por favor, intente nuevamente.";
                    await _logger.LogWarningAsync("Error al ejecutar logout", "LoginViewModel", "ExecuteLogoutAsync");
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al cerrar sesión: {ex.Message}";
                await _logger.LogErrorAsync("Error al intentar cerrar sesión", ex, "LoginViewModel", "ExecuteLogoutAsync");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Actualiza el estado de autenticación desde el servicio
        /// </summary>
        public void RefreshAuthenticationState()
        {
            IsAuthenticated = _authService.IsAuthenticated;
        }

        /// <summary>
        /// Carga las credenciales guardadas si el usuario ha habilitado "Recordar"
        /// </summary>
        private async Task LoadSavedCredentialsAsync()
        {
            try
            {
                var rememberMeValue = await _secureStorage.GetAsync(Key_RememberMe);
                if (rememberMeValue == "true")
                {
                    _rememberMe = true;
                    OnPropertyChanged(nameof(RememberMe));
                    
                    var savedUsername = await _secureStorage.GetAsync(Key_SavedUsername);
                    
                    if (!string.IsNullOrEmpty(savedUsername))
                    {
                        _user = savedUsername;
                        OnPropertyChanged(nameof(User));
                    }
                    
                    await _logger.LogDebugAsync("Usuario guardado cargado desde almacenamiento seguro", "LoginViewModel", "LoadSavedCredentialsAsync");
                }
            }
            catch (Exception ex)
            {
                await _logger.LogWarningAsync($"Error al cargar credenciales guardadas: {ex.Message}", "LoginViewModel", "LoadSavedCredentialsAsync");
                // Ignorar errores, el usuario puede ingresar sus credenciales manualmente
            }
        }

        /// <summary>
        /// Guarda las credenciales del usuario en almacenamiento seguro
        /// </summary>
        private async Task SaveCredentialsAsync()
        {
            try
            {
                if (RememberMe && !string.IsNullOrEmpty(User))
                {
                    await _secureStorage.SetAsync(Key_SavedUsername, User);
                    await _logger.LogDebugAsync("Usuario guardado en almacenamiento seguro", "LoginViewModel", "SaveCredentialsAsync");
                }
            }
            catch (Exception ex)
            {
                await _logger.LogWarningAsync($"Error al guardar credenciales: {ex.Message}", "LoginViewModel", "SaveCredentialsAsync");
                // No lanzar excepción, esto no debería impedir el login exitoso
            }
        }

        /// <summary>
        /// Guarda la preferencia de recordar credenciales
        /// </summary>
        private async Task SaveRememberMePreferenceAsync(bool value)
        {
            try
            {
                await _secureStorage.SetAsync(Key_RememberMe, value ? "true" : "false");
                
                if (!value)
                {
                    // Si se deshabilita "Recordar", limpiar credenciales guardadas
                    await ClearSavedCredentialsAsync();
                }
                
                await _logger.LogDebugAsync($"Preferencia de recordar actualizada: {value}", "LoginViewModel", "SaveRememberMePreferenceAsync");
            }
            catch (Exception ex)
            {
                await _logger.LogWarningAsync($"Error al guardar preferencia de recordar: {ex.Message}", "LoginViewModel", "SaveRememberMePreferenceAsync");
            }
        }

        /// <summary>
        /// Limpia las credenciales guardadas del almacenamiento seguro
        /// </summary>
        private async Task ClearSavedCredentialsAsync()
        {
            try
            {
                await _secureStorage.RemoveAsync(Key_RememberMe);
                await _secureStorage.RemoveAsync(Key_SavedUsername);
                _rememberMe = false;
                OnPropertyChanged(nameof(RememberMe));
                await _logger.LogDebugAsync("Credenciales guardadas eliminadas", "LoginViewModel", "ClearSavedCredentialsAsync");
            }
            catch (Exception ex)
            {
                await _logger.LogWarningAsync($"Error al limpiar credenciales guardadas: {ex.Message}", "LoginViewModel", "ClearSavedCredentialsAsync");
            }
        }

        /// <summary>
        /// Intenta hacer login automático con las credenciales guardadas
        /// </summary>
        /// <returns>True si el login automático fue exitoso, false en caso contrario</returns>
        public async Task<bool> TryAutoLoginAsync()
        {
            try
            {
                // Verificar si hay credenciales guardadas
                var rememberMeValue = await _secureStorage.GetAsync(Key_RememberMe);
                if (rememberMeValue != "true")
                {
                    await _logger.LogDebugAsync("Auto-login omitido: RecordarMe no está habilitado", "LoginViewModel", "TryAutoLoginAsync");
                    return false;
                }

                var savedUsername = await _secureStorage.GetAsync(Key_SavedUsername);

                if (string.IsNullOrEmpty(savedUsername))
                {
                    await _logger.LogDebugAsync("Auto-login omitido: No hay usuario guardado", "LoginViewModel", "TryAutoLoginAsync");
                    return false;
                }

                // Auto-login usando sesión restaurada (refresh token), no contraseña guardada
                var restored = await _authService.TryRestoreSessionAsync();
                if (restored)
                {
                    _user = savedUsername;
                    _rememberMe = true;
                    IsAuthenticated = true;
                    OnPropertyChanged(nameof(User));
                    OnPropertyChanged(nameof(RememberMe));
                    LoginSuccessful = true;
                    await _logger.LogInformationAsync("Sesión restaurada automáticamente", "LoginViewModel", "TryAutoLoginAsync");

                    return true;
                }

                await _logger.LogDebugAsync("Auto-login omitido: no se pudo restaurar la sesión", "LoginViewModel", "TryAutoLoginAsync");
                return false;
            }
            catch (InvalidOperationException ex)
            {
                await _logger.LogWarningAsync($"No se pudo restaurar la sesión automáticamente por un fallo temporal: {ex.Message}", "LoginViewModel", "TryAutoLoginAsync");
                throw;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error durante el login automático", ex, "LoginViewModel", "TryAutoLoginAsync");
                return false;
            }
        }
    }
}
