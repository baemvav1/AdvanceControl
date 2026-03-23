using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Advance_Control.Models
{
    /// <summary>
    /// Representa una imagen asociada a un nodo de levantamiento.
    /// </summary>
    public sealed class LevantamientoImageItem : INotifyPropertyChanged
    {
        private string _fileName = string.Empty;
        private string _filePath = string.Empty;
        private string _title = string.Empty;
        private int _imageNumber;

        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Nombre del archivo (ej: CuartoMaquinas_5_1.jpg)
        /// </summary>
        public string FileName
        {
            get => _fileName;
            set => SetProperty(ref _fileName, value);
        }

        /// <summary>
        /// Ruta completa en disco para acceder a la imagen.
        /// </summary>
        public string FilePath
        {
            get => _filePath;
            set => SetProperty(ref _filePath, value);
        }

        /// <summary>
        /// Titulo descriptivo (ej: CuartoMaquinas - Levantamiento 5 - Imagen 1)
        /// </summary>
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        /// <summary>
        /// Numero secuencial de la imagen dentro del nodo.
        /// </summary>
        public int ImageNumber
        {
            get => _imageNumber;
            set => SetProperty(ref _imageNumber, value);
        }

        private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }
    }
}
