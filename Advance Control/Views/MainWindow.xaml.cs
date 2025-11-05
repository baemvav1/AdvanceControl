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

            // Set the DataContext to the ViewModel
            this.DataContext = _viewModel;

            // Initialize navigation with the content frame
            _viewModel.InitializeNavigation(contentFrame);

            // Subscribe to NavigationView events and delegate to ViewModel
            nvSample.ItemInvoked += (sender, args) => _viewModel.OnNavigationItemInvoked(sender, args);
            nvSample.BackRequested += (sender, args) => _viewModel.OnBackRequested(sender, args);
        }
    }
}
