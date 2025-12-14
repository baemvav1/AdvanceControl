using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Advance_Control.Models
{
    /// <summary>
    /// DTO para la entidad Proveedor
    /// </summary>
    public class ProveedorDto : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        [JsonPropertyName("idProveedor")]
        public int IdProveedor { get; set; }

        [JsonPropertyName("rfc")]
        public string? Rfc { get; set; }

        [JsonPropertyName("razonSocial")]
        public string? RazonSocial { get; set; }

        [JsonPropertyName("nombreComercial")]
        public string? NombreComercial { get; set; }

        [JsonPropertyName("estatus")]
        public bool? Estatus { get; set; }

        [JsonPropertyName("nota")]
        public string? Nota { get; set; }

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

        private ObservableCollection<RelacionProveedorRefaccionDto> _relacionesRefaccion = new ObservableCollection<RelacionProveedorRefaccionDto>();

        /// <summary>
        /// Colección de relaciones refacción para este proveedor.
        /// Se carga desde el endpoint de relaciones cuando se expande el item.
        /// </summary>
        [JsonIgnore]
        public ObservableCollection<RelacionProveedorRefaccionDto> RelacionesRefaccion
        {
            get => _relacionesRefaccion;
            set
            {
                if (_relacionesRefaccion != value)
                {
                    _relacionesRefaccion = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ShowNoRelacionesRefaccionMessage));
                }
            }
        }

        private bool _relacionesRefaccionLoaded = false;

        /// <summary>
        /// Indica si las relaciones refacción ya han sido cargadas para este proveedor.
        /// </summary>
        [JsonIgnore]
        public bool RelacionesRefaccionLoaded
        {
            get => _relacionesRefaccionLoaded;
            set
            {
                if (_relacionesRefaccionLoaded != value)
                {
                    _relacionesRefaccionLoaded = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ShowNoRelacionesRefaccionMessage));
                }
            }
        }

        private bool _isLoadingRelacionesRefaccion = false;

        /// <summary>
        /// Indica si las relaciones refacción están siendo cargadas.
        /// </summary>
        [JsonIgnore]
        public bool IsLoadingRelacionesRefaccion
        {
            get => _isLoadingRelacionesRefaccion;
            set
            {
                if (_isLoadingRelacionesRefaccion != value)
                {
                    _isLoadingRelacionesRefaccion = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Indica si se debe mostrar el mensaje de que no hay relaciones refacción.
        /// True cuando RelacionesRefaccionLoaded es true y RelacionesRefaccion está vacía.
        /// </summary>
        [JsonIgnore]
        public bool ShowNoRelacionesRefaccionMessage => RelacionesRefaccionLoaded && RelacionesRefaccion.Count == 0;

        /// <summary>
        /// Actualiza el estado del mensaje de no relaciones refacción.
        /// Debe llamarse después de modificar RelacionesRefaccion o RelacionesRefaccionLoaded.
        /// </summary>
        public void NotifyNoRelacionesRefaccionMessageChanged()
        {
            OnPropertyChanged(nameof(ShowNoRelacionesRefaccionMessage));
        }
    }
}
