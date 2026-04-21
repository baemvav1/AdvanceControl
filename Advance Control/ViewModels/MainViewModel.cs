using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Navigation;
using Advance_Control.Services.OnlineCheck;
using Advance_Control.Services.Logging;
using Advance_Control.Services.Auth;
using Microsoft.UI.Xaml.Controls;
using Advance_Control.Services.Dialog;
using Advance_Control.Views.Login;
using Microsoft.Extensions.DependencyInjection;
using Advance_Control.Services.Notificacion;
using Advance_Control.Models;
using Advance_Control.Services.AccessControl;
using Advance_Control.Services.UserInfo;
using Advance_Control.Services.Alertas;
using Advance_Control.Services.Activity;
using Advance_Control.Services.PermisosUi;
using Advance_Control.Services.Mensajeria;
using Advance_Control.Utilities;

namespace Advance_Control.ViewModels
{
    public class MainViewModel : ViewModelBase, IDisposable
    {
        private readonly INavigationService _navigationService;
        private readonly IOnlineCheck _onlineCheck;
        private readonly ILoggingService _logger;
        private readonly IAuthService _authService;
        private readonly IDialogService _dialogService;
        private readonly IServiceProvider _serviceProvider;
        private readonly INotificacionService _notificacionService;
        private readonly IUserInfoService _userInfoService;
        private readonly INotificacionAlertaService _alertaService;
        private readonly IActivityService _activityService;
        private readonly IPermisoUiRuntimeService _permisoUiRuntimeService;
        private readonly IMensajeriaService _mensajeria;
        private readonly SemaphoreSlim _loginStateLock = new(1, 1);
        private bool _disposed;
        private Frame? _contentFrame;

