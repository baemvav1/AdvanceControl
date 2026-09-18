using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.ConfiguracionEmisor;
using Advance_Control.Services.Facturas;
using Advance_Control.Services.SatCatalogo;
using Advance_Control.Utilities;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Graphics;

namespace Advance_Control.Views.Windows
{
    /// <summary>
    /// Ventana para timbrar la factura de la iguala mensual de un contrato de suscripción
    /// (POST api/factura/contrato-suscripcion/{idContrato}/timbrar-iguala). Un solo concepto
    /// (la iguala), a diferencia de FacturarDirectoWindow que maneja múltiples cargos.
    /// </summary>
    public sealed partial class TimbrarIgualaWindow : Window
    {
        private readonly IConfiguracionEmisorService _configuracionEmisorService;
        private readonly ISatCatalogoService _satCatalogoService;
        private readonly IFacturaService _facturaService;

        private readonly ContratoSuscripcionDto _contrato;
        private readonly CustomerDto _cliente;
        private readonly string _periodo;

        private bool _emisorListo;

        public TimbrarIgualaWindow(ContratoSuscripcionDto contrato, CustomerDto cliente, string periodo)
        {
            _contrato = contrato ?? throw new ArgumentNullException(nameof(contrato));
            _cliente = cliente ?? throw new ArgumentNullException(nameof(cliente));
            _periodo = periodo;

            _configuracionEmisorService = AppServices.Get<IConfiguracionEmisorService>();
            _satCatalogoService = AppServices.Get<ISatCatalogoService>();
            _facturaService = AppServices.Get<IFacturaService>();

            InitializeComponent();
            AjustarTamano(760, 700);

            Title = "Timbrar iguala mensual";
            TxtTitulo.Text = $"Iguala mensual — {_contrato.Nivel} — {_periodo}";

            var periodoTexto = DateTime.TryParseExact(_periodo, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var periodoFecha)
                ? periodoFecha.ToString("MMMM yyyy", new CultureInfo("es-MX"))
                : _periodo;
            TxtDescripcion.Text = $"Iguala mensual de mantenimiento — {_contrato.Nivel} — {periodoTexto}";
            TxtMontoResumen.Text = $"Monto: {_contrato.MontoMensualTexto} + IVA";

            CmbMetodoPago.SelectedIndex = 0; // PUE
            CmbFormaPago.SelectedIndex = 2;  // 03 - Transferencia (forma de pago habitual de la iguala)
            CmbTasaIva.SelectedIndex = 0;    // 16%
            UnidadBox.Text = "E48";

            Activated += TimbrarIgualaWindow_Activated;
        }

        private async void TimbrarIgualaWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            Activated -= TimbrarIgualaWindow_Activated;
            await CargarEmisorAsync();
            CargarReceptor();
        }

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
                if (configuracion == null)
                {
                    EmisorInfoBar.Severity = InfoBarSeverity.Error;
                    EmisorInfoBar.Message = "No hay datos del emisor configurados. Configura el emisor y el CSD antes de timbrar.";
                    EmisorInfoBar.IsOpen = true;
                    _emisorListo = false;
                    return;
                }

                TxtEmisorResumen.Text = $"{configuracion.RazonSocial} ({configuracion.Rfc}) — {configuracion.RegimenFiscal}";

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

        private void CargarReceptor()
        {
            var faltantes = new System.Collections.Generic.List<string>();
            if (string.IsNullOrWhiteSpace(_cliente.CodigoPostal)) faltantes.Add("código postal");
            if (string.IsNullOrWhiteSpace(_cliente.RegimenFiscal)) faltantes.Add("régimen fiscal");
            if (string.IsNullOrWhiteSpace(_cliente.UsoCfdi)) faltantes.Add("uso de CFDI");

            TxtReceptorResumen.Text = $"{_cliente.RazonSocial} ({_cliente.Rfc}) — CP {_cliente.CodigoPostal} — Régimen {_cliente.RegimenFiscal} — Uso CFDI {_cliente.UsoCfdi}";

            if (faltantes.Count > 0)
            {
                ReceptorInfoBar.Severity = InfoBarSeverity.Warning;
                ReceptorInfoBar.Message = $"Al cliente le falta: {string.Join(", ", faltantes)}. Complétalo en la ficha del cliente antes de timbrar.";
                ReceptorInfoBar.IsOpen = true;
            }
        }

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

