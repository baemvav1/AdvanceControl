using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Advance_Control.Services.Dialog;
using Advance_Control.ViewModels.Login;

namespace Advance_Control.Views.Login
{
    public sealed partial class LoginView : UserControl, IDialogContent<bool>, IAsyncDialogContent
    {
        private readonly LoginViewModel? _viewModel;

        public LoginView()
        {
            this.InitializeComponent();
        }

        public LoginView(LoginViewModel viewModel) : this()
        {
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            this.DataContext = _viewModel;
        }

        /// <summary>
        /// Gets the boolean result of the login operation
        /// </summary>
        public bool GetResult()
        {
            return _viewModel?.LoginResult ?? false;
        }

        /// <summary>
        /// Called when the dialog's primary button is clicked
        /// Triggers the login process and returns true only if login succeeded
        /// </summary>
        public async System.Threading.Tasks.Task<bool> OnPrimaryButtonClickAsync()
        {
            if (_viewModel != null)
            {
                await _viewModel.LoginAsync();
                // Only close the dialog if login was successful
                return _viewModel.LoginResult;
            }
            return false;
        }
    }

    /// <summary>
    /// Converter to invert boolean values
    /// </summary>
    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool boolValue)
                return !boolValue;
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is bool boolValue)
                return !boolValue;
            return false;
        }
    }

    /// <summary>
    /// Converter to collapse element when string is empty
    /// </summary>
    public class EmptyStringToCollapsedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return string.IsNullOrWhiteSpace(value as string) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter to convert boolean to Visibility
    /// </summary>
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool boolValue)
                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is Visibility visibility)
                return visibility == Visibility.Visible;
            return false;
        }
    }
}
