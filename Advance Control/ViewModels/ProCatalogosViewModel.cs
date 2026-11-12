using Advance_Control.Models;

namespace Advance_Control.ViewModels
{
    /// <summary>
    /// Estado del prototipo de Catálogos. Por ahora solo el equipo elegido en el grid "Equipos":
    /// vacío = botón "+"; con equipo = tarjeta y botón de basura para limpiarla.
    /// </summary>
    public class ProCatalogosViewModel : ViewModelBase
    {
        private EquipoBusquedaDto? _equipo;

        public EquipoBusquedaDto? Equipo
        {
            get => _equipo;
            set
            {
                if (SetProperty(ref _equipo, value))
                {
                    OnPropertyChanged(nameof(HayEquipo));
                    OnPropertyChanged(nameof(SinEquipo));
                }
            }
        }

        public bool HayEquipo => _equipo is not null;
        public bool SinEquipo => _equipo is null;

        /// <summary>Basura: limpia la tarjeta y regresa al estado inicial.</summary>
        public void LimpiarEquipo() => Equipo = null;
    }
}
