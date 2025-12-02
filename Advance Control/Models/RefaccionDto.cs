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
    }
}
