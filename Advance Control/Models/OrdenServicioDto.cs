using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Advance_Control.Models
{
    /// <summary>
    /// DTO para los datos de la orden de servicio que se reciben desde la API
    /// </summary>
    public class OrdenServicioDto : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// ID de la orden de servicio
        /// </summary>
        [JsonPropertyName("idOrdenServicio")]
        public int? IdOrdenServicio { get; set; }

        /// <summary>
        /// Tipo de mantenimiento asociado a la orden
        /// </summary>
        [JsonPropertyName("tipoMantenimiento")]
        public string? TipoMantenimiento { get; set; }

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
        /// Nota asociada a la orden de servicio
        /// </summary>
        [JsonPropertyName("descripcion")]
        public string? Descripcion { get; set; }

        /// <summary>
        /// Identificador del equipo
        /// </summary>
        [JsonPropertyName("identificador")]
        public string? Identificador { get; set; }

        /// <summary>
        /// ID de la credencial del usuario que creó la orden de servicio
        /// </summary>
        [JsonPropertyName("credencialId")]
        public int? CredencialId { get; set; }

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
