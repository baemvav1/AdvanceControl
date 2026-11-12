using Advance_Control.Models;
using Advance_Control.Services.Notificacion;
using Advance_Control.Utilities;
using Advance_Control.Views.Dialogs;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Advance_Control.Views.Pages
{
    /// <summary>
    /// Lienzo de prototipos del grupo Catálogos. Visible solo para nivel 1 (devs)
    /// vía el sistema de permisos UI. Sin funcionalidad fija: se construye libremente aquí.
    /// Prototipo actual: buscador de equipos (BuscadorEquiposUserControl), en línea y como modal.
    /// </summary>
    public sealed partial class ProCatalogosPage : Page
    {
        private readonly INotificacionService _notificacionService;

        public ProCatalogosPage()
        {
            _notificacionService = AppServices.Get<INotificacionService>();
            InitializeComponent();
        }

        private async void AbrirModal_Click(object sender, RoutedEventArgs e)
        {
            var equipo = await BuscadorEquiposDialog.MostrarAsync(XamlRoot);
            await Avisar(equipo);
        }

        private async void Buscador_EquipoSeleccionado(object? sender, EquipoBusquedaDto equipo) => await Avisar(equipo);

        private async void Buscador_Cancelado(object? sender, System.EventArgs e) => await Avisar(null);

        /// <summary>En el prototipo solo avisa qué devolvería el modal.</summary>
        private System.Threading.Tasks.Task Avisar(EquipoBusquedaDto? equipo) =>
            equipo is null
                ? _notificacionService.MostrarNotificacionAsync("Buscador de equipos", "Cancelado (devolvería null)")
                : _notificacionService.MostrarNotificacionAsync(
                    $"Equipo {equipo.Identificador}",
                    $"{equipo.MarcaTexto} · {equipo.ClientesTexto} · {equipo.UbicacionAreaTexto}");
    }
}
