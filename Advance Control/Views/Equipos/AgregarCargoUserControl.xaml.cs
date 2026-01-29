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
        private int? _selectedIdRelacionCargo;
        private string? _selectedRelacionText;

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
                       _selectedIdRelacionCargo.HasValue &&
                       _selectedIdRelacionCargo.Value > 0 &&
                       !double.IsNaN(MontoNumberBox.Value) &&
                       MontoNumberBox.Value > 0;
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
                Operacion = "create",
                IdOperacion = _idOperacion,
                IdTipoCargo = idTipoCargo,
                IdRelacionCargo = _selectedIdRelacionCargo.Value,
                Monto = MontoNumberBox.Value,
                Nota = string.IsNullOrWhiteSpace(NotaTextBox.Text) ? null : NotaTextBox.Text.Trim()
            };
        }

        /// <summary>
        /// Maneja el cambio de selección en el ComboBox de tipo de cargo
        /// </summary>
        private void TipoCargoComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Limpiar la selección anterior
            _selectedIdRelacionCargo = null;
            _selectedRelacionText = null;

            // Habilitar el botón de selección y actualizar el texto
            IdRelacionCargoButton.IsEnabled = TipoCargoComboBox.SelectedIndex >= 0;

            if (TipoCargoComboBox.SelectedIndex >= 0)
            {
                var selectedItem = TipoCargoComboBox.SelectedItem as ComboBoxItem;
                var idTipoCargo = selectedItem?.Tag != null ? Convert.ToInt32(selectedItem.Tag) : 0;

                if (idTipoCargo == 1)
                {
                    IdRelacionCargoButtonText.Text = "Seleccionar refacción...";
                }
                else if (idTipoCargo == 2)
                {
                    IdRelacionCargoButtonText.Text = "Seleccionar servicio...";
                }
            }
            else
            {
                IdRelacionCargoButtonText.Text = "Seleccione primero el tipo de cargo...";
            }
        }

        /// <summary>
        /// Maneja el clic en el botón de ID Relación Cargo
        /// </summary>
        private async void IdRelacionCargoButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = TipoCargoComboBox.SelectedItem as ComboBoxItem;
            if (selectedItem == null)
                return;

            var idTipoCargo = Convert.ToInt32(selectedItem.Tag);

            if (idTipoCargo == 1)
            {
                // Mostrar selector de refacción
                await ShowRefaccionSelectorAsync();
            }
            else if (idTipoCargo == 2)
            {
                // Mostrar selector de servicio
                await ShowServicioSelectorAsync();
            }
        }

        /// <summary>
        /// Muestra el diálogo de selección de refacción
        /// </summary>
        private async System.Threading.Tasks.Task ShowRefaccionSelectorAsync()
        {
            var selectorControl = new SeleccionarRefaccionUserControl();

            var dialog = new ContentDialog
            {
                Title = "Seleccionar Refacción",
                Content = selectorControl,
                PrimaryButtonText = "Aceptar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary && selectorControl.HasSelection)
            {
                var selectedRefaccion = selectorControl.SelectedRefaccion;
                if (selectedRefaccion != null)
                {
                    _selectedIdRelacionCargo = selectedRefaccion.IdRefaccion;
                    _selectedRelacionText = $"{selectedRefaccion.Marca} - {selectedRefaccion.Serie}";
                    IdRelacionCargoButtonText.Text = $"Refacción: {_selectedRelacionText} (ID: {_selectedIdRelacionCargo})";
                }
            }
        }

        /// <summary>
        /// Muestra el diálogo de selección de servicio
        /// </summary>
        private async System.Threading.Tasks.Task ShowServicioSelectorAsync()
        {
            var selectorControl = new SeleccionarServicioUserControl();

            var dialog = new ContentDialog
            {
                Title = "Seleccionar Servicio",
                Content = selectorControl,
                PrimaryButtonText = "Aceptar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary && selectorControl.HasSelection)
            {
                var selectedServicio = selectorControl.SelectedServicio;
                if (selectedServicio != null)
                {
                    _selectedIdRelacionCargo = selectedServicio.IdServicio;
                    _selectedRelacionText = $"{selectedServicio.Concepto}";
                    IdRelacionCargoButtonText.Text = $"Servicio: {_selectedRelacionText} (ID: {_selectedIdRelacionCargo})";
                }
            }
        }
    }
}
