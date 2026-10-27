using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Advance_Control.Models;
using Advance_Control.Utilities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Advance_Control.Views.Dialogs
{
    /// <summary>
    /// Envoltorio seleccionable de un abono pendiente de complementar, usado en
    /// el checklist de GenerarComplementoPagoDialog.
    /// </summary>
    public class AbonoSeleccionableDto : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public AbonoPendienteComplementoDto Abono { get; }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
                }
            }
        }

        public AbonoSeleccionableDto(AbonoPendienteComplementoDto abono, bool preseleccionado)
        {
            Abono = abono;
            _isSelected = preseleccionado;
        }
    }

    /// <summary>
    /// Diálogo para generar un Complemento de Pago real (timbrado): muestra los abonos
    /// pendientes de complementar de la factura que disparó el diálogo y de cualquier otra
    /// factura del mismo receptor (para cuando un solo pago cubrió varias facturas), agrupa
    /// los abonos marcados por fecha en nodos &lt;Pago&gt; y arma el request para el API.
    /// </summary>
    public sealed partial class GenerarComplementoPagoDialog : ContentDialog
    {
        private readonly FacturaResumenDto _factura;

        public ObservableCollection<AbonoSeleccionableDto> Abonos { get; } = new();

        public GenerarComplementoPagoRequestDto? ResultadoRequest { get; private set; }

        public GenerarComplementoPagoDialog(FacturaResumenDto factura, List<AbonoPendienteComplementoDto> abonosPendientes, XamlRoot xamlRoot)
        {
            InitializeComponent();
            XamlRoot = xamlRoot;
            _factura = factura ?? throw new ArgumentNullException(nameof(factura));

            ResumenTextBlock.Text = $"Complemento de pago para {factura.ReceptorNombre ?? "el cliente"} (RFC {factura.ReceptorRfc}).";

            foreach (var abono in abonosPendientes.OrderBy(a => a.IdFactura != factura.IdFactura).ThenBy(a => a.FechaAbono))
            {
                Abonos.Add(new AbonoSeleccionableDto(abono, preseleccionado: abono.IdFactura == factura.IdFactura));
            }

            AbonosRepeater.ItemsSource = Abonos;

            PrimaryButtonClick += OnPrimaryButtonClick;
        }

        private void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            EstadoInfoBar.IsOpen = false;

            var seleccionados = Abonos.Where(a => a.IsSelected).Select(a => a.Abono).ToList();
            var formaPagoCombo = (CmbFormaPago.SelectedItem as ComboBoxItem)?.Tag as string;

            var error = ComplementoPagoArmador.Validar(seleccionados, formaPagoCombo);
            if (error != null)
            {
                EstadoInfoBar.Message = error;
                EstadoInfoBar.IsOpen = true;
                args.Cancel = true;
                return;
            }

            var grupos = ComplementoPagoArmador.Armar(seleccionados, formaPagoCombo, NumOperacionTextBox.Text);

            ResultadoRequest = new GenerarComplementoPagoRequestDto
            {
                ReceptorRfc = _factura.ReceptorRfc ?? string.Empty,
                Pagos = grupos
            };
        }
    }
}
