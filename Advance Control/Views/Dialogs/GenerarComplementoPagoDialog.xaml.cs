using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Advance_Control.Models;
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
            if (seleccionados.Count == 0)
            {
                EstadoInfoBar.Message = "Selecciona al menos un abono para incluir en el complemento.";
                EstadoInfoBar.IsOpen = true;
                args.Cancel = true;
                return;
            }

            if (CmbFormaPago.SelectedItem is not ComboBoxItem formaPagoItem || formaPagoItem.Tag is not string formaPago)
            {
                EstadoInfoBar.Message = "Selecciona la forma de pago.";
                EstadoInfoBar.IsOpen = true;
                args.Cancel = true;
                return;
            }

            var numOperacion = string.IsNullOrWhiteSpace(NumOperacionTextBox.Text) ? null : NumOperacionTextBox.Text.Trim();

            // Abonos con la misma fecha (día) se agrupan en un solo nodo <Pago> -- así un pago que
            // cubrió varias facturas queda en un solo nodo con varios <DoctoRelacionado>, mientras
            // que abonos de fechas distintas (pagos reales distintos) quedan en nodos separados.
            var grupos = seleccionados
                .GroupBy(a => a.FechaAbono.Date)
                .OrderBy(g => g.Key)
                .Select(g => new GrupoPagoComplementoDto
                {
                    FechaPago = g.Key,
                    FormaPago = formaPago,
                    Moneda = g.First().Moneda,
                    TipoCambio = 1m,
                    NumOperacion = numOperacion,
                    Doctos = g.Select(a => new DoctoRelacionadoComplementoDto
                    {
                        IdAbonoFactura = a.IdAbonoFactura,
                        IdFacturaPagada = a.IdFactura,
                        NumParcialidad = a.NumParcialidad,
                        ImpSaldoAnt = a.ImpSaldoAnt,
                        ImpPagado = a.MontoAbono,
                        ImpSaldoInsoluto = a.ImpSaldoInsoluto,
                        ObjetoImp = "02"
                    }).ToList()
                })
                .ToList();

            ResultadoRequest = new GenerarComplementoPagoRequestDto
            {
                ReceptorRfc = _factura.ReceptorRfc ?? string.Empty,
                Pagos = grupos
            };
        }
    }
}
