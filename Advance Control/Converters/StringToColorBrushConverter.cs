using System;
using System.Reflection;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace Advance_Control.Converters
{
    /// <summary>
    /// Convierte un nombre de color (string) en SolidColorBrush.
    /// Usa los colores del sistema Windows.UI.Colors (ej. "Gold", "MediumSeaGreen", "DimGray").
    /// </summary>
    public class StringToColorBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string colorName && !string.IsNullOrWhiteSpace(colorName))
            {
                var color = GetSystemColorByName(colorName);
                return new SolidColorBrush(color);
            }
            return new SolidColorBrush(Colors.Transparent);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => DependencyProperty.UnsetValue;

        private static Color GetSystemColorByName(string name)
        {
            var prop = typeof(Colors).GetProperty(name.Trim(),
                BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase);
            if (prop != null && prop.PropertyType == typeof(Color))
            {
                var v = prop.GetValue(null);
                if (v is Color c) return c;
            }
            return Colors.Transparent;
        }
    }
}
