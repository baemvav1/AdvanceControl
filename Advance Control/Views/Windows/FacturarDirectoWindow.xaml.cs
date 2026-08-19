using System;
using Advance_Control.Models;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Graphics;

namespace Advance_Control.Views.Windows
{
    /// <summary>
    /// Ventana para timbrar directamente una operación vía FEL Bilkon (POST
    /// api/factura/operacion/{id}/timbrar). Por ahora solo trae el shell (header
    /// con los datos de la operación) y el área de contenido vacía -- el diseño
    /// del formulario se va a ir completando aparte.
    /// </summary>
    public sealed partial class FacturarDirectoWindow : Window
    {
        public OperacionSinFacturaDto Operacion { get; }

        public FacturarDirectoWindow(OperacionSinFacturaDto operacion)
        {
            Operacion = operacion ?? throw new ArgumentNullException(nameof(operacion));

            InitializeComponent();
            AjustarTamano(900, 700);

            Title = "Facturar";
            TxtTitulo.Text = "Facturar";
            TxtSubtitulo.Text = $"{Operacion.RazonSocial} · {Operacion.IdOperacionTexto}";
            TxtMonto.Text = Operacion.MontoTexto;
        }

        private void AjustarTamano(int ancho, int alto)
        {
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);
            appWindow.Resize(new SizeInt32(ancho, alto));
        }
    }
}
