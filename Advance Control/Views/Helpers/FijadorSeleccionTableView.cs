using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation.Collections;
using WinUI.TableView;

namespace Advance_Control.Views.Helpers
{
    /// <summary>
    /// Hace que en un <see cref="TableView"/> los renglones seleccionados suban a la primera
    /// posición (el más reciente primero), la tabla haga scroll hasta ahí y se queden fijos arriba
    /// hasta que el usuario los deseleccione, aunque se ordene o filtre por encabezado. Los
    /// renglones fijados se muestran al doble de alto y con un contorno del color de acento (el
    /// tinte de fondo lo ponen los recursos TableViewRowBackgroundSelected* de la página).
    ///
    /// Detalles de TableView 1.4.1 que obligan a hacerlo así:
    /// - Cualquier reordenamiento reinicia la vista (Reset) y la lista pierde la selección, por eso
    ///   los fijados se guardan aquí y se vuelven a seleccionar tras cada reinicio.
    /// - Su ordenamiento (List.Sort) no es estable: sin orden de columna se desempata por el orden
    ///   de carga para que los demás renglones no se revuelvan.
    /// - Ordenar por encabezado borra todos los SortDescriptions y deselecciona: se intercepta
    ///   ClearSorting para quitar solo los órdenes de columna y conservar el de fijados.
    /// </summary>
    public sealed class FijadorSeleccionTableView
    {
        private readonly TableView _tabla;
        private readonly Func<object, int> _ordenOriginal;
        private readonly SortDescription _ordenFijados;
        private readonly List<object> _fijados = new();
        private readonly Dictionary<object, int> _posicionFijado = new();
        private bool _suprimirSeleccion;
        private int _versionVista;
        private bool _reselectPendiente;

        /// <summary>Se dispara cuando cambia qué renglones están fijados (seleccionados).</summary>
        public event EventHandler? FijadosCambiados;

        /// <summary>Renglones fijados, el más reciente primero.</summary>
        public IReadOnlyList<object> Fijados => _fijados;

        public FijadorSeleccionTableView(TableView tabla, Func<object, int> ordenOriginal)
        {
            _tabla = tabla ?? throw new ArgumentNullException(nameof(tabla));
            _ordenOriginal = ordenOriginal ?? throw new ArgumentNullException(nameof(ordenOriginal));
            _ordenFijados = new SortDescription(null, SortDirection.Ascending, new ComparadorFijados(this), item => item);

            _tabla.FilterHandler = new FiltroConFijados(_tabla, this);
            _tabla.SelectionChanged += Tabla_SelectionChanged;
            _tabla.ClearSorting += Tabla_ClearSorting;
            _tabla.ContainerContentChanging += Tabla_ContainerContentChanging;
            _tabla.RegisterPropertyChangedCallback(TableView.ItemsSourceProperty, (_, _) => LimpiarFijados());

            if (_tabla.CollectionView is IObservableVector<object> vista)
            {
                vista.VectorChanged += Vista_VectorChanged;
            }

            if (_tabla.SortDescriptions is INotifyCollectionChanged ordenes)
            {
                ordenes.CollectionChanged += Ordenes_CollectionChanged;
            }

            AsegurarOrdenFijadosPrimero();
        }

        private bool EstaFijado(object? item) => item is not null && _posicionFijado.ContainsKey(item);

        private void Tabla_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suprimirSeleccion || _reselectPendiente)
            {
                return;
            }

            // Se evalúa después de que TableView termine lo que esté haciendo: si en medio hubo un
            // reinicio de la vista, los "deseleccionados" los quitó TableView, no el usuario.
            var version = _versionVista;
            var agregados = e.AddedItems.Cast<object>().ToList();
            var quitados = e.RemovedItems.Cast<object>().ToList();

