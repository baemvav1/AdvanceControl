using Advance_Control.Models;
using Advance_Control.Views.Pages;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Graphics;

namespace Advance_Control.Views.Windows
{
    /// <summary>
    /// Ventana independiente para visualizar/gestionar una operación.
    /// Antes se abría navegando dentro del frame principal de la app; ahora
    /// cada operación abre en su propia ventana para poder trabajar varias a la vez.
    /// </summary>
    public sealed partial class OperacionVisorWindow : Window
    {
        public OperacionVisorWindow(OperacionVisorNavigationContext contexto)
        {
            InitializeComponent();

            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);
            appWindow.Resize(new SizeInt32(1400, 900));
            if (appWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.IsMaximizable = true;
                presenter.IsResizable = true;
            }

            Title = "Operación";

            VisorFrame.Navigate(typeof(OperacionVisorPage), contexto);
            if (VisorFrame.Content is OperacionVisorPage page)
                page.HostWindow = this;
        }

        public static OperacionVisorWindow Open(OperacionDto operacion)
        {
            var window = new OperacionVisorWindow(OperacionVisorNavigationContext.FromOperacion(operacion));
            window.Activate();
            return window;
        }

        public static OperacionVisorWindow? Open(MensajeDto mensaje)
        {
            if (!mensaje.IdReferencia.HasValue)
                return null;

            var window = new OperacionVisorWindow(OperacionVisorNavigationContext.FromMensaje(mensaje));
            window.Activate();
            return window;
        }

        public static OperacionVisorWindow Open(int idOperacion)
        {
            var window = new OperacionVisorWindow(new OperacionVisorNavigationContext { IdOperacion = idOperacion });
            window.Activate();
            return window;
        }
    }
}
