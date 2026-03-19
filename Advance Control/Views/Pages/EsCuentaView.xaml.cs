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
using global::Windows.Foundation;
using global::Windows.Foundation.Collections;
using Advance_Control.ViewModels;
using Advance_Control.Services.Activity;
using Advance_Control.Services.EstadoCuenta;
using Advance_Control.Services.Logging;
using Advance_Control.Utilities;
using Advance_Control.Models;
using Advance_Control.Views.Details;
using Advance_Control.Views.Windows;
using WinRT.Interop;
using Microsoft.Extensions.DependencyInjection;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Advance_Control.Views.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class EsCuentaView : Page
    {
        private static readonly List<Window> VentanasDetalleAbiertas = new();
        public EsCuentaViewModel ViewModel { get; }
        private readonly IActivityService _activityService;

        public EsCuentaView()
        {
            InitializeComponent();
            ButtonClickLogger.Attach(this, AppServices.Get<ILoggingService>(), nameof(EsCuentaView));
            ViewModel = AppServices.Get<EsCuentaViewModel>();
            _activityService = AppServices.Get<IActivityService>();
            DataContext = ViewModel;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            await ViewModel.CargarEstadosCuentaExistentesAsync();
        }

        private async void BtnCargarXml_Click(object sender, RoutedEventArgs e)
        {
            // Obtener el window handle para el FileOpenPicker
            var hwnd = WindowNative.GetWindowHandle(App.MainWindow);
            await ViewModel.CargarArchivoXmlAsync(hwnd);
            if (!string.IsNullOrEmpty(ViewModel.SuccessMessage))
                _activityService.Registrar("EsCuenta", "XML cargado y guardado");
        }

        private async void BtnActualizarListado_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.CargarEstadosCuentaExistentesAsync();
        }

        private void BtnLimpiarFiltrosEstados_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.LimpiarFiltrosEstados();
        }

        private void EstadosCuentaListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is EstadoCuentaResumenDto estadoCuenta)
            {
                var ventanaDetalle = new DetailEstadoCuentaWindow(estadoCuenta);
                ventanaDetalle.Closed += VentanaDetalle_Closed;
                VentanasDetalleAbiertas.Add(ventanaDetalle);
                ventanaDetalle.Activate();
            }
        }

        private static void VentanaDetalle_Closed(object sender, WindowEventArgs args)
        {
            if (sender is Window ventana)
            {
                ventana.Closed -= VentanaDetalle_Closed;
                VentanasDetalleAbiertas.Remove(ventana);
            }
        }
    }
}
