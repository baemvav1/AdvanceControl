using System;
using System.Threading;
using Microsoft.UI.Dispatching;
using Microsoft.Windows.AppLifecycle;

namespace Advance_Control
{
    /// <summary>
    /// Entry point personalizado que garantiza instancia única.
    /// Al hacer clic en una notificación, redirige a la instancia existente
    /// en lugar de abrir una nueva ventana.
    /// </summary>
    public static class Program
    {
        [STAThread]
        static int Main(string[] args)
        {
            WinRT.ComWrappersSupport.InitializeComWrappers();

            if (!TryRegisterMainInstance())
                return 0;

            Microsoft.UI.Xaml.Application.Start((p) =>
            {
                var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
                SynchronizationContext.SetSynchronizationContext(context);
                _ = new App();
            });

            return 0;
        }

        /// <summary>
        /// Registra esta instancia como principal. Si ya existe una, redirige la activación
        /// a ella de forma síncrona para no ceder el thread STA antes de Application.Start.
        /// </summary>
        private static bool TryRegisterMainInstance()
        {
            try
            {
                var mainInstance = AppInstance.FindOrRegisterForKey("AdvanceControl_Main");

                if (!mainInstance.IsCurrent)
                {
                    var activatedArgs = AppInstance.GetCurrent().GetActivatedEventArgs();
                    mainInstance.RedirectActivationToAsync(activatedArgs).AsTask().GetAwaiter().GetResult();
                    return false;
                }

                mainInstance.Activated += OnInstanceActivated;
                return true;
            }
            catch
            {
                return true;
            }
        }

        /// <summary>
        /// Se dispara cuando otra instancia redirige su activación aquí.
        /// Trae la ventana principal al frente.
        /// </summary>
        private static void OnInstanceActivated(object? sender, AppActivationArguments args)
        {
            var mainWindow = App.MainWindow;
            mainWindow?.DispatcherQueue?.TryEnqueue(() =>
            {
                if (mainWindow is null) return;
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(mainWindow);

                // Solo restaurar si está minimizada; si está maximizada no tocar
                if (IsIconic(hwnd))
                    ShowWindow(hwnd, 9); // SW_RESTORE

                SetForegroundWindow(hwnd);

                // Abrir el panel de chat
                if (mainWindow is MainWindow mw)
                    mw.MostrarChatPanel();
            });
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);
    }
}
