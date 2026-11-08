using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.Activity;
using Advance_Control.Services.CheckOperacion;
using Advance_Control.Services.Session;
using Advance_Control.Services.Aprobaciones;
using Advance_Control.Services.Cargos;
using Advance_Control.Services.ImageViewer;
using Advance_Control.Services.LocalStorage;
using Advance_Control.Views.Helpers;
using Microsoft.UI.Xaml.Input;
using WinUI.TableView;
using Advance_Control.Services.Contactos;
using Advance_Control.Services.Notificacion;
using Advance_Control.Utilities;
using Advance_Control.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Advance_Control.Views.Pages
{
    /// <summary>
    /// Lienzo de prototipos del grupo Operaciones. Visible solo para nivel 1 (devs)
    /// vía el sistema de permisos UI. Sin funcionalidad fija: se construye libremente aquí.
    /// Por ahora precarga la operación <see cref="IdOperacionPrototipo"/> con los mismos datos
    /// que muestra el visor de operaciones.
    /// </summary>
    public sealed partial class ProOperacionesPage : Page
    {
        private const int IdOperacionPrototipo = 84;

        private readonly OperacionesViewModel _operaciones = AppServices.Get<OperacionesViewModel>();
        private readonly IAprobacionService _aprobaciones = AppServices.Get<IAprobacionService>();
        private readonly IContactoService _contactos = AppServices.Get<IContactoService>();
        private readonly INotificacionService _notificaciones = AppServices.Get<INotificacionService>();

        private OperacionDto? _operacion;
        private OperacionAprobacionDto? _aprobacion;
        private bool _cargandoCombo;

        private readonly ICargoService _cargos = AppServices.Get<ICargoService>();
        private readonly FijadorSeleccionTableView _fijadorCargos;

        public ProOperacionesPage()
        {
            InitializeComponent();

            // Igual que en Conciliación: el renglón seleccionado sube a la posición 1 y se queda
            // fijo hasta deseleccionarlo; Esc limpia la selección.
            // El renglón en blanco del final no se selecciona ni se fija: solo sirve para el doble clic.
            _fijadorCargos = new FijadorSeleccionTableView(TablaCargos, fila => ((ProCargoFila)fila).Orden, fila => !((ProCargoFila)fila).EsNuevo);
            _fijadorCargos.FijadosCambiados += async (_, _) => await MostrarFotosAsync();
            FotosRepeater.ItemsSource = _fotos;
            var esc = new KeyboardAccelerator { Key = global::Windows.System.VirtualKey.Escape };
            esc.Invoked += (_, args) =>
            {
                args.Handled = true;
                _fijadorCargos.Limpiar();
            };
            KeyboardAccelerators.Add(esc);

            TablaCargos.RegisterPropertyChangedCallback(TableView.CellsHorizontalOffsetProperty, (_, _) => AjustarColumnaDetalle());
            Loaded += async (_, _) => await CargarOperacionAsync();
        }

        private async Task CargarCargosAsync()
        {
            var cargos = await _cargos.GetCargosAsync(new CargoEditDto { IdOperacion = IdOperacionPrototipo });
            var filas = new ObservableCollection<ProCargoFila>(cargos.Select(ProCargoFila.Desde));
            filas.Add(ProCargoFila.Nuevo(filas.Count));
            TablaCargos.ItemsSource = filas;
            ActualizarBotones();
        }

        private IEnumerable<ProCargoFila> FilasCargos =>
            (TablaCargos.ItemsSource as IEnumerable<ProCargoFila> ?? Enumerable.Empty<ProCargoFila>()).Where(f => !f.EsNuevo);

        // ---------------- Renglón en blanco: doble clic = Agregar cargo ----------------

        private readonly IUserSessionService _sesion = AppServices.Get<IUserSessionService>();
        private readonly IActivityService _actividad = AppServices.Get<IActivityService>();

        private async void TablaCargos_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if ((e.OriginalSource as FrameworkElement)?.DataContext is not ProCargoFila { EsNuevo: true })
            {
                return;
            }

            e.Handled = true;
            if (!await PuedeEditarCargosAsync("agregar cargos"))
            {
                return;
            }

            var ctrl = new Dialogs.AgregarCargoUserControl(IdOperacionPrototipo, _sesion.IdProveedor > 0 ? _sesion.IdProveedor : null);
            var dialogo = new ContentDialog
            {
                Title = "Agregar Cargo",
                Content = ctrl,
                PrimaryButtonText = "Agregar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = XamlRoot
            };
            dialogo.PrimaryButtonClick += (_, args) => { if (!ctrl.IsValid) args.Cancel = true; };
            if (await dialogo.ShowAsync() != ContentDialogResult.Primary)
            {
                return;
            }

            try
            {
                var nuevo = await _cargos.CreateCargoAsync(ctrl.GetCargoEditDto());
                if (nuevo == null)
                {
                    await _notificaciones.MostrarAsync("Agregar cargo", "No se pudo crear el cargo.");
                    return;
                }

                _actividad.Registrar("Operaciones", "Cargo agregado");
                await CargarCargosAsync();
                await CargosCambiaronAsync();
            }
            catch (Exception ex)
            {
                await _notificaciones.MostrarAsync("Agregar cargo", ex.Message);
            }
        }

        /// <summary>Igual que el visor: los PDF quedan viejos y el monto de la operación se recalcula.</summary>
        private async Task CargosCambiaronAsync()
        {
            _operaciones.DeleteOperacionPdfs(IdOperacionPrototipo, "*");
            var total = (decimal)FilasCargos.Sum(f => f.Monto);
            if (await _operaciones.UpdateOperacionAsync(IdOperacionPrototipo, monto: total) && _operacion != null)
            {
                _operacion.Monto = total;
            }
        }

        private async Task<bool> PuedeEditarCargosAsync(string accion)
        {
            if (_operacion == null)
            {
                return false;
            }

            if (_operacion.IsFinalized)
            {
                await _notificaciones.MostrarAsync("Operación finalizada", $"La operación está finalizada; reábrela para {accion}.");
                return false;
            }

            if (_operacion.CotFinalizada)
            {
                await _notificaciones.MostrarAsync("Cotización cerrada", $"La cotización está cerrada; reábrela para {accion}.");
                return false;
            }

            return true;
        }

        // ---------------- Acciones: Eliminar / Reabrir / Finalizar ----------------

        private void ActualizarBotones()
        {
            var op = _operacion;
            EliminarButton.IsEnabled = op is { CanEditCargos: true };

            ReabrirText.Text = op?.IsFinalized == true ? "Reabrir operación" : "Reabrir cotización";
            ReabrirButton.IsEnabled = op != null && (op.IsFinalized || op.CotFinalizada);
            ToolTipService.SetToolTip(ReabrirButton, op?.IsFinalized == true
                ? "Reabrir la operación finalizada"
                : op?.CotFinalizada == true ? "Reabrir la cotización para modificar cargos" : "La cotización ya está abierta");

            FinalizarText.Text = op?.TFinalizado == true ? "Deshacer finalización" : "Finalizar";
            FinalizarIcon.Glyph = op?.TFinalizado == true ? "" : "";
            FinalizarButton.IsEnabled = op is { CanMutateOperation: true };
        }

        private async Task<bool> ConfirmarAsync(string titulo, string mensaje, string boton)
        {
            var dialogo = new ContentDialog
            {
                Title = titulo,
                Content = mensaje,
                PrimaryButtonText = boton,
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = XamlRoot
            };
            return await dialogo.ShowAsync() == ContentDialogResult.Primary;
        }

        private async void EliminarButton_Click(object sender, RoutedEventArgs e)
        {
            var seleccion = _fijadorCargos.Fijados.OfType<ProCargoFila>().Where(f => !f.EsNuevo).ToList();
            if (seleccion.Count == 0)
            {
                await _notificaciones.MostrarAsync("Eliminar", "Selecciona al menos un cargo.");
                return;
            }

            if (!await PuedeEditarCargosAsync("eliminar cargos"))
            {
                return;
            }

            var mensaje = seleccion.Count == 1 ? $"¿Eliminar el cargo {seleccion[0].IdCargo}?" : $"¿Eliminar {seleccion.Count} cargos seleccionados?";
            if (!await ConfirmarAsync("Confirmar eliminación", mensaje, "Eliminar"))
            {
                return;
            }

            var errores = 0;
            foreach (var fila in seleccion)
            {
                try
                {
                    if (!await _cargos.DeleteCargoAsync(fila.IdCargo))
                    {
                        errores++;
                    }
                }
                catch (Exception)
                {
                    errores++;
                }
            }

            if (errores < seleccion.Count)
            {
                _actividad.Registrar("Operaciones", "Cargo eliminado");
            }

            await CargarCargosAsync();
            await CargosCambiaronAsync();
            if (errores > 0)
            {
                await _notificaciones.MostrarAsync("Eliminar", $"{errores} cargo(s) no pudieron eliminarse.");
            }
        }

        private async void ReabrirButton_Click(object sender, RoutedEventArgs e)
        {
            if (_operacion == null)
            {
                return;
            }

            try
            {
                if (_operacion.IsFinalized)
                {
                    if (!await ConfirmarAsync("Reabrir operación", "¿Desea reabrir esta operación? Se eliminará la fecha de finalización.", "Reabrir"))
                    {
                        return;
                    }

                    if (!await _operaciones.ReopenOperacionAsync(IdOperacionPrototipo))
                    {
                        await _notificaciones.MostrarAsync("Reabrir operación", "No se pudo reabrir la operación.");
                        return;
                    }

                    _actividad.Registrar("Operaciones", "Operación reabierta");
                }
                else if (_operacion.CotFinalizada)
                {
                    var aprobada = _aprobacion?.Estado == "Aprobada";
                    var mensaje = aprobada
                        ? "La cotización ya fue aprobada por el cliente. Al reabrirla, esa aprobación queda como reemplazada y, cuando la vuelvas a cerrar, el cliente tendrá que aprobarla de nuevo.\n\nMientras esté abierta, la operación no aparecerá en el Portal de Clientes."
                        : "Podrás modificar los cargos. Mientras esté abierta, la operación no aparecerá en el Portal de Clientes.";
                    if (!await ConfirmarAsync("Reabrir cotización", mensaje, "Reabrir"))
                    {
                        return;
                    }

                    await _aprobaciones.ReabrirCotizacionAsync(IdOperacionPrototipo);
                    _actividad.Registrar("Operaciones", aprobada ? "Cotización aprobada reabierta" : "Cotización reabierta");
                }
                else
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                await _notificaciones.MostrarAsync("Reabrir", ex.Message);
                return;
            }

            await CargarOperacionAsync();
        }

        private async void FinalizarButton_Click(object sender, RoutedEventArgs e)
        {
            if (_operacion is not { CanMutateOperation: true })
            {
                return;
            }

            var deshacer = _operacion.TFinalizado;
            if (!await ConfirmarAsync(
                    deshacer ? "Deshacer finalización de trabajo" : "Finalizar trabajo",
                    deshacer ? "¿Desea desmarcar el trabajo de esta operación como finalizado?" : "¿Desea marcar el trabajo de esta operación como finalizado?",
                    deshacer ? "Deshacer" : "Finalizar"))
            {
                return;
            }

            try
            {
                var ok = deshacer
                    ? await _operaciones.DesfinalizarTrabajoAsync(IdOperacionPrototipo)
                    : await _operaciones.FinalizarTrabajoAsync(IdOperacionPrototipo);
                if (!ok)
                {
                    await _notificaciones.MostrarAsync("Finalizar", "No se pudo cambiar el estado del trabajo.");
                    return;
                }

                _actividad.Registrar("Operaciones", deshacer ? "Trabajo desmarcado como finalizado" : "Trabajo marcado como finalizado");
                _operacion.TFinalizado = !deshacer;
                ActualizarBotones();
            }
            catch (Exception ex)
            {
                await _notificaciones.MostrarAsync("Finalizar", ex.Message);
            }
        }

        // ---------------- Línea de tiempo ----------------

        private static readonly SolidColorBrush BrochaCumplido = new(ColorHelper.FromArgb(0xFF, 0x22, 0xC5, 0x5E));
        private static readonly SolidColorBrush BrochaPendiente = new(ColorHelper.FromArgb(0xFF, 0x9E, 0x9E, 0x9E));

        /// <summary>
        /// Un paso por columna: punto sobre la línea, nombre del paso y, si ya se cumplió, el último
        /// que lo cumplió (botón que abre el historial del paso). Los pasos marcados antes de que
        /// existiera el historial muestran "Sin registro".
        /// </summary>
        private void ConstruirLineaTiempo()
        {
            LineaTiempo.Children.Clear();
            LineaTiempo.ColumnDefinitions.Clear();
            if (_operacion == null)
            {
                return;
            }

            var pasos = _operacion.CkPasos;
            for (var i = 0; i < pasos.Count; i++)
            {
                var paso = pasos[i];
                var siguienteCumplido = i + 1 < pasos.Count && pasos[i + 1].Completado;
                LineaTiempo.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                // Tramo de línea: la mitad izquierda llega a este punto, la derecha sale al siguiente.
                var tramo = new Grid { Height = 16 };
                tramo.ColumnDefinitions.Add(new ColumnDefinition());
                tramo.ColumnDefinitions.Add(new ColumnDefinition());
                if (i > 0)
                {
                    tramo.Children.Add(new Border { Height = 2, Background = paso.Completado ? BrochaCumplido : BrochaPendiente });
                }

                if (i + 1 < pasos.Count)
                {
                    var derecha = new Border { Height = 2, Background = siguienteCumplido ? BrochaCumplido : BrochaPendiente };
                    Grid.SetColumn(derecha, 1);
                    tramo.Children.Add(derecha);
                }

                var punto = new Microsoft.UI.Xaml.Shapes.Ellipse
                {
                    Width = 14,
                    Height = 14,
                    Fill = paso.Completado ? BrochaCumplido : new SolidColorBrush(Colors.White),
                    Stroke = paso.Completado ? BrochaCumplido : BrochaPendiente,
                    StrokeThickness = 2
                };
                Grid.SetColumnSpan(punto, 2);
                tramo.Children.Add(punto);

                var columna = new StackPanel { Spacing = 2 };
                columna.Children.Add(tramo);
                columna.Children.Add(new TextBlock
                {
                    Text = paso.Nombre,
                    FontSize = 11,
                    FontWeight = paso.Completado ? Microsoft.UI.Text.FontWeights.SemiBold : Microsoft.UI.Text.FontWeights.Normal,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextAlignment = TextAlignment.Center,
                    TextWrapping = TextWrapping.Wrap
                });
                if (paso.Completado)
                {
                    var quien = UltimoQueCumplio(paso.Campo);
                    var boton = new HyperlinkButton
                    {
                        Content = new TextBlock { Text = quien ?? "Sin registro", FontSize = 10, TextTrimming = TextTrimming.CharacterEllipsis },
                        Padding = new Thickness(4, 0, 4, 0),
                        MinHeight = 0,
                        Opacity = quien == null ? 0.6 : 1,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Tag = paso
                    };
                    ToolTipService.SetToolTip(boton, "Ver historial");
                    boton.Click += PasoHistorial_Click;
                    columna.Children.Add(boton);
                }

                Grid.SetColumn(columna, i);
                LineaTiempo.Children.Add(columna);
            }
        }

        // ---------------- Historial de los pasos ----------------

        private readonly ICheckOperacionService _checks = AppServices.Get<ICheckOperacionService>();
        private List<CheckHistorialDto> _historialChecks = new();

        private async Task CargarHistorialChecksAsync()
        {
            try
            {
                _historialChecks = await _checks.GetHistorialAsync(IdOperacionPrototipo);
            }
            catch (Exception)
            {
                // Sin historial (p. ej. API sin desplegar): la línea muestra "Sin registro".
                _historialChecks = new();
            }
        }

        /// <summary>El último que marcó el paso (el historial viene del más reciente al más viejo).</summary>
        private string? UltimoQueCumplio(string campo) =>
            _historialChecks.FirstOrDefault(h => h.Campo == campo && h.Valor && !string.IsNullOrWhiteSpace(h.RealizadoPor))?.RealizadoPor;

        private async void PasoHistorial_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement { Tag: CheckPasoItem paso })
            {
                return;
            }

            var eventos = _historialChecks.Where(h => h.Campo == paso.Campo).ToList();
            var lista = new StackPanel { Spacing = 10, MinWidth = 380 };
            if (eventos.Count == 0)
            {
                lista.Children.Add(new TextBlock
                {
                    Text = "No hay eventos registrados. El paso se marcó antes de que se guardara el historial.",
                    TextWrapping = TextWrapping.Wrap,
                    Opacity = 0.7
                });
            }

            foreach (var evento in eventos)
            {
                var renglon = new Grid { ColumnSpacing = 10 };
                renglon.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                renglon.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                renglon.Children.Add(new FontIcon
                {
                    Glyph = evento.Valor ? "" : "",
                    FontSize = 14,
                    Foreground = evento.Valor ? BrochaCumplido : BrochaPendiente,
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(0, 2, 0, 0)
                });

                var texto = new StackPanel { Spacing = 1 };
                texto.Children.Add(new TextBlock
                {
                    Text = $"{(evento.Valor ? "Marcado" : "Desmarcado")} por {evento.RealizadoPor ?? "(sin registro)"}",
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    TextWrapping = TextWrapping.Wrap
                });
                texto.Children.Add(new TextBlock
                {
                    Text = evento.Fecha.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
                    FontSize = 12,
                    Opacity = 0.7
                });
                if (!string.IsNullOrWhiteSpace(evento.Detalle))
                {
                    texto.Children.Add(new TextBlock { Text = evento.Detalle, FontSize = 12, Opacity = 0.7, TextWrapping = TextWrapping.Wrap });
                }

                Grid.SetColumn(texto, 1);
                renglon.Children.Add(texto);
                lista.Children.Add(renglon);
            }

            var dialogo = new ContentDialog
            {
                Title = $"Historial · {paso.Nombre}",
                Content = new ScrollViewer { Content = lista, MaxHeight = 420 },
                CloseButtonText = "Cerrar",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = XamlRoot
            };
            await dialogo.ShowAsync();
        }

        // ---------------- Panel Info: fotos del cargo seleccionado ----------------

        private readonly ICargoImageService _imagenesCargo = AppServices.Get<ICargoImageService>();
        private readonly IImageViewerService _visorImagenes = AppServices.Get<IImageViewerService>();
        private readonly ObservableCollection<CargoImageDto> _fotos = new();
        private int _versionFotos;

        /// <summary>
        /// Muestra las fotos del cargo seleccionado más reciente. Si la selección cambia mientras
        /// se descargan, el resultado viejo se descarta.
        /// </summary>
        private async Task MostrarFotosAsync()
        {
            var version = ++_versionFotos;
            _fotos.Clear();

            if (_fijadorCargos.Fijados.FirstOrDefault() is not ProCargoFila { EsNuevo: false } cargo)
            {
                InfoTitulo.Text = "Fotos";
                MostrarMensajeInfo("Selecciona un cargo para ver sus fotos.");
                return;
            }

            InfoTitulo.Text = string.IsNullOrWhiteSpace(cargo.Detalle) ? $"Fotos · {cargo.Tipo}" : $"Fotos · {cargo.Detalle}";
            MostrarMensajeInfo(null);
            InfoCargando.IsActive = true;
            try
            {
                var imagenes = await _imagenesCargo.GetImagesAsync(IdOperacionPrototipo, cargo.IdCargo);
                if (version != _versionFotos)
                {
                    return;
                }

                foreach (var imagen in imagenes)
                {
                    _fotos.Add(imagen);
                }

                MostrarMensajeInfo(imagenes.Count == 0 ? "Este cargo no tiene fotos." : null);
            }
            catch (Exception ex)
            {
                if (version == _versionFotos)
                {
                    MostrarMensajeInfo($"No se pudieron cargar las fotos. {ex.Message}");
                }
            }
            finally
            {
                if (version == _versionFotos)
                {
                    InfoCargando.IsActive = false;
                }
            }
        }

        private void MostrarMensajeInfo(string? mensaje)
        {
            InfoMensaje.Text = mensaje ?? string.Empty;
            InfoMensaje.Visibility = mensaje == null ? Visibility.Collapsed : Visibility.Visible;
        }

        private async void FotoButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { Tag: CargoImageDto imagen })
            {
                await _visorImagenes.ShowImageAsync(imagen.Url, imagen.FileName);
            }
        }

        private void TablaCargos_SizeChanged(object sender, SizeChangedEventArgs e) => AjustarColumnaDetalle();

        // Mismo ajuste que en Conciliación: TableView deja un hueco a la derecha de la última
        // columna, así que Detalle toma el ancho que sobra (sin bajar de 120 px).
        private const double EspacioBarraScroll = 18;
        private readonly Dictionary<TableViewColumn, double> _anchosBase = new();

        private void AjustarColumnaDetalle()
        {
            if (TablaCargos.ActualWidth <= 0)
            {
                return;
            }

            var columnasFijas = TablaCargos.Columns.Where(columna => columna != ColumnaDetalle).ToList();
            foreach (var columna in columnasFijas)
            {
                _anchosBase.TryAdd(columna, columna.Width.Value);
            }

            var restante = TablaCargos.ActualWidth - TablaCargos.CellsHorizontalOffset
                - columnasFijas.Sum(columna => _anchosBase[columna]) - EspacioBarraScroll;
            ColumnaDetalle.Width = new GridLength(Math.Max(restante, 120));
        }

        private async Task CargarOperacionAsync()
        {
            try
            {
                var acceso = await _operaciones.GetOperacionVisorAsync(IdOperacionPrototipo, null);
                if (acceso?.Operacion == null)
                {
                    NumeroOperacionText.Text = $"Sin acceso a la operación {IdOperacionPrototipo}";
                    return;
                }

                _operacion = acceso.Operacion;
                NumeroOperacionText.Text = $"#{_operacion.FolioTexto}";
                ClienteText.Text = _operacion.RazonSocial ?? "Sin cliente";
                EquipoText.Text = _operacion.Identificador ?? "Sin equipo";
                NotaText.Text = _operacion.Nota ?? string.Empty;
                ToolTipService.SetToolTip(NotaText, string.IsNullOrWhiteSpace(_operacion.Nota) ? null : _operacion.Nota);
                MostrarEstadoOperacion(_operacion);

                _aprobacion = await _aprobaciones.ObtenerOperacionAsync(IdOperacionPrototipo);
                _operacion.CotFinalizada = _aprobacion.CotFinalizada;
                CotizacionEstadoText.Text = _aprobacion.EstadoTexto ?? string.Empty;

                // El check completo (incluye "Cotización aprobada"); si falla quedan los campos en línea.
                _operacion.BuildCheckFromInlineFields();
                await _operaciones.LoadCheckAsync(_operacion);
                await CargarHistorialChecksAsync();
                ConstruirLineaTiempo();
                ActualizarBotones();

                await CargarContactosAsync();
                await CargarCargosAsync();
            }
            catch (Exception ex)
            {
                NumeroOperacionText.Text = $"Error al cargar la operación {IdOperacionPrototipo}";
                await _notificaciones.MostrarAsync("Prototipo de operaciones", ex.Message);
            }
        }

        private async Task CargarContactosAsync()
        {
            if (_operacion?.IdCliente is not int idCliente)
            {
                DirigidoCombo.IsEnabled = false;
                return;
            }

            List<ContactoDto> contactos = (await _contactos.GetContactosAsync(new ContactoQueryDto { IdCliente = idCliente }))
                .OrderBy(c => c.NombreCompleto)
                .ToList();

            _cargandoCombo = true;
            DirigidoCombo.ItemsSource = contactos;
            DirigidoCombo.SelectedItem = contactos.FirstOrDefault(c => c.ContactoId == _aprobacion?.IdContactoDirigido);
            DirigidoCombo.IsEnabled = _operacion.IsEditable;
            _cargandoCombo = false;
        }

        /// <summary>Igual que "Cambiar contacto dirigido" del visor: guarda el contacto elegido.</summary>
        private async void DirigidoCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_cargandoCombo || DirigidoCombo.SelectedItem is not ContactoDto contacto
                || contacto.ContactoId == _aprobacion?.IdContactoDirigido)
            {
                return;
            }

            try
            {
                _aprobacion = await _aprobaciones.SetDirigidoAsync(IdOperacionPrototipo, contacto.ContactoId);
                CotizacionEstadoText.Text = _aprobacion.EstadoTexto ?? string.Empty;
            }
            catch (InvalidOperationException ex)
            {
                await _notificaciones.MostrarAsync("Contacto dirigido", ex.Message);
                await CargarContactosAsync();
            }
        }

        private void MostrarEstadoOperacion(OperacionDto operacion)
        {
            var color = operacion.IsFinalized
                ? ColorHelper.FromArgb(0xFF, 0x80, 0x80, 0x80)
                : ColorHelper.FromArgb(0xFF, 0x2D, 0xA4, 0x4E);
            EstadoBadgeText.Text = operacion.IsFinalized ? "✓ Finalizada" : "● Abierta";
            EstadoBadgeText.Foreground = new SolidColorBrush(color);
            EstadoBadge.BorderBrush = new SolidColorBrush(color);
            EstadoBadge.Background = new SolidColorBrush(ColorHelper.FromArgb(0x15, color.R, color.G, color.B));
        }
    }
}
