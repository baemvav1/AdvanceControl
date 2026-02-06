using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Advance_Control.Models
{
    /// <summary>
    /// DTO for cargo image data stored in Google Cloud Storage
    /// </summary>
    public class CargoImageDto : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private int _idCargoImage;
        private int _idCargo;
        private string? _imageName;
        private string? _imageUrl;
        private int _imageNumber;

        /// <summary>
        /// Unique ID of the cargo image
        /// </summary>
        [JsonPropertyName("idCargoImage")]
        public int IdCargoImage
        {
            get => _idCargoImage;
            set
            {
                if (_idCargoImage != value)
                {
                    _idCargoImage = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// ID of the associated cargo
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
        /// Image name in Google Cloud Storage (e.g., Cargo_Id_45_1)
        /// </summary>
        [JsonPropertyName("imageName")]
        public string? ImageName
        {
            get => _imageName;
            set
            {
                if (_imageName != value)
                {
                    _imageName = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// URL to access the image in Google Cloud Storage
        /// </summary>
        [JsonPropertyName("imageUrl")]
        public string? ImageUrl
        {
            get => _imageUrl;
            set
            {
                if (_imageUrl != value)
                {
                    _imageUrl = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Sequential number of the image for this cargo (1, 2, 3, etc.)
        /// </summary>
        [JsonPropertyName("imageNumber")]
        public int ImageNumber
        {
            get => _imageNumber;
            set
            {
                if (_imageNumber != value)
                {
                    _imageNumber = value;
                    OnPropertyChanged();
                }
            }
        }
    }
}
