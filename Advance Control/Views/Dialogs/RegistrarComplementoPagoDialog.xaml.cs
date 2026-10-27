using System;
using System.Globalization;
using System.Linq;
using Advance_Control.Models;
using Advance_Control.Utilities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Advance_Control.Views.Dialogs;

public sealed partial class RegistrarComplementoPagoDialog : ContentDialog
{
    private sealed record FormaPagoOpcion(string Clave, string Texto);

    private readonly FacturaResumenDto _factura;

    public RegistrarAbonoFacturaRequestDto? ResultadoRequest { get; private set; }

    public RegistrarComplementoPagoDialog(FacturaResumenDto factura, XamlRoot xamlRoot)
    {
        InitializeComponent();
        XamlRoot = xamlRoot;
        _factura = factura ?? throw new ArgumentNullException(nameof(factura));

        ResumenFacturaTextBlock.Text = $"Factura {factura.FolioTitulo} · {factura.ReceptorNombre ?? "Sin receptor"}";
        SaldoPendienteTextBlock.Text = $"Saldo pendiente: {factura.SaldoPendienteTexto}";
        FechaDatePicker.Date = DateTimeOffset.Now;

        FormaPagoComboBox.ItemsSource = FormaPagoAbonoManualCatalogo.Claves
            .Select(clave => new FormaPagoOpcion(clave, FormaPagoAbonoManualCatalogo.Describir(clave)))
            .ToList();

        if (factura.SaldoPendiente > 0)
        {
            MontoTextBox.Text = factura.SaldoPendiente.ToString("0.00", CultureInfo.InvariantCulture);
        }

        PrimaryButtonClick += OnPrimaryButtonClick;
    }

    private void BtnUsarSaldoCompleto_Click(object sender, RoutedEventArgs e)
    {
        MontoTextBox.Text = _factura.SaldoPendiente.ToString("0.00", CultureInfo.InvariantCulture);
    }

    private void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        EstadoInfoBar.IsOpen = false;

        if (!TryParseDecimal(MontoTextBox.Text, out var monto) || monto <= 0m)
        {
            EstadoInfoBar.Message = "Captura un monto válido mayor que cero.";
            EstadoInfoBar.IsOpen = true;
            args.Cancel = true;
            return;
        }

        // La BD lo rechaza igual (fn_facturas_registrar_abono); validarlo aquí deja corregir el
        // monto sin cerrar el diálogo.
        if (_factura.SaldoPendiente <= 0m)
        {
            EstadoInfoBar.Message = "Esta factura ya no tiene saldo pendiente.";
            EstadoInfoBar.IsOpen = true;
            args.Cancel = true;
            return;
        }

        if (monto > _factura.SaldoPendiente)
        {
            EstadoInfoBar.Message = $"El monto no puede exceder el saldo pendiente ({_factura.SaldoPendienteTexto}).";
            EstadoInfoBar.IsOpen = true;
            args.Cancel = true;
            return;
        }

        if (FechaDatePicker.Date == null)
        {
            EstadoInfoBar.Message = "Selecciona la fecha del pago.";
            EstadoInfoBar.IsOpen = true;
            args.Cancel = true;
            return;
        }

        if (FormaPagoComboBox.SelectedItem is not FormaPagoOpcion formaPago)
        {
            EstadoInfoBar.Message = "Selecciona cómo llegó el pago (efectivo, transferencia, SPEI o cheque).";
            EstadoInfoBar.IsOpen = true;
            args.Cancel = true;
            return;
        }

        ResultadoRequest = new RegistrarAbonoFacturaRequestDto
        {
            IdFactura = _factura.IdFactura,
            // Mediodía del día elegido: el VPS guarda/lee en UTC y la PC muestra en hora de México
            // (-6 h); con la hora real, un abono capturado de madrugada (o una fecha elegida a las
            // 00:00) regresaría con el día anterior y así saldría en el Complemento de Pago.
            FechaAbono = FechaDatePicker.Date.Value.Date.AddHours(12),
            MontoAbono = monto,
            Referencia = string.IsNullOrWhiteSpace(ReferenciaTextBox.Text) ? null : ReferenciaTextBox.Text.Trim(),
            Observaciones = string.IsNullOrWhiteSpace(ObservacionesTextBox.Text) ? null : ObservacionesTextBox.Text.Trim(),
            FormaPago = formaPago.Clave
        };
    }

    private static bool TryParseDecimal(string? text, out decimal value)
    {
        value = 0m;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var normalized = text.Trim();
        return decimal.TryParse(normalized, NumberStyles.Number, new CultureInfo("es-MX"), out value)
            || decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
    }
}
