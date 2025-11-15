using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Advance_Control.ViewModels;

namespace Advance_Control
{
    public sealed partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        // Constructor adapted for DI to inject MainViewModel
        public MainWindow(MainViewModel viewModel)
        {
            this.InitializeComponent();
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

            // Set the DataContext to the ViewModel on the root Grid
            // Note: Window class in WinUI 3 doesn't have a DataContext property
            RootGrid.DataContext = _viewModel;

            // Initialize navigation with the content frame
            _viewModel.InitializeNavigation(contentFrame);

            // Subscribe to NavigationView events and delegate to ViewModel
            nvSample.ItemInvoked += (sender, args) => _viewModel.OnNavigationItemInvoked(sender, args);
            nvSample.BackRequested += (sender, args) => _viewModel.OnBackRequested(sender, args);
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
    }
}
