using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Advance_Control.Models
{
    /// <summary>
    /// DTO para los datos del inmueble que se reciben desde la API
    /// </summary>
    public class InmuebleDto : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        [JsonPropertyName("idInmueble")]
        public int IdInmueble { get; set; }

        [JsonPropertyName("identificador")]
        public string? Identificador { get; set; }

        [JsonPropertyName("descripcion")]
        public string? Descripcion { get; set; }

        [JsonPropertyName("idTipoInmueble")]
        public int? IdTipoInmueble { get; set; }

        [JsonPropertyName("tipoInmueble")]
        public string? TipoInmueble { get; set; }

        [JsonPropertyName("superficieM2")]
        public double? SuperficieM2 { get; set; }

        [JsonPropertyName("codigoPostal")]
        public string? CodigoPostal { get; set; }

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
        /// Colección de relaciones cliente para este inmueble.
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
        /// Indica si las relaciones ya han sido cargadas para este inmueble.
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

        private UbicacionDto? _ubicacion;

        /// <summary>
        /// Ubicación asociada al inmueble.
        /// Se carga desde el endpoint de ubicaciones cuando se expande el item.
        /// </summary>
        [JsonIgnore]
        public UbicacionDto? Ubicacion
        {
            get => _ubicacion;
            set
            {
                if (_ubicacion != value)
                {
                    _ubicacion = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(HasUbicacion));
                    OnPropertyChanged(nameof(ShowNoUbicacionMessage));
                }
            }
        }

        private bool _isLoadingUbicacion = false;

        /// <summary>
        /// Indica si la ubicación está siendo cargada.
        /// </summary>
        [JsonIgnore]
        public bool IsLoadingUbicacion
        {
            get => _isLoadingUbicacion;
            set
            {
                if (_isLoadingUbicacion != value)
                {
                    _isLoadingUbicacion = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Indica si el inmueble tiene una ubicación asignada.
        /// </summary>
        [JsonIgnore]
        public bool HasUbicacion => IdUbicacion.HasValue && IdUbicacion.Value > 0;

        /// <summary>
        /// Indica si se debe mostrar el mensaje de que no hay ubicación.
        /// </summary>
        [JsonIgnore]
        public bool ShowNoUbicacionMessage => !HasUbicacion;
    }
}
