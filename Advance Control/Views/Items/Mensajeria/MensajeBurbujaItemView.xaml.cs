using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Advance_Control.Views.Items.Mensajeria
{
    public sealed partial class MensajeBurbujaItemView : UserControl
    {
        /// <summary>
        /// Evento que se dispara al hacer clic en un botón de PDF.
        /// El sender es el Button con la URL en Tag.
        /// </summary>
        public event EventHandler<RoutedEventArgs>? PdfButtonClick;

        public MensajeBurbujaItemView()
        {
            InitializeComponent();
        }

        private void PdfButton_Click(object sender, RoutedEventArgs e)
        {
            PdfButtonClick?.Invoke(sender, e);
        }
    }
}
