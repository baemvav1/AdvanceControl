using System;
using Advance_Control.Models;
using Advance_Control.Views.Pages;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Graphics;

namespace Advance_Control.Views.Windows
{
    /// <summary>
    /// Ventana ligera que hospeda <see cref="ProFinancieroPage"/> -- reemplaza a
    /// DetailFacturaWindow como pantalla de detalle abierta desde el botón "Abrir" de Facturas.
    /// ProFinancieroPage sigue siendo una Page normal (también navegable desde el navbar de
    /// prototipos); esta ventana solo le da un Frame propio y le pasa la factura seleccionada.
    /// </summary>
    public sealed partial class FacturaVisorWindow : Window
    {
        public FacturaVisorWindow(FacturaResumenDto factura)
        {
            if (factura == null)
            {
                throw new ArgumentNullException(nameof(factura));
            }

            InitializeComponent();
            Title = factura.FolioTitulo;
            AjustarTamano(1300, 800);

            ContenidoFrame.Navigate(typeof(ProFinancieroPage), factura);
        }

        private void AjustarTamano(int ancho, int alto)
        {
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);
            appWindow.Resize(new SizeInt32(ancho, alto));
        }
    }
}
