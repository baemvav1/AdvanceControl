using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Advance_Control.ViewModels;
using Advance_Control.Services.Dialog;

namespace Advance_Control
{
    public sealed partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;
        private readonly IDialogService _dialogService;

        // Constructor adapted for DI to inject MainViewModel and DialogService
        public MainWindow(MainViewModel viewModel, IDialogService dialogService)
        {
            this.InitializeComponent();
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

            // Set the DataContext to the ViewModel on the root Grid
            // Note: Window class in WinUI 3 doesn't have a DataContext property
            RootGrid.DataContext = _viewModel;

            // Initialize the DialogService with the XamlRoot from the window content
            // This must be done after InitializeComponent
            if (this.Content is FrameworkElement element && element.XamlRoot != null)
            {
                _dialogService.SetXamlRoot(element.XamlRoot);
            }

            // Initialize navigation with the content frame
            _viewModel.InitializeNavigation(contentFrame);

            // Subscribe to NavigationView events and delegate to ViewModel
            nvSample.ItemInvoked += (sender, args) => _viewModel.OnNavigationItemInvoked(sender, args);
            nvSample.BackRequested += (sender, args) => _viewModel.OnBackRequested(sender, args);

            // Show LoginView on startup before any AuthService operations
            this.Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Show the login dialog automatically on startup
            // Note: ShowLoginDialogAsync is async void, so we don't await it
            _viewModel.ShowLoginDialogAsync();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.ShowLoginDialogAsync();
        }
    }
}
