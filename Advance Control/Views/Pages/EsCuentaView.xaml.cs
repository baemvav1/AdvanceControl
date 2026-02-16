using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Advance_Control.ViewModels;
using WinRT.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Advance_Control.Views.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class EsCuentaView : Page
    {
        public EsCuentaViewModel ViewModel { get; }

        public EsCuentaView()
        {
            InitializeComponent();
            ViewModel = new EsCuentaViewModel();
        }

        private async void BtnCargarXml_Click(object sender, RoutedEventArgs e)
        {
            // Obtener el window handle para el FileOpenPicker
            var hwnd = WindowNative.GetWindowHandle(App.MainWindow);
            await ViewModel.CargarArchivoXmlAsync(hwnd);
        }
    }
}
