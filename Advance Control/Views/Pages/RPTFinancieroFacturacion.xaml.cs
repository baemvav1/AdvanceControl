using Advance_Control.Utilities;
using Advance_Control.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Advance_Control.Views.Pages
{
    public sealed partial class RPTFinancieroFacturacion : Page
    {
        public RPTFinancieroFacturacionViewModel ViewModel { get; }

        public RPTFinancieroFacturacion()
        {
            InitializeComponent();
            ViewModel = AppServices.Get<RPTFinancieroFacturacionViewModel>();
            DataContext = ViewModel;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            await CargarReporteSeguroAsync();
        }

        private async void BtnBuscar_Click(object sender, RoutedEventArgs e)
        {
            await CargarReporteSeguroAsync();
        }

        private async void BtnLimpiar_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.LimpiarFiltros();
            await CargarReporteSeguroAsync();
        }

        private async void BtnGenerarReporte_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var rutaArchivo = await ViewModel.GenerarReporteAsync();
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(rutaArchivo)
                {
                    UseShellExecute = true
                });
            }
            catch (System.Exception ex)
            {
                ViewModel.ErrorMessage = $"No se pudo generar el reporte financiero de facturación: {ex.Message}";
                ViewModel.SuccessMessage = null;
            }
        }

        private async System.Threading.Tasks.Task CargarReporteSeguroAsync()
        {
            try
            {
                await ViewModel.CargarReporteAsync();
            }
            catch (System.Exception ex)
            {
                ViewModel.ErrorMessage = $"No se pudo cargar el reporte financiero de facturación: {ex.Message}";
                ViewModel.SuccessMessage = null;
            }
        }
    }
}
