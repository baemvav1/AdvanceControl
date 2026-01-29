using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Advance_Control.Models;

namespace Advance_Control.Views.Equipos
{
    /// <summary>
    /// UserControl para agregar un cargo a una operación
    /// </summary>
    public sealed partial class AgregarCargoUserControl : UserControl, INotifyPropertyChanged
    {
        // Constantes para los tipos de cargo
        private const int TIPO_CARGO_REFACCION = 1;
        private const int TIPO_CARGO_SERVICIO = 2;

        private int _idOperacion;
        private int? _idAtiende;
        private int _selectedCargoType = 0;
        private SeleccionarRefaccionUserControl? _refaccionSelector;
        private SeleccionarServicioUserControl? _servicioSelector;

        public event PropertyChangedEventHandler PropertyChanged;

        public AgregarCargoUserControl(int idOperacion, int? idAtiende = null)
        {
            this.InitializeComponent();
            
            _idOperacion = idOperacion;
            _idAtiende = idAtiende;
            IdOperacionTextBlock.Text = idOperacion.ToString();
        }

        /// <summary>
        /// The currently selected cargo type (1 = Refaccion, 2 = Servicio)
        /// </summary>
        public int SelectedCargoType
        {
            get => _selectedCargoType;
            private set
            {
                if (_selectedCargoType != value)
                {
                    _selectedCargoType = value;
                    OnPropertyChanged();
                    
                    // Lazy load the appropriate selector when cargo type changes
                    LoadSelectorForCargoType(value);
                }
            }
        }

        /// <summary>
        /// Lazy loads the selector control for the specified cargo type
        /// </summary>
        private void LoadSelectorForCargoType(int cargoType)
        {
            if (cargoType == TIPO_CARGO_REFACCION && _refaccionSelector == null)
            {
                _refaccionSelector = new SeleccionarRefaccionUserControl();
                _refaccionSelector.CostoChanged += OnRefaccionCostoChanged;
                RefaccionSelectorContainer.Content = _refaccionSelector;
            }
            else if (cargoType == TIPO_CARGO_SERVICIO && _servicioSelector == null)
            {
                _servicioSelector = new SeleccionarServicioUserControl();
                ServicioSelectorContainer.Content = _servicioSelector;
            }
        }

        /// <summary>
        /// Maneja el cambio del costo de la refacción seleccionada
        /// </summary>
        private void OnRefaccionCostoChanged(object? sender, double? costo)
        {
            if (costo.HasValue && costo.Value >= 0)
            {
                MontoNumberBox.Value = costo.Value;
            }
            else
            {
                // Clear the value by setting to 0 instead of NaN for better UI behavior
                MontoNumberBox.Value = 0;
            }
        }

        /// <summary>
        /// Indica si todos los campos requeridos están completos
        /// </summary>
        public bool IsValid
        {
            get
            {
                if (TipoCargoComboBox.SelectedIndex < 0)
                    return false;

                if (double.IsNaN(MontoNumberBox.Value) || MontoNumberBox.Value <= 0)
                    return false;

                // Check if a selection has been made in the appropriate selector
                if (SelectedCargoType == TIPO_CARGO_REFACCION)
                {
                    return _refaccionSelector?.HasSelection == true;
                }
                else if (SelectedCargoType == TIPO_CARGO_SERVICIO)
                {
                    return _servicioSelector?.HasSelection == true;
                }

                return false;
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

            int idRelacionCargo = 0;
            int? idProveedor = null;
            
            if (idTipoCargo == TIPO_CARGO_REFACCION && _refaccionSelector?.HasSelection == true)
            {
                idRelacionCargo = _refaccionSelector.SelectedRefaccion?.IdRefaccion ?? 0;
            }
            else if (idTipoCargo == TIPO_CARGO_SERVICIO && _servicioSelector?.HasSelection == true)
            {
                idRelacionCargo = _servicioSelector.SelectedServicio?.IdServicio ?? 0;
                // For Servicio, automatically copy idAtiende from operation as idProveedor
                idProveedor = _idAtiende;
            }

            return new CargoEditDto
            {
                Operacion = "create",
                IdOperacion = _idOperacion,
                IdTipoCargo = idTipoCargo,
                IdRelacionCargo = idRelacionCargo,
                Monto = MontoNumberBox.Value,
                Nota = string.IsNullOrWhiteSpace(NotaTextBox.Text) ? null : NotaTextBox.Text.Trim(),
                IdProveedor = idProveedor
            };
        }

        /// <summary>
        /// Maneja el cambio de selección en el ComboBox de tipo de cargo
        /// </summary>
        private void TipoCargoComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TipoCargoComboBox.SelectedIndex >= 0)
            {
                var selectedItem = TipoCargoComboBox.SelectedItem as ComboBoxItem;
                var idTipoCargo = selectedItem?.Tag != null ? Convert.ToInt32(selectedItem.Tag) : 0;
                
                SelectedCargoType = idTipoCargo;
            }
            else
            {
                SelectedCargoType = 0;
            }
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
