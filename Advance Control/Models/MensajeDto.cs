using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
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
        public bool EsImagen => (Tipo ?? string.Empty) == "imagen" && !string.IsNullOrEmpty(ArchivoUrl);

        [JsonIgnore]
        public bool EsPdf => (Tipo ?? string.Empty) == "pdf" && !string.IsNullOrEmpty(ArchivoUrl);

        [JsonIgnore]
        public bool EsTexto => !EsImagen && !EsPdf;

        // Propiedades Visibility explícitas para evitar conversión implícita bool→Visibility en x:Bind
        [JsonIgnore]
        public Visibility EsMioVisibility => EsMio ? Visibility.Visible : Visibility.Collapsed;

        [JsonIgnore]
        public Visibility NotEsMioVisibility => !EsMio ? Visibility.Visible : Visibility.Collapsed;

        [JsonIgnore]
        public Visibility EsImagenVisibility => EsImagen ? Visibility.Visible : Visibility.Collapsed;

        [JsonIgnore]
        public Visibility EsPdfVisibility => EsPdf ? Visibility.Visible : Visibility.Collapsed;

        [JsonIgnore]
        public Visibility EsTextoVisibility => EsTexto ? Visibility.Visible : Visibility.Collapsed;

        [JsonIgnore]
        public string HoraFormateada => CreatedAt.ToString("HH:mm");

        [JsonIgnore]
        public string FechaFormateada => CreatedAt.Date == DateTime.Today
            ? "Hoy" : CreatedAt.Date == DateTime.Today.AddDays(-1)
            ? "Ayer" : CreatedAt.ToString("dd/MM/yyyy");

        /// <summary>URL completa de la imagen para binding.</summary>
        [JsonIgnore]
        public string? ImagenUrlCompleta { get; set; }

        /// <summary>ImageSource seguro para binding (evita conversión implícita string→ImageSource).</summary>
        [JsonIgnore]
        public BitmapImage? ImagenSource
        {
            get
            {
                if (string.IsNullOrEmpty(ImagenUrlCompleta)) return null;
                try { return new BitmapImage(new Uri(ImagenUrlCompleta)); }
                catch { return null; }
            }
        }

        /// <summary>Contenido seguro para binding (nunca null).</summary>
        [JsonIgnore]
        public string ContenidoSeguro => Contenido ?? string.Empty;

        /// <summary>Ícono de lectura: doble check si leído, check simple si no.</summary>
        public string GetReadIcon(bool esLeido) => esLeido ? "\uE8FB" : "\uE73E";

        /// <summary>Glyph de lectura para binding (propiedad, no función).</summary>
        [JsonIgnore]
        public string ReadIconGlyph => EsLeido ? "\uE8FB" : "\uE73E";

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
