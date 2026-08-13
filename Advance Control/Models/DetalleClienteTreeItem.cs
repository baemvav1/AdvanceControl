using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace Advance_Control.Models
{
    /// <summary>
    /// Tipo de rama del árbol de detalle del cliente, usado para decidir qué timeline mostrar
    /// en el panel Graficos cuando se selecciona un nodo.
    /// </summary>
    public enum DetalleNodoTipo
    {
        Facturado,
        Operaciones,
        Adeudo,
        EquiposTodos,
        Equipo
    }

    /// <summary>
    /// Nodo genérico para árboles jerárquicos de detalle (usado por el TreeView de salud del cliente).
    /// </summary>
    public class DetalleClienteTreeItem
    {
        public DetalleClienteTreeItem(string etiqueta, string? valorTexto = null, Color? color = null, int nivel = 0)
        {
            Etiqueta = etiqueta;
            ValorTexto = valorTexto;
            Nivel = nivel;
            ColorTextoBrush = color.HasValue ? new SolidColorBrush(color.Value) : ObtenerBrushPorNivel(nivel);
        }

        public string Etiqueta { get; }

        public string? ValorTexto { get; }

        /// <summary>
        /// Profundidad del nodo dentro del árbol (0 = raíz), usada para elegir el color de texto
        /// por defecto cuando no se especifica un color semántico explícito.
        /// </summary>
        public int Nivel { get; }

        public Brush ColorTextoBrush { get; }

        /// <summary>
        /// Color de texto por nivel usando los tonos de texto propios de WinUI (Primary/Secondary/Tertiary),
        /// que ya están resueltos para verse bien tanto en tema claro como oscuro — así los distintos
        /// niveles del árbol se distinguen visualmente sin necesitar colores hardcodeados por tema.
        /// </summary>
        private static Brush ObtenerBrushPorNivel(int nivel)
        {
            var key = nivel switch
            {
                0 => "TextFillColorPrimaryBrush",
                1 => "TextFillColorSecondaryBrush",
                _ => "TextFillColorTertiaryBrush"
            };

            if (Application.Current?.Resources != null && Application.Current.Resources.TryGetValue(key, out var recurso) && recurso is Brush brush)
            {
                return brush;
            }

            return new SolidColorBrush(Color.FromArgb(0xFF, 0x80, 0x80, 0x80));
        }

        public ObservableCollection<DetalleClienteTreeItem> Hijos { get; } = new();

        /// <summary>
        /// Rama a la que pertenece este nodo (heredado de la raíz), usada para saber qué timeline
        /// renderizar cuando se selecciona cualquier nodo dentro de la rama.
        /// </summary>
        public DetalleNodoTipo? Tipo { get; set; }

        /// <summary>
        /// Identificador de equipo, solo presente en los nodos hoja de la rama Equipos.
        /// </summary>
        public string? EquipoIdentificador { get; set; }

        /// <summary>
        /// Marca este nodo y todos sus descendientes con el mismo <see cref="Tipo"/>.
        /// </summary>
        public void EtiquetarTipo(DetalleNodoTipo tipo)
        {
            Tipo = tipo;
            foreach (var hijo in Hijos)
            {
                hijo.EtiquetarTipo(tipo);
            }
        }
    }
}
