using CommunityToolkit.Mvvm.ComponentModel;

namespace Advance_Control.Models
{
    /// <summary>
    /// ViewModel observable para una notificación silenciada, usado en el panel de notificaciones.
    /// Permite bindear el ToggleSwitch bidireccional desde la UI.
    /// </summary>
    public partial class NotificacionSilenciadaVm : ObservableObject
    {
        [ObservableProperty]
        private bool _habilitada;

        public string Categoria { get; init; } = string.Empty;
        public string? Page { get; init; }

        /// <summary>Etiqueta legible: Categoria (Page) si hay page, o solo Categoria.</summary>
        public string Label => string.IsNullOrWhiteSpace(Page)
            ? Categoria
            : $"{Categoria}  ·  {Page}";
    }
}
