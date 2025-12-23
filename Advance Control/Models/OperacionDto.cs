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

        private ObservableCollection<RelacionOperacionProveedorRefaccionDto> _relacionesRefaccion = new ObservableCollection<RelacionOperacionProveedorRefaccionDto>();

        /// <summary>
        /// Colección de relaciones refacción para esta operación.
        /// Se carga desde el endpoint de relaciones cuando se expande el item.
        /// </summary>
        [JsonIgnore]
        public ObservableCollection<RelacionOperacionProveedorRefaccionDto> RelacionesRefaccion
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
        /// Indica si las relaciones refacción ya han sido cargadas para esta operación.
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
