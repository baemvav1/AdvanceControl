using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Advance_Control.Models
{
    /// <summary>
    /// DTO para representar una imagen asociada a un cargo
    /// </summary>
    public class CargoImageDto : INotifyPropertyChanged
    {
        private string _fileName = string.Empty;
        private string _url = string.Empty;
        private int _idCargo;
        private int _imageNumber;

        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Nombre del archivo de la imagen
        /// Formato: {idOperacion}_{idCargo}_{numeroImagen}_Cargo.extension
        /// </summary>
        [JsonPropertyName("fileName")]
        public string FileName
        {
            get => _fileName;
            set => SetProperty(ref _fileName, value);
        }

        /// <summary>
        /// Ruta local para acceder a la imagen
        /// </summary>
        [JsonPropertyName("url")]
        public string Url
        {
            get => _url;
            set => SetProperty(ref _url, value);
        }

        /// <summary>
        /// ID del cargo al que pertenece la imagen
        /// </summary>
        [JsonPropertyName("idCargo")]
        public int IdCargo
        {
            get => _idCargo;
            set => SetProperty(ref _idCargo, value);
        }

        /// <summary>
        /// Número secuencial de la imagen para este cargo
        /// </summary>
        [JsonPropertyName("imageNumber")]
        public int ImageNumber
        {
            get => _imageNumber;
            set => SetProperty(ref _imageNumber, value);
        }

        private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return false;
            }

            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }
    }
}
