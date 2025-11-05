using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Advance_Control.Views.Login;

namespace Advance_Control.Services.Dialog
{
    /// <summary>
    /// Implementación del servicio de diálogos para WinUI 3.
    /// </summary>
    public class DialogService : IDialogService
    {
        private XamlRoot? _xamlRoot;

        /// <summary>
        /// Inicializa el servicio con el XamlRoot necesario para mostrar diálogos.
        /// </summary>
        /// <param name="xamlRoot">El XamlRoot del elemento UI principal.</param>
        public void Initialize(XamlRoot xamlRoot)
        {
            _xamlRoot = xamlRoot ?? throw new ArgumentNullException(nameof(xamlRoot));
        }

        /// <summary>
        /// Muestra el LoginView como un diálogo.
        /// </summary>
        public async Task ShowLoginDialogAsync()
        {
            EnsureInitialized();

            var loginView = new LoginView();
            var dialog = new ContentDialog
            {
                Title = "Iniciar Sesión",
                Content = loginView,
                CloseButtonText = "Cerrar",
                XamlRoot = _xamlRoot
            };

            await dialog.ShowAsync();
        }

        /// <summary>
        /// Muestra un diálogo de mensaje simple.
        /// </summary>
        public async Task ShowMessageDialogAsync(string title, string message)
        {
            EnsureInitialized();

            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = "Aceptar",
                XamlRoot = _xamlRoot
            };

            await dialog.ShowAsync();
        }

        /// <summary>
        /// Muestra un diálogo de confirmación.
        /// </summary>
        public async Task<bool> ShowConfirmationDialogAsync(string title, string message, string primaryButtonText = "Aceptar", string secondaryButtonText = "Cancelar")
        {
            EnsureInitialized();

            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                PrimaryButtonText = primaryButtonText,
                SecondaryButtonText = secondaryButtonText,
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = _xamlRoot
            };

            var result = await dialog.ShowAsync();
            return result == ContentDialogResult.Primary;
        }

        private void EnsureInitialized()
        {
            if (_xamlRoot == null)
            {
                throw new InvalidOperationException("DialogService no está inicializado. Llame a Initialize(xamlRoot) primero.");
            }
        }
    }
}
