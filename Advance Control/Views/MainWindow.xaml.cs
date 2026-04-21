using System;
using System.ComponentModel;
using System.Linq;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Advance_Control.ViewModels;
using Advance_Control.Services.Session;
using Advance_Control.Services.Theme;
using Advance_Control.Services.Notificacion;

namespace Advance_Control
{
    public sealed partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;
        private readonly IUserSessionService _sessionService;
        private readonly IThemeService _themeService;
        private const double ChatPanelExpandedWidth = 420;
        private const double ChatPanelCollapsedWidth = 0;
        private bool _autoLoginAttempted;
        private MensajesViewModel? _mensajesVmSuscrito;

        // Constructor adapted for DI to inject MainViewModel
        public MainWindow(MainViewModel viewModel, IUserSessionService sessionService, IThemeService themeService)
        {
            this.InitializeComponent();
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
            _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));

            // Aplicar tema guardado antes de mostrar contenido
            _themeService.Initialize(RootGrid);

            // Set the DataContext to the ViewModel on the root Grid
            // Note: Window class in WinUI 3 doesn't have a DataContext property
            RootGrid.DataContext = _viewModel;

            // Initialize navigation with the content frame
            _viewModel.InitializeNavigation(contentFrame);

            // Subscribe to NavigationView events and delegate to ViewModel
            nvSample.ItemInvoked += (sender, args) => _viewModel.OnNavigationItemInvoked(sender, args);
            nvSample.BackRequested += (sender, args) => _viewModel.OnBackRequested(sender, args);
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;

            // Detectar cambio de página para alternar panel contextual
            contentFrame.Navigated += ContentFrame_Navigated;

            // Inicializar ChatPanelView
            ChatPanelControl.Initialize();

            // Fallback in-app notifications cuando el sistema toast no está disponible
            InAppNotificacionMessenger.NotificacionSolicitada += OnNotificacionInApp;

            // Subscribe to Loaded event to attempt auto-login after the window is ready
            RootGrid.Loaded += RootGrid_Loaded;
            UpdateNavigationVisibility();
        }

        private void OnNotificacionInApp(object? sender, (string Titulo, string? Nota) e)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                bool esError = e.Titulo.Contains("Error", StringComparison.OrdinalIgnoreCase)
                            || e.Titulo.Contains("Validación", StringComparison.OrdinalIgnoreCase);

                NotificacionInfoBar.Severity = esError
                    ? InfoBarSeverity.Error
                    : InfoBarSeverity.Success;

                NotificacionInfoBar.Title = e.Titulo;
                NotificacionInfoBar.Message = e.Nota ?? string.Empty;
                NotificacionInfoBar.IsOpen = true;
            });
        }
        
        private async void RootGrid_Loaded(object sender, RoutedEventArgs e)
        {
            // Only attempt auto-login once
            if (_autoLoginAttempted)
                return;

            _autoLoginAttempted = true;

            // Desuscribirse del evento al cerrar ventana
            this.Closed += (_, _) => InAppNotificacionMessenger.NotificacionSolicitada -= OnNotificacionInApp;

            // Attempt auto-login when the window is loaded
            await _viewModel.TryAutoLoginAsync();
            UpdateNavigationVisibility();
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            await _viewModel.ShowLoginDialogAsync();
        }

        private void ToggleChat_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.IsChatPanelVisible = !_viewModel.IsChatPanelVisible;

            // Ajustar el ancho de la columna según la visibilidad
            NotificacionesColumn.Width = _viewModel.IsChatPanelVisible
                ? new GridLength(ChatPanelExpandedWidth)
                : new GridLength(ChatPanelCollapsedWidth);

            // Al ocultar el panel, limpiar usuario visible del ChatPanel
            if (!_viewModel.IsChatPanelVisible)
            {
                ChatPanelControl.ViewModel.IsPanelVisible = false;
            }
            else
            {
                ChatPanelControl.ViewModel.IsPanelVisible = true;
            }
        }

        /// <summary>
        /// Abre el panel lateral de chat (si no está visible).
        /// Llamado desde la activación de notificaciones.
        /// </summary>
        public void MostrarChatPanel()
        {
            if (_viewModel.IsChatPanelVisible) return;

            _viewModel.IsChatPanelVisible = true;
            NotificacionesColumn.Width = new GridLength(ChatPanelExpandedWidth);
            ChatPanelControl.ViewModel.IsPanelVisible = true;
        }

        /// <summary>
        /// Abre la conversación con un usuario específico en el chat panel.
        /// Llamado al hacer clic en una notificación de mensaje.
        /// </summary>
        public void AbrirChatConUsuario(long credencialId)
        {
            ChatPanelControl.ViewModel.AbrirConversacionPorId(credencialId);
        }

        /// <summary>
        /// Alterna el contenido del panel lateral según la página activa:
        /// MensajesPage → UsuarioInfoView, cualquier otra → ChatPanelView.
        /// </summary>
        private void ContentFrame_Navigated(object sender, NavigationEventArgs e)
        {
            // Desuscribir del ViewModel anterior para evitar memory leaks
            if (_mensajesVmSuscrito != null)
            {
                _mensajesVmSuscrito.PropertyChanged -= MensajesViewModel_PropertyChanged;
                _mensajesVmSuscrito = null;
            }

            var esMensajesPage = e.SourcePageType == typeof(Views.Pages.MensajesPage);

            ChatPanelControl.Visibility = esMensajesPage ? Visibility.Collapsed : Visibility.Visible;
            UsuarioInfoControl.Visibility = esMensajesPage ? Visibility.Visible : Visibility.Collapsed;

            // Cuando se está en MensajesPage, vincular la selección de usuario al panel info
            if (esMensajesPage && contentFrame.Content is Views.Pages.MensajesPage mensajesPage)
            {
                _mensajesVmSuscrito = mensajesPage.ViewModel;
                _mensajesVmSuscrito.PropertyChanged += MensajesViewModel_PropertyChanged;
                UsuarioInfoControl.SetUsuario(mensajesPage.ViewModel.UsuarioSeleccionado);
            }
        }

        private void MensajesViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MensajesViewModel.UsuarioSeleccionado) && sender is MensajesViewModel vm)
            {
                UsuarioInfoControl.SetUsuario(vm.UsuarioSeleccionado);
            }
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.IsAuthenticated))
            {
                UpdateNavigationVisibility();
            }
        }

        private void UpdateNavigationVisibility()
        {
            UpdateNavigationItemsVisibility(nvSample.MenuItems.Cast<object>());
        }

        private int UpdateNavigationItemsVisibility(System.Collections.Generic.IEnumerable<object> menuItemsSource)
        {
            var menuItems = menuItemsSource.ToList();
            var visibleItems = 0;

            foreach (var menuItem in menuItems)
            {
                if (menuItem is NavigationViewItem navigationItem)
                {
                    var isVisible = navigationItem.MenuItems.Count > 0
                        ? UpdateNavigationItemsVisibility(navigationItem.MenuItems.Cast<object>()) > 0
                        : _viewModel.ShouldDisplayNavigationTag(navigationItem.Tag?.ToString());

                    navigationItem.Visibility = isVisible
                        ? Visibility.Visible
                        : Visibility.Collapsed;

                    if (isVisible)
                    {
                        visibleItems++;
                    }
                }
                else if (menuItem is NavigationViewItemSeparator separator)
                {
                    separator.Visibility = Visibility.Collapsed;
                }
            }

            for (var index = 0; index < menuItems.Count; index++)
            {
                if (menuItems[index] is not NavigationViewItemSeparator separator)
                    continue;

                var hasVisibleBefore = menuItems.Take(index).Any(IsVisibleNavigationItem);
                var hasVisibleAfter = menuItems.Skip(index + 1).Any(IsVisibleNavigationItem);
                separator.Visibility = hasVisibleBefore && hasVisibleAfter
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }

            return visibleItems;
        }

        private static bool IsVisibleNavigationItem(object menuItem)
        {
            return menuItem is NavigationViewItem navigationItem
                && navigationItem.Visibility == Visibility.Visible;
        }
    }
}
