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
        [JsonPropertyName("IdOperacion")]
        public int? IdOperacion { get; set; }

        /// <summary>
        /// ID del tipo de operación
        /// </summary>
        [JsonPropertyName("IdTipo")]
        public int? IdTipo { get; set; }

        /// <summary>
        /// Razón social del cliente
        /// </summary>
        [JsonPropertyName("RazonSocial")]
        public string? RazonSocial { get; set; }

        /// <summary>
        /// Identificador del equipo
        /// </summary>
        [JsonPropertyName("Identificador")]
        public string? Identificador { get; set; }

        /// <summary>
        /// Nombre de quien atiende
        /// </summary>
        [JsonPropertyName("Atiende")]
        public string? Atiende { get; set; }

        /// <summary>
        /// Monto de la operación
        /// </summary>
        [JsonPropertyName("Monto")]
        public decimal Monto { get; set; }

        /// <summary>
        /// Nota asociada a la operación
        /// </summary>
        [JsonPropertyName("Nota")]
        public string? Nota { get; set; }

        /// <summary>
        /// Fecha de inicio de la operación
        /// </summary>
        [JsonPropertyName("FechaInicio")]
        public DateTime? FechaInicio { get; set; }

        /// <summary>
        /// Fecha de finalización de la operación
        /// </summary>
        [JsonPropertyName("FechaFinal")]
        public DateTime? FechaFinal { get; set; }

        /// <summary>
        /// Indica si la operación ha finalizado
        /// </summary>
        [JsonPropertyName("Finalizado")]
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

        private bool _cargosLoaded = false;

        /// <summary>
        /// Indica si los cargos ya fueron cargados desde el servidor
        /// </summary>
        [JsonIgnore]
        public bool CargosLoaded
        {
            get => _cargosLoaded;
            set
            {
                if (_cargosLoaded != value)
                {
                    _cargosLoaded = value;
                    OnPropertyChanged();
                }
            }
        }
    }
}
