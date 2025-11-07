using Microsoft.UI.Xaml.Controls;
using Advance_Control.ViewModels;
using System;

namespace Advance_Control.Views.Login
{
    /// <summary>
    /// Vista de inicio de sesión que permite al usuario autenticarse.
    /// </summary>
    public sealed partial class LoginView : UserControl
    {
        /// <summary>
        /// ViewModel para el inicio de sesión
        /// </summary>
        public LoginViewModel ViewModel { get; }

        public LoginView(LoginViewModel viewModel)
        {
            ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            
            this.InitializeComponent();
            
            // Establecer el DataContext para los bindings
            this.DataContext = ViewModel;
        }
    }
}
