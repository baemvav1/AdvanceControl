using Advance_Control.Models;
using Advance_Control.Views.Windows;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Advance_Control.Views.Items.RPTFacturasMovimientos
{
    public sealed partial class ReporteFacturacionDetalleItemView : UserControl
    {
        public static readonly DependencyProperty DetalleProperty = DependencyProperty.Register(
            nameof(Detalle),
            typeof(ReporteFinancieroFacturacionDetalleDto),
            typeof(ReporteFacturacionDetalleItemView),
            new PropertyMetadata(null));

        public ReporteFacturacionDetalleItemView()
        {
            InitializeComponent();
        }

        public ReporteFinancieroFacturacionDetalleDto? Detalle
        {
            get => (ReporteFinancieroFacturacionDetalleDto?)GetValue(DetalleProperty);
            set => SetValue(DetalleProperty, value);
        }

        private void FolioLink_Click(object sender, RoutedEventArgs e)
        {
            if (Detalle is not { IdFactura: > 0 } detalle)
            {
                return;
            }

            var factura = new FacturaResumenDto
            {
                IdFactura = detalle.IdFactura,
                Folio = detalle.Folio,
                Total = detalle.Total,
                EmisorRfc = detalle.EmisorRfc,
                ReceptorRfc = detalle.ReceptorRfc,
                ReceptorNombre = detalle.ReceptorNombre,
            };

            var visor = new FacturaVisorWindow(factura);
            visor.Activate();
        }
    }
}
