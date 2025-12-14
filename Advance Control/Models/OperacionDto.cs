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
        /// ID de la operación
        /// </summary>
        [JsonPropertyName("idOperacion")]
        public int? IdOperacion { get; set; }

        /// <summary>
        /// Tipo de operación
        /// </summary>
        [JsonPropertyName("tipoOperacion")]
        public string? TipoOperacion { get; set; }

        /// <summary>
        /// Nombre comercial del cliente
        /// </summary>
        [JsonPropertyName("nombreComercial")]
        public string? NombreComercial { get; set; }

        /// <summary>
        /// Razón social del cliente
        /// </summary>
        [JsonPropertyName("razonSocial")]
        public string? RazonSocial { get; set; }

        /// <summary>
        /// Nota asociada a la operación
        /// </summary>
        [JsonPropertyName("nota")]
        public string? Nota { get; set; }

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
