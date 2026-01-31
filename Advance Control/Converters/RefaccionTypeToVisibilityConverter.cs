using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace Advance_Control.Converters
{
    /// <summary>
    /// Convierte un string a Visibility.Visible si es igual a "Refaccion" (case-insensitive), de lo contrario Visibility.Collapsed.
    /// Usado para mostrar botones solo cuando el tipo de cargo es "Refaccion".
    /// </summary>
    public class RefaccionTypeToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string tipoCargo)
            {
                return string.Equals(tipoCargo, "Refaccion", StringComparison.OrdinalIgnoreCase)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
