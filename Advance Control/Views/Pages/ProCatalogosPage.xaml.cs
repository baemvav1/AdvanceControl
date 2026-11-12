using Advance_Control.Utilities;
using Advance_Control.ViewModels;
using Advance_Control.Views.Dialogs;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Advance_Control.Views.Pages
{
    /// <summary>
    /// Lienzo de prototipos del grupo Catálogos. Visible solo para nivel 1 (devs)
    /// vía el sistema de permisos UI. Sin funcionalidad fija: se construye libremente aquí.
    /// Grid "Equipos": "+" abre el buscador de equipos; al elegir uno se muestra su tarjeta
    /// y el botón de basura la limpia.
    /// </summary>
    public sealed partial class ProCatalogosPage : Page
    {
        public ProCatalogosViewModel ViewModel { get; }

        public ProCatalogosPage()
        {
            ViewModel = AppServices.Get<ProCatalogosViewModel>();
            InitializeComponent();
        }

        private async void AgregarEquipo_Click(object sender, RoutedEventArgs e)
        {
            var equipo = await BuscadorEquiposDialog.MostrarAsync(XamlRoot);
            if (equipo is not null)
                ViewModel.Equipo = equipo;
        }

        private void LimpiarEquipo_Click(object sender, RoutedEventArgs e) => ViewModel.LimpiarEquipo();
    }
}
