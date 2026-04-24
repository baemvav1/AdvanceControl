using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
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
            set
            {
                if (_leidoEn != value)
                {
                    _leidoEn = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(EsLeido));
                    OnPropertyChanged(nameof(ReadIconGlyph));
                    OnPropertyChanged(nameof(ReadIconBrush));
                    OnPropertyChanged(nameof(ReadStatusTooltip));
                }
            }
        }

        private DateTime? _entregadoEn;
        [JsonPropertyName("entregadoEn")]
        public DateTime? EntregadoEn
        {
            get => _entregadoEn;
            set
            {
                if (_entregadoEn != value)
                {
                    _entregadoEn = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(EsEntregado));
                    OnPropertyChanged(nameof(ReadIconGlyph));
                    OnPropertyChanged(nameof(ReadIconBrush));
                    OnPropertyChanged(nameof(ReadStatusTooltip));
                }
            }
        }

        [JsonPropertyName("actualizadoEn")]
        public DateTime? ActualizadoEn { get; set; }

        [JsonPropertyName("archivoUrl")]
        public string? ArchivoUrl { get; set; }

        private DateTime? _eliminadoEn;
        [JsonPropertyName("eliminadoEn")]
        public DateTime? EliminadoEn
        {
            get => _eliminadoEn;
            set
            {
                if (_eliminadoEn != value)
                {
                    _eliminadoEn = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(EstaEliminado));
                    OnPropertyChanged(nameof(EstaEliminadoVisibility));
                    OnPropertyChanged(nameof(NoEstaEliminadoVisibility));
                    OnPropertyChanged(nameof(EsImagenVisibility));
                    OnPropertyChanged(nameof(EsPdfVisibility));
                    OnPropertyChanged(nameof(EsTextoVisibility));
                    OnPropertyChanged(nameof(EsRespuestaVisibility));
                }
            }
        }

        [JsonPropertyName("respuestaAMensajeId")]
        public long? RespuestaAMensajeId { get; set; }

        [JsonPropertyName("respuestaAAutor")]
        public string? RespuestaAAutor { get; set; }

        [JsonPropertyName("respuestaAResumen")]
        public string? RespuestaAResumen { get; set; }

        // Propiedades computadas para la UI
        [JsonIgnore]
        public bool EsLeido => LeidoEn.HasValue;

        [JsonIgnore]
        public bool EsEntregado => EntregadoEn.HasValue;

        [JsonIgnore]
        public bool EsMio { get; set; }

        [JsonIgnore]
        public bool NotEsMio => !EsMio;

        [JsonIgnore]
        public bool EsImagen => (Tipo ?? string.Empty) == "imagen" && !string.IsNullOrEmpty(ArchivoUrl) && !EstaEliminado;

        [JsonIgnore]
        public bool EsPdf => (Tipo ?? string.Empty) == "pdf" && !string.IsNullOrEmpty(ArchivoUrl) && !EstaEliminado;

        [JsonIgnore]
        public bool EsTexto => !EsImagen && !EsPdf && !EstaEliminado;

        [JsonIgnore]
        public bool EstaEliminado => EliminadoEn.HasValue;

        [JsonIgnore]
        public Visibility EstaEliminadoVisibility => EstaEliminado ? Visibility.Visible : Visibility.Collapsed;

        [JsonIgnore]
        public Visibility NoEstaEliminadoVisibility => EstaEliminado ? Visibility.Collapsed : Visibility.Visible;

        [JsonIgnore]
        public bool EsRespuesta => RespuestaAMensajeId.HasValue && !string.IsNullOrWhiteSpace(RespuestaAAutor);

        [JsonIgnore]
        public Visibility EsRespuestaVisibility => EsRespuesta ? Visibility.Visible : Visibility.Collapsed;

        [JsonIgnore]
        public string ContenidoMostrado => EstaEliminado ? "Mensaje eliminado" : (Contenido ?? string.Empty);

        [JsonIgnore]
        public string RespuestaAResumenSeguro => RespuestaAResumen ?? string.Empty;

        [JsonIgnore]
        public string RespuestaAAutorSeguro => RespuestaAAutor ?? string.Empty;

        [JsonIgnore]
        public bool EsReferenciaOperacion =>
            IdReferencia.HasValue &&
            string.Equals(TipoReferencia, "Operacion", StringComparison.OrdinalIgnoreCase);

        [JsonIgnore]
        public Visibility EsReferenciaOperacionVisibility => EsReferenciaOperacion ? Visibility.Visible : Visibility.Collapsed;

        [JsonIgnore]
        public string TituloReferenciaOperacion => IdReferencia.HasValue
            ? $"Operación {IdReferencia.Value}"
            : "Operación";

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
        public string? ImagenUrlCompleta
        {
            get => _imagenUrlCompleta;
            set
            {
                if (_imagenUrlCompleta == value) return;

                _imagenUrlCompleta = value;
                _imagenSource = CreateImagenSource(value);
                OnPropertyChanged();
                OnPropertyChanged(nameof(ImagenSource));
            }
        }

        /// <summary>ImageSource seguro para binding (evita conversión implícita string→ImageSource).</summary>
        [JsonIgnore]
        public BitmapImage? ImagenSource => _imagenSource;

        private string? _imagenUrlCompleta;
        private BitmapImage? _imagenSource;

        private static BitmapImage? CreateImagenSource(string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl)) return null;

            try
            {
                return new BitmapImage(new Uri(imageUrl));
            }
            catch
            {
                return null;
            }
        }

        /// <summary>Contenido seguro para binding (nunca null).</summary>
        [JsonIgnore]
        public string ContenidoSeguro => Contenido ?? string.Empty;

        /// <summary>Ícono de lectura: doble check si leído, check simple si no.</summary>
        public string GetReadIcon(bool esLeido) => esLeido ? "\uE8FB" : "\uE73E";

        /// <summary>
        /// Glyph del check de estado: enviado (✓) si solo creado, doble check (✓✓) si entregado o leído.
        /// El color (ReadIconBrush) diferencia entregado vs leído.
        /// </summary>
        [JsonIgnore]
        public string ReadIconGlyph => (EsEntregado || EsLeido) ? "\uE8FB" : "\uE73E";

        /// <summary>
        /// Color del check para burbujas propias:
        /// - enviado/entregado: blanco semitransparente.
        /// - leído: azul claro estilo WhatsApp para destacar.
        /// </summary>
        [JsonIgnore]
        public Brush ReadIconBrush
        {
            get
            {
                if (EsLeido)
                    return new SolidColorBrush(Windows.UI.Color.FromArgb(0xFF, 0x4F, 0xC3, 0xF7));
                return new SolidColorBrush(Windows.UI.Color.FromArgb(0xCC, 0xFF, 0xFF, 0xFF));
            }
        }

        /// <summary>Tooltip "Enviado/Entregado/Leído HH:mm".</summary>
        [JsonIgnore]
        public string ReadStatusTooltip
        {
            get
            {
                if (EsLeido && LeidoEn.HasValue)
                    return $"Leído {LeidoEn.Value.ToLocalTime():HH:mm}";
                if (EsEntregado && EntregadoEn.HasValue)
                    return $"Entregado {EntregadoEn.Value.ToLocalTime():HH:mm}";
                return $"Enviado {CreatedAt.ToLocalTime():HH:mm}";
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
