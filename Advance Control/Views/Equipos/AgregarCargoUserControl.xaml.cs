using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Advance_Control.Models;

namespace Advance_Control.Views.Equipos
{
    /// <summary>
    /// UserControl para agregar un cargo a una operación
    /// </summary>
    public sealed partial class AgregarCargoUserControl : UserControl
    {
        private int _idOperacion;

        public AgregarCargoUserControl(int idOperacion)
        {
            this.InitializeComponent();
            
            _idOperacion = idOperacion;
            IdOperacionTextBlock.Text = idOperacion.ToString();
        }

        /// <summary>
        /// Indica si todos los campos requeridos están completos
        /// </summary>
        public bool IsValid
        {
            get
            {
                return TipoCargoComboBox.SelectedIndex >= 0 &&
                       !double.IsNaN(IdRelacionCargoNumberBox.Value) &&
                       IdRelacionCargoNumberBox.Value > 0 &&
                       !double.IsNaN(MontoNumberBox.Value) &&
                       MontoNumberBox.Value >= 0;
            }
        }

        /// <summary>
        /// Obtiene un objeto CargoEditDto con los valores ingresados
        /// </summary>
        public CargoEditDto GetCargoEditDto()
        {
            if (!IsValid)
                throw new InvalidOperationException("Los campos requeridos no están completos");

            var selectedItem = TipoCargoComboBox.SelectedItem as ComboBoxItem;
            var idTipoCargo = selectedItem?.Tag != null ? Convert.ToInt32(selectedItem.Tag) : 0;

            return new CargoEditDto
            {
                IdOperacion = _idOperacion,
                IdTipoCargo = idTipoCargo,
                IdRelacionCargo = Convert.ToInt32(IdRelacionCargoNumberBox.Value),
                Monto = MontoNumberBox.Value,
                Nota = string.IsNullOrWhiteSpace(NotaTextBox.Text) ? null : NotaTextBox.Text.Trim()
            };
        }

        private void TipoCargoComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Notify parent that validation state may have changed
            // This could be used to enable/disable the dialog's primary button
        }
    }
}
