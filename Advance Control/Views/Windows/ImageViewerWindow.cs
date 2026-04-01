using Advance_Control.Views.Viewers;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Graphics;

namespace Advance_Control.Views.Windows
{
    public sealed class ImageViewerWindow : Window
    {
        private readonly ViewerImagenes _viewer;

        public ImageViewerWindow(string imagePath, string? title = null)
        {
            Title = string.IsNullOrWhiteSpace(title) ? "Visor de imagen" : title;

            _viewer = new ViewerImagenes();
            _viewer.SetImageSource(imagePath);
            Content = _viewer;

            ConfigureWindow();
        }

        private void ConfigureWindow()
        {
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);
            appWindow.Resize(new SizeInt32(1440, 960));

            if (appWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.IsResizable = true;
                presenter.IsMaximizable = true;
                presenter.IsMinimizable = true;
            }

            // Pasar el HWND al viewer para que el FileSavePicker funcione desde esta ventana
            _viewer.WindowHandle = hwnd;
        }
    }
}
