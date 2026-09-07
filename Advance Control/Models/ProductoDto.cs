using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Advance_Control.Models
{
    /// <summary>
    /// DTO para los datos del producto (materiales/insumos suministrados al cliente final)
    /// que se reciben desde la API.
    /// </summary>
    public class ProductoDto : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        [JsonPropertyName("idProducto")]
        public int IdProducto { get; set; }

        [JsonPropertyName("concepto")]
        public string? Concepto { get; set; }

        [JsonPropertyName("descripcion")]
        public string? Descripcion { get; set; }

        /// <summary>Nuestro costo, antes de IVA.</summary>
        [JsonPropertyName("costoDirecto")]
        public double? CostoDirecto { get; set; }

        /// <summary>% de utilidad aplicado sobre el costo directo para obtener el costo final al cliente.</summary>
        [JsonPropertyName("porcentajeUtilidad")]
        public double? PorcentajeUtilidad { get; set; }

        /// <summary>Costo final al cliente (calculado por la API: costo_directo * (1 + porcentaje_utilidad/100)), antes de IVA.</summary>
        [JsonPropertyName("costoFinal")]
        public double? CostoFinal { get; set; }

        [JsonPropertyName("estatus")]
        public bool? Estatus { get; set; }

        private bool _expand = false;

        /// <summary>
        /// Propiedad interna para controlar el estado de expansión en la UI.
        /// No se deserializa desde el endpoint.
        /// </summary>
        [JsonIgnore]
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
