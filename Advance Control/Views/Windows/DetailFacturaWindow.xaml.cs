using System;
using Advance_Control.Models;
using Advance_Control.Utilities;
using Advance_Control.ViewModels;
using Microsoft.UI.Xaml;

namespace Advance_Control.Views.Windows
{
    public sealed partial class DetailFacturaWindow : Window
    {
        private readonly int _idFactura;
        public DetailFacturaViewModel ViewModel { get; }

        public DetailFacturaWindow(FacturaResumenDto factura)
        {
            if (factura == null)
            {
                throw new ArgumentNullException(nameof(factura));
            }

            _idFactura = factura.IdFactura;
            ViewModel = AppServices.Get<DetailFacturaViewModel>();

            InitializeComponent();
            RootGrid.DataContext = this;
            Title = factura.FolioTitulo;
            Activated += DetailFacturaWindow_Activated;
        }

        private async void DetailFacturaWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            Activated -= DetailFacturaWindow_Activated;
            await ViewModel.CargarDetalleAsync(_idFactura);
        }

        private void BtnUsarSaldoCompleto_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.UsarSaldoCompleto();
        }

        private async void BtnRegistrarAbono_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.RegistrarAbonoAsync();
        }
    }
}
