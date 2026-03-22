using Advance_Control.Views.Viewers;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Graphics;

namespace Advance_Control.Views.Windows
{
    public sealed class ImageViewerWindow : Window
    {
        public ImageViewerWindow(string imagePath, string? title = null)
        {
            Title = string.IsNullOrWhiteSpace(title) ? "Visor de imagen" : title;

            var viewer = new ViewerImagenes();
            viewer.SetImageSource(imagePath);
            Content = viewer;

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
        }
    }
}
