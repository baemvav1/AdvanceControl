using Advance_Control.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Advance_Control.Views.Items.Conciliacion
{
    public sealed partial class ConciliacionFacturaItemView : UserControl
    {
        public static readonly DependencyProperty FacturaProperty = DependencyProperty.Register(
            nameof(Factura),
            typeof(FacturaResumenDto),
            typeof(ConciliacionFacturaItemView),
            new PropertyMetadata(null));

        public ConciliacionFacturaItemView()
        {
            InitializeComponent();
        }

        public FacturaResumenDto? Factura
        {
            get => (FacturaResumenDto?)GetValue(FacturaProperty);
            set => SetValue(FacturaProperty, value);
        }
    }
}
