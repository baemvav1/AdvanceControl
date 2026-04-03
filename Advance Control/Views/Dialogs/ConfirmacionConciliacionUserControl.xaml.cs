using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using Advance_Control.Models;
using Advance_Control.Services.Facturas;
using Advance_Control.Utilities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Windows.System;

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

        // Referencia al HyperlinkButton del folio cuyo Flyout está abierto actualmente
        private HyperlinkButton? _btnFolioActivo;

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
            foreach (var propuesta in propuestas.OrderByDescending(p => p.Movimiento.Abono))
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

        // ─── Reasignación de folio (solo propuestas 1 a 1) ──────────────────────

        private void BtnFolioReasignar_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not HyperlinkButton btn)
            {
                return;
            }

            _btnFolioActivo = btn;
            FlyoutBase.ShowAttachedFlyout(btn);
        }

        private void FlyoutFolio_Opening(object sender, object e)
        {
            if (sender is not Flyout flyout || flyout.Content is not StackPanel panel)
            {
                return;
            }

            // Limpiar estado del flyout cada vez que se abre
            if (panel.FindName("TxtFolioNuevo") is TextBox txt)
            {
                txt.Text = string.Empty;
            }

            if (panel.FindName("PanelEstadoFolio") is StackPanel panelEstado)
            {
                panelEstado.Visibility = Visibility.Collapsed;
            }
        }

        private void TxtFolioNuevo_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key != VirtualKey.Enter)
            {
                return;
            }

            // Simular click en "Comprobar" cuando el usuario presiona Enter
            if (sender is TextBox txt
                && ObtenerAncestro<StackPanel>(txt) is StackPanel panel
                && panel.FindName("BtnComprobarFolio") is Button btnComprobar)
            {
                BtnComprobarFolio_Click(btnComprobar, new RoutedEventArgs());
            }
        }

        private async void BtnComprobarFolio_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btnComprobar)
            {
                return;
            }

            var panel = ObtenerAncestro<StackPanel>(btnComprobar);
            if (panel is null)
            {
                return;
            }

            var folioNuevo = (panel.FindName("TxtFolioNuevo") as TextBox)?.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(folioNuevo))
            {
                MostrarEstadoFlyout(panel, cargando: false, mensaje: "Escribe un folio antes de comprobar.", esError: true);
                return;
            }

            if (btnComprobar.DataContext is not ConciliacionMatchPropuestaDto propuesta || !propuesta.EsUnoAUno)
            {
                return;
            }

            MostrarEstadoFlyout(panel, cargando: true, mensaje: "Verificando...", esError: false);
            btnComprobar.IsEnabled = false;

            try
            {
                var facturaService = AppServices.Get<IFacturaService>();
                var facturaEncontrada = await facturaService
                    .BuscarFacturaPorFolioAsync(folioNuevo, CancellationToken.None)
                    .ConfigureAwait(true);

                if (facturaEncontrada is null)
                {
                    MostrarEstadoFlyout(panel, cargando: false,
                        mensaje: $"No se encontró ninguna factura con folio \"{folioNuevo}\".",
                        esError: true);
                    return;
                }

                var montoMovimiento = decimal.Round(propuesta.Movimiento.Abono, 2);
                var totalFactura = decimal.Round(facturaEncontrada.Total, 2);

                if (totalFactura != montoMovimiento)
                {
                    MostrarEstadoFlyout(panel, cargando: false,
                        mensaje: $"El total de la factura ({totalFactura:C2}) no coincide con el abono del movimiento ({montoMovimiento:C2}).",
                        esError: true);
                    return;
                }

                var facturaOriginal = propuesta.Facturas[0];

                // Buscar si el folio ya existe en otra propuesta 1-a-1 del mismo monto
                var otraPropuesta = _todasLasPropuestas
                    .FirstOrDefault(p => p != propuesta
                        && p.EsUnoAUno
                        && string.Equals(p.FacturaPrincipal?.Folio, folioNuevo, StringComparison.OrdinalIgnoreCase)
                        && decimal.Round(p.Movimiento.Abono, 2) == montoMovimiento);

                if (otraPropuesta is not null)
                {
                    // SWAP: intercambiar facturas entre las dos propuestas
                    propuesta.Facturas[0] = facturaEncontrada;
                    otraPropuesta.Facturas[0] = facturaOriginal;
                }
                else
                {
                    // Reemplazo directo
                    propuesta.Facturas[0] = facturaEncontrada;
                }

                // Cerrar el flyout y refrescar la lista
                if (_btnFolioActivo is not null)
                {
                    FlyoutBase.GetAttachedFlyout(_btnFolioActivo)?.Hide();
                    _btnFolioActivo = null;
                }

                RefrescarListaVisible();
            }
            catch (Exception ex)
            {
                MostrarEstadoFlyout(panel, cargando: false,
                    mensaje: $"Error al verificar el folio: {ex.Message}",
                    esError: true);
            }
            finally
            {
                btnComprobar.IsEnabled = true;
            }
        }

        private static void MostrarEstadoFlyout(StackPanel panel, bool cargando, string mensaje, bool esError)
        {
            var panelEstado = panel.FindName("PanelEstadoFolio") as StackPanel;
            var ring = panel.FindName("RingFolio") as ProgressRing;
            var txt = panel.FindName("TxtEstadoFolio") as TextBlock;

            if (panelEstado is null || ring is null || txt is null)
            {
                return;
            }

            panelEstado.Visibility = Visibility.Visible;
            ring.Visibility = cargando ? Visibility.Visible : Visibility.Collapsed;
            ring.IsActive = cargando;
            txt.Text = mensaje;
            txt.Foreground = esError
                ? (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["SystemFillColorCriticalBrush"]
                : (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["SystemFillColorSuccessBrush"];
        }

        /// <summary>Recorre el árbol visual hacia arriba hasta encontrar un ancestro del tipo T.</summary>
        private static T? ObtenerAncestro<T>(DependencyObject elemento) where T : DependencyObject
        {
            var actual = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(elemento);
            while (actual is not null)
            {
                if (actual is T resultado)
                {
                    return resultado;
                }
                actual = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(actual);
            }
            return null;
        }
    }
}
