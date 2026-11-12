using Advance_Control.Services.Notificacion;
using Advance_Control.Utilities;
using Advance_Control.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using Campo = Advance_Control.ViewModels.BuscadorEquiposViewModel.Campo;

namespace Advance_Control.Views.Pages
{
    /// <summary>
    /// Lienzo de prototipos del grupo Catálogos. Visible solo para nivel 1 (devs)
    /// vía el sistema de permisos UI. Sin funcionalidad fija: se construye libremente aquí.
    /// Prototipo actual: buscador de equipos (diseño del usuario, futuro modal).
    /// La lista queda vacía hasta el primer criterio (texto o una opción elegida).
    /// </summary>
    public sealed partial class ProCatalogosPage : Page
    {
        public BuscadorEquiposViewModel ViewModel { get; }
        private readonly INotificacionService _notificacionService;

        public ProCatalogosPage()
        {
            ViewModel = AppServices.Get<BuscadorEquiposViewModel>();
            _notificacionService = AppServices.Get<INotificacionService>();
            InitializeComponent();
            Loaded += ProCatalogosPage_Loaded;
        }

        private async void ProCatalogosPage_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= ProCatalogosPage_Loaded;
            BusquedaTextBox.Focus(FocusState.Programmatic);
            await ViewModel.CargarAsync();
        }

        private static Campo CampoDe(object sender) =>
            (sender as FrameworkElement)?.Tag as string switch
            {
                "Marca" => Campo.Marca,
                "Area" => Campo.Area,
                _ => Campo.Ubicacion
            };

        // ── Marca / Área / Ubicación ───────────────────────────────────────

        private void Opcion_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is not AutoSuggestBox box) return;
            ViewModel.ActualizarSugerencias(CampoDe(box), box.Text);
            box.IsSuggestionListOpen = true;
        }

        private void Opcion_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput) return;
            var campo = CampoDe(sender);
            if (string.IsNullOrWhiteSpace(sender.Text))
                ViewModel.QuitarOpcion(campo);
            ViewModel.ActualizarSugerencias(campo, sender.Text);
        }

        private void Opcion_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            if (args.SelectedItem is string valor)
                ViewModel.ElegirOpcion(CampoDe(sender), valor);
        }

        private void Opcion_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            var campo = CampoDe(sender);
            var elegido = ViewModel.ElegirOpcion(campo, args.ChosenSuggestion as string ?? args.QueryText);
            sender.Text = elegido ?? string.Empty;
        }

        /// <summary>Al salir, el texto muestra la opción que de verdad está filtrando (o queda vacío).</summary>
        private void Opcion_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is AutoSuggestBox box)
                box.Text = ViewModel.FiltroActual(CampoDe(box)) ?? string.Empty;
        }

        // ── Texto y lista ─────────────────────────────────────────────────

        private void BusquedaTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Down && ViewModel.Resultados.Count > 0)
            {
                ViewModel.Seleccionado ??= ViewModel.Resultados[0];
                EquiposListView.Focus(FocusState.Keyboard);
                e.Handled = true;
            }
            else if (e.Key == VirtualKey.Enter && ViewModel.HaySeleccion)
            {
                Seleccionar();
                e.Handled = true;
            }
        }

        private void EquiposListView_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e) => Seleccionar();

        // ── Botones ───────────────────────────────────────────────────────

        private void Seleccionar_Click(object sender, RoutedEventArgs e) => Seleccionar();

        private void Cancelar_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.Reiniciar();
            MarcaBox.Text = AreaBox.Text = UbicacionBox.Text = string.Empty;
        }

        /// <summary>En el prototipo solo avisa qué equipo devolvería el modal.</summary>
        private async void Seleccionar()
        {
            if (ViewModel.Seleccionado is not { } equipo) return;
            await _notificacionService.MostrarNotificacionAsync(
                $"Equipo {equipo.Identificador}",
                $"{equipo.MarcaTexto} · {equipo.ClientesTexto} · {equipo.UbicacionAreaTexto}");
        }
    }
}
