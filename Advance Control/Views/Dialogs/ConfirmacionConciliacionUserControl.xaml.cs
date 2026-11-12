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

        /// <summary>Se dispara al desmarcar cualquier propuesta (1 a 1, combinacional o abonos).</summary>
        public event Action<ConciliacionMatchPropuestaDto>? PropuestaDescartada;
        public event Action<ConciliacionAbonoMovimientoItemDto>? MovimientoAbonosDescartado;

        public bool EsModoAbonos => _modoAbonos;

        /// <summary>
        /// Valida en el servidor que la factura del folio escrito pueda ir con el movimiento
        /// (idMovimiento, folio) → (factura, motivo del rechazo). Lo asigna la ventana.
        /// </summary>
        public Func<int, string, System.Threading.Tasks.Task<(FacturaResumenDto? Factura, string? Motivo)>>? ValidarFolio { get; set; }

        /// <summary>Igual que <see cref="ValidarFolio"/> con la factura ya identificada (intercambios).</summary>
        public Func<int, int, System.Threading.Tasks.Task<(FacturaResumenDto? Factura, string? Motivo)>>? ValidarFactura { get; set; }

        /// <summary>
        /// Varias a 1: valida en el servidor que los folios escritos puedan ir juntos con el movimiento
        /// (idMovimiento, folios) → (facturas, motivo del rechazo). Lo asigna la ventana.
        /// </summary>
        public Func<int, IReadOnlyList<string>, System.Threading.Tasks.Task<(IReadOnlyList<FacturaResumenDto>? Facturas, string? Motivo)>>? ValidarFolios { get; set; }

        /// <summary>Igual que <see cref="ValidarFolios"/> con las facturas ya identificadas (intercambios).</summary>
        public Func<int, IReadOnlyList<int>, System.Threading.Tasks.Task<(IReadOnlyList<FacturaResumenDto>? Facturas, string? Motivo)>>? ValidarFacturas { get; set; }

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
            if (_suspendiendoEventos)
            {
                return;
            }

            if (sender is CheckBox { DataContext: ConciliacionMatchPropuestaDto propuesta })
            {
                PropuestaDescartada?.Invoke(propuesta);
            }
        }

        private void BtnDescartarMovimientoAbono_Click(object sender, RoutedEventArgs e)
        {
            // Este boton solo existe en la plantilla visual de filas tipo "Abonos"
            // (Visibility={x:Bind EsAbonos...}), asi que no hace falta condicionar por
            // el modo del paso completo -- un paso combinado (Cheques) puede mostrar
            // filas de otros tipos junto a filas de Abonos.
            if (_suspendiendoEventos)
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

            // Limpiar estado del flyout cada vez que se abre. En Varias a 1 se precarga la lista
            // actual de folios para editarla (quitar, agregar o cambiar alguno).
            var propuesta = _btnFolioActivo?.DataContext as ConciliacionMatchPropuestaDto;
            var esVariasAUno = propuesta?.EsVariasAUno == true;

            if (panel.FindName("TxtTituloFolio") is TextBlock titulo)
            {
                titulo.Text = esVariasAUno ? "Cambiar facturas del pago" : "Reasignar folio de factura";
            }

            if (panel.FindName("TxtFolioNuevo") is TextBox txt)
            {
                txt.Header = esVariasAUno ? "Folios separados por coma" : "Nuevo folio";
                txt.PlaceholderText = esVariasAUno ? "Ej. A-459, A-460" : "Ej. A-459";
                txt.Text = esVariasAUno ? propuesta!.FoliosEditablesTexto : string.Empty;
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

            if (btnComprobar.DataContext is not ConciliacionMatchPropuestaDto propuesta || !propuesta.EsFolioEditable)
            {
                return;
            }

            if (propuesta.EsVariasAUno)
            {
                btnComprobar.IsEnabled = false;
                try
                {
                    await ComprobarFoliosVariasAUnoAsync(panel, propuesta, folioNuevo).ConfigureAwait(true);
                }
                catch (Exception ex)
                {
                    MostrarEstadoFlyout(panel, cargando: false, mensaje: $"Error al verificar los folios: {ex.Message}", esError: true);
                }
                finally
                {
                    btnComprobar.IsEnabled = true;
                }

                return;
            }

            if (ValidarFolio is null || ValidarFactura is null)
            {
                MostrarEstadoFlyout(panel, cargando: false, mensaje: "La validación de folios no está disponible.", esError: true);
                return;
            }

            MostrarEstadoFlyout(panel, cargando: true, mensaje: "Verificando...", esError: false);
            btnComprobar.IsEnabled = false;

            try
            {
                // Mismas reglas del motor (saldo exacto, DTC, RFC, no cancelada...), evaluadas en el servidor.
                var (facturaNueva, motivo) = await ValidarFolio(propuesta.Movimiento.IdMovimiento, folioNuevo).ConfigureAwait(true);
                if (facturaNueva is null)
                {
                    MostrarEstadoFlyout(panel, cargando: false, mensaje: motivo ?? "La factura no cumple las reglas.", esError: true);
                    return;
                }

                var facturaOriginal = propuesta.Facturas[0];
                if (facturaNueva.IdFactura == facturaOriginal.IdFactura)
                {
                    MostrarEstadoFlyout(panel, cargando: false, mensaje: "Esa factura ya es la propuesta.", esError: true);
                    return;
                }

                // Si la factura nueva ya estaba en otra propuesta, se intercambian; pero solo si la
                // factura original también cumple con el movimiento de esa otra propuesta. Si no,
                // esa otra propuesta se queda sin factura y se quita (no se puede aplicar a medias).
                var otraPropuesta = _todasLasPropuestas
                    .FirstOrDefault(p => p != propuesta && p.Facturas.Any(f => f.IdFactura == facturaNueva.IdFactura));

                string? aviso = null;
                if (otraPropuesta is not null)
                {
                    if (!otraPropuesta.EsUnoAUno)
                    {
                        MostrarEstadoFlyout(panel, cargando: false,
                            mensaje: $"La factura {facturaNueva.FolioTitulo} ya está en una propuesta {otraPropuesta.Tipo}; descártala primero.",
                            esError: true);
                        return;
                    }

                    var (facturaIntercambio, motivoIntercambio) = await ValidarFactura(
                        otraPropuesta.Movimiento.IdMovimiento, facturaOriginal.IdFactura).ConfigureAwait(true);

                    if (facturaIntercambio is not null)
                    {
                        otraPropuesta.Facturas[0] = facturaIntercambio;
                    }
                    else
                    {
                        _todasLasPropuestas.Remove(otraPropuesta);
                        _propuestas.Remove(otraPropuesta);
                        aviso = $"Se quitó la propuesta del movimiento del {otraPropuesta.Movimiento.FechaTexto} "
                            + $"({otraPropuesta.Movimiento.AbonoTexto}): {motivoIntercambio}";
                    }
                }

                propuesta.Facturas[0] = facturaNueva;
                RefrescarListaVisible();

                if (aviso is not null)
                {
                    // El flyout queda abierto para que se lea el aviso.
                    MostrarEstadoFlyout(panel, cargando: false, mensaje: $"Factura cambiada a {facturaNueva.FolioTitulo}. {aviso}", esError: false);
                    return;
                }

                if (_btnFolioActivo is not null)
                {
                    FlyoutBase.GetAttachedFlyout(_btnFolioActivo)?.Hide();
                    _btnFolioActivo = null;
                }
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

        /// <summary>
        /// Varias a 1: reemplaza las facturas del pago por las de los folios escritos, validadas en el
        /// servidor con las mismas reglas del motor. Si alguna ya estaba en otra propuesta, esa otra
        /// recibe las facturas que este pago soltó, pero solo si así también cumple; si no, se quita
        /// con aviso (no se puede aplicar a medias ni usar una factura dos veces).
        /// </summary>
        private async System.Threading.Tasks.Task ComprobarFoliosVariasAUnoAsync(StackPanel panel, ConciliacionMatchPropuestaDto propuesta, string textoFolios)
        {
            if (ValidarFolios is null || ValidarFacturas is null)
            {
                MostrarEstadoFlyout(panel, cargando: false, mensaje: "La validación de folios no está disponible.", esError: true);
                return;
            }

            var folios = textoFolios
                .Split(new[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (folios.Count < 2)
            {
                MostrarEstadoFlyout(panel, cargando: false,
                    mensaje: "Escribe al menos 2 folios separados por coma; con uno solo es conciliación 1 a 1.", esError: true);
                return;
            }

            MostrarEstadoFlyout(panel, cargando: true, mensaje: "Verificando...", esError: false);

            var (facturasNuevas, motivo) = await ValidarFolios(propuesta.Movimiento.IdMovimiento, folios).ConfigureAwait(true);
            if (facturasNuevas is null)
            {
                MostrarEstadoFlyout(panel, cargando: false, mensaje: motivo ?? "Las facturas no cumplen las reglas.", esError: true);
                return;
            }

            var idsNuevos = facturasNuevas.Select(f => f.IdFactura).ToHashSet();
            if (idsNuevos.SetEquals(propuesta.Facturas.Select(f => f.IdFactura)))
            {
                MostrarEstadoFlyout(panel, cargando: false, mensaje: "Esas facturas ya son las propuestas.", esError: true);
                return;
            }

            var otras = _todasLasPropuestas
                .Where(p => p != propuesta && p.Facturas.Any(f => idsNuevos.Contains(f.IdFactura)))
                .ToList();

            var noEditable = otras.FirstOrDefault(p => !p.EsVariasAUno);
            if (noEditable is not null)
            {
                MostrarEstadoFlyout(panel, cargando: false,
                    mensaje: $"Alguna factura ya está en una propuesta {noEditable.Tipo}; descártala primero.", esError: true);
                return;
            }

            var avisos = new List<string>();
            var soltadas = propuesta.Facturas.Where(f => !idsNuevos.Contains(f.IdFactura)).ToList();

            if (otras.Count == 1 && soltadas.Count > 0)
            {
                // Intercambio: la otra propuesta cede las facturas que tomamos y recibe las que soltamos.
                var otra = otras[0];
                var candidatas = otra.Facturas
                    .Where(f => !idsNuevos.Contains(f.IdFactura))
                    .Concat(soltadas)
                    .ToList();

                var (facturasIntercambio, motivoIntercambio) = await ValidarFacturas(
                    otra.Movimiento.IdMovimiento, candidatas.Select(f => f.IdFactura).ToList()).ConfigureAwait(true);

                if (facturasIntercambio is not null)
                {
                    otra.Facturas.Clear();
                    otra.Facturas.AddRange(facturasIntercambio);
                    avisos.Add($"El pago del {otra.Movimiento.FechaTexto} ({otra.Movimiento.AbonoTexto}) quedó con {otra.FoliosTexto}.");
                }
                else
                {
                    QuitarPropuesta(otra);
                    avisos.Add($"Se quitó la propuesta del pago del {otra.Movimiento.FechaTexto} ({otra.Movimiento.AbonoTexto}): {motivoIntercambio}");
                }
            }
            else
            {
                foreach (var otra in otras)
                {
                    QuitarPropuesta(otra);
                    avisos.Add($"Se quitó la propuesta del pago del {otra.Movimiento.FechaTexto} ({otra.Movimiento.AbonoTexto}) porque usaba alguna de esas facturas.");
                }
            }

            propuesta.Facturas.Clear();
            propuesta.Facturas.AddRange(facturasNuevas);
            RefrescarListaVisible();

            if (avisos.Count > 0)
            {
                // El flyout queda abierto para que se lea el aviso.
                MostrarEstadoFlyout(panel, cargando: false,
                    mensaje: $"Facturas cambiadas a {propuesta.FoliosTexto}. {string.Join(" ", avisos)}", esError: false);
                return;
            }

            if (_btnFolioActivo is not null)
            {
                FlyoutBase.GetAttachedFlyout(_btnFolioActivo)?.Hide();
                _btnFolioActivo = null;
            }
        }

        private void QuitarPropuesta(ConciliacionMatchPropuestaDto propuesta)
        {
            _todasLasPropuestas.Remove(propuesta);
            _propuestas.Remove(propuesta);
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