            _tabla.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
            {
                if (version != _versionVista)
                {
                    ReseleccionarFijados();
                    return;
                }

                AplicarCambioDeSeleccion(agregados, quitados);
            });
        }

        private void AplicarCambioDeSeleccion(List<object> agregados, List<object> quitados)
        {
            var cambio = false;

            foreach (var item in quitados.Where(EstaFijado))
            {
                _fijados.Remove(item);
                cambio = true;
            }

            foreach (var item in agregados.Where(item => !EstaFijado(item)))
            {
                _fijados.Insert(0, item);
                cambio = true;
            }

            if (!cambio)
            {
                return;
            }

            RecalcularPosiciones();
            ReordenarYReseleccionar(scrollAlPrimero: agregados.Count > 0);

            foreach (var item in quitados.Concat(agregados))
            {
                AplicarEstiloFijado(_tabla.ContainerFromItem(item) as Control, item);
            }

            FijadosCambiados?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>Suelta todos los fijados y deselecciona la tabla.</summary>
        public void Limpiar()
        {
            if (_fijados.Count == 0)
            {
                return;
            }

            var soltados = _fijados.ToList();
            LimpiarFijados();

            _suprimirSeleccion = true;
            try
            {
                _tabla.DeselectAll();
                _tabla.RefreshSorting();
            }
            finally
            {
                _suprimirSeleccion = false;
            }

            foreach (var item in soltados)
            {
                AplicarEstiloFijado(_tabla.ContainerFromItem(item) as Control, item);
            }
        }

        // Los contenedores se reciclan al hacer scroll: el estilo se reaplica cada vez que un
        // contenedor recibe un renglón.
        private void Tabla_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs e) =>
            AplicarEstiloFijado(e.ItemContainer, e.Item);

        /// <summary>
        /// Renglón fijado: alto mínimo al doble del normal (RowMinHeight de la tabla; las celdas
        /// tienen alto automático, así que se estiran) y contorno de 1 px del color de acento.
        /// Renglón no fijado: se quitan los valores locales y vuelve al estilo de la tabla.
        /// </summary>
        private void AplicarEstiloFijado(Control? contenedor, object? item)
        {
            if (contenedor is null)
            {
                return;
            }

            if (EstaFijado(item))
            {
                contenedor.MinHeight = _tabla.RowMinHeight * 2;
                contenedor.BorderBrush = ObtenerBrochaAcento();
                contenedor.BorderThickness = new Thickness(1);
            }
            else
            {
                contenedor.ClearValue(FrameworkElement.MinHeightProperty);
                contenedor.ClearValue(Control.BorderBrushProperty);
                contenedor.ClearValue(Control.BorderThicknessProperty);
            }
        }

        private static Brush? ObtenerBrochaAcento() =>
            Application.Current.Resources.TryGetValue("AccentFillColorDefaultBrush", out var brocha)
                ? brocha as Brush
                : null;

        private void ReordenarYReseleccionar(bool scrollAlPrimero)
        {
            AsegurarOrdenFijadosPrimero();

            _suprimirSeleccion = true;
            try
            {
                _tabla.RefreshSorting();

                // Un renglón recién soltado que no pasa el filtro activo debe desaparecer (mientras
                // estuvo fijado el filtro lo dejaba pasar). RefreshFilter quita renglón por renglón,
                // sin reiniciar la vista.
                if (_tabla.IsFiltered)
                {
                    _tabla.RefreshFilter();
                }

                SeleccionarFijadosEnLista();
            }
            finally
            {
                _suprimirSeleccion = false;
            }

            if (scrollAlPrimero && _fijados.Count > 0)
            {
                _tabla.ScrollIntoView(_fijados[0], ScrollIntoViewAlignment.Leading);
            }
        }

        private void ReseleccionarFijados()
        {
            _suprimirSeleccion = true;
            try
            {
                SeleccionarFijadosEnLista();
            }
            finally
            {
                _suprimirSeleccion = false;
            }
        }

        private void SeleccionarFijadosEnLista()
        {
            var seleccionados = new HashSet<object>(_tabla.SelectedItems.Cast<object>());
            foreach (var item in _fijados.Where(item => !seleccionados.Contains(item)))
            {
                _tabla.SelectedItems.Add(item);
            }
        }

        private void Vista_VectorChanged(IObservableVector<object> sender, IVectorChangedEventArgs e)
        {
            if (e.CollectionChange != CollectionChange.Reset)
            {
                return;
            }

            _versionVista++;

            // Un reinicio que no provocamos nosotros (filtro, orden por encabezado) deja la lista
            // sin selección: se vuelven a seleccionar los fijados cuando TableView termine.
            if (!_suprimirSeleccion && !_reselectPendiente && _fijados.Count > 0)
            {
                _reselectPendiente = true;
                _tabla.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
                {
                    _reselectPendiente = false;
                    ReseleccionarFijados();
                });
            }
        }

        private void Tabla_ClearSorting(object? sender, TableViewClearSortingEventArgs e)
        {
            // Sustituye la limpieza de TableView (que borra todo y deselecciona): solo se quitan los
            // órdenes de columna; el de fijados se queda.
            e.Handled = true;

            foreach (var orden in _tabla.SortDescriptions.Where(orden => orden != _ordenFijados).ToList())
            {
                _tabla.SortDescriptions.Remove(orden);
            }

            foreach (var columna in _tabla.Columns.Where(columna => columna.SortDirection is not null))
            {
                columna.SortDirection = null;
            }
        }

        private void Ordenes_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // Si algo quitó el orden de fijados (p. ej. "limpiar orden" del botón de esquina), se
            // vuelve a poner primero. No se puede modificar la colección dentro de su propio evento.
            if (!_tabla.SortDescriptions.Contains(_ordenFijados) || _tabla.SortDescriptions.IndexOf(_ordenFijados) != 0)
            {
                _tabla.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
                {
                    if (AsegurarOrdenFijadosPrimero())
                    {
                        ReordenarYReseleccionar(scrollAlPrimero: false);
                    }
                });
            }
        }

        /// <summary>Pone el orden de fijados en la posición 0. Devuelve true si tuvo que moverlo.</summary>
        private bool AsegurarOrdenFijadosPrimero()
        {
            var indice = _tabla.SortDescriptions.IndexOf(_ordenFijados);
            if (indice == 0)
            {
                return false;
            }

            if (indice > 0)
            {
                _tabla.SortDescriptions.RemoveAt(indice);
            }

            _tabla.SortDescriptions.Insert(0, _ordenFijados);
            return true;
        }

        private void LimpiarFijados()
        {
            if (_fijados.Count == 0)
            {
                return;
            }

            _fijados.Clear();
            _posicionFijado.Clear();
            FijadosCambiados?.Invoke(this, EventArgs.Empty);
        }

        private void RecalcularPosiciones()
        {
            _posicionFijado.Clear();
            for (var i = 0; i < _fijados.Count; i++)
            {
                _posicionFijado[_fijados[i]] = i;
            }
        }

        /// <summary>
        /// Fijados arriba en el orden de la lista. Entre no fijados: si hay orden de columna activo
        /// devuelve 0 para que decida ese orden; si no, desempata por el orden de carga.
        /// </summary>
        private sealed class ComparadorFijados : IComparer
        {
            private readonly FijadorSeleccionTableView _fijador;

            public ComparadorFijados(FijadorSeleccionTableView fijador) => _fijador = fijador;

            public int Compare(object? x, object? y)
            {
                var posicionX = -1;
                var posicionY = -1;
                var fijadoX = x is not null && _fijador._posicionFijado.TryGetValue(x, out posicionX);
                var fijadoY = y is not null && _fijador._posicionFijado.TryGetValue(y, out posicionY);

                if (fijadoX && fijadoY)
                {
                    return posicionX.CompareTo(posicionY);
                }

                if (fijadoX != fijadoY)
                {
                    return fijadoX ? -1 : 1;
                }

                if (_fijador._tabla.SortDescriptions.Count > 1 || x is null || y is null)
                {
                    return 0;
                }

                return _fijador._ordenOriginal(x).CompareTo(_fijador._ordenOriginal(y));
            }
        }

        /// <summary>Filtro de columnas normal, excepto que un renglón fijado siempre se muestra.</summary>
        private sealed class FiltroConFijados : ColumnFilterHandler
        {
            private readonly FijadorSeleccionTableView _fijador;

            public FiltroConFijados(TableView tabla, FijadorSeleccionTableView fijador)
                : base(tabla)
            {
                _fijador = fijador;
            }

            public override bool Filter(TableViewColumn column, object? item) =>
                _fijador.EstaFijado(item) || base.Filter(column, item);
        }
    }
}
