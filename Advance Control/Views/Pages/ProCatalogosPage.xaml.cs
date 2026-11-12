using Advance_Control.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Advance_Control.Views.Pages
{
    /// <summary>
    /// Lienzo de prototipos del grupo Catálogos. Visible solo para nivel 1 (devs)
    /// vía el sistema de permisos UI. Sin funcionalidad fija: se construye libremente aquí.
    /// Prototipo actual: buscador de equipos a tamaño real de modal.
    /// </summary>
    public sealed partial class ProCatalogosPage : Page
    {
        public ProCatalogosPage()
        {
            InitializeComponent();
        }

        private void Buscador_EquipoElegido(object? sender, EquipoBusquedaDto equipo) => MostrarResultado(equipo);

        private void Seleccionar_Click(object sender, RoutedEventArgs e)
        {
            if (Buscador.Seleccionado is { } equipo)
                MostrarResultado(equipo);
        }

        private void Cancelar_Click(object sender, RoutedEventArgs e)
        {
            ResultadoInfoBar.Severity = InfoBarSeverity.Informational;
            ResultadoInfoBar.Title = "El modal devolvería";
            ResultadoInfoBar.Message = "null (cancelado)";
            ResultadoInfoBar.IsOpen = true;
        }

        private void MostrarResultado(EquipoBusquedaDto equipo)
        {
            ResultadoInfoBar.Severity = InfoBarSeverity.Success;
            ResultadoInfoBar.Title = "El modal devolvería";
            ResultadoInfoBar.Message = $"Equipo #{equipo.IdEquipo} · {equipo.Identificador} · {equipo.MarcaTexto} · {equipo.ClientesTexto} · {equipo.UbicacionAreaTexto}";
            ResultadoInfoBar.IsOpen = true;
        }
    }
}
