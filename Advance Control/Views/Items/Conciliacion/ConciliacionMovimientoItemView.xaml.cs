using Advance_Control.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Advance_Control.Views.Items.Conciliacion
{
    public sealed partial class ConciliacionMovimientoItemView : UserControl
    {
        public static readonly DependencyProperty MovimientoProperty = DependencyProperty.Register(
            nameof(Movimiento),
            typeof(ConciliacionMovimientoResumenDto),
            typeof(ConciliacionMovimientoItemView),
            new PropertyMetadata(null));

        public ConciliacionMovimientoItemView()
        {
            InitializeComponent();
        }

        public ConciliacionMovimientoResumenDto? Movimiento
        {
            get => (ConciliacionMovimientoResumenDto?)GetValue(MovimientoProperty);
            set => SetValue(MovimientoProperty, value);
        }
    }
}
