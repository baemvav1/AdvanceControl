using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Advance_Control.Models
{
    /// <summary>
    /// DTO para los datos de la refacción que se reciben desde la API
    /// </summary>
    public class RefaccionDto : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        [JsonPropertyName("idRefaccion")]
        public int IdRefaccion { get; set; }

        [JsonPropertyName("marca")]
        public string? Marca { get; set; }

        [JsonPropertyName("serie")]
        public string? Serie { get; set; }

        [JsonPropertyName("costo")]
        public double? Costo { get; set; }

        [JsonPropertyName("descripcion")]
        public string? Descripcion { get; set; }

        [JsonPropertyName("estatus")]
        public bool? Estatus { get; set; }

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

        private ObservableCollection<RelacionEquipoDto> _relacionesEquipo = new ObservableCollection<RelacionEquipoDto>();

        /// <summary>
        /// Colección de relaciones equipo para esta refacción.
        /// Se carga desde el endpoint de relaciones cuando se expande el item.
        /// </summary>
        [JsonIgnore]
        public ObservableCollection<RelacionEquipoDto> RelacionesEquipo
        {
            get => _relacionesEquipo;
            set
            {
                if (_relacionesEquipo != value)
                {
                    _relacionesEquipo = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ShowNoRelacionesEquipoMessage));
                }
            }
        }

        private bool _relacionesEquipoLoaded = false;

        /// <summary>
        /// Indica si las relaciones equipo ya han sido cargadas para esta refacción.
        /// </summary>
        [JsonIgnore]
        public bool RelacionesEquipoLoaded
        {
            get => _relacionesEquipoLoaded;
            set
            {
                if (_relacionesEquipoLoaded != value)
                {
                    _relacionesEquipoLoaded = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ShowNoRelacionesEquipoMessage));
                }
            }
        }

        private bool _isLoadingRelacionesEquipo = false;

        /// <summary>
        /// Indica si las relaciones equipo están siendo cargadas.
        /// </summary>
        [JsonIgnore]
        public bool IsLoadingRelacionesEquipo
        {
            get => _isLoadingRelacionesEquipo;
            set
            {
                if (_isLoadingRelacionesEquipo != value)
                {
                    _isLoadingRelacionesEquipo = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Indica si se debe mostrar el mensaje de que no hay relaciones equipo.
        /// True cuando RelacionesEquipoLoaded es true y RelacionesEquipo está vacía.
        /// </summary>
        [JsonIgnore]
        public bool ShowNoRelacionesEquipoMessage => RelacionesEquipoLoaded && RelacionesEquipo.Count == 0;

        /// <summary>
        /// Actualiza el estado del mensaje de no relaciones equipo.
        /// Debe llamarse después de modificar RelacionesEquipo o RelacionesEquipoLoaded.
        /// </summary>
        public void NotifyNoRelacionesEquipoMessageChanged()
        {
            OnPropertyChanged(nameof(ShowNoRelacionesEquipoMessage));
        }
    }
}
