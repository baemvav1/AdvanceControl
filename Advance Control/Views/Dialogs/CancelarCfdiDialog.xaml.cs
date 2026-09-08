using System;
using System.Linq;
using Advance_Control.Models;
using Advance_Control.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Advance_Control.Views.Dialogs;

public sealed partial class CancelarCfdiDialog : ContentDialog
{
    private readonly FacturaResumenDto _factura;
    private readonly FacturasViewModel _viewModel;
    private FacturaResumenDto? _facturaSustitutaSeleccionada;

    public CancelarCfdiRequestDto? ResultadoRequest { get; private set; }

    public CancelarCfdiDialog(FacturaResumenDto factura, FacturasViewModel viewModel, XamlRoot xamlRoot)
    {
        InitializeComponent();
        XamlRoot = xamlRoot;
        _factura = factura ?? throw new ArgumentNullException(nameof(factura));
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

        ResumenFacturaTextBlock.Text = $"Factura {factura.FolioTitulo} · {factura.ReceptorNombre ?? "Sin receptor"} · UUID {factura.Uuid}";

        PrimaryButtonClick += OnPrimaryButtonClick;

        // La selección por defecto se fija aquí (no con IsSelected="True" en el XAML): poner
        // IsSelected en el XAML dispara SelectionChanged durante InitializeComponent(), antes de
        // que SustitucionPanel (declarado más abajo en el árbol visual) ya tenga su campo asignado
        // -- causaba NullReferenceException. Para cuando esto se ejecuta, InitializeComponent ya
        // terminó y todos los campos x:Name existen.
        MotivoComboBox.SelectedIndex = 1; // "02 · Comprobante emitido con errores sin relación"
    }

    private void MotivoComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SustitucionPanel == null)
        {
            return;
        }

        var esMotivo01 = MotivoSeleccionado() == "01";
        SustitucionPanel.Visibility = esMotivo01 ? Visibility.Visible : Visibility.Collapsed;
        if (!esMotivo01)
        {
            _facturaSustitutaSeleccionada = null;
            SustitucionAutoSuggestBox.Text = string.Empty;
        }
    }

    private void SustitucionAutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput)
        {
            return;
        }

        _facturaSustitutaSeleccionada = null;
        sender.ItemsSource = _viewModel
            .ObtenerCandidatasSustitucion(_factura, sender.Text)
            .Select(f => f.SugerenciaTexto)
            .ToList();
    }

    private void SustitucionAutoSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        var texto = args.SelectedItem as string;
        _facturaSustitutaSeleccionada = _viewModel
            .ObtenerCandidatasSustitucion(_factura, SustitucionAutoSuggestBox.Text)
            .FirstOrDefault(f => f.SugerenciaTexto == texto);
    }

    private string MotivoSeleccionado()
        => (MotivoComboBox.SelectedItem as ComboBoxItem)?.Tag as string ?? "02";

    private void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        EstadoInfoBar.IsOpen = false;
        var motivo = MotivoSeleccionado();

        if (ConfirmacionCheckBox.IsChecked != true)
        {
            EstadoInfoBar.Message = "Debes confirmar que entiendes que esta acción es irreversible.";
            EstadoInfoBar.IsOpen = true;
            args.Cancel = true;
            return;
        }

        string? uuidSustitucion = null;
        if (motivo == "01")
        {
            if (_facturaSustitutaSeleccionada == null || string.IsNullOrWhiteSpace(_facturaSustitutaSeleccionada.Uuid))
            {
                EstadoInfoBar.Message = "Selecciona de la lista la factura (con Serie+Folio propio) que sustituye a esta.";
                EstadoInfoBar.IsOpen = true;
                args.Cancel = true;
                return;
            }

            uuidSustitucion = _facturaSustitutaSeleccionada.Uuid;
        }

        ResultadoRequest = new CancelarCfdiRequestDto { Motivo = motivo, UuidSustitucion = uuidSustitucion };
    }
}
