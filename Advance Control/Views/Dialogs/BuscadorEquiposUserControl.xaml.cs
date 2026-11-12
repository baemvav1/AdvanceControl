using System;
using Advance_Control.Models;
using Advance_Control.Utilities;
using Advance_Control.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace Advance_Control.Views.Dialogs
{
    /// <summary>
    /// Buscador de equipos con tamaño fijo de modal (820 x 520). Pensado para mostrarse con
    /// DialogService.ShowDialogAsync(..., getResult: c => c.Seleccionado).
    /// Teclado: escribir filtra; ↓ pasa a la lista; Enter elige (en la búsqueda, si hay un
    /// solo resultado o uno seleccionado). Doble clic en un renglón también elige.
    /// </summary>
    public sealed partial class BuscadorEquiposUserControl : UserControl
    {
        public BuscadorEquiposViewModel ViewModel { get; }

        /// <summary>Equipo seleccionado (lo que devolvería el modal).</summary>
        public EquipoBusquedaDto? Seleccionado => ViewModel.Seleccionado;

        /// <summary>El usuario eligió un equipo (Enter o doble clic). El modal lo usará para cerrar con Primary.</summary>
        public event EventHandler<EquipoBusquedaDto>? EquipoElegido;

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

        private void Elegir()
        {
            if (ViewModel.Seleccionado is { } equipo)
                EquipoElegido?.Invoke(this, equipo);
        }

        private void BusquedaTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Down && ViewModel.Resultados.Count > 0)
            {
                ViewModel.Seleccionado ??= ViewModel.Resultados[0];
                ResultadosListView.Focus(FocusState.Keyboard);
                e.Handled = true;
            }
            else if (e.Key == VirtualKey.Enter)
            {
                Elegir();
                e.Handled = true;
            }
        }

        private void ResultadosListView_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                Elegir();
                e.Handled = true;
            }
        }

        private void ResultadosListView_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e) => Elegir();

        private void LimpiarFiltros_Click(object sender, RoutedEventArgs e) => ViewModel.LimpiarFiltros();
    }
}
