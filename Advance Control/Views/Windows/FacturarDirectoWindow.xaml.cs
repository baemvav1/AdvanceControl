using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.Cargos;
using Advance_Control.Services.Clientes;
using Advance_Control.Services.ConfiguracionEmisor;
using Advance_Control.Services.SatCatalogo;
using Advance_Control.Services.TipoCargo;
using Advance_Control.Utilities;
using Microsoft.UI.Text;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics;

namespace Advance_Control.Views.Windows
{
    /// <summary>
    /// Ventana para timbrar directamente una operación vía FEL Bilkon (POST
    /// api/factura/operacion/{id}/timbrar). Las 6 secciones (Emisor, Receptor,
    /// Forma de pago, Conceptos, Total, ControlesEmision) ya están conectadas.
    /// </summary>
    public sealed partial class FacturarDirectoWindow : Window
    {
        private readonly IConfiguracionEmisorService _configuracionEmisorService;
        private readonly IClienteService _clienteService;
        private readonly ICargoService _cargoService;
        private readonly ISatCatalogoService _satCatalogoService;
        private readonly ITipoCargoService _tipoCargoService;
        private readonly Services.Facturas.IFacturaService _facturaService;

        /// <summary>Cliente encontrado por RFC al cargar la ventana, o null si no existe todavía.</summary>
        private CustomerDto? _clienteReceptor;

        /// <summary>Controles de cada fila de Conceptos, para leer sus valores al armar el CFDI.</summary>
        private readonly List<ConceptoRowControls> _conceptoRows = new();

        /// <summary>True si el emisor está configurado y su CSD está vigente -- se puede timbrar.</summary>
        private bool _emisorListo;

        /// <summary>
        /// Panel de depuración (muestra las respuestas crudas del API dentro de la ventana).
        /// Flag hardcodeada a propósito -- cámbiala a false para apagarlo sin quitar el código.
        /// </summary>
        private const bool MostrarPanelDepuracion = true;

        private static readonly JsonSerializerOptions DebugJsonOptions = new() { WriteIndented = true };
        private readonly StringBuilder _debugLog = new();

        private sealed class ConceptoRowControls
        {
            public required CargoDto Cargo { get; init; }
            public required AutoSuggestBox ProdServBox { get; init; }
            public required AutoSuggestBox UnidadBox { get; init; }
            public required ComboBox TasaIvaCombo { get; init; }
        }

        public OperacionSinFacturaDto Operacion { get; }

        public FacturarDirectoWindow(OperacionSinFacturaDto operacion)
        {
            Operacion = operacion ?? throw new ArgumentNullException(nameof(operacion));

            _configuracionEmisorService = AppServices.Get<IConfiguracionEmisorService>();
            _clienteService = AppServices.Get<IClienteService>();
            _cargoService = AppServices.Get<ICargoService>();
            _satCatalogoService = AppServices.Get<ISatCatalogoService>();
            _tipoCargoService = AppServices.Get<ITipoCargoService>();
            _facturaService = AppServices.Get<Services.Facturas.IFacturaService>();

            InitializeComponent();
            AjustarTamano(900, 700);
            PanelDepuracion.Visibility = MostrarPanelDepuracion ? Visibility.Visible : Visibility.Collapsed;

            Title = "Facturar";
            TxtTitulo.Text = "Facturar";
            TxtSubtitulo.Text = $"{Operacion.RazonSocial} · {Operacion.IdOperacionTexto}";
            TxtMonto.Text = Operacion.MontoTexto;

            CmbMetodoPago.SelectedIndex = 0; // PUE
            CmbFormaPago.SelectedIndex = 0;  // 01 - Efectivo

            Activated += FacturarDirectoWindow_Activated;
        }

        private async void FacturarDirectoWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            Activated -= FacturarDirectoWindow_Activated;
            await CargarEmisorAsync();
            await CargarCatalogosCfdiAsync();
            await CargarReceptorAsync();
            await CargarConceptosAsync();
        }

        private async Task CargarCatalogosCfdiAsync()
        {
            var regimenes = await _satCatalogoService.ListarRegimenFiscalAsync();
            CmbReceptorRegimenFiscal.ItemsSource = regimenes;

            var usos = await _satCatalogoService.ListarUsoCfdiAsync();
            CmbReceptorUsoCfdi.ItemsSource = usos;
        }