        private string _title = "Advance Control";
        private bool _isAuthenticated;
        private bool _isBackEnabled;
        private bool _isChatPanelVisible = false;
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
            IUserInfoService userInfoService,
            INotificacionAlertaService alertaService,
            IActivityService activityService,
            IPermisoUiRuntimeService permisoUiRuntimeService,
            IMensajeriaService mensajeria)
        {
            _navigationService   = navigationService   ?? throw new ArgumentNullException(nameof(navigationService));
            _onlineCheck         = onlineCheck         ?? throw new ArgumentNullException(nameof(onlineCheck));
            _logger              = logger              ?? throw new ArgumentNullException(nameof(logger));
            _authService         = authService         ?? throw new ArgumentNullException(nameof(authService));
            _dialogService       = dialogService       ?? throw new ArgumentNullException(nameof(dialogService));
            _serviceProvider     = serviceProvider     ?? throw new ArgumentNullException(nameof(serviceProvider));
            _notificacionService = notificacionService ?? throw new ArgumentNullException(nameof(notificacionService));
            _userInfoService     = userInfoService     ?? throw new ArgumentNullException(nameof(userInfoService));
            _alertaService       = alertaService       ?? throw new ArgumentNullException(nameof(alertaService));
            _activityService     = activityService     ?? throw new ArgumentNullException(nameof(activityService));
            _permisoUiRuntimeService = permisoUiRuntimeService ?? throw new ArgumentNullException(nameof(permisoUiRuntimeService));
            _mensajeria          = mensajeria          ?? throw new ArgumentNullException(nameof(mensajeria));

            var sessionService = _serviceProvider.GetService<Services.Session.IUserSessionService>();
            _isAuthenticated = _authService.IsAuthenticated && sessionService?.IsLoaded == true;
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

        public bool IsChatPanelVisible
        {
            get => _isChatPanelVisible;
            set => SetProperty(ref _isChatPanelVisible, value);
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

        public INavigationService NavigationService => _navigationService;

        public void InitializeNavigation(Frame contentFrame)
        {
            if (contentFrame == null)
                throw new ArgumentNullException(nameof(contentFrame));

            _contentFrame = contentFrame;
            // Initialize the navigation service with the Frame
            _navigationService.Initialize(contentFrame);

            // Configure routes for each page
            _navigationService.Configure<Views.Pages.DashboardPage>("Inicio");
            _navigationService.Configure<Views.Pages.OperacionesPage>("Operaciones");
            _navigationService.Configure<Views.Pages.AsesoriaPage>("Asesoria");
            _navigationService.Configure<Views.Pages.OrdenServicioPage>("OrdenServicio");
            _navigationService.Configure<Views.Pages.LevantamientosView>("Levantamientos");
            _navigationService.Configure<Views.Pages.LevantamientoView>("Levantamiento");
            _navigationService.Configure<Views.Pages.ClientesPage>("Clientes");
            _navigationService.Configure<Views.Pages.EntidadesPage>("Entidades");
            _navigationService.Configure<Views.Pages.ContactosPage>("Contactos");
            _navigationService.Configure<Views.Pages.EquiposPage>("Equipos");
            _navigationService.Configure<Views.Pages.RefaccionPage>("Refacciones");
            _navigationService.Configure<Views.Pages.ProveedoresPage>("Proveedores");
            _navigationService.Configure<Views.Pages.ServiciosPage>("Servicios");
            _navigationService.Configure<Views.Pages.UbicacionesPage>("Ubicaciones");
            _navigationService.Configure<Views.Pages.AreasPage>("Areas");
            _navigationService.Configure<Views.Pages.EstadoCuentaPage>("EstadoCuenta");
            _navigationService.Configure<Views.Pages.ConciliacionPage>("Conciliacion");
            _navigationService.Configure<Views.Pages.FacturasPage>("Facturas");
            _navigationService.Configure<Views.Pages.ReporteFinancieroFacturacionPage>("ReporteFinancieroFacturacion");
            _navigationService.Configure<Views.Pages.CorreoPage>("Correo");
            _navigationService.Configure<Views.Pages.MensajesPage>("Mensajes");
            _navigationService.Configure<Views.Pages.AdministracionPage>("Administracion");
            _navigationService.Configure<Views.Pages.DevOpsPage>("DevOps");
            _navigationService.Configure<Views.Pages.OperacionVisorPage>("OperacionVisor");
            _navigationService.Configure<Views.Pages.SettingsPage>("Settings");

            // Subscribe to Frame navigation events
            contentFrame.Navigated += OnFrameNavigated;

            // Navigate to initial page
            _navigationService.Navigate("Inicio");

            // Update back button state
            UpdateBackButtonState();
        }

        public async void OnNavigationItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            // El engranaje de Settings no tiene InvokedItemContainer; se detecta con IsSettingsInvoked
            if (args.IsSettingsInvoked)
            {
                _navigationService.Navigate("Settings");
                Title = "Advance Control : Configuración";
                return;
            }

            if (args.InvokedItemContainer is NavigationViewItem item)
            {
                var tag = item.Tag?.ToString();
                if (!string.IsNullOrEmpty(tag))
                {
                    if (string.Equals(tag, "Inicio", StringComparison.OrdinalIgnoreCase))
                    {
                        await NavigateToInicioAsync(forceReload: false);
                        return;
                    }

                    if (!IsAuthenticated)
                    {
                        await MostrarAccesoDenegadoAsync();
                        return;
                    }

                    var pageType = _navigationService.GetPageType(tag);

                    if (pageType != null && !_permisoUiRuntimeService.IsInitialized)
                    {
                        var nivelActual = AccessControlService.Current.NivelUsuario;
                        if (nivelActual > 0)
                        {
                            await _permisoUiRuntimeService.InitializeAsync(nivelActual);
                        }
                    }

                    var moduleKey = pageType != null
                        ? _permisoUiRuntimeService.BuildModuleKey(pageType)
                        : null;
                    var hasUiPermissionRule = pageType != null
                        && moduleKey != null
                        && _permisoUiRuntimeService.TryGetModulo(moduleKey, out _);

                    // Permisos controlados exclusivamente desde la base de datos.
                    // Si el módulo no existe en el catálogo DB, se permite el acceso
                    // (se sincronizará en la siguiente inicialización).
                    if (hasUiPermissionRule
                        && moduleKey != null
                        && !_permisoUiRuntimeService.CanAccessModule(moduleKey))
                    {
                        await MostrarAccesoDenegadoAsync();
                        return;
                    }

                    _navigationService.Navigate(tag);
                    Title = $"Advance Control : {item.Content}";
                }
            }
        }

        public bool ShouldDisplayNavigationTag(string? tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
                return false;

            if (string.Equals(tag, "Inicio", StringComparison.OrdinalIgnoreCase))
                return true;

            if (!IsAuthenticated)
                return false;

            var pageType = _navigationService.GetPageType(tag);
            if (pageType != null && _permisoUiRuntimeService.IsInitialized)
            {
                var moduleKey = _permisoUiRuntimeService.BuildModuleKey(pageType);
                if (_permisoUiRuntimeService.TryGetModulo(moduleKey, out _))
                    return _permisoUiRuntimeService.CanAccessModule(moduleKey);
            }

            // Si el catálogo de permisos aún no se cargó o el módulo no existe en DB,
            // se permite el acceso; la restricción real vendrá del catálogo DB una vez sincronizado.
            return true;
        }

        private async Task MostrarAccesoDenegadoAsync()
        {
            const string mensaje = "No tienes acceso a este modulo";

            try
            {
                await DialogHelper.MostrarInfoAsync(GetXamlRoot(), "Acceso denegado", mensaje);
            }
            catch (Exception ex)
            {
                await _logger.LogWarningAsync($"No fue posible mostrar el diálogo de acceso denegado: {ex.Message}", "MainViewModel", nameof(MostrarAccesoDenegadoAsync));
                await _notificacionService.MostrarAsync("Acceso denegado", mensaje);
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

            if (_contentFrame?.Content is Page page)
            {
                page.Loaded -= ApplyPermissionsWhenPageLoaded;
                page.Loaded += ApplyPermissionsWhenPageLoaded;
            }
        }

        private void ApplyPermissionsWhenPageLoaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (sender is Page page)
            {
                page.Loaded -= ApplyPermissionsWhenPageLoaded;
                PermisoUiVisualBinder.ApplyToPage(page, _permisoUiRuntimeService);
            }
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

                var sessionService = _serviceProvider.GetService<Services.Session.IUserSessionService>();
                sessionService?.Clear();

                await HandleLogoutStateAsync();
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
        /// Intenta iniciar sesión automáticamente usando credenciales guardadas o tokens almacenados.
        /// Si las credenciales/tokens son válidos, carga la información del usuario.
        /// Si no hay credenciales guardadas o los tokens están expirados, muestra el diálogo de login.
        /// </summary>
        /// <returns>True si se autenticó exitosamente (ya sea automáticamente o mediante diálogo), false si el usuario canceló el login</returns>
        public async Task<bool> TryAutoLoginAsync()
        {
            try
            {
                await _logger.LogInformationAsync("Intentando inicio de sesión automático", "MainViewModel", "TryAutoLoginAsync");

                // Primero, intentar login automático con credenciales guardadas (si RememberMe está habilitado)
                var loginViewModel = _serviceProvider.GetRequiredService<LoginViewModel>();
                var credentialsLoginSuccessful = await loginViewModel.TryAutoLoginAsync();

                if (credentialsLoginSuccessful)
                {
                    var bootstrapSuccessful = await HandleLoginSuccessAsync();
                    if (bootstrapSuccessful)
                    {
                        await _logger.LogInformationAsync("Inicio de sesión automático exitoso con credenciales guardadas", "MainViewModel", "TryAutoLoginAsync");
                        return true;
                    }

                    await _logger.LogWarningAsync(
                        "La autenticación automática fue válida, pero el bootstrap de la sesión no se pudo completar",
                        "MainViewModel",
                        "TryAutoLoginAsync");
                }

                // Si no hay credenciales guardadas, intentar restaurar la sesión desde los tokens almacenados
                var sessionRestored = await _authService.TryRestoreSessionAsync();

                if (sessionRestored)
                {
                    var bootstrapSuccessful = await HandleLoginSuccessAsync();
                    if (bootstrapSuccessful)
                    {
                        await _logger.LogInformationAsync("Inicio de sesión automático exitoso con tokens", "MainViewModel", "TryAutoLoginAsync");
                        return true;
                    }

                    await _logger.LogWarningAsync(
                        "La sesión se restauró desde tokens, pero el bootstrap de la sesión no se pudo completar",
                        "MainViewModel",
                        "TryAutoLoginAsync");
                }

                // No se pudo restaurar la sesión - mostrar diálogo de login
                await _logger.LogInformationAsync("No se pudo restaurar sesión, mostrando diálogo de login", "MainViewModel", "TryAutoLoginAsync");
                return await ShowLoginDialogAsync();
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error durante inicio de sesión automático", ex, "MainViewModel", "TryAutoLoginAsync");
                // En caso de error, mostrar el diálogo de login
                return await ShowLoginDialogAsync();
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

                var result = await dialog.ShowAsync();

                if (loginViewModel.LoginSuccessful)
                {
                    return await HandleLoginSuccessAsync();
                }

                if (!loginViewModel.IsAuthenticated && IsAuthenticated)
                {
                    await HandleLogoutStateAsync();
                }

                return false;
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
        /// Carga la información del usuario autenticado
        /// </summary>
        public async Task LoadUserInfoAsync()
        {
            try
            {
                var sessionService = _serviceProvider.GetService<Services.Session.IUserSessionService>();
                if (sessionService?.IsLoaded == true && !string.IsNullOrWhiteSpace(sessionService.NombreCompleto))
                {
                    var sessionInitials = GetInitials(sessionService.NombreCompleto);
                    var sessionUserType = sessionService.TipoUsuario ?? string.Empty;

                    await UpdateUIPropertiesAsync(() =>
                    {
                        UserInitials = sessionInitials;
                        UserType = sessionUserType;
                    });

                    await _logger.LogInformationAsync(
                        $"Información de usuario cargada desde la sesión: {sessionService.NombreCompleto} ({sessionService.TipoUsuario})",
                        "MainViewModel",
                        "LoadUserInfoAsync");

                    if (sessionService.CredencialId > 0)
                        _ = CargarAlertasAsync(sessionService.CredencialId);

                    return;
                }

                var userInfo = await _userInfoService.GetUserInfoAsync();
                
                if (userInfo != null)
                {
                    // Extraer iniciales del nombre completo
                    var initials = GetInitials(userInfo.NombreCompleto);
                    var userType = userInfo.TipoUsuario ?? string.Empty;
                    
                    // Update properties on UI thread to avoid cross-thread exceptions
                    await UpdateUIPropertiesAsync(() =>
                    {
                        UserInitials = initials;
                        UserType = userType;
                    });
                    
                    await _logger.LogInformationAsync($"Información de usuario cargada: {userInfo.NombreCompleto} ({userInfo.TipoUsuario})", "MainViewModel", "LoadUserInfoAsync");

                    // Generar y cargar alertas inteligentes para el usuario recién autenticado
                    var loadedSessionService = _serviceProvider.GetService<Services.Session.IUserSessionService>();
                    if (loadedSessionService?.IsLoaded == true && loadedSessionService.CredencialId > 0)
                        _ = CargarAlertasAsync(loadedSessionService.CredencialId);
                }
                else
                {
                    // Limpiar información si no se pudo obtener
                    await UpdateUIPropertiesAsync(() =>
                    {
                        UserInitials = string.Empty;
                        UserType = string.Empty;
                    });
                    await _logger.LogWarningAsync("No se pudo obtener la información del usuario", "MainViewModel", "LoadUserInfoAsync");
                }
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error al cargar información del usuario", ex, "MainViewModel", "LoadUserInfoAsync");
                await UpdateUIPropertiesAsync(() =>
                {
                    UserInitials = string.Empty;
                    UserType = string.Empty;
                });
            }
        }

        /// <summary>
        /// Genera y carga las alertas inteligentes del usuario desde la BD.
        /// Las muestra como notificaciones de Windows y las marca como vistas.
        /// </summary>
        private async Task CargarAlertasAsync(int credencialId)
        {
            try
            {
                var alertas = await _alertaService.GenerarYObtenerAsync(credencialId).ConfigureAwait(false);
                foreach (var alerta in alertas)
                    await _notificacionService.MostrarNotificacionAsync(alerta.Titulo, alerta.Origen).ConfigureAwait(false);

                if (alertas.Count > 0)
                    await _alertaService.MarcarVistasAsync(credencialId).ConfigureAwait(false);
            }
            catch (Exception)
            {
                await _logger.LogWarningAsync("No se pudieron cargar alertas del sistema", "MainViewModel", "CargarAlertasAsync");
            }
        }

        private async Task<bool> HandleLoginSuccessAsync()
        {
            await _loginStateLock.WaitAsync();
            try
            {
                await EnsureSessionContextAsync();
                await LoadUserInfoAsync();
                await TryConnectMensajeriaAsync();

                await UpdateUIPropertiesAsync(() =>
                {
                    IsAuthenticated = true;
                });

                await NavigateToInicioAsync(forceReload: true);
                return true;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error al aplicar el estado posterior al login", ex, "MainViewModel", nameof(HandleLoginSuccessAsync));
                await ResetAuthenticatedStateAsync();
                await _notificacionService.MostrarAsync(
                    "Error de sesión",
                    "No se pudo completar la carga inicial de la sesión. Inicia sesión nuevamente.");
                return false;
            }
            finally
            {
                _loginStateLock.Release();
            }
        }

        private async Task EnsureSessionContextAsync()
        {
            var sessionService = _serviceProvider.GetService<Services.Session.IUserSessionService>();
            if (sessionService == null)
            {
                throw new InvalidOperationException("No se pudo resolver el servicio de sesión del usuario.");
            }

            if (!sessionService.IsLoaded)
            {
                await sessionService.LoadAsync();
            }

            if (!sessionService.IsLoaded || sessionService.CredencialId <= 0)
            {
                throw new InvalidOperationException("La sesión del usuario quedó incompleta después de autenticarse.");
            }

            if (sessionService.Nivel > 0
                && !_permisoUiRuntimeService.IsInitialized)
            {
                await _permisoUiRuntimeService.InitializeAsync(sessionService.Nivel);
            }
        }

        private async Task TryConnectMensajeriaAsync()
        {
            var token = await _authService.GetAccessTokenAsync();
            if (string.IsNullOrWhiteSpace(token))
            {
                await _logger.LogWarningAsync(
                    "Se omitió la conexión de mensajería porque no hay token de acceso disponible",
                    "MainViewModel",
                    nameof(TryConnectMensajeriaAsync));
                return;
            }

            try
            {
                await _mensajeria.ConectarAsync(token);
            }
            catch (Exception ex)
            {
                await _logger.LogWarningAsync(
                    $"La mensajería no pudo conectarse durante el inicio de sesión: {ex.Message}",
                    "MainViewModel",
                    nameof(TryConnectMensajeriaAsync));
                await _notificacionService.MostrarAsync(
                    "Mensajería no disponible",
                    "La sesión inició correctamente, pero el chat no pudo conectarse en este momento.");
            }
        }

        private async Task ResetAuthenticatedStateAsync()
        {
            var sessionService = _serviceProvider.GetService<Services.Session.IUserSessionService>();
            sessionService?.Clear();

            await _authService.ClearTokenAsync();

            try
            {
                await _mensajeria.DesconectarAsync();
            }
            catch (Exception ex)
            {
                await _logger.LogWarningAsync(
                    $"No se pudo cerrar la conexión de mensajería durante el reinicio de sesión: {ex.Message}",
                    "MainViewModel",
                    nameof(ResetAuthenticatedStateAsync));
            }

            await UpdateUIPropertiesAsync(() =>
            {
                IsAuthenticated = false;
                UserInitials = string.Empty;
                UserType = string.Empty;
                IsChatPanelVisible = false;
            });
        }

        private async Task HandleLogoutStateAsync()
        {
            try
            {
                // Desconectar SignalR al cerrar sesión
                await _mensajeria.DesconectarAsync();

                await UpdateUIPropertiesAsync(() =>
                {
                    IsAuthenticated = false;
                    UserInitials = string.Empty;
                    UserType = string.Empty;
                    IsChatPanelVisible = false;
                });

                await NavigateToInicioAsync(forceReload: true);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error al aplicar el estado seguro posterior al logout", ex, "MainViewModel", nameof(HandleLogoutStateAsync));
            }
        }

        private async Task NavigateToInicioAsync(bool forceReload)
        {
            var navigationParameter = forceReload ? $"refresh:{Guid.NewGuid():N}" : null;
            var navigated = false;

            await UpdateUIPropertiesAsync(() =>
            {
                navigated = _navigationService.Navigate("Inicio", navigationParameter);

                if (_contentFrame != null)
                {
                    _contentFrame.BackStack.Clear();
                }

                UpdateBackButtonState();
                Title = "Advance Control : Inicio";
            });

            if (!navigated && forceReload && _contentFrame?.Content is Views.Pages.DashboardPage dashboardPage)
            {
                await dashboardPage.ViewModel.LoadAsync();
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

        /// <summary>
        /// Updates UI properties safely on the UI thread
        /// </summary>
        /// <param name="action">Action to execute on UI thread</param>
        private async Task UpdateUIPropertiesAsync(Action action)
        {
            if (App.MainWindow?.DispatcherQueue != null)
            {
                var tcs = new TaskCompletionSource<bool>();
                
                App.MainWindow.DispatcherQueue.TryEnqueue(() =>
                {
                    try
                    {
                        action();
                        tcs.SetResult(true);
                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
                    }
                });
                
                await tcs.Task;
            }
            else
            {
                // If we can't get the dispatcher, execute directly (might be on UI thread already)
                // This can happen during testing or before the main window is initialized
                await _logger.LogWarningAsync(
                    "DispatcherQueue not available, executing property update directly. This may cause threading issues if not on UI thread.",
                    "MainViewModel",
                    "UpdateUIPropertiesAsync");
                action();
            }
        }

        /// <summary>
        /// Releases unmanaged resources and unsubscribes from events
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged resources and unsubscribes from events
        /// </summary>
        /// <param name="disposing">True if disposing managed resources</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                // Recursos futuros se liberan aquí
            }

            _disposed = true;
        }
    }
}
