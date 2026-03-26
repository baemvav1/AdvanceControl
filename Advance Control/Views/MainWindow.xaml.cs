using System;
using System.ComponentModel;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Advance_Control.ViewModels;
using Advance_Control.Services.Session;
using Advance_Control.Services.Theme;

namespace Advance_Control
{
    public sealed partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;
        private readonly IUserSessionService _sessionService;
        private readonly IThemeService _themeService;
        private bool _autoLoginAttempted;

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

            // Subscribe to Loaded event to attempt auto-login after the window is ready
            RootGrid.Loaded += RootGrid_Loaded;
            UpdateNavigationVisibility();
        }

        private async void RootGrid_Loaded(object sender, RoutedEventArgs e)
        {
            // Only attempt auto-login once
            if (_autoLoginAttempted)
                return;

            _autoLoginAttempted = true;

            // Attempt auto-login when the window is loaded
            await _viewModel.TryAutoLoginAsync();
            UpdateNavigationVisibility();
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            await _viewModel.ShowLoginDialogAsync();
        }

        private void ToggleNotificaciones_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.IsNotificacionesVisible = !_viewModel.IsNotificacionesVisible;
            
            // Ajustar el ancho de la columna según la visibilidad
            NotificacionesColumn.Width = _viewModel.IsNotificacionesVisible 
                ? new GridLength(2, GridUnitType.Star) 
                : new GridLength(0);
        }

        private async void DescartarAlertas_Click(object sender, RoutedEventArgs e)
        {
            if (_sessionService.IsLoaded && _sessionService.CredencialId > 0)
                await _viewModel.DescartarAlertasAsync(_sessionService.CredencialId);
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
