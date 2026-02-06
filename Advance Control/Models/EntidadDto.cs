using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Advance_Control.Models
{
    /// <summary>
    /// DTO para representar una entidad
    /// </summary>
    public class EntidadDto : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        [JsonPropertyName("idEntidad")]
        public int IdEntidad { get; set; }

        [JsonPropertyName("nombreComercial")]
        public string? NombreComercial { get; set; }

        [JsonPropertyName("razonSocial")]
        public string? RazonSocial { get; set; }

        [JsonPropertyName("rfc")]
        public string? RFC { get; set; }

        [JsonPropertyName("cp")]
        public string? CP { get; set; }

        [JsonPropertyName("estado")]
        public string? Estado { get; set; }

        [JsonPropertyName("ciudad")]
        public string? Ciudad { get; set; }

        [JsonPropertyName("pais")]
        public string? Pais { get; set; }

        [JsonPropertyName("calle")]
        public string? Calle { get; set; }

        [JsonPropertyName("numExt")]
        public string? NumExt { get; set; }

        [JsonPropertyName("numInt")]
        public string? NumInt { get; set; }

        [JsonPropertyName("colonia")]
        public string? Colonia { get; set; }

        [JsonPropertyName("apoderado")]
        public string? Apoderado { get; set; }

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
