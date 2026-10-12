using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.Clientes;
using Advance_Control.Services.ConfiguracionEmisor;
using Advance_Control.Services.Facturas;
using Advance_Control.Services.SatCatalogo;
using Advance_Control.Utilities;
using Microsoft.UI.Text;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Graphics;
using NumberFormatting = Windows.Globalization.NumberFormatting;

namespace Advance_Control.Views.Windows
{
    /// <summary>
    /// Panel para timbrar una factura desde cero (POST api/factura/timbrar-libre): el receptor
    /// se elige de los clientes registrados (sus datos fiscales no se capturan aquí, se toman de
    /// la ficha del cliente) y los conceptos se capturan libremente. A diferencia de
    /// FacturarDirectoWindow, no hay operación ni cargos de origen.
    /// </summary>
    public sealed partial class NuevaFacturaWindow : Window
    {
        private readonly IConfiguracionEmisorService _configuracionEmisorService;
        private readonly IClienteService _clienteService;
        private readonly ISatCatalogoService _satCatalogoService;
        private readonly IFacturaService _facturaService;

        private static readonly CultureInfo CulturaMx = new("es-MX");

        private List<CustomerDto> _clientes = new();
        private List<SatCatalogoItemDto> _regimenes = new();
        private CustomerDto? _clienteSeleccionado;

        private readonly List<ConceptoRowControls> _conceptoRows = new();

        /// <summary>True si el emisor está configurado y su CSD está vigente -- se puede timbrar.</summary>
        private bool _emisorListo;

        /// <summary>True una vez timbrada: la ventana queda de solo lectura para no timbrar dos veces.</summary>
        private bool _timbrada;

        /// <summary>Se dispara después de timbrar con éxito, para que la lista de facturas se recargue.</summary>
        public event EventHandler<TimbrarResultadoDto>? FacturaTimbrada;

        private sealed class ConceptoRowControls
        {
            public required UIElement Contenedor { get; init; }
            public required TextBox DescripcionBox { get; init; }
            public required NumberBox CantidadBox { get; init; }
            public required NumberBox ValorUnitarioBox { get; init; }
            public required AutoSuggestBox ProdServBox { get; init; }
            public required AutoSuggestBox UnidadBox { get; init; }
            public required ComboBox TasaIvaCombo { get; init; }
            public required TextBlock ImporteText { get; init; }
        }

        public NuevaFacturaWindow()
        {
            _configuracionEmisorService = AppServices.Get<IConfiguracionEmisorService>();
            _clienteService = AppServices.Get<IClienteService>();
            _satCatalogoService = AppServices.Get<ISatCatalogoService>();
            _facturaService = AppServices.Get<IFacturaService>();

            InitializeComponent();
            AjustarTamano(980, 820);

            Title = "Nueva factura";

            CmbMetodoPago.SelectedIndex = 0; // PUE
            CmbFormaPago.SelectedIndex = 2;  // 03 - Transferencia

            AgregarFilaConcepto();
            ActualizarTotales();

            Activated += NuevaFacturaWindow_Activated;
        }

        private async void NuevaFacturaWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            Activated -= NuevaFacturaWindow_Activated;
            await CargarEmisorAsync();
            await CargarCatalogosAsync();
            await CargarClientesAsync();
        }

