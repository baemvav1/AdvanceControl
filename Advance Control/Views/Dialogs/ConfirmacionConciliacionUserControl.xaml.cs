using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Advance_Control.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Advance_Control.Views.Dialogs
{
    public sealed partial class ConfirmacionConciliacionUserControl : UserControl
    {
        // Lista completa (fuente de verdad para aprobaciones y filtro)
        private readonly List<ConciliacionMatchPropuestaDto> _todasLasPropuestas = new();
        // Lista visible (filtrada)
        private readonly ObservableCollection<ConciliacionMatchPropuestaDto> _propuestas = new();
        private bool _modoAbonos;
        private bool _suspendiendoEventos;

        public event Action<ConciliacionMatchPropuestaDto>? PropuestaAbonosDescartada;
        public event Action<ConciliacionAbonoMovimientoItemDto>? MovimientoAbonosDescartado;

        public bool EsModoAbonos => _modoAbonos;

        public ConfirmacionConciliacionUserControl()
        {
            InitializeComponent();
            ListaPropuestas.ItemsSource = _propuestas;
        }

        public void SetModo(ConciliacionAutomaticaModo? modo)
        {
            _modoAbonos = modo == ConciliacionAutomaticaModo.Abonos;
            HeaderGenerico.Visibility = _modoAbonos ? Visibility.Collapsed : Visibility.Visible;
            HeaderAbonos.Visibility = _modoAbonos ? Visibility.Visible : Visibility.Collapsed;
        }

        public void SetPropuestas(IReadOnlyList<ConciliacionMatchPropuestaDto> propuestas)
        {
            _suspendiendoEventos = true;
            _todasLasPropuestas.Clear();
            foreach (var propuesta in propuestas)
            {
                propuesta.Aprobado = true;
                _todasLasPropuestas.Add(propuesta);
            }
            AplicarFiltroMetadato(string.Empty);
            _suspendiendoEventos = false;
        }

        // Filtra la lista visible por MetadatosTexto (contiene, insensible a mayúsculas)
        public void AplicarFiltroMetadato(string filtro)
        {
            _propuestas.Clear();
            var filtradas = string.IsNullOrWhiteSpace(filtro)
                ? _todasLasPropuestas
                : _todasLasPropuestas
                    .Where(p => p.MetadatosAgregados
                        .Contains(filtro, System.StringComparison.OrdinalIgnoreCase))
                    .ToList();

            foreach (var propuesta in filtradas)
            {
                _propuestas.Add(propuesta);
            }
        }

        // Retorna aprobadas de TODA la lista (incluyendo las filtradas/ocultas)
        public IReadOnlyList<ConciliacionMatchPropuestaDto> ObtenerAprobadas()
            => _todasLasPropuestas.Where(p => p.Aprobado).ToList();

        private void BtnAprobarTodo_Click(object sender, RoutedEventArgs e)
        {
            foreach (var propuesta in _todasLasPropuestas)
            {
                propuesta.Aprobado = true;
            }

            RefrescarListaVisible();
        }

        private void PropuestaCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_suspendiendoEventos || !_modoAbonos)
            {
                return;
            }

            if (sender is CheckBox { DataContext: ConciliacionMatchPropuestaDto propuesta })
            {
                PropuestaAbonosDescartada?.Invoke(propuesta);
            }
        }

        private void BtnDescartarMovimientoAbono_Click(object sender, RoutedEventArgs e)
        {
            if (_suspendiendoEventos || !_modoAbonos)
            {
                return;
            }

            if (sender is Button { DataContext: ConciliacionAbonoMovimientoItemDto movimiento })
            {
                MovimientoAbonosDescartado?.Invoke(movimiento);
            }
        }

        private void RefrescarListaVisible()
        {
            var visibles = _propuestas.ToList();
            _propuestas.Clear();
            foreach (var item in visibles)
            {
                _propuestas.Add(item);
            }
        }
    }
}
