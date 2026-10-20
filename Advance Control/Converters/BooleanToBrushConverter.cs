using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace Advance_Control.Converters
{
    /// <summary>
    /// Convierte un bool en uno de dos Brush. A diferencia de BooleanToSystemColorBrushConverter
    /// (colores fijos), aquí los brushes se asignan desde XAML con {ThemeResource ...}, así el
    /// fondo respeta el tema claro/oscuro y el texto por defecto sigue siendo legible.
    /// </summary>
    public class BooleanToBrushConverter : IValueConverter
    {
        public Brush? TrueBrush { get; set; }

        public Brush? FalseBrush { get; set; }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is not bool boolean)
                return DependencyProperty.UnsetValue;

            return (boolean ? TrueBrush : FalseBrush) ?? (object)DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotSupportedException();
    }
}
