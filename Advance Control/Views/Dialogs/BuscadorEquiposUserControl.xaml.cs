using System;
using Advance_Control.Models;
using Advance_Control.Utilities;
using Advance_Control.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using Campo = Advance_Control.ViewModels.BuscadorEquiposViewModel.Campo;

namespace Advance_Control.Views.Dialogs
{
    /// <summary>
    /// Buscador de equipos (diseño del usuario). Se usa con <see cref="BuscadorEquiposDialog.MostrarAsync"/>.
    /// La lista queda vacía hasta el primer criterio: texto (solo identificador) o una opción
    /// elegida en Marca, Área o Ubicación.
    /// </summary>
    public sealed partial class BuscadorEquiposUserControl : UserControl
    {
        public BuscadorEquiposViewModel ViewModel { get; }

        /// <summary>Se eligió un equipo (botón Seleccionar, Enter o doble clic).</summary>
        public event EventHandler<EquipoBusquedaDto>? EquipoSeleccionado;

        /// <summary>Se presionó Cancelar.</summary>
        public event EventHandler? Cancelado;

        public BuscadorEquiposUserControl()
        {
            ViewModel = AppServices.Get<BuscadorEquiposViewModel>();
            InitializeComponent();
            Loaded += BuscadorEquiposUserControl_Loaded;
        }

        private async void BuscadorEquiposUserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= BuscadorEquiposUserControl_Loaded;
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
            var elegido = ViewModel.ElegirOpcion(CampoDe(sender), args.ChosenSuggestion as string ?? args.QueryText);
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
            Cancelado?.Invoke(this, EventArgs.Empty);
        }

        private void Seleccionar()
        {
            if (ViewModel.Seleccionado is { } equipo)
                EquipoSeleccionado?.Invoke(this, equipo);
        }
    }
}
