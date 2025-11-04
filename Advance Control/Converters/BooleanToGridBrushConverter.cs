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
    /// Convierte un bool en SolidColorBrush usando únicamente colores del sistema (Windows.UI.Colors),
    /// p. ej. "Green", "DarkBlue", "Red", "Transparent", etc.
    /// - Propiedades TrueColorName/FalseColorName para valores por defecto.
    /// - ConverterParameter admite "invert" y/o especificar los nombres de color (separados por ',' o '|'):
    ///     "invert;Green,Red"  ó  "DarkBlue|LightGray"  ó  "invert;DarkBlue|LightGray"
    /// Si no se encuentra el nombre de color, se usa Transparent.
    /// </summary>
    public class BooleanToSystemColorBrushConverter : IValueConverter
    {
        /// <summary>Nombre del color del sistema a usar cuando el valor es true (p. ej. "Green").</summary>
        public string TrueColorName { get; set; } = "Green";

        /// <summary>Nombre del color del sistema a usar cuando el valor es false (p. ej. "Transparent").</summary>
        public string FalseColorName { get; set; } = "Transparent";

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            // Manejo robusto de valores encajados: solo aceptamos cuando value está boxed como bool.
            if (!(value is bool boolean))
            {
                // Si value es null (por ejemplo bool? sin valor) o no es bool, dejamos que el binding lo gestione.
                return DependencyProperty.UnsetValue;
            }

            // Valores por defecto desde las propiedades
            bool invert = false;
            string trueName = TrueColorName;
            string falseName = FalseColorName;

            if (parameter is string paramStr && !string.IsNullOrWhiteSpace(paramStr))
            {
                // permitir combinaciones: "invert;TrueName,FalseName" o "TrueName|FalseName" etc.
                var semiParts = paramStr.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var token in semiParts)
                {
                    var t = token.Trim();
                    if (t.Equals("invert", StringComparison.OrdinalIgnoreCase))
                    {
                        invert = true;
                        continue;
                    }

                    var parts = t.Split(new[] { ',', '|' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 1) trueName = parts[0].Trim();
                    if (parts.Length >= 2) falseName = parts[1].Trim();
                }
            }

            if (invert) boolean = !boolean;

            string chosenName = boolean ? trueName : falseName;
            var color = GetSystemColorByName(chosenName);
            return new SolidColorBrush(color);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (!(value is SolidColorBrush brush))
                return DependencyProperty.UnsetValue;

            // Determinamos los nombres según propiedades y parámetro (similar a Convert)
            bool invert = false;
            string trueName = TrueColorName;
            string falseName = FalseColorName;

            if (parameter is string paramStr && !string.IsNullOrWhiteSpace(paramStr))
            {
                var semiParts = paramStr.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var token in semiParts)
                {
                    var t = token.Trim();
                    if (t.Equals("invert", StringComparison.OrdinalIgnoreCase))
                    {
                        invert = true;
                        continue;
                    }

                    var parts = t.Split(new[] { ',', '|' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 1) trueName = parts[0].Trim();
                    if (parts.Length >= 2) falseName = parts[1].Trim();
                }
            }

            var tc = GetSystemColorByName(trueName);
            var fc = GetSystemColorByName(falseName);

            if (brush.Color.Equals(tc)) return invert ? false : true;
            if (brush.Color.Equals(fc)) return invert ? true : false;

            return DependencyProperty.UnsetValue;
        }

        /// <summary>
        /// Busca en Windows.UI.Colors una propiedad pública estática con el nombre dado (ignore case).
        /// Si no existe, devuelve Colors.Transparent.
        /// </summary>
        private static Color GetSystemColorByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return Colors.Transparent;

            var n = name.Trim();

            if (n.Equals("Transparent", StringComparison.OrdinalIgnoreCase))
                return Colors.Transparent;

            // Buscar propiedad estática en Windows.UI.Colors (ignore case)
            var prop = typeof(Colors).GetProperty(n, BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase);
            if (prop != null && prop.PropertyType == typeof(Color))
            {
                var v = prop.GetValue(null);
                if (v is Color c) return c;
            }

            // Intentar quitar espacios (por ejemplo "Light Blue" -> "LightBlue")
            var compact = n.Replace(" ", string.Empty);
            prop = typeof(Colors).GetProperty(compact, BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase);
            if (prop != null && prop.PropertyType == typeof(Color))
            {
                var v = prop.GetValue(null);
                if (v is Color c) return c;
            }

            return Colors.Transparent;
        }
    }
}