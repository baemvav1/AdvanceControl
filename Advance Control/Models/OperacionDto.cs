using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Windows.Globalization.NumberFormatting;

namespace Advance_Control.Models
{
    /// <summary>
    /// DTO para los datos de operación que se reciben desde la API
    /// </summary>
    public class OperacionDto : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string? propertyName = null)
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
        /// ID del cliente asociado a la operación
        /// </summary>
        [JsonPropertyName("idCliente")]
        public int? IdCliente { get; set; }

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
        /// Nombre de quien atiende
        /// </summary>
        [JsonPropertyName("idAtiende")]
        public int? IdAtiende { get; set; }

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

        private DateTime? _fechaFinal;

        /// <summary>
        /// Fecha de finalización de la operación
        /// </summary>
        [JsonPropertyName("fechaFinal")]
        public DateTime? FechaFinal
        {
            get => _fechaFinal;
            set
            {
                if (_fechaFinal != value)
                {
                    _fechaFinal = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsFinalized));
                    OnPropertyChanged(nameof(IsEditable));
                    OnPropertyChanged(nameof(StatusText));
                }
            }
        }

        /// <summary>
        /// True si la operación tiene fecha final (está finalizada)
        /// </summary>
        [JsonIgnore]
        public bool IsFinalized => FechaFinal != null;

        /// <summary>
        /// True si la operación puede ser editada (no está finalizada)
        /// </summary>
        [JsonIgnore]
        public bool IsEditable => FechaFinal == null;

        /// <summary>
        /// Fecha de inicio formateada de forma corta para mostrar en el header (dd/MM/yy)
        /// </summary>
        [JsonIgnore]
        public string FechaInicioCorta => FechaInicio?.ToString("dd/MM/yy") ?? "—";

        /// <summary>
        /// Texto descriptivo del estado de la operación
        /// </summary>
        [JsonIgnore]
        public string StatusText => IsFinalized ? "Finalizada" : "Abierta";

        /// <summary>
        /// Indica si la operación ha finalizado
        /// </summary>
        [JsonPropertyName("finalizado")]
        public bool Finalizado { get; set; }

        private string? _cotizacionPdfPath;

        /// <summary>
        /// Ruta al PDF de cotización generado para esta operación. Null si no existe.
        /// </summary>
        [JsonIgnore]
        public string? CotizacionPdfPath
        {
            get => _cotizacionPdfPath;
            set
            {
                if (_cotizacionPdfPath != value)
                {
                    _cotizacionPdfPath = value;
                    OnPropertyChanged();
                }
            }
        }

        private string? _reportePdfPath;

        /// <summary>
        /// Ruta al PDF de reporte generado para esta operación. Null si no existe.
        /// </summary>
        [JsonIgnore]
        public string? ReportePdfPath
        {
            get => _reportePdfPath;
            set
            {
                if (_reportePdfPath != value)
                {
                    _reportePdfPath = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _hasFactura = false;

        /// <summary>
        /// Indica si existe factura PDF para esta operación
        /// </summary>
        [JsonIgnore]
        public bool HasFactura
        {
            get => _hasFactura;
            set
            {
                if (_hasFactura != value)
                {
                    _hasFactura = value;
                    OnPropertyChanged();
                }
            }
        }

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

        private ObservableCollection<CargoDto> _cargos = new ObservableCollection<CargoDto>();
        private INumberFormatter2? _currencyFormatter;

        /// <summary>
        /// Colección de cargos asociados a esta operación
        /// </summary>
        [JsonIgnore]
        public ObservableCollection<CargoDto> Cargos
        {
            get => _cargos;
            set
            {
                if (_cargos != value)
                {
                    _cargos = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(TotalMonto));
                }
            }
        }

        /// <summary>
        /// Calcula el total de todos los montos en la colección de cargos
        /// </summary>
        [JsonIgnore]
        public double TotalMonto
        {
            get
            {
                if (Cargos == null || Cargos.Count == 0)
                    return 0.0;

                return Cargos.Sum(c => c.Monto ?? 0.0);
            }
        }

        /// <summary>
        /// Currency formatter para formatear el total como moneda mexicana
        /// </summary>
        [JsonIgnore]
        public INumberFormatter2 CurrencyFormatter
        {
            get
            {
                if (_currencyFormatter == null)
                {
                    var formatter = new CurrencyFormatter("MXN");
                    formatter.FractionDigits = 2;
                    _currencyFormatter = formatter;
                }
                return _currencyFormatter;
            }
        }

        private bool _cargosLoaded = false;

        /// <summary>
        /// Indica si los cargos ya fueron cargados desde el servidor
        /// </summary>
        [JsonIgnore]
        public bool CargosLoaded
        {
            get => _cargosLoaded;
            set
            {
                if (_cargosLoaded != value)
                {
                    _cargosLoaded = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _isLoadingCargos = false;

        /// <summary>
        /// Indica si los cargos se están cargando actualmente
        /// </summary>
        [JsonIgnore]
        public bool IsLoadingCargos
        {
            get => _isLoadingCargos;
            set
            {
                if (_isLoadingCargos != value)
                {
                    _isLoadingCargos = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _imagesLoaded = false;

        /// <summary>
        /// Indica si los indicadores de imágenes ya fueron cargados. Evita repetir 4 HTTP calls por cada expansión de card.
        /// </summary>
        [JsonIgnore]
        public bool ImagesLoaded
        {
            get => _imagesLoaded;
            set
            {
                if (_imagesLoaded != value)
                {
                    _imagesLoaded = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _collectionChangedSubscribed = false;

        /// <summary>
        /// Indica si ya se suscribió al CollectionChanged de Cargos. Evita acumulación de handlers.
        /// </summary>
        [JsonIgnore]
        public bool CollectionChangedSubscribed
        {
            get => _collectionChangedSubscribed;
            set => _collectionChangedSubscribed = value;
        }

        private CheckOperacionDto? _checkOperacion;

        /// <summary>
        /// Estado del checklist de pasos de esta operación. Se carga al expandir el card.
        /// </summary>
        [JsonIgnore]
        public CheckOperacionDto? CheckOperacion
        {
            get => _checkOperacion;
            set
            {
                if (_checkOperacion != value)
                {
                    _checkOperacion = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(TieneCheck));
                }
            }
        }

        /// <summary>True cuando el check ya fue cargado.</summary>
        [JsonIgnore]
        public bool TieneCheck => _checkOperacion != null;

        private bool _hasPrefactura = false;

        /// <summary>
        /// Indica si existen imágenes de prefactura para esta operación
        /// </summary>
        [JsonIgnore]
        public bool HasPrefactura
        {
            get => _hasPrefactura;
            set
            {
                if (_hasPrefactura != value)
                {
                    _hasPrefactura = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _hasHojaServicio = false;

        /// <summary>
        /// Indica si existen imágenes de hoja de servicio para esta operación
        /// </summary>
        [JsonIgnore]
        public bool HasHojaServicio
        {
            get => _hasHojaServicio;
            set
            {
                if (_hasHojaServicio != value)
                {
                    _hasHojaServicio = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _hasOrdenCompra = false;

        /// <summary>
        /// Indica si existen imágenes de orden de compra para esta operación
        /// </summary>
        [JsonIgnore]
        public bool HasOrdenCompra
        {
            get => _hasOrdenCompra;
            set
            {
                if (_hasOrdenCompra != value)
                {
                    _hasOrdenCompra = value;
                    OnPropertyChanged();
                }
            }
        }

        private ObservableCollection<OperacionImageDto> _imagenesPrefactura = new ObservableCollection<OperacionImageDto>();

        /// <summary>
        /// Colección de imágenes de prefactura para esta operación
        /// </summary>
        [JsonIgnore]
        public ObservableCollection<OperacionImageDto> ImagenesPrefactura
        {
            get => _imagenesPrefactura;
            set
            {
                if (_imagenesPrefactura != value)
                {
                    _imagenesPrefactura = value;
                    OnPropertyChanged();
                }
            }
        }

        private ObservableCollection<OperacionImageDto> _imagenesHojaServicio = new ObservableCollection<OperacionImageDto>();

        /// <summary>
        /// Colección de imágenes de hoja de servicio para esta operación
        /// </summary>
        [JsonIgnore]
        public ObservableCollection<OperacionImageDto> ImagenesHojaServicio
        {
            get => _imagenesHojaServicio;
            set
            {
                if (_imagenesHojaServicio != value)
                {
                    _imagenesHojaServicio = value;
                    OnPropertyChanged();
                }
            }
        }

        private ObservableCollection<OperacionImageDto> _imagenesOrdenCompra = new ObservableCollection<OperacionImageDto>();

        /// <summary>
        /// Colección de imágenes de orden de compra para esta operación
        /// </summary>
        [JsonIgnore]
        public ObservableCollection<OperacionImageDto> ImagenesOrdenCompra
        {
            get => _imagenesOrdenCompra;
            set
            {
                if (_imagenesOrdenCompra != value)
                {
                    _imagenesOrdenCompra = value;
                    OnPropertyChanged();
                }
            }
        }
    }
}
