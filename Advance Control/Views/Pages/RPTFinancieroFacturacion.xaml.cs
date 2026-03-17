using Advance_Control.Models;
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
            ViewModel = AppServices.Get<RPTFinancieroFacturacionViewModel>();
            InitializeComponent();
            DataContext = ViewModel;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            await ViewModel.CargarReporteAsync();
        }

        private async void BtnBuscar_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.CargarReporteAsync();
        }

        private async void BtnLimpiar_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.LimpiarFiltros();
            await ViewModel.CargarReporteAsync();
        }

        private void CabeceraButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is ReporteFinancieroFacturacionCabeceraDto cabecera)
            {
                ViewModel.SeleccionarCabecera(cabecera);
            }
        }
    }
}
