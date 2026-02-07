using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Advance_Control.Models
{
    /// <summary>
    /// DTO para los datos de cargo que se reciben desde la API
    /// </summary>
    public class CargoDto : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private int _idCargo;
        private int? _idTipoCargo;
        private int? _idOperacion;
        private int? _idRelacionCargo;
        private double? _monto;
        private string? _nota;
        private string? _detalleRelacionado;
        private string? _tipoCargo;
        private string? _proveedor;

        /// <summary>
        /// ID único del cargo
        /// </summary>
        [JsonPropertyName("idCargo")]
        public int IdCargo
        {
            get => _idCargo;
            set
            {
                if (_idCargo != value)
                {
                    _idCargo = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// ID del tipo de cargo
        /// </summary>
        [JsonPropertyName("idTipoCargo")]
        public int? IdTipoCargo
        {
            get => _idTipoCargo;
            set
            {
                if (_idTipoCargo != value)
                {
                    _idTipoCargo = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// ID de la operación asociada al cargo
        /// </summary>
        [JsonPropertyName("idOperacion")]
        public int? IdOperacion
        {
            get => _idOperacion;
            set
            {
                if (_idOperacion != value)
                {
                    _idOperacion = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// ID de la relación del cargo (referencia a IdRefaccion o IdServicio según el tipo de cargo)
        /// NOTA: Este campo es crítico para poder ver los detalles de refacciones/servicios asociados al cargo.
        /// El backend debe retornar este campo en la respuesta. Si llega null, el botón "Ver detalles" no funcionará.
        /// </summary>
        [JsonPropertyName("idRelacionCargo")]
        public int? IdRelacionCargo
        {
            get => _idRelacionCargo;
            set
            {
                if (_idRelacionCargo != value)
                {
                    _idRelacionCargo = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Monto del cargo
        /// </summary>
        [JsonPropertyName("monto")]
        public double? Monto
        {
            get => _monto;
            set
            {
                if (_monto != value)
                {
                    _monto = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Nota del cargo
        /// </summary>
        [JsonPropertyName("nota")]
        public string? Nota
        {
            get => _nota;
            set
            {
                if (_nota != value)
                {
                    _nota = value;
                    OnPropertyChanged();
                }
            }
        }


        /// <summary>
        /// Nota del cargo
        /// </summary>
        [JsonPropertyName("detalleRelacionado")]
        public string? DetalleRelacionado
        {
            get => _detalleRelacionado;
            set
            {
                if (_detalleRelacionado != value)
                {
                    _detalleRelacionado = value;
                    OnPropertyChanged();
                }
            }
        }


        /// <summary>
        /// Nota del cargo
        /// </summary>
        [JsonPropertyName("tipoCargo")]
        public string? TipoCargo
        {
            get => _tipoCargo;
            set
            {
                if (_tipoCargo != value)
                {
                    _tipoCargo = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Proveedor asociado al cargo
        /// </summary>
        [JsonPropertyName("proveedor")]
        public string? Proveedor
        {
            get => _proveedor;
            set
            {
                if (_proveedor != value)
                {
                    _proveedor = value;
                    OnPropertyChanged();
                }
            }
        }

        private double? _cantidad;
        /// <summary>
        /// Cantidad del cargo
        /// </summary>
        [JsonPropertyName("cantidad")]
        public double? Cantidad
        {
            get => _cantidad;
            set
            {
                if (_cantidad != value)
                {
                    _cantidad = value;
                    OnPropertyChanged();
                    RecalculateMonto();
                }
            }
        }

        private double? _unitario;
        /// <summary>
        /// Precio unitario del cargo
        /// </summary>
        [JsonPropertyName("unitario")]
        public double? Unitario
        {
            get => _unitario;
            set
            {
                if (_unitario != value)
                {
                    _unitario = value;
                    OnPropertyChanged();
                    RecalculateMonto();
                }
            }
        }

        private bool _isEditing;
        /// <summary>
        /// Indica si el cargo está en modo edición
        /// </summary>
        [JsonIgnore]
        public bool IsEditing
        {
            get => _isEditing;
            set
            {
                if (_isEditing != value)
                {
                    _isEditing = value;
                    OnPropertyChanged();
                }
            }
        }

        private ObservableCollection<CargoImageDto> _images = new ObservableCollection<CargoImageDto>();
        /// <summary>
        /// Colección de imágenes asociadas al cargo
        /// </summary>
        [JsonIgnore]
        public ObservableCollection<CargoImageDto> Images
        {
            get => _images;
            set
            {
                if (_images != value)
                {
                    _images = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(HasImages));
                }
            }
        }

        /// <summary>
        /// Indica si el cargo tiene imágenes cargadas
        /// </summary>
        [JsonIgnore]
        public bool HasImages => _images.Count > 0;

        private bool _imagesLoaded;
        /// <summary>
        /// Indica si las imágenes del cargo ya fueron cargadas
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

        private bool _isLoadingImages;
        /// <summary>
        /// Indica si se están cargando las imágenes
        /// </summary>
        [JsonIgnore]
        public bool IsLoadingImages
        {
            get => _isLoadingImages;
            set
            {
                if (_isLoadingImages != value)
                {
                    _isLoadingImages = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _isGalleryExpanded;
        /// <summary>
        /// Indica si la galería de imágenes está expandida (visible)
        /// </summary>
        [JsonIgnore]
        public bool IsGalleryExpanded
        {
            get => _isGalleryExpanded;
            set
            {
                if (_isGalleryExpanded != value)
                {
                    _isGalleryExpanded = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ShouldShowGallery));
                }
            }
        }

        private bool _isSelected;
        /// <summary>
        /// Indica si el cargo está seleccionado en la lista
        /// </summary>
        [JsonIgnore]
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Indica si se debe mostrar la galería (tiene imágenes Y está expandida)
        /// </summary>
        [JsonIgnore]
        public bool ShouldShowGallery => HasImages && IsGalleryExpanded;

        /// <summary>
        /// Notifica que la colección de imágenes ha cambiado
        /// </summary>
        public void NotifyImagesChanged()
        {
            OnPropertyChanged(nameof(Images));
            OnPropertyChanged(nameof(HasImages));
            OnPropertyChanged(nameof(ShouldShowGallery));
        }

        /// <summary>
        /// Recalcula el monto basado en cantidad * unitario
        /// </summary>
        private void RecalculateMonto()
        {
            if (_cantidad.HasValue && _unitario.HasValue)
            {
                Monto = _cantidad.Value * _unitario.Value;
            }
        }
    }
}
