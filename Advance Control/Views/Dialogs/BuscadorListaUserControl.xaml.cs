using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Utilities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace Advance_Control.Views.Dialogs
{
    /// <summary>
    /// Buscador de lista genérico. Quien lo abre le da el título, el texto de búsqueda y una función
    /// que carga los renglones; la lista queda vacía hasta que se escribe algo (cada palabra debe
    /// aparecer, sin acentos ni mayúsculas).
    /// </summary>
    public sealed partial class BuscadorListaUserControl : UserControl
    {
        private readonly Func<Task<List<BuscadorListaItem>>> _cargar;
        private readonly string _indicacion;
        private readonly bool _mostrarTodoSinTexto;
        private List<BuscadorListaItem> _todos = new();
        private bool _cargado;

        public event EventHandler<BuscadorListaItem>? Elegido;
        public event EventHandler? Cancelado;
        public event EventHandler? NuevoSolicitado;

        /// <param name="textoNuevo">Texto del botón "Nuevo…"; null lo oculta.</param>
        /// <param name="mostrarTodoSinTexto">true: sin texto se ve toda la lista (listas cortas, p. ej. áreas).</param>
        public BuscadorListaUserControl(string titulo, string placeholder, string indicacion,
            string? textoNuevo, Func<Task<List<BuscadorListaItem>>> cargar, bool mostrarTodoSinTexto = false)
        {
            _cargar = cargar;
            _mostrarTodoSinTexto = mostrarTodoSinTexto;
            _indicacion = indicacion;
            InitializeComponent();
            TituloText.Text = titulo;
            BusquedaTextBox.PlaceholderText = placeholder;
            if (textoNuevo is null)
                NuevoButton.Visibility = Visibility.Collapsed;
            else
                NuevoText.Text = textoNuevo;
            EstadoText.Text = indicacion;
            Loaded += async (_, _) =>
            {
                BusquedaTextBox.Focus(FocusState.Programmatic);
                await RecargarAsync();
            };
        }

        public BuscadorListaItem? Seleccionado => ResultadosListView.SelectedItem as BuscadorListaItem;

        public async Task RecargarAsync()
        {
            CargandoRing.IsActive = true;
            EstadoText.Text = string.Empty;
            try
            {
                _todos = await _cargar();
                _cargado = true;
                Filtrar();
            }
            catch (Exception ex)
            {
                EstadoText.Text = $"No se pudo cargar la lista: {ex.Message}";
            }
            finally
            {
                CargandoRing.IsActive = false;
            }
        }

        /// <summary>Elige directamente el renglón cuyo dato cumple la condición (p. ej. el recién creado).</summary>
        public bool ElegirDonde(Func<object, bool> condicion)
        {
            var item = _todos.FirstOrDefault(i => condicion(i.Dato));
            if (item is null) return false;
            Elegido?.Invoke(this, item);
            return true;
        }

        private void Filtrar()
        {
            if (!_cargado) return;
            var texto = TextoBusqueda.Normalizar(BusquedaTextBox.Text);
            var anterior = Seleccionado;
            List<BuscadorListaItem> resultados;

            if (texto.Length == 0 && _mostrarTodoSinTexto)
            {
                resultados = _todos.ToList();
                EstadoText.Text = resultados.Count == 0 ? _indicacion : string.Empty;
            }
            else if (texto.Length == 0)
            {
                resultados = new List<BuscadorListaItem>();
                EstadoText.Text = _indicacion;
            }
            else
            {
                var palabras = texto.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                resultados = _todos.Where(i => palabras.All(p => i.Busqueda.Contains(p))).ToList();
                EstadoText.Text = resultados.Count == 0 ? "Sin coincidencias" : string.Empty;
            }

            ResultadosListView.ItemsSource = resultados;
            ResultadosListView.SelectedItem = anterior is not null && resultados.Contains(anterior) ? anterior
                : resultados.Count == 1 ? resultados[0] : null;
            SeleccionarButton.IsEnabled = Seleccionado is not null;
        }

        private void BusquedaTextBox_TextChanged(object sender, TextChangedEventArgs e) => Filtrar();

        private void BusquedaTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Down && ResultadosListView.Items.Count > 0)
            {
                ResultadosListView.SelectedIndex = Math.Max(0, ResultadosListView.SelectedIndex);
                ResultadosListView.Focus(FocusState.Keyboard);
                e.Handled = true;
            }
            else if (e.Key == VirtualKey.Enter && Seleccionado is not null)
            {
                Elegir();
                e.Handled = true;
            }
        }

        private void ResultadosListView_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
            SeleccionarButton.IsEnabled = Seleccionado is not null;

        private void ResultadosListView_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e) => Elegir();

        private void Seleccionar_Click(object sender, RoutedEventArgs e) => Elegir();

        private void Cancelar_Click(object sender, RoutedEventArgs e) => Cancelado?.Invoke(this, EventArgs.Empty);

        private void Nuevo_Click(object sender, RoutedEventArgs e) => NuevoSolicitado?.Invoke(this, EventArgs.Empty);

        private void Elegir()
        {
            if (Seleccionado is { } item)
                Elegido?.Invoke(this, item);
        }
    }
}
