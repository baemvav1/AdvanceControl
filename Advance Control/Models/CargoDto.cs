using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Advance_Control.Models
{
    /// <summary>
    /// DTO para los datos de cargo que se reciben desde la API
    /// </summary>
    public class CargoDto : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private int _idCargo;
        private int? _idTipoCargo;
        private int? _idRelacionCargo;
        private double? _monto;
        private string? _nota;

        /// <summary>
        /// ID único del cargo
        /// </summary>
        [JsonPropertyName("idCargo")]
        public int IdCargo
        {
            get => _idCargo;
            set
            {
                if (_idCargo != value)
                {
                    _idCargo = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// ID del tipo de cargo
        /// </summary>
        [JsonPropertyName("idTipoCargo")]
        public int? IdTipoCargo
        {
            get => _idTipoCargo;
            set
            {
                if (_idTipoCargo != value)
                {
                    _idTipoCargo = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// ID de la relación del cargo
        /// </summary>
        [JsonPropertyName("idRelacionCargo")]
        public int? IdRelacionCargo
        {
            get => _idRelacionCargo;
            set
            {
                if (_idRelacionCargo != value)
                {
                    _idRelacionCargo = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Monto del cargo
        /// </summary>
        [JsonPropertyName("monto")]
        public double? Monto
        {
            get => _monto;
            set
            {
                if (_monto != value)
                {
                    _monto = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Nota del cargo
        /// </summary>
        [JsonPropertyName("nota")]
        public string? Nota
        {
            get => _nota;
            set
            {
                if (_nota != value)
                {
                    _nota = value;
                    OnPropertyChanged();
                }
            }
        }
    }
}