        private void AjustarTamano(int ancho, int alto)
        {
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);
            appWindow.Resize(new SizeInt32(ancho, alto));
        }

        // --- Emisor ---

        private async Task CargarEmisorAsync()
        {
            EmisorProgressRing.IsActive = true;
            try
            {
                var configuracion = await _configuracionEmisorService.ObtenerConfiguracionAsync();
                if (configuracion == null)
                {
                    EmisorInfoBar.Severity = InfoBarSeverity.Error;
                    EmisorInfoBar.Message = "No hay datos del emisor configurados. Configura el emisor y el CSD antes de timbrar.";
                    EmisorInfoBar.IsOpen = true;
                    _emisorListo = false;
                    return;
                }

                TxtEmisorResumen.Text = $"{configuracion.RazonSocial} ({configuracion.Rfc}) — Régimen {configuracion.RegimenFiscal} — Lugar de expedición {configuracion.LugarExpedicion}";

                var estadoCsd = await _configuracionEmisorService.ObtenerEstadoCsdAsync();
                if (!estadoCsd.Vigente)
                {
                    EmisorInfoBar.Severity = InfoBarSeverity.Warning;
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

        private async Task CargarCatalogosAsync()
        {
            _regimenes = await _satCatalogoService.ListarRegimenFiscalAsync();
            CmbReceptorUsoCfdi.ItemsSource = await _satCatalogoService.ListarUsoCfdiAsync();
        }

        // --- Receptor: solo clientes registrados ---

        private async Task CargarClientesAsync()
        {
            ClientesProgressRing.IsActive = true;
            try
            {
                var clientes = await _clienteService.GetClientesAsync();
                _clientes = clientes
                    .Where(c => c.Estatus && !string.IsNullOrWhiteSpace(c.Rfc))
                    .OrderBy(c => c.RazonSocial)
                    .ToList();

                if (_clientes.Count == 0)
                {
                    MostrarAvisoReceptor("No hay clientes activos registrados. Da de alta al cliente en el módulo Clientes antes de facturar.", InfoBarSeverity.Warning);
                }
            }
            catch (Exception ex)
            {
                MostrarAvisoReceptor($"No se pudieron cargar los clientes: {ex.Message}", InfoBarSeverity.Error);
            }
            finally
            {
                ClientesProgressRing.IsActive = false;
            }
        }

        private static string TextoCliente(CustomerDto c)
        {
            var nombreComercial = string.IsNullOrWhiteSpace(c.NombreComercial) || string.Equals(c.NombreComercial, c.RazonSocial, StringComparison.OrdinalIgnoreCase)
                ? string.Empty
                : $" ({c.NombreComercial})";
            return $"{c.RazonSocial}{nombreComercial} — {c.Rfc}";
        }

        private void ClienteAutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput) return;

            var texto = sender.Text?.Trim() ?? string.Empty;
            var coincidencias = string.IsNullOrEmpty(texto)
                ? _clientes
                : _clientes.Where(c =>
                    (c.RazonSocial?.Contains(texto, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (c.NombreComercial?.Contains(texto, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (c.Rfc?.Contains(texto, StringComparison.OrdinalIgnoreCase) ?? false)).ToList();

            sender.ItemsSource = coincidencias.Take(30).Select(c => new ClienteSugerencia(c)).ToList();
        }

        private void ClienteAutoSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            if (args.SelectedItem is ClienteSugerencia sugerencia)
            {
                sender.Text = sugerencia.ToString();
            }
        }

        private void ClienteAutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (args.ChosenSuggestion is ClienteSugerencia sugerencia)
            {
                SeleccionarCliente(sugerencia.Cliente);
                return;
            }

            // Enter sin elegir de la lista: solo se acepta si hay exactamente una coincidencia.
            var texto = args.QueryText?.Trim() ?? string.Empty;
            var coincidencias = _clientes.Where(c =>
                string.Equals(c.Rfc, texto, StringComparison.OrdinalIgnoreCase) ||
                (c.RazonSocial?.Contains(texto, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (c.NombreComercial?.Contains(texto, StringComparison.OrdinalIgnoreCase) ?? false)).ToList();

            if (coincidencias.Count == 1)
            {
                sender.Text = TextoCliente(coincidencias[0]);
                SeleccionarCliente(coincidencias[0]);
            }
            else
            {
                MostrarAvisoReceptor(coincidencias.Count == 0
                    ? "No hay ningún cliente registrado que coincida. Solo se puede facturar a clientes dados de alta."
                    : "Hay varios clientes que coinciden; elige uno de la lista.", InfoBarSeverity.Informational);
            }
        }

        private void SeleccionarCliente(CustomerDto cliente)
        {
            _clienteSeleccionado = cliente;

            TxtReceptorRfc.Text = cliente.Rfc;
            TxtReceptorRazonSocial.Text = cliente.RazonSocial;
            TxtReceptorCodigoPostal.Text = string.IsNullOrWhiteSpace(cliente.CodigoPostal) ? "—" : cliente.CodigoPostal;

            var regimen = _regimenes.FirstOrDefault(r => string.Equals(r.Clave, cliente.RegimenFiscal, StringComparison.OrdinalIgnoreCase));
            TxtReceptorRegimenFiscal.Text = regimen?.ToString() ?? (string.IsNullOrWhiteSpace(cliente.RegimenFiscal) ? "—" : cliente.RegimenFiscal);

            CmbReceptorUsoCfdi.SelectedItem = CmbReceptorUsoCfdi.Items
                .OfType<SatCatalogoItemDto>()
                .FirstOrDefault(i => string.Equals(i.Clave, cliente.UsoCfdi, StringComparison.OrdinalIgnoreCase));

            ReceptorDatosGrid.Visibility = Visibility.Visible;
            TxtSubtitulo.Text = cliente.RazonSocial;

            if (cliente.DiasCredito is > 0 && string.IsNullOrWhiteSpace(TxtCondicionesDePago.Text))
            {
                TxtCondicionesDePago.Text = $"{cliente.DiasCredito} días";
            }

            var faltantes = FaltantesFiscalesCliente(cliente);
            if (faltantes.Count > 0)
            {
                MostrarAvisoReceptor($"Al cliente le falta: {string.Join(", ", faltantes)}. Complétalo en la ficha del cliente (módulo Clientes) antes de timbrar.", InfoBarSeverity.Warning);
            }
            else
            {
                ReceptorInfoBar.IsOpen = false;
            }
        }

        private static List<string> FaltantesFiscalesCliente(CustomerDto cliente)
        {
            var faltantes = new List<string>();
            if (string.IsNullOrWhiteSpace(cliente.RazonSocial)) faltantes.Add("razón social");
            if (string.IsNullOrWhiteSpace(cliente.CodigoPostal)) faltantes.Add("código postal fiscal");
            if (string.IsNullOrWhiteSpace(cliente.RegimenFiscal)) faltantes.Add("régimen fiscal");
            return faltantes;
        }

        private void MostrarAvisoReceptor(string mensaje, InfoBarSeverity severidad)
        {
            ReceptorInfoBar.Severity = severidad;
            ReceptorInfoBar.Message = mensaje;
            ReceptorInfoBar.IsOpen = true;
        }

        /// <summary>Envoltura para que el AutoSuggestBox muestre un texto legible del cliente.</summary>
        private sealed class ClienteSugerencia
        {
            public ClienteSugerencia(CustomerDto cliente) => Cliente = cliente;
            public CustomerDto Cliente { get; }
            public override string ToString() => TextoCliente(Cliente);
        }

        // --- Forma de pago ---

        /// <summary>
        /// CFDI 4.0 exige que cuando MetodoPago = PPD, FormaPago sea "99 - Por definir"
        /// (el detalle real se reporta después vía complemento de pago).
        /// </summary>
        private void CmbMetodoPago_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var metodoPago = (CmbMetodoPago.SelectedItem as ComboBoxItem)?.Tag as string;

            if (metodoPago == "PPD")
            {
                var item99 = CmbFormaPago.Items.OfType<ComboBoxItem>().FirstOrDefault(i => (string)i.Tag == "99");
                if (item99 != null) CmbFormaPago.SelectedItem = item99;
                CmbFormaPago.IsEnabled = false;
            }
            else
            {
                CmbFormaPago.IsEnabled = true;
            }
        }

        // --- Conceptos ---

        private void BtnAgregarConcepto_Click(object sender, RoutedEventArgs e)
        {
            var fila = AgregarFilaConcepto();
            ActualizarTotales();
            fila.DescripcionBox.Focus(FocusState.Programmatic);
        }

        private ConceptoRowControls AgregarFilaConcepto()
        {
            var descripcionBox = new TextBox
            {
                PlaceholderText = "Descripción del concepto (aparece en el CFDI)",
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                MaxLength = 1000
            };

            var cantidadBox = new NumberBox
            {
                Value = 1,
                Minimum = 0,
                SmallChange = 1,
                SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact,
                NumberFormatter = CrearFormateador(0, 6)
            };

            var valorUnitarioBox = new NumberBox
            {
                Value = 0,
                Minimum = 0,
                PlaceholderText = "0.00",
                NumberFormatter = CrearFormateador(2, 6)
            };

            var prodServBox = new AutoSuggestBox { PlaceholderText = "Buscar clave de producto/servicio SAT" };
            prodServBox.TextChanged += async (s, e) => await BuscarSugerenciasAsync(prodServBox, esProdServ: true, e);
            prodServBox.SuggestionChosen += (s, e) =>
            {
                if (e.SelectedItem is SatClaveDto clave) prodServBox.Text = clave.ToString();
            };

            var btnAgregarProdServ = new Button { Content = "+", Padding = new Thickness(10, 0, 10, 0) };
            ToolTipService.SetToolTip(btnAgregarProdServ, "Agregar una clave nueva al catálogo");
            btnAgregarProdServ.Click += async (s, e) => await AgregarClaveDialogAsync(esProdServ: true, prodServBox);

            var unidadBox = new AutoSuggestBox { PlaceholderText = "Buscar unidad SAT", Text = "E48" };
            unidadBox.TextChanged += async (s, e) => await BuscarSugerenciasAsync(unidadBox, esProdServ: false, e);
            unidadBox.SuggestionChosen += (s, e) =>
            {
                if (e.SelectedItem is SatClaveDto clave) unidadBox.Text = clave.ToString();
            };

            var btnAgregarUnidad = new Button { Content = "+", Padding = new Thickness(10, 0, 10, 0) };
            ToolTipService.SetToolTip(btnAgregarUnidad, "Agregar una clave nueva al catálogo");
            btnAgregarUnidad.Click += async (s, e) => await AgregarClaveDialogAsync(esProdServ: false, unidadBox);

            var tasaIvaCombo = new ComboBox { HorizontalAlignment = HorizontalAlignment.Stretch };
            tasaIvaCombo.Items.Add(new ComboBoxItem { Content = "16% IVA", Tag = "0.16" });
            tasaIvaCombo.Items.Add(new ComboBoxItem { Content = "8% IVA (franja fronteriza)", Tag = "0.08" });
            tasaIvaCombo.Items.Add(new ComboBoxItem { Content = "0% IVA (tasa cero)", Tag = "0.00" });
            tasaIvaCombo.Items.Add(new ComboBoxItem { Content = "Exento / sin impuesto", Tag = "" });
            tasaIvaCombo.SelectedIndex = 0;

            var importeText = new TextBlock
            {
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["AccentTextFillColorPrimaryBrush"]
            };

            var btnEliminar = new Button
            {
                Content = new SymbolIcon(Symbol.Delete),
                Padding = new Thickness(8, 4, 8, 4)
            };
            ToolTipService.SetToolTip(btnEliminar, "Quitar este concepto");

            // --- Layout ---
            var grid = new Grid { RowSpacing = 8, ColumnSpacing = 12 };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var descripcionStack = Campo("Descripción:", descripcionBox);
            Grid.SetRow(descripcionStack, 0);
            Grid.SetColumn(descripcionStack, 0);
            Grid.SetColumnSpan(descripcionStack, 3);

            Grid.SetRow(btnEliminar, 0);
            Grid.SetColumn(btnEliminar, 3);
            btnEliminar.VerticalAlignment = VerticalAlignment.Bottom;

            var cantidadStack = Campo("Cantidad:", cantidadBox);
            Grid.SetRow(cantidadStack, 1);
            Grid.SetColumn(cantidadStack, 1);

            var valorStack = Campo("Valor unitario (sin IVA):", valorUnitarioBox);
            Grid.SetRow(valorStack, 1);
            Grid.SetColumn(valorStack, 2);

            var importeStack = Campo("Importe:", importeText);
            Grid.SetRow(importeStack, 1);
            Grid.SetColumn(importeStack, 3);

            var prodServRow = new Grid { ColumnSpacing = 4 };
            prodServRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            prodServRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            prodServRow.Children.Add(prodServBox);
            Grid.SetColumn(btnAgregarProdServ, 1);
            prodServRow.Children.Add(btnAgregarProdServ);
            var prodServStack = Campo("Clave de producto/servicio SAT:", prodServRow);
            Grid.SetRow(prodServStack, 1);
            Grid.SetColumn(prodServStack, 0);

            var unidadRow = new Grid { ColumnSpacing = 4 };
            unidadRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            unidadRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            unidadRow.Children.Add(unidadBox);
            Grid.SetColumn(btnAgregarUnidad, 1);
            unidadRow.Children.Add(btnAgregarUnidad);
            var unidadStack = Campo("Clave de unidad SAT:", unidadRow);
            Grid.SetRow(unidadStack, 2);
            Grid.SetColumn(unidadStack, 0);

            var tasaIvaStack = Campo("IVA:", tasaIvaCombo);
            Grid.SetRow(tasaIvaStack, 2);
            Grid.SetColumn(tasaIvaStack, 1);

            grid.Children.Add(descripcionStack);
            grid.Children.Add(btnEliminar);
            grid.Children.Add(prodServStack);
            grid.Children.Add(cantidadStack);
            grid.Children.Add(valorStack);
            grid.Children.Add(importeStack);
            grid.Children.Add(unidadStack);
            grid.Children.Add(tasaIvaStack);

            var contenedor = new Border
            {
                Padding = new Thickness(12),
                Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["LayerFillColorDefaultBrush"],
                BorderBrush = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Child = grid
            };

            var fila = new ConceptoRowControls
            {
                Contenedor = contenedor,
                DescripcionBox = descripcionBox,
                CantidadBox = cantidadBox,
                ValorUnitarioBox = valorUnitarioBox,
                ProdServBox = prodServBox,
                UnidadBox = unidadBox,
                TasaIvaCombo = tasaIvaCombo,
                ImporteText = importeText
            };

            cantidadBox.ValueChanged += (s, e) => ActualizarTotales();
            valorUnitarioBox.ValueChanged += (s, e) => ActualizarTotales();
            tasaIvaCombo.SelectionChanged += (s, e) => ActualizarTotales();
            btnEliminar.Click += (s, e) => QuitarFilaConcepto(fila);

            _conceptoRows.Add(fila);
            ConceptosListPanel.Children.Add(contenedor);
            return fila;
        }

        private void QuitarFilaConcepto(ConceptoRowControls fila)
        {
            if (_timbrada) return;
            _conceptoRows.Remove(fila);
            ConceptosListPanel.Children.Remove(fila.Contenedor);
            ActualizarTotales();
        }

        private static StackPanel Campo(string etiqueta, FrameworkElement control)
        {
            var stack = new StackPanel();
            stack.Children.Add(new TextBlock { Text = etiqueta, Margin = new Thickness(0, 0, 0, 4) });
            if (control is Control c) c.HorizontalAlignment = HorizontalAlignment.Stretch;
            stack.Children.Add(control);
            return stack;
        }

        private static NumberFormatting.DecimalFormatter CrearFormateador(int decimalesMin, int decimalesMax)
        {
            var redondeo = new NumberFormatting.IncrementNumberRounder
            {
                Increment = Math.Pow(10, -decimalesMax),
                RoundingAlgorithm = NumberFormatting.RoundingAlgorithm.RoundHalfUp
            };
            return new NumberFormatting.DecimalFormatter
            {
                FractionDigits = decimalesMin,
                IntegerDigits = 1,
                IsGrouped = true,
                NumberRounder = redondeo
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
                XamlRoot = Content.XamlRoot
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

        private static decimal ValorDe(NumberBox box) => double.IsNaN(box.Value) ? 0m : (decimal)box.Value;

        private static decimal? TasaDe(ConceptoRowControls fila)
        {
            var tasaTag = (fila.TasaIvaCombo.SelectedItem as ComboBoxItem)?.Tag as string;
            return decimal.TryParse(tasaTag, NumberStyles.Number, CultureInfo.InvariantCulture, out var t) ? t : null;
        }

        private List<CfdiConceptoTimbrarDto> ObtenerConceptos()
        {
            var conceptos = new List<CfdiConceptoTimbrarDto>();
            foreach (var fila in _conceptoRows)
            {
                var claveUnidad = ExtraerClave(fila.UnidadBox.Text);
                var descripcionUnidad = ExtraerDescripcion(fila.UnidadBox.Text);

                conceptos.Add(new CfdiConceptoTimbrarDto
                {
                    ClaveProdServ = ExtraerClave(fila.ProdServBox.Text),
                    ClaveUnidad = claveUnidad,
                    Unidad = string.IsNullOrWhiteSpace(descripcionUnidad) ? claveUnidad : descripcionUnidad,
                    Descripcion = fila.DescripcionBox.Text.Trim(),
                    Cantidad = ValorDe(fila.CantidadBox),
                    ValorUnitario = Math.Round(ValorDe(fila.ValorUnitarioBox), 6, MidpointRounding.AwayFromZero),
                    TasaIva = TasaDe(fila)
                });
            }
            return conceptos;
        }

        /// <summary>Mismo redondeo que CfdiBuilderService en la API (importe e IVA a 2 decimales por concepto).</summary>
        private void ActualizarTotales()
        {
            decimal subtotal = 0m, iva = 0m;
            foreach (var fila in _conceptoRows)
            {
                var importe = Math.Round(ValorDe(fila.CantidadBox) * ValorDe(fila.ValorUnitarioBox), 2, MidpointRounding.AwayFromZero);
                fila.ImporteText.Text = importe.ToString("C2", CulturaMx);
                subtotal += importe;

                var tasa = TasaDe(fila);
                if (tasa.HasValue)
                    iva += Math.Round(importe * tasa.Value, 2, MidpointRounding.AwayFromZero);
            }

            var total = subtotal + iva;
            TxtSubtotal.Text = subtotal.ToString("C2", CulturaMx);
            TxtIva.Text = iva.ToString("C2", CulturaMx);
            TxtTotalGeneral.Text = total.ToString("C2", CulturaMx);
            TxtMonto.Text = total.ToString("C2", CulturaMx);
        }

        // --- Emisión ---

        private List<string> ValidarAntesDeTimbrar()
        {
            var errores = new List<string>();

            if (!_emisorListo)
                errores.Add("el emisor/CSD no está listo");

            if (_clienteSeleccionado == null)
                errores.Add("elige un cliente registrado");
            else
            {
                var faltantes = FaltantesFiscalesCliente(_clienteSeleccionado);
                if (faltantes.Count > 0)
                    errores.Add($"al cliente le falta {string.Join(", ", faltantes)}");
                if (CmbReceptorUsoCfdi.SelectedItem == null)
                    errores.Add("falta el uso de CFDI");
            }

            if (CmbFormaPago.SelectedItem == null || CmbMetodoPago.SelectedItem == null)
                errores.Add("falta forma/método de pago");

            if (_conceptoRows.Count == 0)
            {
                errores.Add("agrega al menos un concepto");
            }
            else
            {
                if (_conceptoRows.Any(f => string.IsNullOrWhiteSpace(f.DescripcionBox.Text)))
                    errores.Add("hay conceptos sin descripción");
                if (_conceptoRows.Any(f => ValorDe(f.CantidadBox) <= 0))
                    errores.Add("la cantidad de cada concepto debe ser mayor a 0");
                if (_conceptoRows.Any(f => ValorDe(f.ValorUnitarioBox) <= 0))
                    errores.Add("el valor unitario de cada concepto debe ser mayor a 0");
                if (_conceptoRows.Any(f => string.IsNullOrWhiteSpace(ExtraerClave(f.ProdServBox.Text)) || string.IsNullOrWhiteSpace(ExtraerClave(f.UnidadBox.Text))))
                    errores.Add("faltan claves SAT en uno o más conceptos");
            }

            return errores;
        }

        private async void BtnTimbrar_Click(object sender, RoutedEventArgs e)
        {
            if (_timbrada) return;

            var errores = ValidarAntesDeTimbrar();
            if (errores.Count > 0)
            {
                EmisionInfoBar.Severity = InfoBarSeverity.Error;
                EmisionInfoBar.Message = "No se puede timbrar: " + string.Join(" · ", errores);
                EmisionInfoBar.IsOpen = true;
                return;
            }

            var cliente = _clienteSeleccionado!;
            var confirmacion = new ContentDialog
            {
                Title = "¿Timbrar factura?",
                Content = $"Se emitirá un CFDI a {cliente.RazonSocial} ({cliente.Rfc}) por {TxtTotalGeneral.Text} con {_conceptoRows.Count} concepto(s).\n\n" +
                          "Timbrar consume un timbre y el comprobante queda registrado ante el SAT; para revertirlo habría que cancelarlo.",
                PrimaryButtonText = "Timbrar",
                CloseButtonText = "Revisar",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = Content.XamlRoot
            };
            if (await confirmacion.ShowAsync() != ContentDialogResult.Primary) return;

            BtnTimbrar.IsEnabled = false;
            EmisionProgressRing.IsActive = true;
            EmisionInfoBar.IsOpen = false;
            try
            {
                var request = new CfdiTimbrarRequestDto
                {
                    ReceptorRfc = cliente.Rfc.Trim(),
                    ReceptorRazonSocial = cliente.RazonSocial.Trim(),
                    ReceptorCodigoPostal = cliente.CodigoPostal?.Trim() ?? string.Empty,
                    ReceptorRegimenFiscal = cliente.RegimenFiscal.Trim(),
                    ReceptorUsoCfdi = (CmbReceptorUsoCfdi.SelectedItem as SatCatalogoItemDto)?.Clave ?? string.Empty,
                    FormaPago = (CmbFormaPago.SelectedItem as ComboBoxItem)?.Tag as string ?? string.Empty,
                    MetodoPago = (CmbMetodoPago.SelectedItem as ComboBoxItem)?.Tag as string ?? string.Empty,
                    CondicionesDePago = string.IsNullOrWhiteSpace(TxtCondicionesDePago.Text) ? null : TxtCondicionesDePago.Text.Trim(),
                    Moneda = "MXN",
                    Conceptos = ObtenerConceptos()
                };

                var resultado = await _facturaService.TimbrarLibreAsync(request);

                if (resultado.Success)
                {
                    _timbrada = true;
                    EmisionInfoBar.Severity = InfoBarSeverity.Success;
                    EmisionInfoBar.Message = $"CFDI timbrado correctamente. {resultado.Message}".Trim();
                    EmisionInfoBar.IsOpen = true;
                    BtnTimbrar.Content = "Timbrado";
                    BloquearEdicion();
                    FacturaTimbrada?.Invoke(this, resultado);
                }
                else
                {
                    EmisionInfoBar.Severity = InfoBarSeverity.Error;
                    var detalle = string.IsNullOrWhiteSpace(resultado.Observacion) ? "" : $" — {resultado.Observacion}";
                    EmisionInfoBar.Message = $"{resultado.Message}{detalle}";
                    EmisionInfoBar.IsOpen = true;
                    BtnTimbrar.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                EmisionInfoBar.Severity = InfoBarSeverity.Error;
                EmisionInfoBar.Message = $"Error al timbrar: {ex.Message}";
                EmisionInfoBar.IsOpen = true;
                BtnTimbrar.IsEnabled = true;
            }
            finally
            {
                EmisionProgressRing.IsActive = false;
            }
        }

        private void BloquearEdicion()
        {
            ClienteAutoSuggestBox.IsEnabled = false;
            CmbReceptorUsoCfdi.IsEnabled = false;
            CmbMetodoPago.IsEnabled = false;
            CmbFormaPago.IsEnabled = false;
            TxtCondicionesDePago.IsEnabled = false;
            BtnAgregarConcepto.IsEnabled = false;
            ConceptosListPanel.IsHitTestVisible = false;
            ConceptosListPanel.Opacity = 0.7;
        }

        private void BtnCerrar_Click(object sender, RoutedEventArgs e) => Close();

        private async Task MostrarMensajeAsync(string mensaje)
        {
            var dialog = new ContentDialog
            {
                Title = "Aviso",
                Content = mensaje,
                CloseButtonText = "Aceptar",
                XamlRoot = Content.XamlRoot
            };
            await dialog.ShowAsync();
        }
    }
}
