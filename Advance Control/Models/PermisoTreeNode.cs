using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace Advance_Control.Models
{
    public enum PermisoNodoTipo
    {
        Grupo,
        Modulo,
        Accion
    }

    /// <summary>
    /// Nodo del árbol Grupo → Módulo → Acción de la pestaña Permisos. A diferencia de
    /// <see cref="DetalleClienteTreeItem"/> necesita INotifyPropertyChanged porque
    /// <see cref="EsAccesibleParaRol"/> cambia en vivo cuando se mueve el combo box de rol,
    /// sin reconstruir el árbol.
    /// </summary>
    public class PermisoTreeNode : INotifyPropertyChanged
    {
        private bool _esAccesibleParaRol = true;

        public PermisoTreeNode(string etiqueta, PermisoNodoTipo tipo, int nivelRequerido = 0, PermisoModuloDto? modulo = null, PermisoAccionModuloDto? accion = null)
        {
            Etiqueta = etiqueta;
            Tipo = tipo;
            NivelRequerido = nivelRequerido;
            Modulo = modulo;
            Accion = accion;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string Etiqueta { get; }

        public PermisoNodoTipo Tipo { get; }

        /// <summary>0 para nodos Grupo (no aplica, solo agrupan).</summary>
        public int NivelRequerido { get; }

        public string NivelTexto => Tipo == PermisoNodoTipo.Grupo ? string.Empty : $"Nivel {NivelRequerido}";

        /// <summary>Set solo cuando Tipo == Modulo.</summary>
        public PermisoModuloDto? Modulo { get; }

        /// <summary>Set solo cuando Tipo == Accion.</summary>
        public PermisoAccionModuloDto? Accion { get; }

        public bool MuestraBotonCambiarNivel => Tipo != PermisoNodoTipo.Grupo;

        /// <summary>Solo los nodos Grupo arrancan expandidos — con 50 módulos y 343 acciones,
        /// expandir todo por defecto sería inmanejable.</summary>
        public bool ExpandirPorDefecto => Tipo == PermisoNodoTipo.Grupo;

        public ObservableCollection<PermisoTreeNode> Hijos { get; } = new();

        public bool EsAccesibleParaRol
        {
            get => _esAccesibleParaRol;
            set
            {
                if (_esAccesibleParaRol != value)
                {
                    _esAccesibleParaRol = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ColorTextoBrush));
                }
            }
        }

        public Brush ColorTextoBrush => EsAccesibleParaRol
            ? ObtenerBrushPorTipo(Tipo)
            : new SolidColorBrush(Color.FromArgb(0x60, 0x80, 0x80, 0x80));

        private static Brush ObtenerBrushPorTipo(PermisoNodoTipo tipo)
        {
            var key = tipo switch
            {
                PermisoNodoTipo.Grupo => "TextFillColorPrimaryBrush",
                PermisoNodoTipo.Modulo => "TextFillColorPrimaryBrush",
                _ => "TextFillColorSecondaryBrush"
            };

            if (Application.Current?.Resources != null && Application.Current.Resources.TryGetValue(key, out var recurso) && recurso is Brush brush)
            {
                return brush;
            }

            return new SolidColorBrush(Color.FromArgb(0xFF, 0x80, 0x80, 0x80));
        }

        /// <summary>
        /// Actualiza EsAccesibleParaRol en este nodo y en todos sus descendientes.
        /// nivelRolSeleccionado null = sin filtro (todo accesible). Los nodos Grupo siempre
        /// quedan accesibles (solo agrupan, no gatean nada).
        /// </summary>
        public void ActualizarAccesibilidad(int? nivelRolSeleccionado)
        {
            EsAccesibleParaRol = Tipo == PermisoNodoTipo.Grupo
                || !nivelRolSeleccionado.HasValue
                || nivelRolSeleccionado.Value <= NivelRequerido;

            foreach (var hijo in Hijos)
            {
                hijo.ActualizarAccesibilidad(nivelRolSeleccionado);
            }
        }
    }
}
