using Advance_Control.Utilities;
using Advance_Control.Services.PermisosUi;
using Advance_Control.Views.Pages;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Graphics;

namespace Advance_Control.Views.Windows
{
    /// <summary>
    /// Asistentes → Elevadores → Gestionar: el asistente de equipos abre en su propia ventana.
    /// No navega ni cambia nada en la ventana principal.
    /// </summary>
    public sealed class AsistenteEquiposWindow : Window
    {
        public AsistenteEquiposWindow()
        {
            Title = "Gestionar elevadores";

            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);
            appWindow.Resize(new SizeInt32(1500, 950));
            if (appWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.IsMaximizable = true;
                presenter.IsResizable = true;
            }

            var frame = new Frame();
            Content = frame;
            frame.Navigate(typeof(AsistenteEquiposPage));

            // Mismos permisos de UI que la ventana principal aplica a sus páginas.
            if (frame.Content is Page page)
            {
                void AplicarPermisos(object sender, RoutedEventArgs e)
                {
                    page.Loaded -= AplicarPermisos;
                    PermisoUiVisualBinder.ApplyToPage(page, AppServices.Get<IPermisoUiRuntimeService>());
                }
                page.Loaded += AplicarPermisos;
            }
        }

        public static AsistenteEquiposWindow Open()
        {
            var window = new AsistenteEquiposWindow();
            window.Activate();
            return window;
        }
    }
}
