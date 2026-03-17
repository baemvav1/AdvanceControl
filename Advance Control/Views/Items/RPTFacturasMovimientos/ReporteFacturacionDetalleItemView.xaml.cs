using Advance_Control.Models;
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
    }
}
