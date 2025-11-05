using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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
        public async Task<bool> OnPrimaryButtonClickAsync()
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
}

