using Advance_Control.Models;
using Advance_Control.Services.Activity;
using Advance_Control.Services.Logging;
using Advance_Control.Utilities;
using Advance_Control.ViewModels;
using Advance_Control.Views.Windows;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using WinRT.Interop;

namespace Advance_Control.Views.Pages
{
    public sealed partial class FacturasView : Page
    {
        public FacturasViewModel ViewModel { get; }
        private readonly IActivityService _activityService;

        public FacturasView()
        {
            InitializeComponent();
            ButtonClickLogger.Attach(this, AppServices.Get<ILoggingService>(), nameof(FacturasView));
            ViewModel = AppServices.Get<FacturasViewModel>();
            _activityService = AppServices.Get<IActivityService>();
            DataContext = ViewModel;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            await ViewModel.CargarFacturasExistentesAsync();
        }

        private async void BtnCargarFacturaXml_Click(object sender, RoutedEventArgs e)
        {
            var hwnd = WindowNative.GetWindowHandle(App.MainWindow);
            await ViewModel.CargarArchivoXmlAsync(hwnd);
            if (!string.IsNullOrEmpty(ViewModel.SuccessMessage))
            {
                _activityService.Registrar("Facturas", "XML de factura cargado");
            }
        }

        private async void BtnGuardarFactura_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.GuardarFacturaAsync();
            if (!string.IsNullOrEmpty(ViewModel.SuccessMessage))
            {
                _activityService.Registrar("Facturas", "Factura guardada");
            }
        }

        private async void BtnCargarMultiplesFacturas_Click(object sender, RoutedEventArgs e)
        {
            var hwnd = WindowNative.GetWindowHandle(App.MainWindow);
            await ViewModel.CargarYGuardarMultiplesFacturasAsync(hwnd);
            if (!string.IsNullOrEmpty(ViewModel.SuccessMessage))
            {
                _activityService.Registrar("Facturas", "Carga masiva de facturas ejecutada");
            }
        }

        private async void BtnActualizarFacturas_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.CargarFacturasExistentesAsync();
        }

        private void BtnLimpiarFiltrosFacturas_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.LimpiarFiltrosFacturas();
        }

        private void FacturasListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is FacturaResumenDto factura)
            {
                var detalleWindow = new DetailFacturaWindow(factura);
                detalleWindow.Activate();
            }
        }
    }
}
