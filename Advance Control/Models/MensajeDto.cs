using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Advance_Control.Models
{
    public class MensajeDto : INotifyPropertyChanged
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("deCredencialId")]
        public long DeCredencialId { get; set; }

        [JsonPropertyName("paraCredencialId")]
        public long ParaCredencialId { get; set; }

        [JsonPropertyName("deNombre")]
        public string? DeNombre { get; set; }

        [JsonPropertyName("paraNombre")]
        public string? ParaNombre { get; set; }

        [JsonPropertyName("tipo")]
        public string Tipo { get; set; } = "mensaje";

        [JsonPropertyName("contenido")]
        public string Contenido { get; set; } = string.Empty;

        [JsonPropertyName("idReferencia")]
        public int? IdReferencia { get; set; }

        [JsonPropertyName("tipoReferencia")]
        public string? TipoReferencia { get; set; }

        private string _estatus = "enviado";
        [JsonPropertyName("estatus")]
        public string Estatus
        {
            get => _estatus;
            set { if (_estatus != value) { _estatus = value; OnPropertyChanged(); } }
        }

        [JsonPropertyName("fechaLimite")]
        public DateTime? FechaLimite { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        private DateTime? _leidoEn;
        [JsonPropertyName("leidoEn")]
        public DateTime? LeidoEn
        {
            get => _leidoEn;
            set { if (_leidoEn != value) { _leidoEn = value; OnPropertyChanged(); OnPropertyChanged(nameof(EsLeido)); } }
        }

        [JsonPropertyName("actualizadoEn")]
        public DateTime? ActualizadoEn { get; set; }

        [JsonPropertyName("archivoUrl")]
        public string? ArchivoUrl { get; set; }

        // Propiedades computadas para la UI
        [JsonIgnore]
        public bool EsLeido => LeidoEn.HasValue;

        [JsonIgnore]
        public bool EsMio { get; set; }

        [JsonIgnore]
        public bool NotEsMio => !EsMio;

        [JsonIgnore]
        public bool EsImagen => Tipo == "imagen" && !string.IsNullOrEmpty(ArchivoUrl);

        [JsonIgnore]
        public bool EsPdf => Tipo == "pdf" && !string.IsNullOrEmpty(ArchivoUrl);

        [JsonIgnore]
        public bool EsTexto => !EsImagen && !EsPdf;

        [JsonIgnore]
        public string HoraFormateada => CreatedAt.ToString("HH:mm");

        [JsonIgnore]
        public string FechaFormateada => CreatedAt.Date == DateTime.Today
            ? "Hoy" : CreatedAt.Date == DateTime.Today.AddDays(-1)
            ? "Ayer" : CreatedAt.ToString("dd/MM/yyyy");

        /// <summary>URL completa de la imagen para binding.</summary>
        [JsonIgnore]
        public string? ImagenUrlCompleta { get; set; }

        /// <summary>Ícono de lectura: doble check si leído, check simple si no.</summary>
        public string GetReadIcon(bool esLeido) => esLeido ? "\uE8FB" : "\uE73E";

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
