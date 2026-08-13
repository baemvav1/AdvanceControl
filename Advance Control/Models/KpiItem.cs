using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace Advance_Control.Models
{
    /// <summary>
    /// Card de KPI mostrada en el row "Kpis" de DetalleClientesView: un número grande con etiqueta,
    /// contextual a lo que esté seleccionado (cartera completa, cliente, o rama del árbol de detalle).
    /// </summary>
    public class KpiItem
    {
        public KpiItem(string etiqueta, string valor, Color? color = null)
        {
            Etiqueta = etiqueta;
            Valor = valor;
            ColorValorBrush = color.HasValue ? new SolidColorBrush(color.Value) : ObtenerBrushPorDefecto();
        }

        public string Etiqueta { get; }

        public string Valor { get; }

        public Brush ColorValorBrush { get; }

        private static Brush ObtenerBrushPorDefecto()
        {
            const string key = "TextFillColorPrimaryBrush";

            if (Application.Current?.Resources != null && Application.Current.Resources.TryGetValue(key, out var recurso) && recurso is Brush brush)
            {
                return brush;
            }

            return new SolidColorBrush(Color.FromArgb(0xFF, 0x20, 0x20, 0x20));
        }
    }
}