        /// <summary>Selecciona en el combo la clave indicada, o deja sin selección si el catálogo no la tiene (p. ej. clave dada de baja).</summary>
        private static void SeleccionarClave(ComboBox combo, string? clave)
        {
            if (string.IsNullOrWhiteSpace(clave)) return;
            combo.SelectedItem = combo.Items
                .OfType<SatCatalogoItemDto>()
                .FirstOrDefault(i => string.Equals(i.Clave, clave, StringComparison.OrdinalIgnoreCase));
        }

        private static string ClaveSeleccionada(ComboBox combo) => (combo.SelectedItem as SatCatalogoItemDto)?.Clave ?? string.Empty;

        private void AjustarTamano(int ancho, int alto)
        {
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);
            appWindow.Resize(new SizeInt32(ancho, alto));
        }

        private async Task CargarEmisorAsync()
        {
            EmisorProgressRing.IsActive = true;
            try
            {
                var configuracion = await _configuracionEmisorService.ObtenerConfiguracionAsync();
                RegistrarDebug("ConfiguracionEmisorService.ObtenerConfiguracionAsync", configuracion);

                if (configuracion == null)
                {
                    EmisorDatosGrid.Visibility = Visibility.Collapsed;
                    EmisorInfoBar.Severity = Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error;
                    EmisorInfoBar.Message = "No hay datos del emisor configurados. Configura el emisor y el CSD antes de timbrar.";
                    EmisorInfoBar.IsOpen = true;
                    _emisorListo = false;
                    return;
                }

                TxtEmisorRfc.Text = configuracion.Rfc;
                TxtEmisorRazonSocial.Text = configuracion.RazonSocial;
                TxtEmisorRegimenFiscal.Text = configuracion.RegimenFiscal;
                TxtEmisorLugarExpedicion.Text = configuracion.LugarExpedicion;
                EmisorDatosGrid.Visibility = Visibility.Visible;

                var estadoCsd = await _configuracionEmisorService.ObtenerEstadoCsdAsync();
                RegistrarDebug("ConfiguracionEmisorService.ObtenerEstadoCsdAsync", estadoCsd);
                if (!estadoCsd.Vigente)
                {
                    EmisorInfoBar.Severity = Microsoft.UI.Xaml.Controls.InfoBarSeverity.Warning;
                    EmisorInfoBar.Message = estadoCsd.Cargado
                        ? "El CSD cargado ya no está vigente. No podrás timbrar hasta actualizarlo."
                        : "No hay un CSD cargado. No podrás timbrar hasta configurarlo.";
                    EmisorInfoBar.IsOpen = true;
                    _emisorListo = false;
                }
                else
                {
                    EmisorInfoBar.IsOpen = false;
                    _emisorListo = true;
                }
            }
            finally
            {
                EmisorProgressRing.IsActive = false;
            }
        }

        private async Task CargarReceptorAsync()
        {
            ReceptorProgressRing.IsActive = true;
            try
            {
                var rfc = Operacion.RfcCliente?.Trim() ?? string.Empty;

                TxtReceptorRfc.Text = rfc;
                TxtReceptorRazonSocial.Text = Operacion.RazonSocial ?? string.Empty;

                if (string.IsNullOrWhiteSpace(rfc))
                {
                    MostrarAvisoReceptor("La operación no tiene un RFC de cliente asociado. Complétalo manualmente.", esAdvertencia: true);
                    return;
                }

                var clientes = await _clienteService.GetClientesAsync(new ClienteQueryDto { Rfc = rfc });
                RegistrarDebug($"ClienteService.GetClientesAsync(rfc={rfc})", clientes);
                _clienteReceptor = clientes.FirstOrDefault(c => string.Equals(c.Rfc?.Trim(), rfc, StringComparison.OrdinalIgnoreCase));

                if (_clienteReceptor == null)
                {
                    MostrarAvisoReceptor("No se encontró un cliente con este RFC. Se creará uno nuevo al guardar los datos fiscales.", esAdvertencia: false);
                    return;
                }

                TxtReceptorRazonSocial.Text = _clienteReceptor.RazonSocial ?? Operacion.RazonSocial ?? string.Empty;
                TxtReceptorCodigoPostal.Text = _clienteReceptor.CodigoPostal ?? string.Empty;
                SeleccionarClave(CmbReceptorRegimenFiscal, _clienteReceptor.RegimenFiscal);
                SeleccionarClave(CmbReceptorUsoCfdi, _clienteReceptor.UsoCfdi);

                var faltantes = new System.Collections.Generic.List<string>();
                if (string.IsNullOrWhiteSpace(_clienteReceptor.CodigoPostal)) faltantes.Add("código postal");
                if (CmbReceptorRegimenFiscal.SelectedItem == null) faltantes.Add("régimen fiscal");
                if (CmbReceptorUsoCfdi.SelectedItem == null) faltantes.Add("uso de CFDI");

                if (faltantes.Count > 0)
                {
                    MostrarAvisoReceptor($"Al cliente le falta: {string.Join(", ", faltantes)}. Complétalo y guarda antes de timbrar.", esAdvertencia: true);
                }
                else
                {
                    ReceptorInfoBar.IsOpen = false;
                }
            }
            finally
            {
                ReceptorProgressRing.IsActive = false;
            }
        }

        private void MostrarAvisoReceptor(string mensaje, bool esAdvertencia)
        {
            ReceptorInfoBar.Severity = esAdvertencia
                ? Microsoft.UI.Xaml.Controls.InfoBarSeverity.Warning
                : Microsoft.UI.Xaml.Controls.InfoBarSeverity.Informational;
            ReceptorInfoBar.Message = mensaje;
            ReceptorInfoBar.IsOpen = true;
        }

        /// <summary>
        /// CFDI 4.0 exige que cuando MetodoPago = PPD, FormaPago sea "99 - Por definir"
        /// (el detalle real se reporta después vía complemento de pago). Se fuerza en la UI
        /// para no dejar armar un CFDI que el SAT va a rechazar.
        /// </summary>
        private void CmbMetodoPago_SelectionChanged(object sender, Microsoft.UI.Xaml.Controls.SelectionChangedEventArgs e)
        {
            var metodoPago = (CmbMetodoPago.SelectedItem as Microsoft.UI.Xaml.Controls.ComboBoxItem)?.Tag as string;

            if (metodoPago == "PPD")
            {
                var item99 = CmbFormaPago.Items
                    .OfType<Microsoft.UI.Xaml.Controls.ComboBoxItem>()
                    .FirstOrDefault(i => (string)i.Tag == "99");
                if (item99 != null) CmbFormaPago.SelectedItem = item99;
                CmbFormaPago.IsEnabled = false;
            }
            else
            {
                CmbFormaPago.IsEnabled = true;
            }
        }

        private async Task CargarConceptosAsync()
        {
            ConceptosProgressRing.IsActive = true;
            ConceptosListPanel.Children.Clear();
            _conceptoRows.Clear();
            try
            {
                var cargos = await _cargoService.GetCargosAsync(new CargoEditDto { Operacion = "select", IdOperacion = Operacion.IdOperacion });
                RegistrarDebug($"CargoService.GetCargosAsync(idOperacion={Operacion.IdOperacion})", cargos);

                if (cargos.Count == 0)
                {
                    ConceptosInfoBar.Severity = InfoBarSeverity.Warning;
                    ConceptosInfoBar.Message = "Esta operación no tiene cargos registrados. No hay nada que facturar.";
                    ConceptosInfoBar.IsOpen = true;
                    return;
                }
                ConceptosInfoBar.IsOpen = false;

                var defaults = await _tipoCargoService.ObtenerDefaultsAsync();
                RegistrarDebug("TipoCargoService.ObtenerDefaultsAsync", defaults);
                var defaultsPorTipo = defaults.ToDictionary(d => d.Id);

                foreach (var cargo in cargos)
                {
                    ConceptosListPanel.Children.Add(ConstruirFilaConcepto(cargo, defaultsPorTipo));
                }

                ActualizarTotales();
            }
            finally
            {
                ConceptosProgressRing.IsActive = false;
            }
        }

        private UIElement ConstruirFilaConcepto(CargoDto cargo, Dictionary<int, TipoCargoDefaultDto> defaultsPorTipo)
        {
            TipoCargoDefaultDto? tipoDefault = cargo.IdTipoCargo.HasValue && defaultsPorTipo.TryGetValue(cargo.IdTipoCargo.Value, out var d) ? d : null;

            var prodServBox = new AutoSuggestBox { PlaceholderText = "Buscar clave de producto/servicio SAT" };
            prodServBox.TextChanged += async (s, e) => await BuscarSugerenciasAsync(prodServBox, esProdServ: true, e);
            prodServBox.SuggestionChosen += (s, e) =>
            {
                if (e.SelectedItem is SatClaveDto clave) prodServBox.Text = clave.ToString();
            };
            if (!string.IsNullOrWhiteSpace(tipoDefault?.ClaveProdServDefault))
                prodServBox.Text = tipoDefault!.ClaveProdServDefault!;

            var btnAgregarProdServ = new Button { Content = "+", Padding = new Thickness(10, 0, 10, 0) };
            ToolTipService.SetToolTip(btnAgregarProdServ, "Agregar una clave nueva al catálogo");
            btnAgregarProdServ.Click += async (s, e) => await AgregarClaveDialogAsync(esProdServ: true, prodServBox);

            var unidadBox = new AutoSuggestBox { PlaceholderText = "Buscar unidad SAT" };
            unidadBox.TextChanged += async (s, e) => await BuscarSugerenciasAsync(unidadBox, esProdServ: false, e);
            unidadBox.SuggestionChosen += (s, e) =>
            {
                if (e.SelectedItem is SatClaveDto clave) unidadBox.Text = clave.ToString();
            };
            unidadBox.Text = !string.IsNullOrWhiteSpace(tipoDefault?.ClaveUnidadDefault) ? tipoDefault!.ClaveUnidadDefault! : "E48";

            var btnAgregarUnidad = new Button { Content = "+", Padding = new Thickness(10, 0, 10, 0) };
            ToolTipService.SetToolTip(btnAgregarUnidad, "Agregar una clave nueva al catálogo");
            btnAgregarUnidad.Click += async (s, e) => await AgregarClaveDialogAsync(esProdServ: false, unidadBox);

            var tasaIvaCombo = new ComboBox { HorizontalAlignment = HorizontalAlignment.Stretch };
            tasaIvaCombo.Items.Add(new ComboBoxItem { Content = "16% IVA", Tag = "0.16" });
            tasaIvaCombo.Items.Add(new ComboBoxItem { Content = "8% IVA (franja fronteriza)", Tag = "0.08" });
            tasaIvaCombo.Items.Add(new ComboBoxItem { Content = "0% IVA (tasa cero)", Tag = "0.00" });
            tasaIvaCombo.Items.Add(new ComboBoxItem { Content = "Exento / sin impuesto", Tag = "" });
            tasaIvaCombo.SelectedIndex = 0;
            if (tipoDefault?.TasaIvaDefault.HasValue == true)
            {
                var tasaTexto = tipoDefault.TasaIvaDefault.Value.ToString("0.00");
                var coincidencia = tasaIvaCombo.Items.OfType<ComboBoxItem>().FirstOrDefault(i => (string)i.Tag == tasaTexto);
                if (coincidencia != null) tasaIvaCombo.SelectedItem = coincidencia;
            }
            tasaIvaCombo.SelectionChanged += (s, e) => ActualizarTotales();

            var rowControls = new ConceptoRowControls
            {
                Cargo = cargo,
                ProdServBox = prodServBox,
                UnidadBox = unidadBox,
                TasaIvaCombo = tasaIvaCombo
            };
            _conceptoRows.Add(rowControls);

            var btnRecordar = new Button { Content = $"Recordar para \"{cargo.TipoCargo}\"" };
            btnRecordar.Click += async (s, e) => await RecordarDefaultAsync(rowControls, btnRecordar);

            var grid = new Grid { RowSpacing = 8 };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var encabezado = new Grid();
            encabezado.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            encabezado.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var txtDescripcion = new TextBlock
            {
                Text = $"{cargo.TipoCargo}{(string.IsNullOrWhiteSpace(cargo.Nota) ? "" : $" · {cargo.Nota}")}",
                FontWeight = FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetColumn(txtDescripcion, 0);
            var txtMonto = new TextBlock
            {
                Text = cargo.MontoFormateado,
                FontWeight = FontWeights.SemiBold,
                Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["AccentTextFillColorPrimaryBrush"]
            };
            Grid.SetColumn(txtMonto, 1);
            encabezado.Children.Add(txtDescripcion);
            encabezado.Children.Add(txtMonto);
            Grid.SetRow(encabezado, 0);

            var camposGrid = new Grid { ColumnSpacing = 12 };
            camposGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
            camposGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            camposGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var prodServStack = new StackPanel();
            prodServStack.Children.Add(new TextBlock { Text = "Clave de producto/servicio SAT:", Margin = new Thickness(0, 0, 0, 4) });
            var prodServRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
            prodServBox.Width = 260;
            prodServRow.Children.Add(prodServBox);
            prodServRow.Children.Add(btnAgregarProdServ);
            prodServStack.Children.Add(prodServRow);
            Grid.SetColumn(prodServStack, 0);

            var unidadStack = new StackPanel();
            unidadStack.Children.Add(new TextBlock { Text = "Clave de unidad SAT:", Margin = new Thickness(0, 0, 0, 4) });
            var unidadRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
            unidadBox.Width = 140;
            unidadRow.Children.Add(unidadBox);
            unidadRow.Children.Add(btnAgregarUnidad);
            unidadStack.Children.Add(unidadRow);
            Grid.SetColumn(unidadStack, 1);

            var tasaIvaStack = new StackPanel();
            tasaIvaStack.Children.Add(new TextBlock { Text = "IVA:", Margin = new Thickness(0, 0, 0, 4) });
            tasaIvaStack.Children.Add(tasaIvaCombo);
            Grid.SetColumn(tasaIvaStack, 2);

            camposGrid.Children.Add(prodServStack);
            camposGrid.Children.Add(unidadStack);
            camposGrid.Children.Add(tasaIvaStack);
            Grid.SetRow(camposGrid, 1);

            var piePanel = new StackPanel { HorizontalAlignment = HorizontalAlignment.Right };
            piePanel.Children.Add(btnRecordar);
            Grid.SetRow(piePanel, 2);

            grid.Children.Add(encabezado);
            grid.Children.Add(camposGrid);
            grid.Children.Add(piePanel);

            return new Border
            {
                Padding = new Thickness(12),
                Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["LayerFillColorDefaultBrush"],
                BorderBrush = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Child = grid
            };
        }

        private async Task BuscarSugerenciasAsync(AutoSuggestBox box, bool esProdServ, AutoSuggestBoxTextChangedEventArgs e)
        {
            if (e.Reason != AutoSuggestionBoxTextChangeReason.UserInput) return;

            var texto = box.Text;
            var resultados = esProdServ
                ? await _satCatalogoService.BuscarClaveProdServAsync(texto)
                : await _satCatalogoService.BuscarClaveUnidadAsync(texto);

            box.ItemsSource = resultados;
        }

        private async Task AgregarClaveDialogAsync(bool esProdServ, AutoSuggestBox boxDestino)
        {
            var claveTextBox = new TextBox { PlaceholderText = esProdServ ? "Clave (8 dígitos)" : "Clave (3 caracteres)", MaxLength = esProdServ ? 8 : 3 };
            var descripcionTextBox = new TextBox { PlaceholderText = "Descripción" };

            var dialog = new ContentDialog
            {
                Title = esProdServ ? "Agregar clave de producto/servicio" : "Agregar clave de unidad",
                Content = new StackPanel
                {
                    Spacing = 8,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = "Verifica la clave contra el catálogo oficial del SAT (c_ClaveProdServ / c_ClaveUnidad) antes de agregarla.",
                            TextWrapping = TextWrapping.Wrap,
                            Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
                        },
                        new TextBlock { Text = "Clave:" },
                        claveTextBox,
                        new TextBlock { Text = "Descripción:" },
                        descripcionTextBox
                    }
                },
                PrimaryButtonText = "Agregar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.Content.XamlRoot
            };

            if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;

            if (string.IsNullOrWhiteSpace(claveTextBox.Text) || string.IsNullOrWhiteSpace(descripcionTextBox.Text))
            {
                await MostrarMensajeAsync("La clave y la descripción son obligatorias.");
                return;
            }

            var nueva = esProdServ
                ? await _satCatalogoService.AgregarClaveProdServAsync(claveTextBox.Text, descripcionTextBox.Text)
                : await _satCatalogoService.AgregarClaveUnidadAsync(claveTextBox.Text, descripcionTextBox.Text);

            if (nueva == null)
            {
                await MostrarMensajeAsync("No se pudo agregar la clave. Verifica el formato e intenta de nuevo.");
                return;
            }

            boxDestino.Text = nueva.ToString();
        }

        private async Task RecordarDefaultAsync(ConceptoRowControls fila, Button boton)
        {
            if (fila.Cargo.IdTipoCargo == null) return;

            boton.IsEnabled = false;
            try
            {
                var claveProdServ = ExtraerClave(fila.ProdServBox.Text);
                var claveUnidad = ExtraerClave(fila.UnidadBox.Text);
                var tasaTag = (fila.TasaIvaCombo.SelectedItem as ComboBoxItem)?.Tag as string;
                decimal? tasaIva = decimal.TryParse(tasaTag, out var t) ? t : null;

                var ok = await _tipoCargoService.GuardarDefaultAsync(fila.Cargo.IdTipoCargo.Value, claveProdServ, claveUnidad, tasaIva);
                boton.Content = ok ? "✅ Recordado" : "Error al recordar";
            }
            finally
            {
                boton.IsEnabled = true;
            }
        }

        /// <summary>El texto del AutoSuggestBox puede ser "clave - descripción" (elegido de la lista) o una clave escrita a mano.</summary>
        private static string ExtraerClave(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return string.Empty;
            var separadorIndex = texto.IndexOf(" - ", StringComparison.Ordinal);
            return (separadorIndex > 0 ? texto[..separadorIndex] : texto).Trim();
        }

        /// <summary>La parte descriptiva de "clave - descripción", o vacío si el usuario solo escribió la clave.</summary>
        private static string ExtraerDescripcion(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return string.Empty;
            var separadorIndex = texto.IndexOf(" - ", StringComparison.Ordinal);
            return separadorIndex > 0 ? texto[(separadorIndex + 3)..].Trim() : string.Empty;
        }

        private List<CfdiConceptoTimbrarDto> ObtenerConceptos()
        {
            var conceptos = new List<CfdiConceptoTimbrarDto>();
            foreach (var fila in _conceptoRows)
            {
                var cantidad = fila.Cargo.Cantidad.HasValue && fila.Cargo.Cantidad.Value > 0
                    ? (decimal)fila.Cargo.Cantidad.Value
                    : 1m;
                var valorUnitario = fila.Cargo.Unitario.HasValue && fila.Cargo.Unitario.Value > 0
                    ? (decimal)fila.Cargo.Unitario.Value
                    : cantidad != 0 ? (decimal)(fila.Cargo.Monto ?? 0) / cantidad : 0m;

                var tasaTag = (fila.TasaIvaCombo.SelectedItem as ComboBoxItem)?.Tag as string;
                decimal? tasaIva = decimal.TryParse(tasaTag, out var t) ? t : null;

                var claveUnidad = ExtraerClave(fila.UnidadBox.Text);
                var descripcionUnidad = ExtraerDescripcion(fila.UnidadBox.Text);

                conceptos.Add(new CfdiConceptoTimbrarDto
                {
                    ClaveProdServ = ExtraerClave(fila.ProdServBox.Text),
                    ClaveUnidad = claveUnidad,
                    Unidad = string.IsNullOrWhiteSpace(descripcionUnidad) ? claveUnidad : descripcionUnidad,
                    Descripcion = $"{fila.Cargo.TipoCargo}{(string.IsNullOrWhiteSpace(fila.Cargo.Nota) ? "" : $" - {fila.Cargo.Nota}")}",
                    Cantidad = cantidad,
                    ValorUnitario = valorUnitario,
                    TasaIva = tasaIva
                });
            }
            return conceptos;
        }

        /// <summary>Snapshot legible de cada renglón de Conceptos para el panel de depuración -- muestra
        /// el texto crudo de cada AutoSuggestBox junto a la clave que realmente se extraería de él.</summary>
        private object ObtenerSnapshotConceptosParaDebug() =>
            _conceptoRows.Select(f => new
            {
                f.Cargo.TipoCargo,
                prodServTexto = f.ProdServBox.Text,
                prodServClaveExtraida = ExtraerClave(f.ProdServBox.Text),
                unidadTexto = f.UnidadBox.Text,
                unidadClaveExtraida = ExtraerClave(f.UnidadBox.Text)
            }).ToList();

        private void ActualizarTotales()
        {
            var conceptos = ObtenerConceptos();
            var subtotal = conceptos.Sum(c => c.Cantidad * c.ValorUnitario);
            var iva = conceptos.Sum(c => c.Cantidad * c.ValorUnitario * (c.TasaIva ?? 0m));
            var total = subtotal + iva;

            var culturaMx = new System.Globalization.CultureInfo("es-MX");
            TxtSubtotal.Text = subtotal.ToString("C2", culturaMx);
            TxtIva.Text = iva.ToString("C2", culturaMx);
            TxtTotalGeneral.Text = total.ToString("C2", culturaMx);
        }

        private async void BtnTimbrar_Click(object sender, RoutedEventArgs e)
        {
            var errores = ValidarAntesDeTimbrar();
            if (errores.Count > 0)
            {
                RegistrarDebug("ValidarAntesDeTimbrar -> bloqueado", new { errores, conceptos = ObtenerSnapshotConceptosParaDebug() });
                EmisionInfoBar.Severity = Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error;
                EmisionInfoBar.Message = "No se puede timbrar: " + string.Join(" · ", errores);
                EmisionInfoBar.IsOpen = true;
                return;
            }

            BtnTimbrar.IsEnabled = false;
            EmisionProgressRing.IsActive = true;
            EmisionInfoBar.IsOpen = false;
            try
            {
                var metodoPago = (CmbMetodoPago.SelectedItem as ComboBoxItem)?.Tag as string ?? string.Empty;
                var formaPago = (CmbFormaPago.SelectedItem as ComboBoxItem)?.Tag as string ?? string.Empty;

                var request = new CfdiTimbrarRequestDto
                {
                    Serie = string.IsNullOrWhiteSpace(TxtSerie.Text) ? null : TxtSerie.Text.Trim(),
                    Folio = string.IsNullOrWhiteSpace(TxtFolio.Text) ? null : TxtFolio.Text.Trim(),
                    ReceptorRfc = TxtReceptorRfc.Text.Trim(),
                    ReceptorRazonSocial = TxtReceptorRazonSocial.Text.Trim(),
                    ReceptorCodigoPostal = TxtReceptorCodigoPostal.Text.Trim(),
                    ReceptorRegimenFiscal = ClaveSeleccionada(CmbReceptorRegimenFiscal),
                    ReceptorUsoCfdi = ClaveSeleccionada(CmbReceptorUsoCfdi),
                    FormaPago = formaPago,
                    MetodoPago = metodoPago,
                    CondicionesDePago = string.IsNullOrWhiteSpace(TxtCondicionesDePago.Text) ? null : TxtCondicionesDePago.Text.Trim(),
                    Moneda = "MXN",
                    Conceptos = ObtenerConceptos()
                };

                RegistrarDebug($"FacturaService.TimbrarOperacionAsync(idOperacion={Operacion.IdOperacion}) -> request", request);
                var resultado = await _facturaService.TimbrarOperacionAsync(Operacion.IdOperacion, request);
                RegistrarDebug("FacturaService.TimbrarOperacionAsync -> resultado", resultado);

                if (resultado.Success)
                {
                    EmisionInfoBar.Severity = Microsoft.UI.Xaml.Controls.InfoBarSeverity.Success;
                    EmisionInfoBar.Message = $"CFDI timbrado correctamente. {resultado.Message}".Trim();
                    EmisionInfoBar.IsOpen = true;
                    BtnTimbrar.Content = "Timbrado";
                }
                else
                {
                    EmisionInfoBar.Severity = Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error;
                    var detalle = string.IsNullOrWhiteSpace(resultado.Observacion) ? "" : $" — {resultado.Observacion}";
                    EmisionInfoBar.Message = $"{resultado.Message}{detalle}";
                    EmisionInfoBar.IsOpen = true;
                    BtnTimbrar.IsEnabled = true;
                }
            }
            finally
            {
                EmisionProgressRing.IsActive = false;
            }
        }

        private List<string> ValidarAntesDeTimbrar()
        {
            var errores = new List<string>();

            if (!_emisorListo)
                errores.Add("el emisor/CSD no está listo");

            if (string.IsNullOrWhiteSpace(TxtReceptorRfc.Text) || string.IsNullOrWhiteSpace(TxtReceptorRazonSocial.Text) ||
                string.IsNullOrWhiteSpace(TxtReceptorCodigoPostal.Text) || CmbReceptorRegimenFiscal.SelectedItem == null ||
                CmbReceptorUsoCfdi.SelectedItem == null)
                errores.Add("faltan datos del receptor");

            if (CmbFormaPago.SelectedItem == null || CmbMetodoPago.SelectedItem == null)
                errores.Add("falta forma/método de pago");

            if (_conceptoRows.Count == 0)
                errores.Add("no hay conceptos");
            else if (_conceptoRows.Any(f => string.IsNullOrWhiteSpace(ExtraerClave(f.ProdServBox.Text)) || string.IsNullOrWhiteSpace(ExtraerClave(f.UnidadBox.Text))))
                errores.Add("faltan claves SAT en uno o más conceptos");

            return errores;
        }

        /// <summary>Agrega una entrada al panel de depuración con la respuesta cruda (serializada) del API.</summary>
        private void RegistrarDebug(string etiqueta, object? datos = null)
        {
            if (!MostrarPanelDepuracion) return;

            _debugLog.Append('[').Append(DateTime.Now.ToString("HH:mm:ss")).Append("] ").AppendLine(etiqueta);
            if (datos != null)
            {
                try
                {
                    _debugLog.AppendLine(JsonSerializer.Serialize(datos, DebugJsonOptions));
                }
                catch (Exception ex)
                {
                    _debugLog.AppendLine($"(no se pudo serializar: {ex.Message})");
                }
            }
            _debugLog.AppendLine();

            TxtDebug.Text = _debugLog.ToString();
            ScrollDebug.UpdateLayout();
            ScrollDebug.ChangeView(null, ScrollDebug.ScrollableHeight, null);
        }

        private void BtnLimpiarDebug_Click(object sender, RoutedEventArgs e)
        {
            _debugLog.Clear();
            TxtDebug.Text = "Sin actividad todavía.";
        }

        private async void BtnCopiarDebug_Click(object sender, RoutedEventArgs e)
        {
            var paquete = new DataPackage();
            paquete.SetText(_debugLog.Length > 0 ? _debugLog.ToString() : "Sin actividad todavía.");
            Clipboard.SetContent(paquete);
            await MostrarMensajeAsync("Copiado al portapapeles.");
        }

        private async Task MostrarMensajeAsync(string mensaje)
        {
            var dialog = new ContentDialog
            {
                Title = "Aviso",
                Content = mensaje,
                CloseButtonText = "Aceptar",
                XamlRoot = this.Content.XamlRoot
            };
            await dialog.ShowAsync();
        }

        private async void BtnGuardarReceptor_Click(object sender, RoutedEventArgs e)
        {
            BtnGuardarReceptor.IsEnabled = false;
            TxtReceptorGuardadoStatus.Text = "Guardando...";
            try
            {
                var dto = new ClienteEditDto
                {
                    Rfc = TxtReceptorRfc.Text?.Trim(),
                    RazonSocial = TxtReceptorRazonSocial.Text?.Trim(),
                    NombreComercial = _clienteReceptor?.NombreComercial,
                    CodigoPostal = TxtReceptorCodigoPostal.Text?.Trim(),
                    RegimenFiscal = ClaveSeleccionada(CmbReceptorRegimenFiscal),
                    UsoCfdi = ClaveSeleccionada(CmbReceptorUsoCfdi),
                    Estatus = true
                };

                ClienteOperationResponse resultado;
                if (_clienteReceptor != null)
                {
                    dto.IdCliente = _clienteReceptor.IdCliente;
                    resultado = await _clienteService.UpdateClienteAsync(dto);
                }
                else
                {
                    resultado = await _clienteService.CreateClienteAsync(dto);
                }

                if (resultado.Success)
                {
                    TxtReceptorGuardadoStatus.Text = "Datos guardados";
                    await CargarReceptorAsync();
                }
                else
                {
                    TxtReceptorGuardadoStatus.Text = string.Empty;
                    MostrarAvisoReceptor($"No se pudieron guardar los datos: {resultado.Message}", esAdvertencia: true);
                }
            }
            finally
            {
                BtnGuardarReceptor.IsEnabled = true;
            }
        }
    }
}
