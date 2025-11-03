using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace AdvanceControl.Converters
{
    /// <summary>
    /// Conversor XAML que transforma valores booleanos a Visibility.
    /// Soporta inversión de lógica y uso de Hidden.
    /// </summary>
    public class BooleanToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Convierte un valor booleano a Visibility.
        /// </summary>
        /// <param name="value">Valor booleano a convertir.</param>
        /// <param name="targetType">Tipo de destino (no utilizado).</param>
        /// <param name="parameter">
        /// Parámetro opcional para modificar comportamiento:
        /// - "invert": Invierte la lógica (true → Collapsed, false → Visible)
        /// - "UseHidden": Usa Hidden en lugar de Collapsed
        /// </param>
        /// <param name="language">Idioma (no utilizado).</param>
        /// <returns>
        /// Visibility.Visible si booleano es true (o false si invertido),
        /// Visibility.Collapsed en caso contrario.
        /// </returns>
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool boolean)
            {
                bool invert = parameter is string str && str.Equals("invert", StringComparison.OrdinalIgnoreCase);
                bool useHidden = parameter is string str2 && str2.Equals("UseHidden", StringComparison.OrdinalIgnoreCase);

                if (invert)
                {
                    boolean = !boolean;
                }

                return boolean ? (useHidden ? Visibility.Collapsed : Visibility.Visible) : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        /// <summary>
        /// Convierte Visibility de vuelta a booleano (para two-way binding).
        /// </summary>
        /// <param name="value">Valor Visibility a convertir.</param>
        /// <param name="targetType">Tipo de destino (no utilizado).</param>
        /// <param name="parameter">Parámetro (no utilizado).</param>
        /// <param name="language">Idioma (no utilizado).</param>
        /// <returns>
        /// true si Visibility es Visible,
        /// false si es Collapsed o Hidden,
        /// null si el valor no es Visibility.
        /// </returns>
        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is Visibility visibility)
            {
                return visibility == Visibility.Visible;
            }
            return null;
        }
    }
}