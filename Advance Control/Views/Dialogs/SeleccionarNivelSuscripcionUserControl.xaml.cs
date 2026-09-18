using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Advance_Control.Views.Dialogs
{
    /// <summary>
    /// Selector del nivel de un contrato de suscripción (Oro/Plata/Bronce)
    /// </summary>
    public sealed partial class SeleccionarNivelSuscripcionUserControl : UserControl
    {
        /// <summary>
        /// Nivel seleccionado ("Oro", "Plata" o "Bronce"), o null si no se ha seleccionado ninguno
        /// </summary>
        public string? NivelSeleccionado { get; private set; }

        public SeleccionarNivelSuscripcionUserControl()
        {
            this.InitializeComponent();
        }

        private void NivelRadio_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is string nivel)
            {
                NivelSeleccionado = nivel;
            }
        }
    }
}
