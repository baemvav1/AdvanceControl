using Microsoft.UI.Xaml.Controls;
using Advance_Control.ViewModels;

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

        public LoginView()
        {
            // Inicializar el ViewModel
            ViewModel = new LoginViewModel();
            
            this.InitializeComponent();
            
            // Establecer el DataContext para los bindings
            this.DataContext = ViewModel;
        }
    }
}
