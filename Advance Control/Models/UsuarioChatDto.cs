using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Advance_Control.Models
{
    public class UsuarioChatDto : INotifyPropertyChanged
    {
        [JsonPropertyName("credencialId")]
        public long CredencialId { get; set; }

        [JsonPropertyName("usuario")]
        public string Usuario { get; set; } = string.Empty;

        [JsonPropertyName("nombreVisible")]
        public string NombreVisible { get; set; } = string.Empty;

        [JsonPropertyName("nivel")]
        public int? Nivel { get; set; }

        [JsonPropertyName("estaActiva")]
        public bool EstaActiva { get; set; }

        private bool _estaEnLinea;
        [JsonIgnore]
        public bool EstaEnLinea
        {
            get => _estaEnLinea;
            set { if (_estaEnLinea != value) { _estaEnLinea = value; OnPropertyChanged(); } }
        }

        private bool _estaEscribiendo;
        [JsonIgnore]
        public bool EstaEscribiendo
        {
            get => _estaEscribiendo;
            set { if (_estaEscribiendo != value) { _estaEscribiendo = value; OnPropertyChanged(); } }
        }

        private int _mensajesNoLeidos;
        [JsonIgnore]
        public int MensajesNoLeidos
        {
            get => _mensajesNoLeidos;
            set { if (_mensajesNoLeidos != value) { _mensajesNoLeidos = value; OnPropertyChanged(); OnPropertyChanged(nameof(TieneNoLeidos)); } }
        }

        [JsonIgnore]
        public bool TieneNoLeidos => MensajesNoLeidos > 0;

        /// <summary>Iniciales para el avatar (máx 2 letras)</summary>
        [JsonIgnore]
        public string Iniciales
        {
            get
            {
                if (string.IsNullOrEmpty(NombreVisible)) return "?";
                var parts = NombreVisible.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
                return parts.Length >= 2
                    ? $"{parts[0][0]}{parts[1][0]}".ToUpper()
                    : NombreVisible.Length >= 2
                        ? NombreVisible[..2].ToUpper()
                        : NombreVisible.ToUpper();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
