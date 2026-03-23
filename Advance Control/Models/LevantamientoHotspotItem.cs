using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml.Media;

namespace Advance_Control.Models
{
    public sealed class LevantamientoHotspotItem : INotifyPropertyChanged
    {
        private static readonly Brush DefaultOverlayBrush = new SolidColorBrush(global::Windows.UI.Color.FromArgb(255, 148, 163, 184));
        private static readonly Brush SelectedOverlayBrush = new SolidColorBrush(global::Windows.UI.Color.FromArgb(255, 37, 99, 235));
        private static readonly Brush FailureOverlayBrush = new SolidColorBrush(global::Windows.UI.Color.FromArgb(255, 220, 38, 38));
        private static readonly Brush DefaultBorderBrush = new SolidColorBrush(global::Windows.UI.Color.FromArgb(255, 71, 85, 105));
        private static readonly Brush SelectedBorderBrush = new SolidColorBrush(global::Windows.UI.Color.FromArgb(255, 29, 78, 216));
        private static readonly Brush FailureBorderBrush = new SolidColorBrush(global::Windows.UI.Color.FromArgb(255, 127, 29, 29));

        private bool _isSelected;
        private string? _descripcionFalla;
        private readonly List<string> _imagenes = new();

        public event PropertyChangedEventHandler? PropertyChanged;

        public required string Clave { get; init; }
        public required string Titulo { get; init; }
        public required string Seccion { get; init; }
        public required IReadOnlyList<string> RutaJerarquica { get; init; }
        public double Left { get; init; }
        public double Top { get; init; }
        public double Width { get; init; }
        public double Height { get; init; }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (SetProperty(ref _isSelected, value))
                {
                    OnPropertyChanged(nameof(OverlayBrush));
                    OnPropertyChanged(nameof(BorderBrush));
                    OnPropertyChanged(nameof(OverlayOpacity));
                }
            }
        }

        public string? DescripcionFalla
        {
            get => _descripcionFalla;
            set
            {
                if (SetProperty(ref _descripcionFalla, value))
                {
                    OnPropertyChanged(nameof(TieneFalla));
                    OnPropertyChanged(nameof(DescripcionResumen));
                    OnPropertyChanged(nameof(OverlayBrush));
                    OnPropertyChanged(nameof(BorderBrush));
                    OnPropertyChanged(nameof(OverlayOpacity));
                }
            }
        }

        public bool TieneFalla => !string.IsNullOrWhiteSpace(DescripcionFalla);

        public string DescripcionResumen => TieneFalla
            ? DescripcionFalla!
            : "Sin falla capturada.";

        public IReadOnlyList<string> Imagenes => _imagenes;

        public bool TieneImagenes => _imagenes.Count > 0;

        public int CantidadImagenes => _imagenes.Count;

        public string ResumenImagenes => TieneImagenes
            ? $"{CantidadImagenes} imagen(es) asociada(s)."
            : "Sin imagenes asociadas.";

        public Brush OverlayBrush => TieneFalla
            ? FailureOverlayBrush
            : IsSelected
                ? SelectedOverlayBrush
                : DefaultOverlayBrush;

        public Brush BorderBrush => TieneFalla
            ? FailureBorderBrush
            : IsSelected
                ? SelectedBorderBrush
                : DefaultBorderBrush;

        public double OverlayOpacity => TieneFalla ? 0.34 : IsSelected ? 0.24 : 0.12;

        private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return false;
            }

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void AddImage(string imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath))
            {
                throw new System.ArgumentException("La ruta de la imagen no puede ser vacia.", nameof(imagePath));
            }

            _imagenes.Add(imagePath);
            NotifyImageStateChanged();
        }

        public IReadOnlyList<string> ClearImages()
        {
            if (_imagenes.Count == 0)
            {
                return System.Array.Empty<string>();
            }

            var removedImages = new List<string>(_imagenes);
            _imagenes.Clear();
            NotifyImageStateChanged();
            return removedImages;
        }

        private void NotifyImageStateChanged()
        {
            OnPropertyChanged(nameof(Imagenes));
            OnPropertyChanged(nameof(TieneImagenes));
            OnPropertyChanged(nameof(CantidadImagenes));
            OnPropertyChanged(nameof(ResumenImagenes));
        }
    }
}
