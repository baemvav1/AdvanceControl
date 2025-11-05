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

    /* -----------------------------
       SECCIÓN DOCUMENTADA: Cómo usar DialogService
       -----------------------------
       
       El DialogService permite mostrar diálogos en la aplicación WinUI 3.
       
       INICIALIZACIÓN:
       ---------------
       El DialogService se registra como Singleton en App.xaml.cs y debe inicializarse
       con el XamlRoot de un elemento UI antes de usarlo.
       
       Ejemplo en MainWindow.xaml.cs o en cualquier ViewModel:
       
       public MainWindow(IDialogService dialogService)
       {
           InitializeComponent();
           _dialogService = dialogService;
           
           // Inicializar el DialogService con el XamlRoot del elemento principal
           // Esto debe hacerse después de InitializeComponent()
           _dialogService.Initialize(this.Content.XamlRoot);
       }
       
       USO BÁSICO:
       -----------
       
       1) Mostrar el LoginView como diálogo:
          
          private readonly IDialogService _dialogService;
          
          public async void OnShowLoginClicked()
          {
              await _dialogService.ShowLoginDialogAsync();
          }
       
       2) Mostrar un mensaje simple:
          
          await _dialogService.ShowMessageDialogAsync("Título", "Este es un mensaje de información");
       
       3) Mostrar un diálogo de confirmación:
          
          bool confirmed = await _dialogService.ShowConfirmationDialogAsync(
              "Confirmar Acción", 
              "¿Está seguro que desea continuar?",
              "Sí, continuar",
              "No, cancelar"
          );
          
          if (confirmed)
          {
              // El usuario confirmó la acción
          }
       
       INYECCIÓN DE DEPENDENCIAS:
       --------------------------
       El DialogService está registrado en el contenedor de DI y puede ser inyectado
       en cualquier ViewModel, servicio o ventana:
       
       public class MiViewModel
       {
           private readonly IDialogService _dialogService;
           
           public MiViewModel(IDialogService dialogService)
           {
               _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
           }
           
           public async Task MostrarLoginAsync()
           {
               await _dialogService.ShowLoginDialogAsync();
           }
       }
       
       NOTAS IMPORTANTES:
       ------------------
       - El DialogService DEBE ser inicializado con Initialize(xamlRoot) antes de usar.
       - Solo puede mostrarse un ContentDialog a la vez en WinUI 3.
       - Si intenta mostrar múltiples diálogos simultáneamente, se lanzará una excepción.
       - El XamlRoot debe pertenecer al árbol visual activo de la aplicación.
    */
}
