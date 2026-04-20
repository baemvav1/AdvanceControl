using System;
using System.Threading;
using System.Threading.Tasks;
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
        static async Task<int> Main(string[] args)
        {
            WinRT.ComWrappersSupport.InitializeComWrappers();

            // Registrar esta instancia con una clave fija
            var isMainInstance = await TryRedirectIfNotMainAsync();

            if (!isMainInstance)
            {
                // Ya existe una instancia principal; esta se cierra silenciosamente
                return 0;
            }

            Microsoft.UI.Xaml.Application.Start((p) =>
            {
                var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
                SynchronizationContext.SetSynchronizationContext(context);
                _ = new App();
            });

            return 0;
        }

        /// <summary>
        /// Intenta registrarse como instancia principal. Si ya hay una,
        /// redirige la activación a ella y retorna false.
        /// </summary>
        private static async Task<bool> TryRedirectIfNotMainAsync()
        {
            try
            {
                var mainInstance = AppInstance.FindOrRegisterForKey("AdvanceControl_Main");

                if (!mainInstance.IsCurrent)
                {
                    // Redirigir la activación a la instancia existente
                    var activatedArgs = AppInstance.GetCurrent().GetActivatedEventArgs();
                    await mainInstance.RedirectActivationToAsync(activatedArgs);
                    return false;
                }

                // Somos la instancia principal — escuchar futuras activaciones
                mainInstance.Activated += OnInstanceActivated;
                return true;
            }
            catch
            {
                // Si AppLifecycle no está disponible, continuar como instancia normal
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
