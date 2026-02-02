using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Advance_Control.Models
{
    /// <summary>
    /// DTO para los datos del equipo que se reciben desde la API
    /// </summary>
    public class EquipoDto : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        [JsonPropertyName("idEquipo")]
        public int IdEquipo { get; set; }

        [JsonPropertyName("marca")]
        public string? Marca { get; set; }

        [JsonPropertyName("creado")]
        public int? Creado { get; set; }

        [JsonPropertyName("paradas")]
        public int? Paradas { get; set; }

        [JsonPropertyName("kilogramos")]
        public int? Kilogramos { get; set; }

        [JsonPropertyName("personas")]
        public int? Personas { get; set; }

        [JsonPropertyName("descripcion")]
        public string? Descripcion { get; set; }

        [JsonPropertyName("identificador")]
        public string? Identificador { get; set; }

        [JsonPropertyName("estatus")]
        public bool? Estatus { get; set; }

        [JsonPropertyName("idUbicacion")]
        public int? IdUbicacion { get; set; }

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

        private ObservableCollection<RelacionClienteDto> _relaciones = new ObservableCollection<RelacionClienteDto>();

        /// <summary>
        /// Colección de relaciones cliente para este equipo.
        /// Se carga desde el endpoint de relaciones cuando se expande el item.
        /// </summary>
        [JsonIgnore]
        public ObservableCollection<RelacionClienteDto> Relaciones
        {
            get => _relaciones;
            set
            {
                if (_relaciones != value)
                {
                    _relaciones = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ShowNoRelacionesMessage));
                }
            }
        }

        private bool _relacionesLoaded = false;

        /// <summary>
        /// Indica si las relaciones ya han sido cargadas para este equipo.
        /// </summary>
        [JsonIgnore]
        public bool RelacionesLoaded
        {
            get => _relacionesLoaded;
            set
            {
                if (_relacionesLoaded != value)
                {
                    _relacionesLoaded = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ShowNoRelacionesMessage));
                }
            }
        }

        private bool _isLoadingRelaciones = false;

        /// <summary>
        /// Indica si las relaciones están siendo cargadas.
        /// </summary>
        [JsonIgnore]
        public bool IsLoadingRelaciones
        {
            get => _isLoadingRelaciones;
            set
            {
                if (_isLoadingRelaciones != value)
                {
                    _isLoadingRelaciones = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Indica si se debe mostrar el mensaje de que no hay relaciones.
        /// True cuando RelacionesLoaded es true y Relaciones está vacía.
        /// </summary>
        [JsonIgnore]
        public bool ShowNoRelacionesMessage => RelacionesLoaded && Relaciones.Count == 0;

        /// <summary>
        /// Actualiza el estado del mensaje de no relaciones.
        /// Debe llamarse después de modificar Relaciones o RelacionesLoaded.
        /// </summary>
        public void NotifyNoRelacionesMessageChanged()
        {
            OnPropertyChanged(nameof(ShowNoRelacionesMessage));
        }
    }
}
