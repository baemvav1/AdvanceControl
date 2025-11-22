using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Advance_Control.Navigation;
using Advance_Control.Services.OnlineCheck;
using Advance_Control.Services.Logging;
using Advance_Control.Services.Auth;
using Microsoft.UI.Xaml.Controls;
using Advance_Control.Services.Dialog;
using Advance_Control.Views.Login;
using Microsoft.Extensions.DependencyInjection;
using Advance_Control.Services.Notificacion;
using System.Collections.ObjectModel;
using Advance_Control.Models;
using CommunityToolkit.Mvvm.Input;
using Advance_Control.Services.UserInfo;

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
        private readonly INotificacionService _notificacionService;
        private readonly IUserInfoService _userInfoService;

        private string _title = "Advance Control";
        private bool _isAuthenticated;
        private bool _isBackEnabled;
        private bool _isNotificacionesVisible = true;
        private ObservableCollection<NotificacionDto> _notificaciones;
        private string _userInitials = "";
        private string _userType = "";

        public MainViewModel(
            INavigationService navigationService,
            IOnlineCheck onlineCheck,
            ILoggingService logger,
            IAuthService authService,
            IDialogService dialogService,
            IServiceProvider serviceProvider,
            INotificacionService notificacionService,
            IUserInfoService userInfoService)
        {
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _onlineCheck = onlineCheck ?? throw new ArgumentNullException(nameof(onlineCheck));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _notificacionService = notificacionService ?? throw new ArgumentNullException(nameof(notificacionService));
            _userInfoService = userInfoService ?? throw new ArgumentNullException(nameof(userInfoService));

            // Initialize authentication state
            _isAuthenticated = _authService.IsAuthenticated;

            // Initialize notifications collection
            _notificaciones = new ObservableCollection<NotificacionDto>();
            
            // Subscribe to notification service events if the implementation supports it
            if (_notificacionService is NotificacionService notifService)
            {
                _notificaciones = notifService.NotificacionesObservable;
            }

            // Initialize commands
            EliminarNotificacionCommand = new RelayCommand<Guid>(EliminarNotificacion);

            // Load user info if already authenticated
            if (_isAuthenticated)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await LoadUserInfoAsync();
                    }
                    catch (Exception ex)
                    {
                        // Log error but don't propagate to avoid crashing during initialization
                        await _logger.LogErrorAsync("Error al cargar información del usuario durante la inicialización", ex, "MainViewModel", ".ctor");
                    }
                });
            }
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

        public bool IsNotificacionesVisible
        {
            get => _isNotificacionesVisible;
            set => SetProperty(ref _isNotificacionesVisible, value);
        }

        public ObservableCollection<NotificacionDto> Notificaciones
        {
            get => _notificaciones;
            set => SetProperty(ref _notificaciones, value);
        }

        public string UserInitials
        {
            get => _userInitials;
            set => SetProperty(ref _userInitials, value);
        }

        public string UserType
        {
            get => _userType;
            set => SetProperty(ref _userType, value);
        }

        public ICommand EliminarNotificacionCommand { get; }

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
                // Logout revoca el refresh token en el servidor y limpia tokens locales
                await _authService.LogoutAsync();
                IsAuthenticated = false;
                
                // Limpiar información del usuario
                UserInitials = string.Empty;
                UserType = string.Empty;
                
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
            try
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
                loginView.CloseDialogAction = () => 
                {
                    try
                    {
                        // Asegurar que Hide() se ejecute en el hilo de UI
                        var dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
                        if (dispatcherQueue != null)
                        {
                            _ = dispatcherQueue.TryEnqueue(() =>
                            {
                                try
                                {
                                    dialog.Hide();
                                }
                                catch
                                {
                                    // El diálogo ya puede estar cerrado
                                }
                            });
                        }
                        else
                        {
                            dialog.Hide();
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log pero no propagar - el diálogo ya puede estar cerrado
                        _ = _logger?.LogWarningAsync($"Error al cerrar diálogo de login: {ex.Message}", "MainViewModel", "ShowLoginDialogAsync");
                    }
                };

                // Manejar el cierre automático cuando el login sea exitoso
                loginViewModel.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(LoginViewModel.LoginSuccessful) && loginViewModel.LoginSuccessful)
                    {
                        // Actualizar el estado de autenticación en MainViewModel
                        IsAuthenticated = true;
                        
                        // Cargar información del usuario de forma asíncrona sin bloquear
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await LoadUserInfoAsync();
                            }
                            catch (Exception ex)
                            {
                                // Log error but don't propagate to avoid crashing the app
                                await _logger.LogErrorAsync("Error al cargar información del usuario después del login", ex, "MainViewModel", "ShowLoginDialogAsync");
                            }
                        });
                    }
                };

                var result = await dialog.ShowAsync();
                
                // Retornar true si el login fue exitoso, false si fue cancelado
                return loginViewModel.LoginSuccessful;
            }
            catch (InvalidOperationException ex)
            {
                // Error al obtener XamlRoot o mostrar el diálogo
                await _logger.LogErrorAsync("Error al mostrar el diálogo de login", ex, "MainViewModel", "ShowLoginDialogAsync");
                return false;
            }
            catch (Exception ex)
            {
                // Cualquier otro error inesperado
                await _logger.LogErrorAsync("Error inesperado al iniciar sesión", ex, "MainViewModel", "ShowLoginDialogAsync");
                return false;
            }
        }

        /// <summary>
        /// Obtiene el XamlRoot necesario para mostrar diálogos
        /// </summary>
        /// <returns>XamlRoot de la ventana principal</returns>
        /// <exception cref="InvalidOperationException">Si no hay ventana activa o XamlRoot disponible</exception>
        private Microsoft.UI.Xaml.XamlRoot GetXamlRoot()
        {
            if (App.MainWindow == null)
            {
                throw new InvalidOperationException(
                    "No se pudo obtener el XamlRoot: La ventana principal no está inicializada.");
            }

            if (App.MainWindow.Content is not Microsoft.UI.Xaml.FrameworkElement rootElement)
            {
                throw new InvalidOperationException(
                    "No se pudo obtener el XamlRoot: La ventana principal no tiene contenido.");
            }

            if (rootElement.XamlRoot == null)
            {
                throw new InvalidOperationException(
                    "No se pudo obtener el XamlRoot: El contenido de la ventana no tiene XamlRoot asignado.");
            }

            return rootElement.XamlRoot;
        }

        /// <summary>
        /// Elimina una notificación específica
        /// </summary>
        /// <param name="notificacionId">ID de la notificación a eliminar</param>
        private void EliminarNotificacion(Guid notificacionId)
        {
            _notificacionService.EliminarNotificacion(notificacionId);
        }

        /// <summary>
        /// Carga la información del usuario autenticado
        /// </summary>
        public async Task LoadUserInfoAsync()
        {
            try
            {
                var userInfo = await _userInfoService.GetUserInfoAsync();
                
                if (userInfo != null)
                {
                    // Extraer iniciales del nombre completo
                    UserInitials = GetInitials(userInfo.NombreCompleto);
                    UserType = userInfo.TipoUsuario ?? string.Empty;
                    
                    await _logger.LogInformationAsync($"Información de usuario cargada: {userInfo.NombreCompleto} ({userInfo.TipoUsuario})", "MainViewModel", "LoadUserInfoAsync");
                }
                else
                {
                    // Limpiar información si no se pudo obtener
                    UserInitials = string.Empty;
                    UserType = string.Empty;
                    await _logger.LogWarningAsync("No se pudo obtener la información del usuario", "MainViewModel", "LoadUserInfoAsync");
                }
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error al cargar información del usuario", ex, "MainViewModel", "LoadUserInfoAsync");
                UserInitials = string.Empty;
                UserType = string.Empty;
            }
        }

        /// <summary>
        /// Extrae las iniciales de un nombre completo
        /// </summary>
        /// <param name="nombreCompleto">Nombre completo del usuario</param>
        /// <returns>Iniciales en mayúsculas</returns>
        private string GetInitials(string? nombreCompleto)
        {
            if (string.IsNullOrWhiteSpace(nombreCompleto))
                return string.Empty;

            var words = nombreCompleto.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            
            if (words.Length == 0)
                return string.Empty;

            // Tomar la primera letra de cada palabra (máximo 3 iniciales)
            var initials = string.Join("", words.Take(3).Select(w => w[0].ToString().ToUpper()));
            
            return initials;
        }
    }
}
