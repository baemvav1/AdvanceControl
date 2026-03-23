using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Advance_Control.Models
{
    public sealed class LevantamientoTreeItemModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public string Clave { get; init; } = string.Empty;

        public string Etiqueta { get; init; } = string.Empty;

        public string? DescripcionFalla { get; init; }

        public bool TieneFalla { get; init; }

        public ObservableCollection<LevantamientoTreeItemModel> Hijos { get; } = new();

        public ObservableCollection<LevantamientoImageItem> Imagenes { get; } = new();

        public bool EsHoja => Hijos.Count == 0;

        public bool TieneImagenes => Imagenes.Count > 0;

        public string ResumenImagenes => TieneImagenes
            ? $"{Imagenes.Count} imagen(es)"
            : string.Empty;

        public void AddImage(LevantamientoImageItem image)
        {
            Imagenes.Add(image);
            NotifyImageProperties();
        }

        public void RemoveImage(LevantamientoImageItem image)
        {
            Imagenes.Remove(image);
            NotifyImageProperties();
        }

        public void NotifyImageProperties()
        {
            OnPropertyChanged(nameof(TieneImagenes));
            OnPropertyChanged(nameof(ResumenImagenes));
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
