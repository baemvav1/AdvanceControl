using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Advance_Control.Models
{
    /// <summary>
    /// DTO para las imágenes de operaciones (Prefacturas y Hojas de Servicio)
    /// </summary>
    public class OperacionImageDto : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private string? _fileName;
        private string? _url;
        private int _idOperacion;
        private int _imageNumber;
        private string? _tipo; // "Prefactura" o "HojaServicio"

        /// <summary>
        /// Nombre del archivo de la imagen
        /// </summary>
        public string? FileName
        {
            get => _fileName;
            set
            {
                if (_fileName != value)
                {
                    _fileName = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// URL o ruta local de la imagen
        /// </summary>
        public string? Url
        {
            get => _url;
            set
            {
                if (_url != value)
                {
                    _url = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// ID de la operación a la que pertenece la imagen
        /// </summary>
        public int IdOperacion
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
        /// Número secuencial de la imagen para esta operación
        /// </summary>
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

        /// <summary>
        /// Tipo de imagen: "Prefactura" o "HojaServicio"
        /// </summary>
        public string? Tipo
        {
            get => _tipo;
            set
            {
                if (_tipo != value)
                {
                    _tipo = value;
                    OnPropertyChanged();
                }
            }
        }
    }
}
