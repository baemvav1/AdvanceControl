using Microsoft.UI.Xaml.Data;
using System;

namespace Advance_Control.Converters
{
    /// <summary>
    /// Convierte un valor null a false y un valor no-null a true.
    /// </summary>
    public class NullToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null)
            {
                return false;
            }

            // Para strings, verificar si está vacío o solo whitespace
            if (value is string str)
            {
                return !string.IsNullOrWhiteSpace(str);
            }

            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
