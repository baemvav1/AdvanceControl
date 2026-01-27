using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Advance_Control.Models
{
    /// <summary>
    /// DTO para los datos de operación que se reciben desde la API
    /// </summary>
    public class OperacionDto : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// ID único de la operación (requerido para operaciones de eliminación)
        /// </summary>
        [JsonPropertyName("idOperacion")]
        public int? IdOperacion { get; set; }

        /// <summary>
        /// ID del tipo de operación
        /// </summary>
        [JsonPropertyName("idTipo")]
        public int? IdTipo { get; set; }

        /// <summary>
        /// Razón social del cliente
        /// </summary>
        [JsonPropertyName("razonSocial")]
        public string? RazonSocial { get; set; }

        /// <summary>
        /// Identificador del equipo
        /// </summary>
        [JsonPropertyName("identificador")]
        public string? Identificador { get; set; }

        /// <summary>
        /// Nombre de quien atiende
        /// </summary>
        [JsonPropertyName("atiende")]
        public string? Atiende { get; set; }

        /// <summary>
        /// Monto de la operación
        /// </summary>
        [JsonPropertyName("monto")]
        public decimal Monto { get; set; }

        /// <summary>
        /// Nota asociada a la operación
        /// </summary>
        [JsonPropertyName("nota")]
        public string? Nota { get; set; }

        /// <summary>
        /// Fecha de inicio de la operación
        /// </summary>
        [JsonPropertyName("fechaInicio")]
        public DateTime? FechaInicio { get; set; }

        /// <summary>
        /// Fecha de finalización de la operación
        /// </summary>
        [JsonPropertyName("fechaFinal")]
        public DateTime? FechaFinal { get; set; }

        /// <summary>
        /// Indica si la operación ha finalizado
        /// </summary>
        [JsonPropertyName("finalizado")]
        public bool Finalizado { get; set; }

        private bool _expand = false;

        /// <summary>
        /// Propiedad interna para controlar el estado de expansión en la UI.
        /// No se deserializa desde el endpoint.
        /// </summary>
        public bool Expand
        {
            get => _expand;
            set
            {
                if (_expand != value)
                {
                    _expand = value;
                    OnPropertyChanged();
                }
            }
        }

        private ObservableCollection<CargoDto> _cargos = new ObservableCollection<CargoDto>();

        /// <summary>
        /// Colección de cargos asociados a esta operación
        /// </summary>
        [JsonIgnore]
        public ObservableCollection<CargoDto> Cargos
        {
            get => _cargos;
            set
            {
                if (_cargos != value)
                {
                    _cargos = value;
                    OnPropertyChanged();
                }
            }
        }
    }
}
