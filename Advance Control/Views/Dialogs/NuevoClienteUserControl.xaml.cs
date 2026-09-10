using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Advance_Control.Services.SatCatalogo;
using Advance_Control.Utilities;

namespace Advance_Control.Views.Dialogs
{
    /// <summary>
    /// UserControl para crear un nuevo cliente
    /// </summary>
    public sealed partial class NuevoClienteUserControl : UserControl
    {
        private readonly ISatCatalogoService _satCatalogoService;

        public NuevoClienteUserControl()
        {
            this.InitializeComponent();
            _satCatalogoService = AppServices.Get<ISatCatalogoService>();
            this.Loaded += NuevoClienteUserControl_Loaded;
        }

        private async void NuevoClienteUserControl_Loaded(object sender, RoutedEventArgs e)
        {
            var regimenes = await _satCatalogoService.ListarRegimenFiscalAsync();
            SatCatalogoAutoSuggestHelper.Configurar(RegimenFiscalAutoSuggestBox, regimenes);

            var usos = await _satCatalogoService.ListarUsoCfdiAsync();
            SatCatalogoAutoSuggestHelper.Configurar(UsoCfdiAutoSuggestBox, usos);
        }

        /// <summary>
        /// Indica si todos los campos requeridos están completos
        /// </summary>
        public bool IsValid
        {
            get
            {
                return !string.IsNullOrWhiteSpace(RfcTextBox.Text) &&
                       !string.IsNullOrWhiteSpace(RazonSocialTextBox.Text) &&
                       !string.IsNullOrWhiteSpace(NombreComercialTextBox.Text);
            }
        }

        /// <summary>
        /// RFC del cliente
        /// </summary>
        public string Rfc => RfcTextBox.Text?.Trim() ?? "";

        /// <summary>
        /// Razón Social del cliente
        /// </summary>
        public string RazonSocial => RazonSocialTextBox.Text?.Trim() ?? "";

        /// <summary>
        /// Nombre Comercial del cliente
        /// </summary>
        public string NombreComercial => NombreComercialTextBox.Text?.Trim() ?? "";

        /// <summary>
        /// Régimen Fiscal (opcional)
        /// </summary>
        public string? RegimenFiscal
        {
            get
            {
                var clave = SatCatalogoAutoSuggestHelper.ExtraerClave(RegimenFiscalAutoSuggestBox.Text);
                return string.IsNullOrWhiteSpace(clave) ? null : clave;
            }
        }

        /// <summary>
        /// Uso CFDI (opcional)
        /// </summary>
        public string? UsoCfdi
        {
            get
            {
                var clave = SatCatalogoAutoSuggestHelper.ExtraerClave(UsoCfdiAutoSuggestBox.Text);
                return string.IsNullOrWhiteSpace(clave) ? null : clave;
            }
        }

        /// <summary>
        /// Días de Crédito (opcional)
        /// </summary>
        public int? DiasCredito
        {
            get
            {
                if (!double.IsNaN(DiasCreditoNumberBox.Value))
                {
                    return Convert.ToInt32(Math.Round(DiasCreditoNumberBox.Value));
                }
                return null;
            }
        }

        /// <summary>
        /// Límite de Crédito (opcional)
        /// </summary>
        public decimal? LimiteCredito
        {
            get
            {
                if (!double.IsNaN(LimiteCreditoNumberBox.Value))
                {
                    return Convert.ToDecimal(LimiteCreditoNumberBox.Value);
                }
                return null;
            }
        }

        /// <summary>
        /// Prioridad (opcional)
        /// </summary>
        public int? Prioridad
        {
            get
            {
                if (!double.IsNaN(PrioridadNumberBox.Value))
                {
                    return Convert.ToInt32(Math.Round(PrioridadNumberBox.Value));
                }
                return null;
            }
        }

        /// <summary>
        /// Notas (opcional)
        /// </summary>
        public string? Notas => string.IsNullOrWhiteSpace(NotasTextBox.Text)
            ? null
            : NotasTextBox.Text.Trim();

        /// <summary>
        /// Código Postal del domicilio fiscal (opcional)
        /// </summary>
        public string? CodigoPostal => string.IsNullOrWhiteSpace(CodigoPostalTextBox.Text)
            ? null
            : CodigoPostalTextBox.Text.Trim();

        /// <summary>
        /// Estatus del cliente
        /// </summary>
        public bool Estatus => EstatusCheckBox.IsChecked ?? true;
    }
}
