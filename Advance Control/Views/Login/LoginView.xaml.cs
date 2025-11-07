using Microsoft.UI.Xaml;
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

        /// <summary>
        /// Acción para cerrar el diálogo
        /// </summary>
        public Action? CloseDialogAction { get; set; }

        public LoginView(LoginViewModel viewModel)
        {
            ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            
            this.InitializeComponent();
            
            // Establecer el DataContext para los bindings
            this.DataContext = ViewModel;

            // Suscribirse a cambios de LoginSuccessful para cerrar el diálogo automáticamente
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LoginViewModel.LoginSuccessful) && ViewModel.LoginSuccessful)
            {
                // Cerrar el diálogo cuando el login sea exitoso
                CloseDialogAction?.Invoke();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // Cerrar el diálogo cuando se cancela
            CloseDialogAction?.Invoke();
        }
    }
}
