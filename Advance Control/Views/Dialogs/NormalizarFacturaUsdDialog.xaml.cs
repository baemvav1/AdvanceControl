using System;
using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Advance_Control.Views.Dialogs;

public sealed partial class NormalizarFacturaUsdDialog : ContentDialog
{
    public decimal TipoCambioCapturado { get; private set; }

    public NormalizarFacturaUsdDialog(string? folio, decimal totalUsd, decimal tipoCambioInicial, XamlRoot xamlRoot)
    {
        InitializeComponent();
        XamlRoot = xamlRoot;

        var folioMostrado = string.IsNullOrWhiteSpace(folio) ? "Sin folio" : folio.Trim();
        MensajePrincipalTextBlock.Text = $"Factura Folio {folioMostrado} con monto en dolares.";
        ResumenFacturaTextBlock.Text = $"Total original USD: {totalUsd.ToString("N2", new CultureInfo("es-MX"))}";
        TipoCambioTextBox.Text = tipoCambioInicial > 0m
            ? tipoCambioInicial.ToString("0.######", CultureInfo.InvariantCulture)
            : "1";

        PrimaryButtonClick += OnPrimaryButtonClick;
    }

    private void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        EstadoInfoBar.IsOpen = false;

        if (!TryParseDecimal(TipoCambioTextBox.Text, out var tipoCambio) || tipoCambio <= 0m)
        {
            EstadoInfoBar.Message = "Captura un tipo de cambio valido mayor que cero.";
            EstadoInfoBar.IsOpen = true;
            args.Cancel = true;
            return;
        }

        TipoCambioCapturado = decimal.Round(tipoCambio, 6, MidpointRounding.AwayFromZero);
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
