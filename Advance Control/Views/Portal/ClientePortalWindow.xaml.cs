using Advance_Control.Services.Auth;
using Advance_Control.Services.Session;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using Windows.Graphics;

namespace Advance_Control.Views.Portal
{
    /// <summary>
    /// Shell restringido para usuarios-cliente (Nivel = 10): solo ven sus
    /// operaciones y hojas de mantenimiento preventivo, nunca el MainWindow
    /// ni el menú de empleados.
    /// </summary>
    public sealed partial class ClientePortalWindow : Window
    {
        private readonly IUserSessionService _sessionService;
        private readonly IAuthService _authService;

        public ClientePortalWindow(IUserSessionService sessionService, IAuthService authService)
        {
            InitializeComponent();

            _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));

            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);
            appWindow.Resize(new SizeInt32(1100, 780));
            if (appWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.IsMaximizable = true;
                presenter.IsResizable = true;
            }

            Title = "Portal de Cliente";
            SubtituloTextBlock.Text = _sessionService.NombreCompleto ?? string.Empty;

            PortalFrame.Navigate(typeof(ClienteOperacionesPage));
        }

        private async void CerrarSesionButton_Click(object sender, RoutedEventArgs e)
        {
            await _authService.LogoutAsync();
            _sessionService.Clear();
            Close();
        }
    }
}