        private async void ProdServBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput) return;
            sender.ItemsSource = await _satCatalogoService.BuscarClaveProdServAsync(sender.Text);
        }

        private void ProdServBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            if (args.SelectedItem is SatClaveDto clave) sender.Text = clave.ToString();
        }

        private async void UnidadBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput) return;
            sender.ItemsSource = await _satCatalogoService.BuscarClaveUnidadAsync(sender.Text);
        }

        private void UnidadBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            if (args.SelectedItem is SatClaveDto clave) sender.Text = clave.ToString();
        }

        private static string ExtraerClave(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return string.Empty;
            var idx = texto.IndexOf(" - ", StringComparison.Ordinal);
            return (idx > 0 ? texto[..idx] : texto).Trim();
        }

        private static string ExtraerDescripcion(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return string.Empty;
            var idx = texto.IndexOf(" - ", StringComparison.Ordinal);
            return idx > 0 ? texto[(idx + 3)..].Trim() : string.Empty;
        }

        private async void BtnTimbrar_Click(object sender, RoutedEventArgs e)
        {
            var errores = Validar();
            if (errores.Count > 0)
            {
                EmisionInfoBar.Severity = InfoBarSeverity.Error;
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
                var tasaTag = (CmbTasaIva.SelectedItem as ComboBoxItem)?.Tag as string;
                decimal? tasaIva = decimal.TryParse(tasaTag, NumberStyles.Number, CultureInfo.InvariantCulture, out var t) ? t : null;

                var claveUnidad = ExtraerClave(UnidadBox.Text);
                var descripcionUnidad = ExtraerDescripcion(UnidadBox.Text);

                var request = new CfdiTimbrarRequestDto
                {
                    ReceptorRfc = _cliente.Rfc.Trim(),
                    ReceptorRazonSocial = _cliente.RazonSocial.Trim(),
                    ReceptorCodigoPostal = (_cliente.CodigoPostal ?? string.Empty).Trim(),
                    ReceptorRegimenFiscal = _cliente.RegimenFiscal.Trim(),
                    ReceptorUsoCfdi = _cliente.UsoCfdi.Trim(),
                    FormaPago = formaPago,
                    MetodoPago = metodoPago,
                    Moneda = "MXN",
                    Conceptos = new System.Collections.Generic.List<CfdiConceptoTimbrarDto>
                    {
                        new CfdiConceptoTimbrarDto
                        {
                            ClaveProdServ = ExtraerClave(ProdServBox.Text),
                            ClaveUnidad = claveUnidad,
                            Unidad = string.IsNullOrWhiteSpace(descripcionUnidad) ? claveUnidad : descripcionUnidad,
                            Descripcion = TxtDescripcion.Text,
                            Cantidad = 1,
                            ValorUnitario = _contrato.MontoMensual,
                            TasaIva = tasaIva
                        }
                    }
                };

                var resultado = await _facturaService.TimbrarIgualaMensualAsync(_contrato.Id, _periodo, request);

                if (resultado.Success)
                {
                    EmisionInfoBar.Severity = InfoBarSeverity.Success;
                    EmisionInfoBar.Message = $"CFDI de iguala timbrado correctamente. {resultado.Message}".Trim();
                    EmisionInfoBar.IsOpen = true;
                    BtnTimbrar.Content = "Timbrado";
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
            finally
            {
                EmisionProgressRing.IsActive = false;
            }
        }

        private System.Collections.Generic.List<string> Validar()
        {
            var errores = new System.Collections.Generic.List<string>();

            if (!_emisorListo) errores.Add("el emisor/CSD no está listo");

            if (string.IsNullOrWhiteSpace(_cliente.Rfc) || string.IsNullOrWhiteSpace(_cliente.RazonSocial) ||
                string.IsNullOrWhiteSpace(_cliente.CodigoPostal) || string.IsNullOrWhiteSpace(_cliente.RegimenFiscal) ||
                string.IsNullOrWhiteSpace(_cliente.UsoCfdi))
                errores.Add("faltan datos fiscales del cliente");

            if (CmbFormaPago.SelectedItem == null || CmbMetodoPago.SelectedItem == null)
                errores.Add("falta forma/método de pago");

            if (string.IsNullOrWhiteSpace(ExtraerClave(ProdServBox.Text)))
                errores.Add("falta la clave de producto/servicio SAT");

            if (string.IsNullOrWhiteSpace(ExtraerClave(UnidadBox.Text)))
                errores.Add("falta la clave de unidad SAT");

            if (_contrato.MontoMensual <= 0)
                errores.Add("el monto mensual del contrato debe ser mayor a cero");

            return errores;
        }
    }
}
