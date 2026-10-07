using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Advance_Control.Utilities;
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

        /// <summary>
        /// Constructor que recibe el ViewModel por inyección de dependencias
        /// </summary>
        /// <param name="viewModel">ViewModel de login</param>
        /// <exception cref="ArgumentNullException">Si viewModel es null</exception>
        public LoginView(LoginViewModel viewModel)
        {
            if (viewModel == null)
            {
                throw new ArgumentNullException(nameof(viewModel), 
                    "El LoginViewModel no puede ser null. Asegúrese de que está registrado en el contenedor de DI.");
            }

            ViewModel = viewModel;
            
            this.InitializeComponent();

            EntornoComboBox.SelectedIndex = EntornoApp.EsPruebas ? 1 : 0;
            
            // Establecer el DataContext para los bindings
            this.DataContext = ViewModel;

            // Actualizar el estado de autenticación cuando se carga la vista
            ViewModel.RefreshAuthenticationState();

            // Suscribirse a cambios de LoginSuccessful y IsAuthenticated para cerrar el diálogo automáticamente
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LoginViewModel.LoginSuccessful) && ViewModel.LoginSuccessful)
            {
                // Cerrar el diálogo cuando el login sea exitoso
                CloseDialogAction?.Invoke();
            }
            else if (e.PropertyName == nameof(LoginViewModel.IsAuthenticated) && !ViewModel.IsAuthenticated)
            {
                // Si se cerró sesión exitosamente, cerrar el diálogo
                CloseDialogAction?.Invoke();
            }
        }

        private bool PruebasElegido => EntornoComboBox.SelectedIndex == 1;

        private void EntornoComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Solo se ofrece reiniciar si se eligió un entorno distinto al actual.
            EntornoInfoBar.IsOpen = PruebasElegido != EntornoApp.EsPruebas;
        }

        private void ReiniciarEntorno_Click(object sender, RoutedEventArgs e)
        {
            EntornoApp.Guardar(PruebasElegido);
            var fallo = Microsoft.Windows.AppLifecycle.AppInstance.Restart(string.Empty);
            // Restart solo regresa si no pudo reiniciar.
            EntornoInfoBar.Severity = InfoBarSeverity.Informational;
            EntornoInfoBar.Message = $"Entorno guardado. Cierra y vuelve a abrir Advance Control para conectarte a {(PruebasElegido ? "Pruebas" : "Producción")} ({fallo}).";
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // Limpiar el formulario antes de cerrar
            ViewModel.ClearForm();
            
            // Cerrar el diálogo cuando se cancela
            CloseDialogAction?.Invoke();
        }
    }
}
