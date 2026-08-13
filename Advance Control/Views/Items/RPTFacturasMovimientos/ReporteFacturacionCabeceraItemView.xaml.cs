using System;
using Advance_Control.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Advance_Control.Views.Items.RPTFacturasMovimientos
{
    public sealed partial class ReporteFacturacionCabeceraItemView : UserControl
    {
        public static readonly DependencyProperty CabeceraProperty = DependencyProperty.Register(
            nameof(Cabecera),
            typeof(ReporteFinancieroFacturacionCabeceraDto),
            typeof(ReporteFacturacionCabeceraItemView),
            new PropertyMetadata(null));

        public event EventHandler<ReporteFinancieroFacturacionCabeceraDto>? EnviarAlertaClick;

        public ReporteFacturacionCabeceraItemView()
        {
            InitializeComponent();
        }

        public ReporteFinancieroFacturacionCabeceraDto? Cabecera
        {
            get => (ReporteFinancieroFacturacionCabeceraDto?)GetValue(CabeceraProperty);
            set => SetValue(CabeceraProperty, value);
        }

        private void BtnEnviarAlerta_Click(object sender, RoutedEventArgs e)
        {
            if (Cabecera != null)
            {
                EnviarAlertaClick?.Invoke(this, Cabecera);
            }
        }
    }
}
