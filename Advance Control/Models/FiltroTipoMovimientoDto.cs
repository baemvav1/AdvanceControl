using Advance_Control.ViewModels;

namespace Advance_Control.Models
{
    public class FiltroTipoMovimientoDto : ViewModelBase
    {
        private bool _isSelected = true;

        public string Clave { get; set; } = string.Empty;
        public string Etiqueta { get; set; } = string.Empty;
        public int Cantidad { get; set; }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public string EtiquetaCompleta => $"{Etiqueta} ({Cantidad})";
    }
}
